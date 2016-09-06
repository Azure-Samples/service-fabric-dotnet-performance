// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceFabricRequestSender
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using RequestSenderInterface;
    using ServiceLoadTestUtilities;

    /// <summary>
    /// This class implements common functionality for sending requests to a 
    /// target service that is written using Service Fabric.
    /// </summary>
    public abstract class ServiceFabricRequestSender : IRequestSender
    {
        private const int TotalOperationDataBufferSizeBytes = 256*1024*1024;

        private byte[][] operationDataBuffers;
        private long nextDataBufferIndex;

        public abstract Uri ServiceName { get; }

        public async Task InitializeAsync(RequestSenderSpecifications requestSenderSpecifications)
        {
            // Wait for all partitions of the service to become ready to accept requests.
            FabricClient fabricClient = new FabricClient();
            await AwaitPartitionReadyOperation.PerformAsync(fabricClient, this.ServiceName);

            await this.OnInitializeAsync(requestSenderSpecifications);
        }

        public Task SendReadRequestAsync(int itemIndex)
        {
            return this.OnSendReadRequestAsync(itemIndex);
        }

        public Task SendWriteRequestAsync(int itemIndex)
        {
            return this.OnSendWriteRequestAsync(itemIndex);
        }

        protected void InitializeOperationDataBuffers(int operationDataSizeInBytes)
        {
            // Preallocate some buffers and fill them with random data. These buffers
            // will be used to send requests to the service.
            Random random = new Random();
            int bufferCount = TotalOperationDataBufferSizeBytes/operationDataSizeInBytes;
            if (bufferCount == 0)
            {
                bufferCount = 1;
            }
            this.operationDataBuffers = new byte[bufferCount][];
            for (int i = 0; i < bufferCount; i++)
            {
                this.operationDataBuffers[i] = new byte[operationDataSizeInBytes];
                random.NextBytes(this.operationDataBuffers[i]);
            }
        }

        protected byte[] GetAnyDataBuffer()
        {
            // Loop through our pre-allocated buffers in a round-robin manner.
            long bufferIndex = Interlocked.Increment(ref this.nextDataBufferIndex);
            bufferIndex = bufferIndex%this.operationDataBuffers.Length;
            return this.operationDataBuffers[bufferIndex];
        }

        protected abstract Task OnInitializeAsync(RequestSenderSpecifications requestSenderSpecifications);

        protected abstract Task OnSendReadRequestAsync(int itemIndex);

        protected abstract Task OnSendWriteRequestAsync(int itemIndex);
    }
}