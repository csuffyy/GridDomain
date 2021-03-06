using System;
using GridDomain.CQRS;
using GridDomain.Node;

namespace GridDomain.Tests.Framework
{
    public static class GridNodeExtensions
    {
        public static IMessageWaiter<AnyMessagePublisher> NewDebugWaiter(this GridDomainNode node, TimeSpan? timeout = null)
        {
            return new DebugLocalWaiter(node.Transport, node.System, node.Transport,timeout ??  node.DefaultTimeout);

        }
    }
}