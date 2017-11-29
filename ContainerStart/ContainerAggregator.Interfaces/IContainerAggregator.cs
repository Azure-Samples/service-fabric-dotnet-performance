using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Actors;

namespace ContainerAggregator.Interfaces
{
    public interface IContainerAggregator : IService
    {
        Task<long> GetLivingCount();

        Task ReportAlive(string nodeName, long aliveContainerCount, int ignoreOtherUpdatesForMins);

        Task<List<KeyValuePair<string, long>>> GetKeyValues();

        Task<Queue<KeyValuePair<string, string>>> GetUpdateLogEntries();
    }

    public interface IContainerAggregatorActor : IActor
    {
    }
}
