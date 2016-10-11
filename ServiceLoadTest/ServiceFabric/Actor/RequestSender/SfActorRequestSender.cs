// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SfActorRequestSender
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using RequestSenderInterface;
    using ServiceFabricRequestSender;
    using SfActorInterface;

    /// <summary>
    /// This class contains the implementation for sending requests to a Service Fabric reliable
    /// actor service
    /// </summary>
    public class SfActorRequestSender : ServiceFabricRequestSender
    {
        private static readonly Uri ActorServiceUri = new Uri("fabric:/SFActorApplication/SfActorService");

        private ISfActor[] sfActorInterfaces;

        public override Uri ServiceName
        {
            get { return ActorServiceUri; }
        }

        protected override Task OnInitializeAsync(RequestSenderSpecifications requestSenderSpecifications)
        {
            this.InitializeOperationDataBuffers(requestSenderSpecifications.OperationDataSizeInBytes);
            this.InitializeProxies(requestSenderSpecifications.NumItems);

            return Task.FromResult<object>(null);
        }

        protected override Task OnSendReadRequestAsync(int itemIndex)
        {
            // Get data from the actor specified by the caller. The caller should
            // have called us previously to write data to this actor in order to
            // ensure that the actor has data available.
            return this.sfActorInterfaces[itemIndex].GetDataAsync();
        }

        protected override Task OnSendWriteRequestAsync(int itemIndex)
        {
            // Write data to the actor specified by the caller
            return this.sfActorInterfaces[itemIndex].SetDataAsync(
                this.GetAnyDataBuffer());
        }

        private void InitializeProxies(int numItems)
        {
            // Create random actor IDs for the desired number of actors.
            // Also create actor proxies to communicate with them.
            this.sfActorInterfaces = new ISfActor[numItems];
            for (int i = 0; i < numItems; i++)
            {
                this.sfActorInterfaces[i] = ActorProxy.Create<ISfActor>(ActorId.CreateRandom(), ActorServiceUri);
            }
        }
    }
}