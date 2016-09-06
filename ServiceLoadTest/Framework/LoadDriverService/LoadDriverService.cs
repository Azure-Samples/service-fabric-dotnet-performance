// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverService
{
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading.Tasks;
    using LoadDriverLib;
    using LoadDriverServiceInterface;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;

    /// <summary>
    /// This service serves as the host for a client that generates load on the target service.
    /// </summary>
    internal sealed class LoadDriverService : StatelessService, ILoadDriver
    {
        private TestExecutor testExecutor;

        public LoadDriverService(StatelessServiceContext context)
            : base(context)
        {
        }

        public Task InitializeAsync(TestSpecifications testSpecifications)
        {
            this.testExecutor = new TestExecutor();
            return this.testExecutor.InitializeAsync(testSpecifications);
        }

        public Task<TestResults> RunWriteTestAsync()
        {
            return this.testExecutor.RunWriteTestAsync();
        }

        public Task<TestResults> RunReadTestAsync()
        {
            return this.testExecutor.RunReadTestAsync();
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                new ServiceInstanceListener(
                    (context) =>
                    {
                        // Use HTTP binding so that the client that coordinates the end-to-end test
                        // can talk to this service via reverse proxy.
                        return new WcfCommunicationListener<ILoadDriver>(
                            context,
                            this,
                            BindingUtility.CreateHttpBinding(),
                            (string) null);
                    })
            };
        }
    }
}