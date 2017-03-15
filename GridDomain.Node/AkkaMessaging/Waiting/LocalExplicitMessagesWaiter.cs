using System;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging.Akka;

namespace GridDomain.Node.AkkaMessaging.Waiting
{
    public class LocalExplicitMessagesWaiter : LocalMessagesWaiter<Task<IWaitResults>>
    {
        public LocalExplicitMessagesWaiter(ActorSystem system,
                                           IActorSubscriber subscriber,
                                           TimeSpan defaultTimeout) : this(system, subscriber, defaultTimeout, new ConditionBuilder<Task<IWaitResults>>()) {}

        public LocalExplicitMessagesWaiter(ActorSystem system, 
                                           IActorSubscriber subscriber,
                                           TimeSpan defaultTimeout, 
                                           ConditionBuilder<Task<IWaitResults>> conditionBuilder) : base(system, subscriber, defaultTimeout, conditionBuilder)
        {
            conditionBuilder.CreateResultFunc = Start;
        }
    }
}