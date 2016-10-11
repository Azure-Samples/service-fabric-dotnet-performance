﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SfDictionaryInterface
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Remoting;

    public interface ISfDictionary : IService
    {
        Task SetDataAsync(long key, byte[] data);
        Task<byte[]> GetDataAsync(long key);
    }
}