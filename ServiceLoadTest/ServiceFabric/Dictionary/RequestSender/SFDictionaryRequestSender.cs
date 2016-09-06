// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SfDictionaryRequestSender
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using RequestSenderInterface;
    using ServiceFabricRequestSender;
    using SfDictionaryInterface;

    /// <summary>
    /// This class contains the implementation for sending requests to a Service Fabric service that
    /// uses a reliable dictionary.
    /// </summary>
    public class SfDictionaryRequestSender : ServiceFabricRequestSender
    {
        private static readonly Uri DictionaryServiceUri = new Uri("fabric:/SFDictionaryApplication/SFDictionaryService");

        private long[] keys;
        private ISfDictionary[] sfDictionaryInterfaces;

        public override Uri ServiceName
        {
            get { return DictionaryServiceUri; }
        }

        protected override Task OnInitializeAsync(RequestSenderSpecifications requestSenderSpecifications)
        {
            this.InitializeOperationDataBuffers(requestSenderSpecifications.OperationDataSizeInBytes);

            Random random = new Random();
            this.InitializeKeysAndProxies(random, requestSenderSpecifications.NumItems);

            return Task.FromResult<object>(null);
        }

        protected override Task OnSendReadRequestAsync(int itemIndex)
        {
            // Get data from the key specified by the caller. The caller should
            // have called us previously to write data to this key in order to
            // ensure that the key has data available.
            return this.sfDictionaryInterfaces[itemIndex].GetDataAsync(this.keys[itemIndex]);
        }

        protected override Task OnSendWriteRequestAsync(int itemIndex)
        {
            // Write data to the key specified by the caller
            return this.sfDictionaryInterfaces[itemIndex].SetDataAsync(
                this.keys[itemIndex],
                this.GetAnyDataBuffer());
        }

        private void InitializeKeysAndProxies(Random random, int numItems)
        {
            // Create random keys for the reliable dictionary.
            // Also create service proxies to communicate with the service.
            this.keys = new long[numItems];
            this.sfDictionaryInterfaces = new ISfDictionary[numItems];
            byte[] keyGenerationBuffer = new byte[8];
            for (int i = 0; i < numItems; i++)
            {
                random.NextBytes(keyGenerationBuffer);
                this.keys[i] = BitConverter.ToInt64(keyGenerationBuffer, 0);
                this.sfDictionaryInterfaces[i] = ServiceProxy.Create<ISfDictionary>(
                    DictionaryServiceUri,
                    new ServicePartitionKey(this.keys[i]));
            }
        }
    }
}