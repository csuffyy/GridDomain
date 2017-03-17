﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Actor;
using CommonDomain;
using CommonDomain.Core;
using CommonDomain.Persistence;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Node.Actors.CommandPipe;

namespace GridDomain.Node.Actors
{
    //TODO: add status info, e.g. was any errors during execution or recover

    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    public class SagaActor<TState> : AggregateActor<SagaStateAggregate<TState>> where TState : class, ISagaState
    {
        private readonly ProcessEntry _exceptionOnTransit;
        private readonly ISagaProducer<ISaga<TState>> _producer;
        private readonly IPublisher _publisher;
        private readonly Dictionary<Type, string> _sagaIdFields;
        private readonly ProcessEntry _sagaProducedCommand;
        private readonly HashSet<Type> _sagaStartMessageTypes;
        private readonly ProcessEntry _stateChanged;

        private ISaga<TState> _saga;

        public SagaActor(ISagaProducer<ISaga<TState>> producer,
                         IPublisher publisher,
                         IActorRef schedulerActorRef,
                         IActorRef customHandlersActorRef,
                         ISnapshotsPersistencePolicy snapshotsPersistencePolicy,
                         IConstructAggregates aggregatesConstructor)

            : base(new SagaStateCommandHandler<TState>(), schedulerActorRef,
                   publisher,
                   snapshotsPersistencePolicy,
                   aggregatesConstructor,
                   customHandlersActorRef)
        {
            _publisher = publisher;
            _producer = producer;
            _sagaStartMessageTypes = new HashSet<Type>(producer.KnownDataTypes.Where(t => typeof(DomainEvent).IsAssignableFrom(t)));
            _sagaIdFields = producer.Descriptor.AcceptMessages.ToDictionary(m => m.MessageType, m => m.CorrelationField);

            _exceptionOnTransit = new ProcessEntry(Self.Path.Name,
                                                   SagaActorLiterals.CreatedFaultForSagaTransit,
                                                   SagaActorLiterals.SagaTransitCasedAndError);

            _sagaProducedCommand = new ProcessEntry(Self.Path.Name,
                                                    SagaActorLiterals.PublishingCommand,
                                                    SagaActorLiterals.SagaProducedACommand);

            _stateChanged = new ProcessEntry(Self.Path.Name, "Saga state event published", "Saga changed state");

            AwaitingMessageBehavior();
        }

        private void AwaitingMessageBehavior()
        {
            Command<IMessageMetadataEnvelop<DomainEvent>>(CreateSagaIfNeed,
                                                          e => GetSagaId(e.Message) == Id);

            Command<IMessageMetadataEnvelop<IFault>>(CreateSagaIfNeed,
                                                     e => GetSagaId(e.Message) == Id);
        }

        private void CreateSagaIfNeed(IMessageMetadataEnvelop m)
        {
            var msg = m.Message;
            var metadata = m.Metadata;

            Monitor.IncrementMessagesReceived();
            if (_sagaStartMessageTypes.Contains(msg.GetType()))
            {
                _saga = _producer.Create(msg);

                var cmd = new CreateNewStateCommand<TState>(Id, _saga.State);

                Self.Ask<CommandExecuted>(new MessageMetadataEnvelop<ICommand>(cmd, m.Metadata))
                    .PipeTo(Self);

                BecomeStacked(() => ExecutingCommandBehavior(msg, metadata), nameof(ExecutingCommandBehavior));
                return;
            }

            TransitSaga(msg, metadata);
        }

        private void TransitSaga(object msg, IMessageMetadata metadata)
        {
            //block any other executing until saga completes transition
            //cast is need for dynamic call of Transit
            Task<TransitionResult<TState>> processSagaTask = Saga.PreviewTransit((dynamic) msg);
            processSagaTask.ContinueWith(t => new SagaTransited(t.Result.ProducedCommands.ToArray(),
                                                                metadata,
                                                                _sagaProducedCommand,
                                                                t.Result.State,
                                                                t.Exception))
                           .PipeTo(Self);

            BecomeStacked(() => TransitionBehavior(msg, metadata), nameof(TransitionBehavior));
        }

        private void ExecutingCommandBehavior(object msg, IMessageMetadata metadata)
        {
            AwaitingCommandBehavior();
            Command<CommandExecuted>(cmd =>
                                     {
                                         UnbecomeStacktraced();
                                         TransitSaga(msg, metadata);
                                     });
        }

        public ISaga<TState> Saga => _saga ?? (_saga = _producer.Create(State));

        private Guid GetSagaId(object msg)
        {
            var type = msg.GetType();
            string fieldName;

            if (_sagaIdFields.TryGetValue(type, out fieldName))
                return (Guid) type.GetProperty(fieldName).GetValue(msg);

            throw new CannotFindSagaIdException(msg);
        }

        protected override void TerminatingBehavior()
        {
            Command<IMessageMetadataEnvelop<DomainEvent>>(m =>
                                                          {
                                                              Self.Tell(CancelShutdownRequest.Instance);
                                                              Stash.Stash();
                                                          });
            Command<IMessageMetadataEnvelop<IFault>>(m =>
                                                     {
                                                         Self.Tell(CancelShutdownRequest.Instance);
                                                         Stash.Stash();
                                                     });
            base.TerminatingBehavior();
        }

        /// <summary>
        /// </summary>
        /// <param name="message">Usially it is domain event or fault</param>
        /// <param name="messageMetadata"></param>
        private void TransitionBehavior(object message, IMessageMetadata messageMetadata)
        {
            DefaultBehavior();

            Command<SagaTransited>(r =>
                                   {
                                       var cmd = new SaveStateCommand<TState>(Id, (TState) r.NewSagaState, State.Data.CurrentStateName, message);
                                       var envelop = new MessageMetadataEnvelop<ICommand>(cmd, messageMetadata);
                                       BecomeStacked(() => ExecutingCommandBehavior(message, messageMetadata), nameof(ExecutingCommandBehavior));
                                       //write new data to saga state during command execution
                                       //on aggregate apply method after persist callback
                                       Self.Ask<CommandExecuted>(envelop)
                                           .ContinueWith(t => FinishMessageProcessing(r));
                                   });
            Command<Status.Failure>(f =>
                                    {
                                        //Saga.ClearCommandsToDispatch();
                                        var fault = PublishError(message,
                                                                 messageMetadata,
                                                                 f.Cause.UnwrapSingle());

                                        FinishMessageProcessing(new SagaTransitFault(fault, messageMetadata));
                                    });
        }

        private void FinishMessageProcessing(object message)
        {
            //notify saga process actor that saga transit is done
            Context.Parent.Tell(message);
            Stash.UnstashAll();
            UnbecomeStacktraced();
        }

       protected override void FinishCommandExecution(ICommand cmd)
       {
           base.FinishCommandExecution(cmd);
           Self.Tell(new CommandExecuted(cmd.Id));
       }

        private IFault PublishError(object message, IMessageMetadata messageMetadata, Exception exception)
        {
            var processorType = _producer.Descriptor.StateMachineType;

            Log.Error(exception, "Saga {saga} {id} raised an error on {@message}", processorType, Id, message);
            var fault = Fault.NewGeneric(message, exception, Id, processorType);

            var metadata = messageMetadata.CreateChild(fault.SagaId, _exceptionOnTransit);

            _publisher.Publish(fault, metadata);
            return fault;
        }
    }

    public class CommandExecuted
    {
        public Guid CommandId { get; }

        public CommandExecuted(Guid commandId)
        {
            CommandId = commandId;
        }

        public static CommandExecuted Instance { get; } = new CommandExecuted(Guid.Empty);
    }

    internal class CannotFindSagaIdException : Exception
    {
        public object Msg { get; }

        public CannotFindSagaIdException(object msg)
        {
            Msg = msg;
        }
    }
}