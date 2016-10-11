// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SfActor
{
    using System.Threading;
    using Microsoft.ServiceFabric.Actors.Runtime;

    internal static class Program
    {
        private static void Main()
        {
            ActorRuntime.RegisterActorAsync<SfActor>(
                (context, actorType) => new ActorService(context, actorType, () => new SfActor())).GetAwaiter().GetResult();

            // Prevents this host process from terminating so services keep running.
            Thread.Sleep(Timeout.Infinite);
        }
    }
}