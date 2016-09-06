// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceLoadTestUtilities
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    internal sealed class RetriableOperationFailedException : Exception
    {
        public RetriableOperationFailedException() : base()
        {
        }

        public RetriableOperationFailedException(string message) : base(message)
        {
        }

        public RetriableOperationFailedException(string message, Exception inner)
            : base(message, inner)
        {
        }

        private RetriableOperationFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}