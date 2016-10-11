// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverServiceInterface
{
    using System.ServiceModel;
    using System.Threading.Tasks;
    using LoadDriverLib;
    using Microsoft.ServiceFabric.Services.Remoting;

    /// <summary>
    /// Interface exposed by the load driver service. This is a service that acts 
    /// as the host for a client that generates load on the target service.
    /// </summary>
    [ServiceContract]
    public interface ILoadDriver : IService
    {
        [OperationContract]
        Task InitializeAsync(TestSpecifications testSpecifications);

        [OperationContract]
        Task<TestResults> RunWriteTestAsync();

        [OperationContract]
        Task<TestResults> RunReadTestAsync();
    }
}