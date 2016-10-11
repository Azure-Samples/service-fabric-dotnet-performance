// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceLoadTestUtilities
{
    using System;
    using System.Threading.Tasks;

    public static class RetriableOperation
    {
        public static async Task PerformAsync(
            Func<Task<bool>> operation,
            int maxRetryCount,
            TimeSpan initialRetryInterval)
        {
            int retriesRemaining = maxRetryCount;
            TimeSpan retryInterval = initialRetryInterval;

            // Keep trying to perform the operation periodically until
            // either the operation succeeds or we have reached the maximum
            // retry count.
            while ((retriesRemaining >= 0) && (!(await operation())))
            {
                // Operation failed and we have retries remaining. So
                // wait for some time and then try again.
                await Task.Delay(retryInterval);

                // Decrement the number of retries remaining
                retriesRemaining--;

                // Back-off
                retryInterval = new TimeSpan(retryInterval.Ticks*2);
            }

            // We would get here if the operation succeeded or if we reached
            // the maximum retry count. If it is the latter, throw an exception.
            if (retriesRemaining < 0)
            {
                string message = String.Format(
                    "Operation {0}.{1} was tried {2} times, but it did not succeed.",
                    operation.Method.DeclaringType.Name,
                    operation.Method.Name,
                    (maxRetryCount + 1));
                throw new RetriableOperationFailedException(message);
            }
        }
    }
}