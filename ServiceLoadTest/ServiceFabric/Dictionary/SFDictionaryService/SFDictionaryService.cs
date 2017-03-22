// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SfDictionaryService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.FabricTransport.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using SfDictionaryInterface;

    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class SfDictionaryService : StatefulService, ISfDictionary
    {
        private const string ServiceEndpointResourceName = "SfDictionaryServiceEndpoint";
        private const string DictionaryNamePrefix = "TestDictionary";
        private readonly string dictionaryName;

        public SfDictionaryService(StatefulServiceContext context)
            : base(context)
        {
            this.dictionaryName = String.Concat(DictionaryNamePrefix, "-", context.PartitionId.ToString("D"));
        }

        public async Task SetDataAsync(long key, byte[] data)
        {
            IReliableDictionary<long, byte[]> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, byte[]>>(this.dictionaryName);
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                await dictionary.SetAsync(tx, key, data);
                await tx.CommitAsync();
            }
        }

        public async Task<byte[]> GetDataAsync(long key)
        {
            IReliableDictionary<long, byte[]> dictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<long, byte[]>>(this.dictionaryName);
            byte[] result = null;
            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<byte[]> resultWrapper = await dictionary.TryGetValueAsync(tx, key);
                result = resultWrapper.Value;
                await tx.CommitAsync();
            }
            return result;
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see http://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            ServiceReplicaListener serviceReplicaListener = new ServiceReplicaListener(this.CreateFabricCommunicationListener);
            return new[] {serviceReplicaListener};
        }

        private ICommunicationListener CreateFabricCommunicationListener(ServiceContext context)
        {
            return new FabricTransportServiceRemotingListener(
                context,
                this,
                new FabricTransportRemotingListenerSettings()
                {
                    EndpointResourceName = ServiceEndpointResourceName
                });
        }
    }
}