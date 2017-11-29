using Microsoft.ServiceFabric.Actors.Runtime;
using System.Fabric;
using ContainerAggregator.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.ServiceFabric.Actors.Query;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Samples.Utility;

namespace ContainerAggregator
{
    class ContainerAggregatorActorService : ActorService, IContainerAggregator
    {
        private ILivenessCounter<string> containerLivenessCounter;

        private readonly string configPackageName = "Config";
        private readonly string livenessCounterSettingSectionName = "LivenessCounterSettings";
        private readonly string expirationIntervalInSecondsKeyName = "ExpirationIntervalInSeconds";
        private readonly string fuzzIntervalInSecondsKeyName = "FuzzIntervalInSeconds";
        private readonly ConcurrentDictionary<string, Timer> ignoreOtherUpdatesEntries;
        private readonly Queue<KeyValuePair<string, string>> updateLogQueue;
        private readonly object logUpdateLock = new object();

        private int expirationIntervalInSeconds = 30;
        private int fuzzIntervalInSeconds = 5;

        private const int MaxItemsToReturn = 10000;
        private const int MaxUpdateLogEntriesToKeep = 500;

        public ContainerAggregatorActorService(StatefulServiceContext context, ActorTypeInformation actorTypeInfo)
            : base(context, actorTypeInfo)
        {
            this.LoadLiveCounterSettings();
            this.containerLivenessCounter = new LivenessCounter<string>(expirationIntervalInSeconds, fuzzIntervalInSeconds);
            this.ignoreOtherUpdatesEntries = new ConcurrentDictionary<string, Timer>();
            this.updateLogQueue = new Queue<KeyValuePair<string, string>>(MaxUpdateLogEntriesToKeep);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);

            ContinuationToken continuationToken = null;

            try
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var actors = await this.StateProvider.GetActorsAsync(MaxItemsToReturn, continuationToken, cancellationToken);

                    foreach (var actorId in actors.Items)
                    {
                        var nodeName = actorId.GetStringId();
                        var aliveContainerValue = await this.StateProvider.LoadStateAsync<int>(actorId, nodeName);

                        containerLivenessCounter.ReportAlive(nodeName, aliveContainerValue);
                    }

                    continuationToken = actors.ContinuationToken;

                } while (continuationToken != null);
            }
            catch (FabricNotPrimaryException)
            {
                // Swallow 
            }
        }

        Task<long> IContainerAggregator.GetLivingCount()
        {
            return Task.FromResult(containerLivenessCounter.GetLivingCount());
        }

        Task<List<KeyValuePair<string, long>>> IContainerAggregator.GetKeyValues()
        {
            return Task.FromResult(containerLivenessCounter.GetKeyValues());
        }        

        async Task IContainerAggregator.ReportAlive(string nodeName, long aliveContainerCount, int ignoreOtherUpdatesForMins)
        {
            // If an entry was made to ignore further updates, do not change it.
            if(!this.ignoreOtherUpdatesEntries.ContainsKey(nodeName))
            {
                await this.SaveCount(nodeName, aliveContainerCount);

                long oldCount;
                if(containerLivenessCounter.TryGetValueForKey(nodeName, out oldCount))
                {
                    if(oldCount != aliveContainerCount)
                    {
                        // Update the change for logs.
                        lock(logUpdateLock)
                        {
                           this.AddToUpdateLogQueue(new KeyValuePair<string, string>(nodeName, $"NewCount= {aliveContainerCount}, OldCount= {oldCount}, Timestamp= {DateTime.Now.ToString()}"));
                        }
                    }
                }

                containerLivenessCounter.ReportAlive(nodeName, aliveContainerCount);

                if(ignoreOtherUpdatesForMins > 0)
                {
                   this.ignoreOtherUpdatesEntries.AddOrUpdate(
                       nodeName, 
                       this.CreateExpiryTimer(nodeName, ignoreOtherUpdatesForMins), 
                       (k, v) => { return this.CreateExpiryTimer(nodeName, ignoreOtherUpdatesForMins);}
                       );
                }
            }
        }

        public Task<Queue<KeyValuePair<string, string>>> GetUpdateLogEntries()
        {
            lock(logUpdateLock)
            {
                return Task.FromResult(updateLogQueue);
            }            
        }

        private Task SaveCount(string nodeName, long aliveContainerCount)
        {
            var actorId = new ActorId(nodeName);

            var stateChangeList = new List<ActorStateChange>
            {
                // With Null and Volatile state provider update always works.
                new ActorStateChange(nodeName, typeof(long), aliveContainerCount, StateChangeKind.Update)
            };

            return this.StateProvider.SaveStateAsync(actorId, stateChangeList);
        }

        private void LoadLiveCounterSettings()
        {
            var config =
                this.Context.CodePackageActivationContext.GetConfigurationPackageObject(configPackageName);

            if (config.Settings.Sections != null &&
                config.Settings.Sections.Contains(livenessCounterSettingSectionName))
            {
                var section = config.Settings.Sections[livenessCounterSettingSectionName];

                if (section.Parameters.Contains(expirationIntervalInSecondsKeyName))
                {
                    var value = section.Parameters[expirationIntervalInSecondsKeyName].Value.Trim();
                    this.expirationIntervalInSeconds = int.Parse(value);
                }

                if (section.Parameters.Contains(fuzzIntervalInSecondsKeyName))
                {
                    var value = section.Parameters[fuzzIntervalInSecondsKeyName].Value.Trim();
                    this.fuzzIntervalInSeconds = int.Parse(value);
                }
            }
        }

        private Timer CreateExpiryTimer(string key, int dueTimeInMins)
        {
            return new Timer(
                this.OnEntryExpired, 
                key,
                TimeSpan.FromMinutes(dueTimeInMins), 
                Timeout.InfiniteTimeSpan);
        }

        private void OnEntryExpired(object state)
        {
            Timer timer;

            if(ignoreOtherUpdatesEntries.TryRemove((string)state, out timer))
            {
                timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        private void AddToUpdateLogQueue(KeyValuePair<string, string> item)
        {
            if(updateLogQueue.Count == MaxUpdateLogEntriesToKeep)
            {
                updateLogQueue.Dequeue();
            }

            updateLogQueue.Enqueue(item);
        }
    }
}
