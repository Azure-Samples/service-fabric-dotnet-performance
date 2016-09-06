// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverLib
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class TestResults
    {
        [DataMember] public long OperationLatency100NanoSecRunningTotal;

        [DataMember] public long TotalOperationsPerformed;

        private TimeSpan runTime = TimeSpan.Zero;
        private double averageOperationsPerSec = Double.NaN;
        private double averageOperationLatencyMilliseconds = Double.NaN;

        public override string ToString()
        {
            return String.Format(
                "Run time: {0}, Total operation count: {1}, Average operations/sec: {2}, Average operation latency (milliseconds): {3}",
                this.runTime,
                this.TotalOperationsPerformed,
                this.averageOperationsPerSec,
                this.averageOperationLatencyMilliseconds);
        }

        public static TestResults Combine(TestResults r1, TestResults r2)
        {
            TestResults r = new TestResults();
            r.OperationLatency100NanoSecRunningTotal = r1.OperationLatency100NanoSecRunningTotal + r2.OperationLatency100NanoSecRunningTotal;
            r.TotalOperationsPerformed = r1.TotalOperationsPerformed + r2.TotalOperationsPerformed;
            return r;
        }

        public void ComputeAverages(TimeSpan testRunTime)
        {
            this.runTime = testRunTime;
            this.averageOperationsPerSec = ((double) this.TotalOperationsPerformed)/this.runTime.TotalSeconds;
            this.averageOperationLatencyMilliseconds = ((double) this.OperationLatency100NanoSecRunningTotal)/(((double) this.TotalOperationsPerformed)*10000);
        }
    }
}