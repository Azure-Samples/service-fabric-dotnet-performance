// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace RequestSenderInterface
{
    using System.Threading.Tasks;

    /// <summary>
    /// Interface implemented by the module that is responsible for
    /// sending requests to the target service (i.e. to exercise
    /// load on the target service).
    /// </summary>
    public interface IRequestSender
    {
        /// <summary>
        /// Perform initialization that is needed for sending requests to the service.
        /// </summary>
        /// <param name="requestSenderSpecifications">Information about the operations
        /// that the request sender will be asked to perform when the test is running.</param>
        /// <returns>A task that represents the initialization of the request sender.</returns>
        Task InitializeAsync(RequestSenderSpecifications requestSenderSpecifications);

        /// <summary>
        /// Write data to the specified item in the service.
        /// </summary>
        /// <param name="itemIndex">Index of the item to which the data should be
        /// written. Indexes range from 0 to ([item-count]-1)</param>
        /// <returns>A task that represents the write operation.</returns>
        Task SendWriteRequestAsync(int itemIndex);

        /// <summary>
        /// Read data from the specified item in the service.
        /// </summary>
        /// <param name="itemIndex">Index of the item from which the data should be
        /// read. Indexes range from 0 to ([item-count]-1)</param>
        /// <returns>A task that represents the read operation.</returns>
        Task SendReadRequestAsync(int itemIndex);
    }
}