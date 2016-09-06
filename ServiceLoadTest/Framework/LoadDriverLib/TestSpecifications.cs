// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace LoadDriverLib
{
    using System.Runtime.Serialization;

    [DataContract]
    public class TestSpecifications
    {
        [DataMember] public int NumWriteOperationsTotal;

        [DataMember] public int NumOutstandingWriteOperations;

        [DataMember] public int NumReadOperationsTotal;

        [DataMember] public int NumOutstandingReadOperations;

        [DataMember] public int OperationDataSizeInBytes;

        [DataMember] public int NumItems;

        [DataMember] public string RequestSenderAssemblyName;

        [DataMember] public string RequestSenderTypeName;
    }
}