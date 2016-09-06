// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SimulatorRequestSender
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using RequestSenderInterface;

    /// <summary>
    /// This class contains the implementation for sending requests to a mock service.
    /// It is only used for testing the test framework itself.
    /// </summary>
    public class SimulatorRequestSender : IRequestSender
    {
        private const int OperationDataSizeLowerLimit = 1024;
        private const int OperationDataSizeUpperLimit = 4*1024*1024;
        private const int ConcurrencyComputationDividend = 4*1024*1024;
        private const int MillisecondsDelayPerKByteForReads = 15;
        private const int MillisecondsDelayPerKByteForWrites = 30;
        private TimeSpan readDelay;
        private TimeSpan writeDelay;
        private SemaphoreSlim concurrencyGuard;
        private SemaphoreSlim[] itemLocks;

        public Task InitializeAsync(RequestSenderSpecifications requestSenderSpecifications)
        {
            if ((requestSenderSpecifications.OperationDataSizeInBytes < OperationDataSizeLowerLimit) ||
                (requestSenderSpecifications.OperationDataSizeInBytes > OperationDataSizeUpperLimit))
            {
                string message = String.Format(
                    "Request sender simulator does not support data size {0}. The data size must be between {1} and {2}.",
                    requestSenderSpecifications.OperationDataSizeInBytes,
                    OperationDataSizeLowerLimit,
                    OperationDataSizeUpperLimit);
                throw new ArgumentException(message);
            }
            this.readDelay = TimeSpan.FromMilliseconds(MillisecondsDelayPerKByteForReads*(requestSenderSpecifications.OperationDataSizeInBytes/1024));
            this.writeDelay = TimeSpan.FromMilliseconds(MillisecondsDelayPerKByteForWrites*(requestSenderSpecifications.OperationDataSizeInBytes/1024));
            int concurrencyCount = ConcurrencyComputationDividend/requestSenderSpecifications.OperationDataSizeInBytes;
            this.concurrencyGuard = new SemaphoreSlim(concurrencyCount, concurrencyCount);
            this.itemLocks = new SemaphoreSlim[requestSenderSpecifications.NumItems];
            for (int i = 0; i < requestSenderSpecifications.NumItems; i++)
            {
                this.itemLocks[i] = new SemaphoreSlim(1, 1);
            }
            return Task.FromResult<object>(null);
        }

        public Task SendReadRequestAsync(int itemIndex)
        {
            return this.SendRequestAsync(itemIndex, this.readDelay);
        }

        public Task SendWriteRequestAsync(int itemIndex)
        {
            return this.SendRequestAsync(itemIndex, this.writeDelay);
        }

        public async Task SendRequestAsync(int itemIndex, TimeSpan delay)
        {
            await this.concurrencyGuard.WaitAsync();
            await this.itemLocks[itemIndex].WaitAsync();
            await Task.Delay(delay);
            this.itemLocks[itemIndex].Release();
            this.concurrencyGuard.Release();
        }
    }
}