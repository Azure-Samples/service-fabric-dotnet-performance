// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace SfActor
{
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Runtime;
    using SfActorInterface;

    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Persisted)]
    internal class SfActor : Actor, ISfActor
    {
        private const string StateName = "data";

        public SfActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        Task<byte[]> ISfActor.GetDataAsync()
        {
            return this.StateManager.GetStateAsync<byte[]>(StateName);
        }

        Task ISfActor.SetDataAsync(byte[] data)
        {
            return this.StateManager.SetStateAsync(StateName, data);
        }
    }
}