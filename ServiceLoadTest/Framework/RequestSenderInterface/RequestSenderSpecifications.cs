// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace RequestSenderInterface
{
    /// <summary>
    /// Information provided to the module that is responsible for
    /// sending requests to the target service (i.e. to exercise
    /// load on the target service).
    /// </summary>
    public class RequestSenderSpecifications
    {
        /// <summary>
        /// Number of items in the target service on which the operations
        /// are to be performed. The definition of item is specific to the
        /// target service. For example, it could be the number of actors
        /// in an actor service, number of keys in a dictionary, number of
        /// rows in a database etc.
        /// </summary>
        public int NumItems;

        /// <summary>
        /// Size of payload associated with each operation performed on the
        /// target service. i.e. the number of bytes read or written.
        /// </summary>
        public int OperationDataSizeInBytes;
    }
}