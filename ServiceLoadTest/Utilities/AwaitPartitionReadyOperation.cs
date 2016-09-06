// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceLoadTestUtilities
{
    using System;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class implements helper methods that wait for partitions of a Service Fabric
    /// service to become ready to accept requests.
    /// </summary>
    public static class AwaitPartitionReadyOperation
    {
        private const int DefaultMaxRetryCount = 10;
        private static readonly TimeSpan DefaultRequestTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan DefaultInitialRetryInterval = TimeSpan.FromMilliseconds(250);

        public static Task<ServicePartitionList> PerformAsync(
            FabricClient fabricClient,
            Uri serviceName)
        {
            return PerformAsync(
                fabricClient,
                serviceName,
                DefaultRequestTimeout,
                DefaultMaxRetryCount,
                DefaultInitialRetryInterval);
        }

        public static async Task<ServicePartitionList> PerformAsync(
            FabricClient fabricClient,
            Uri serviceName,
            TimeSpan requestTimeout,
            int maxRetryCount,
            TimeSpan initialRetryInterval)
        {
            ServicePartitionList partitionList = null;
            await RetriableOperation.PerformAsync(
                async () =>
                {
                    try
                    {
                        // Get the list of partitions for the service
                        partitionList = await fabricClient.QueryManager.GetPartitionListAsync(
                            serviceName,
                            null,
                            requestTimeout,
                            CancellationToken.None);
                    }
                    catch (Exception e)
                    {
                        if ((e is TimeoutException) || (e is FabricTransientException))
                        {
                            // For these exceptions, we would like to retry until the maximum
                            // retry count has been reached.
                            return false;
                        }
                        throw;
                    }

                    // Check if all partitions are in ready state
                    if (partitionList.Any(p => (p.PartitionStatus != ServicePartitionStatus.Ready)))
                    {
                        // Some partitions are not ready. Let's check again after some time (unless
                        // the maximum retry count has been reached).
                        return false;
                    }

                    // All partitions are in ready state
                    return true;
                },
                maxRetryCount,
                initialRetryInterval);
            return partitionList;
        }
    }
}