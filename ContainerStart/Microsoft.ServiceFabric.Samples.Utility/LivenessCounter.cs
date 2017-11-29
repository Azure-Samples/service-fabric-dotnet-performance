﻿// ------------------------------------------------------------------
// Copyright (c) Microsoft.  All Rights Reserved.
// ------------------------------------------------------------------

namespace Microsoft.ServiceFabric.Samples.Utility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    public class LivenessCounter<T> : ILivenessCounter<T>
    {
        private readonly Random rand = new Random();
        private readonly ConcurrentDictionary<T, Entry> livenessEntries;
        private readonly int fuzzIntervalInSeconds;
        private readonly int expirationIntervalInSeconds;

        private long aliveCount;

        public LivenessCounter(
            int expirationIntervalInSeconds = 30,
            int fuzzIntervalInSeconds = 5)
        {
            this.livenessEntries = new ConcurrentDictionary<T, Entry>();
            this.expirationIntervalInSeconds = expirationIntervalInSeconds;
            this.fuzzIntervalInSeconds = fuzzIntervalInSeconds;
            this.aliveCount = 0;
        }

        public long GetLivingCount()
        {
            return Interlocked.Read(ref this.aliveCount);
        }

        public void ReportAlive(T group, long numberOfAliveItems = 1)
        {
            this.ExtendExpiration(group, numberOfAliveItems);
        }

        public List<KeyValuePair<T, long>> GetKeyValues()
        {
            List<KeyValuePair<T, long>> items = new List<KeyValuePair<T, long>>();
            foreach (var key in livenessEntries.Keys)
            {
                KeyValuePair<T, long> pair = new KeyValuePair<T, long>(key, livenessEntries[key].NumAlive);
                items.Add(pair);
            }

            return items;
        }        

        public bool TryGetValueForKey(T key, out long value)
        {
            value = 0;
            
            if(!livenessEntries.ContainsKey(key))
            {
                return false;
            }
            
            value = livenessEntries[key].NumAlive;
            return true;
        }

        private void ExtendExpiration(T key, long numberOfAliveItems)
        {
            bool addCalled = false;
            this.livenessEntries.AddOrUpdate(
                key,
                (k) =>
                {
                    addCalled = true;
                    return new Entry(this.CreateExpiryTimer(k), 0);
                },
                (k, e) =>
                {
                    this.ExtendExpiration(e, numberOfAliveItems);
                    return e;
                });

            if (addCalled)
            {
                this.ExtendExpiration(key, numberOfAliveItems);
            }
        }

        private void ExtendExpiration(Entry entry, long numberOfAliveItems)
        {
            long currentAlive;
            lock (entry)
            {
                entry.ExpiryTimer.Change(this.GetDueTime(), Timeout.InfiniteTimeSpan);
                currentAlive = entry.NumAlive;
                entry.NumAlive = numberOfAliveItems;
            }

            this.UpdateLivenessCount(numberOfAliveItems - currentAlive);
        }

        private void RemoveEntry(T key)
        {
            Entry livenessEntry;
            if (this.livenessEntries.TryRemove(key, out livenessEntry))
            {
                long currentAlive;
                lock (livenessEntry)
                {
                    livenessEntry.ExpiryTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                    currentAlive = livenessEntry.NumAlive;
                }

                this.DecrementLivenessCount(currentAlive);
            }
        }

        private Timer CreateExpiryTimer(T key)
        {
            return new Timer(
                this.OnEntryExpired, 
                key,
                Timeout.InfiniteTimeSpan, 
                Timeout.InfiniteTimeSpan);
        }

        private void OnEntryExpired(object state)
        {
            this.RemoveEntry((T)state);
        }

        private TimeSpan GetDueTime()
        {
            var dueTimeMillis =
                this.rand.Next(this.fuzzIntervalInSeconds * 1000) +
                this.expirationIntervalInSeconds * 1000;

            return TimeSpan.FromMilliseconds(dueTimeMillis);
        }

        private void DecrementLivenessCount(long livenessEntryNumAlive)
        {
            for (var i = 0; i < livenessEntryNumAlive; i++)
            {
                Interlocked.Decrement(ref this.aliveCount);
            }
        }

        private void IncrementLivenessCount(long livenessEntryNumAlive)
        {
            for (var i = 0; i < livenessEntryNumAlive; i++)
            {
                Interlocked.Increment(ref this.aliveCount);
            }
        }

        private void UpdateLivenessCount(long i)
        {
            if (i < 0)
            {
                this.DecrementLivenessCount(-i);
            }
            else if (i > 0)
            {
                this.IncrementLivenessCount(i);
            }
        }

        private class Entry
        {
            private readonly Timer timer;

            public Entry(Timer expiryTimer, int numAlive)
            {
                this.timer = expiryTimer;
                this.NumAlive = numAlive;
            }

            public Timer ExpiryTimer => this.timer;

            public long NumAlive { get; set; }
        }
    }
}
