using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Samples.Utility;
using ContainerAggregator.Interfaces;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Generator;

namespace WebFront
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class WebFront : StatelessService
    {
        private ILivenessCounter<string> counter;
        private Timer reportTimer;
        private string nodeName;
        Uri serviceUri;
        IContainerAggregator proxy;
        private readonly string configPackageName = "Config";
        private readonly string livenessCounterSettingSectionName = "LivenessCounterSettings";
        private readonly string expirationIntervalInSecondsKeyName = "ExpirationIntervalInSeconds";
        private readonly string fuzzIntervalInSecondsKeyName = "FuzzIntervalInSeconds";
        private readonly string reportIntervalInSecondsKeyName = "ReportIntervalInSeconds";

        private int expirationIntervalInSeconds = 30;
        private int fuzzIntervalInSeconds = 5;
        private int reportIntervalInSeconds = 5;
        private Random rand = new Random();


        public WebFront(StatelessServiceContext context)
            : base(context)
        {
            this.LoadLiveCounterSettings();
            this.counter = new LivenessCounter<string>(expirationIntervalInSeconds, fuzzIntervalInSeconds);
            nodeName = context.NodeContext.NodeName;
            serviceUri = ActorNameFormat.GetFabricServiceUri(typeof(IContainerAggregatorActor));
            proxy = ActorServiceProxy.Create<IContainerAggregator>(serviceUri, 0);
            reportTimer = new Timer(this.Report, null, TimeSpan.FromSeconds(reportIntervalInSeconds), TimeSpan.FromSeconds(reportIntervalInSeconds));
        }

        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            if (this.reportTimer != null)
            {
                this.reportTimer.Dispose();
            }
            return base.OnCloseAsync(cancellationToken);
       }

        private void Report(object state)
        {
            try
            {
                proxy.ReportAlive(nodeName, this.counter.GetLivingCount(), 0).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                // Do nothing if actor service is down.
            }
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, "ServiceEndpoint", (url, listener) =>
                    {
                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting WebListener on {url}");

                        return new WebHostBuilder().UseKestrel(options=> options.ShutdownTimeout = TimeSpan.FromSeconds(1))
                                    .ConfigureServices(
                                        services => services
                                            .AddSingleton<StatelessServiceContext>(serviceContext))
                                            .ConfigureServices(services => services.AddSingleton<ILivenessCounter<string>>(this.counter))
                                            .ConfigureServices(services => services.AddSingleton<Uri>(this.serviceUri))
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseStartup<Startup>()
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.None)
                                    .UseUrls(url)
                                    .Build();
                    }))
            };
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

                if (section.Parameters.Contains(reportIntervalInSecondsKeyName))
                {
                    var value = section.Parameters[reportIntervalInSecondsKeyName].Value.Trim();
                    this.reportIntervalInSeconds = int.Parse(value);
                }
            }
        }
    }
}
