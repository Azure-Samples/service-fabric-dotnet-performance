// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverServiceInterface
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Channels;

    public class BindingUtility
    {
        private const int DefaultMaxReceivedMessageSize = 4*1024*1024;
        private static readonly TimeSpan DefaultOpenCloseTimeout = TimeSpan.FromSeconds(5);

        // Requests sent to the load driver will time out after the duration specified below.
        //
        // NOTE: When sending requests to the load driver via the reverse proxy, ensure that the
        // Azure load balancer idle timeout for connections to the reverse proxy is at least as
        // high as the duration specified below. This will ensure that connection between the
        // client and the reverse proxy is kept alive while the load driver is processing the
        // client's request. The idle timeout for the reverse proxy connection can be specified
        // via the "idleTimeoutInMinutes" property in the ARM template under the load balancing
        // rules for the reverse proxy. For more information, please see:
        // https://azure.microsoft.com/en-us/documentation/articles/service-fabric-reverseproxy/
        private static readonly TimeSpan DefaultSendReceiveTimeout = TimeSpan.FromMinutes(30);

        public static Binding CreateHttpBinding()
        {
            return new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                SendTimeout = DefaultSendReceiveTimeout,
                ReceiveTimeout = DefaultSendReceiveTimeout,
                OpenTimeout = DefaultOpenCloseTimeout,
                CloseTimeout = DefaultOpenCloseTimeout,
                MaxReceivedMessageSize = DefaultMaxReceivedMessageSize,
                MaxBufferSize = DefaultMaxReceivedMessageSize,
                MaxBufferPoolSize = Environment.ProcessorCount*DefaultMaxReceivedMessageSize,
            };
        }
    }
}