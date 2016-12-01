﻿using System.Linq;
using Akka.Actor;
using CommonDomain.Core;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging.Akka;
using GridDomain.CQRS.Messaging.MessageRouting;
using GridDomain.EventSourcing.Sagas;
using GridDomain.Node.Actors;
using GridDomain.Node.AkkaMessaging;
using GridDomain.Node.AkkaMessaging.Routing;

namespace GridDomain.Node
{

    public class ActorMessagesRouter : IMessagesRouter
    {
        private readonly TypedMessageActor<CreateActorRouteMessage> _routingActorTypedMessageActor;
        private readonly TypedMessageActor<CreateHandlerRouteMessage> _routingTypedMessageActor;

        public ActorMessagesRouter(IActorRef routingActor)
        {
            _routingTypedMessageActor = new TypedMessageActor<CreateHandlerRouteMessage>(routingActor);
            _routingActorTypedMessageActor = new TypedMessageActor<CreateActorRouteMessage>(routingActor);
        }

        public IRouteBuilder<TMessage> Route<TMessage>()
        {
            return new AkkaRouteBuilder<TMessage>(_routingTypedMessageActor);
        }

        public void RegisterAggregate<TAggregate, TCommandHandler>()
            where TAggregate : AggregateBase
            where TCommandHandler : AggregateCommandsHandler<TAggregate>, new()
        {
            var descriptor = new AggregateCommandsHandlerDesriptor<TAggregate>();
            foreach(var info in new TCommandHandler().RegisteredCommands)
                descriptor.RegisterCommand(info.CommandType,info.Property);
            RegisterAggregate(descriptor);
        }

        public void RegisterAggregate(IAggregateCommandsHandlerDesriptor descriptor)
        {
            var name = $"Aggregate_{descriptor.AggregateType.Name}";
            var createActorRoute = CreateActorRouteMessage.ForAggregate(name, descriptor);
            _routingActorTypedMessageActor.Handle(createActorRoute);
        }

        public void RegisterSaga(ISagaDescriptor sagaDescriptor, string name)
        {
            var createActorRoute = CreateActorRouteMessage.ForSaga(sagaDescriptor, name);
            _routingActorTypedMessageActor.Handle(createActorRoute);
        }

        public void RegisterHandler<TMessage, THandler>(string correlationPropertyName) where THandler : IHandler<TMessage>
        {
            Route<TMessage>().ToHandler<THandler>().WithCorrelation(correlationPropertyName).Register();
        }

        public void RegisterProjectionGroup<T>(T @group) where T : IProjectionGroup
        {
            var createActorRoute = new CreateActorRouteMessage(typeof(SynchronizationProcessingActor<T>),
                                                               typeof(T).Name,
                                                               @group.AcceptMessages.ToArray());
            _routingActorTypedMessageActor.Handle(createActorRoute);
        }

    }
}