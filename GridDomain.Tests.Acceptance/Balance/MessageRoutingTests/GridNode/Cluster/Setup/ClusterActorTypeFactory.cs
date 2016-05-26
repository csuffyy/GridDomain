using System;
using GridDomain.Node.AkkaMessaging;

namespace GridDomain.Tests.Acceptance.Balance.MessageRoutingTests.GridNode.Cluster.Setup
{
    class ClusterActorTypeFactory : IHandlerActorTypeFactory
    {
        public Type GetActorTypeFor(Type message, Type handler)
        {
            return typeof(ClusterMessageHandlerActor);
        }
    }
}