using Akka.Actor;
using GridDomain.CQRS;

namespace GridDomain.Tests.Acceptance.MessageRoutingTests.GridNode.SingleSystem.Setup
{
    public class TestHandler : IHandler<TestMessage>
    {
        private readonly IActorRef _notifier;
        private int _handleCounter;

        public TestHandler(IActorRef notifier)
        {
            _notifier = notifier;
        }

        public void Handle(TestMessage msg)
        {
            msg.HandlerHashCode = GetHashCode();
            msg.HandleOrder = ++_handleCounter;
            _notifier.Tell(msg);
        }
    }
}