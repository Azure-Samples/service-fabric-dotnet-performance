// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverService
{
    using System.Threading;
    using Microsoft.ServiceFabric.Services.Runtime;

    internal static class Program
    {
        private static void Main()
        {
            ServiceRuntime.RegisterServiceAsync(
                "LoadDriverServiceType",
                context => new LoadDriverService(context)).GetAwaiter().GetResult();

            // Prevents this host process from terminating so services keep running.
            Thread.Sleep(Timeout.Infinite);
        }
    }
}