// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverLib
{
    /// <summary>
    /// The load test goes through the states in the enum below
    /// </summary>
    internal enum TestExecutorState : int
    {
        Uninitialized,
        Initializing,
        Initialized,
        RunningWritePhase,
        WritePhaseCompleted,
        RunningReadPhase,
        ReadPhaseCompleted
    }
}