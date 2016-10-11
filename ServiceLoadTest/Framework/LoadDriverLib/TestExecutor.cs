// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverLib
{
    using System;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using RequestSenderInterface;

    /// <summary>
    /// This class represents a client that generates load on the target service.
    /// </summary>
    public class TestExecutor
    {
        private int state;
        private TestSpecifications specifications;
        private IRequestSender requestSender;

        public async Task InitializeAsync(TestSpecifications testSpecifications)
        {
            this.DoVerifiedStateTransition(TestExecutorState.Uninitialized, TestExecutorState.Initializing);

            this.specifications = testSpecifications;
            this.requestSender = await this.GetRequestSenderAsync();

            this.DoStateTransition(TestExecutorState.Initialized);
        }

        public async Task<TestResults> RunWriteTestAsync()
        {
            this.DoVerifiedStateTransition(TestExecutorState.Initialized, TestExecutorState.RunningWritePhase);

            TestResults results = await this.RunTestAsync(
                (index) => this.requestSender.SendWriteRequestAsync(index),
                this.specifications.NumWriteOperationsTotal,
                this.specifications.NumOutstandingWriteOperations);

            this.DoStateTransition(TestExecutorState.WritePhaseCompleted);

            return results;
        }

        public async Task<TestResults> RunReadTestAsync()
        {
            this.DoVerifiedStateTransition(TestExecutorState.WritePhaseCompleted, TestExecutorState.RunningReadPhase);

            TestResults results = await this.RunTestAsync(
                (index) => this.requestSender.SendReadRequestAsync(index),
                this.specifications.NumReadOperationsTotal,
                this.specifications.NumOutstandingReadOperations);

            this.DoStateTransition(TestExecutorState.ReadPhaseCompleted);

            return results;
        }

        private async Task<IRequestSender> GetRequestSenderAsync()
        {
            // Create request sender
            Assembly assembly = Assembly.Load(this.specifications.RequestSenderAssemblyName);
            Type type = assembly.GetType(this.specifications.RequestSenderTypeName);
            IRequestSender sender = (IRequestSender) Activator.CreateInstance(type);
            RequestSenderSpecifications requestSenderSpecifications = new RequestSenderSpecifications()
            {
                NumItems = this.specifications.NumItems,
                OperationDataSizeInBytes = this.specifications.OperationDataSizeInBytes
            };

            // Initialize request sender
            await sender.InitializeAsync(requestSenderSpecifications);
            return sender;
        }

        private async Task<TestResults> RunTestAsync(
            Func<int, Task> doTestOperationAsync,
            int numOperationsTotal,
            int numOutstandingOperations)
        {
            // Perfom operations on the service with the desired concurrency.
            ConcurrentOperationsRunner concurrentOpsRunner = new ConcurrentOperationsRunner(
                doTestOperationAsync,
                numOperationsTotal,
                numOutstandingOperations,
                this.specifications.NumItems);
            return await concurrentOpsRunner.RunAll();
        }

        private void DoStateTransition(TestExecutorState newState)
        {
            // Move the test to the desired state.
            Interlocked.Exchange(ref this.state, (int) newState);
        }

        private void DoVerifiedStateTransition(TestExecutorState expectedCurrentState, TestExecutorState newState)
        {
            // Move the test to the desired state after verifying that the move is valid. 
            int currentState = Interlocked.CompareExchange(
                ref this.state,
                (int) expectedCurrentState,
                (int) newState);
            if ((int) expectedCurrentState != currentState)
            {
                string message = String.Format(
                    "Test executor cannot move from current state {0} to new state {1}. In order to move, the current state must be {2}.",
                    currentState,
                    newState,
                    expectedCurrentState);
                throw new InvalidOperationException(message);
            }
        }
    }
}