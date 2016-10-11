// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceLoadTestClient
{
    using System.Collections.Generic;

    internal static class TargetService
    {
        // Information about the .NET type that implements the request sender module for each service type
        internal static Dictionary<Types, Description> SupportedTypes = new Dictionary<Types, Description>()
        {
            {
                Types.SimulatedService,
                new Description()
                {
                    AssemblyName = "SimulatorRequestSender",
                    TypeName = "SimulatorRequestSender.SimulatorRequestSender"
                }
            },
            {
                Types.SfDictionary,
                new Description()
                {
                    AssemblyName = "SFDictionaryRequestSender",
                    TypeName = "SfDictionaryRequestSender.SfDictionaryRequestSender"
                }
            },
            {
                Types.SfActor,
                new Description()
                {
                    AssemblyName = "SFActorRequestSender",
                    TypeName = "SfActorRequestSender.SfActorRequestSender"
                }
            },
        };

        internal class Description
        {
            internal string AssemblyName;
            internal string TypeName;
        }

        /// <summary>
        /// Target service types supported by this test.
        /// </summary>
        internal enum Types
        {
            /// <summary>
            /// This is a mock service type whose only purpose is to help
            /// with testing of the test framework itself.
            /// </summary>
            SimulatedService,

            /// <summary>
            /// Service written using Service Fabric reliable dictionary
            /// </summary>
            SfDictionary,

            /// <summary>
            /// Service written using Service Fabric reliable actors
            /// </summary>
            SfActor
        }
    }
}