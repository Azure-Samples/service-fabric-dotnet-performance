// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverLib
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class implements the functionality to perform a specified number of operations.
    /// The operations are performed concurrently, while maintaining a specified concurrency
    /// count.
    /// The operations are performed on a specified number of items, with the number of 
    /// operations being distributed uniformly among those items.
    /// </summary>
    internal class ConcurrentOperationsRunner
    {
        private readonly Func<int, Task> doOperationAsync;
        private readonly int numOutstandingOperations;
        private readonly int numItems;
        private int numOperationsRemaining;
        private TestResults testResults;

        internal ConcurrentOperationsRunner(
            Func<int, Task> doOperationAsync,
            int numOperationsTotal,
            int numOutstandingOperations,
            int numItems)
        {
            this.doOperationAsync = doOperationAsync;
            this.numOperationsRemaining = numOperationsTotal;
            this.numOutstandingOperations = numOutstandingOperations;
            this.numItems = numItems;
            this.testResults = new TestResults();
        }

        internal async Task<TestResults> RunAll()
        {
            // Each task performs operations serially. We create multiple tasks to
            // achieve concurrency. The number of tasks we create equals the concurrency
            // count that we want to achieve.
            Task[] outstandingOperations = new Task[this.numOutstandingOperations];
            for (int i = 0; i < this.numOutstandingOperations; i++)
            {
                // Perform operations serially.
                outstandingOperations[i] = this.RunOperationsSerially();
            }
            // Wait for all the tasks to complete
            await Task.WhenAll(outstandingOperations);
            return this.testResults;
        }

        private async Task RunOperationsSerially()
        {
            Stopwatch stopwatch = new Stopwatch();
            do
            {
                // Check if there are any more operations left to perform.
                int decrementedValue = Interlocked.Decrement(ref this.numOperationsRemaining);
                if (decrementedValue < 0)
                {
                    // No more operations left. We're done.
                    break;
                }

                // Determine which item we want to perform the operation on.
                int itemIndex = decrementedValue%this.numItems;

                // Perform the operation. And measure how long it takes.
                stopwatch.Restart();
                await this.doOperationAsync(itemIndex);
                stopwatch.Stop();

                // Update the results
                Interlocked.Increment(ref this.testResults.TotalOperationsPerformed);
                Interlocked.Add(ref this.testResults.OperationLatency100NanoSecRunningTotal, stopwatch.Elapsed.Ticks);
            } while (true);
        }
    }
}