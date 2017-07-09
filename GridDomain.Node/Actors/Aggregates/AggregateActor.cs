using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors.Aggregates.Messages;
using GridDomain.Node.Actors.CommandPipe.Messages;
using GridDomain.Node.Actors.EventSourced;
using GridDomain.Node.Actors.EventSourced.Messages;

namespace GridDomain.Node.Actors.Aggregates
{
    /// <summary>
    ///     Name should be parse by AggregateActorName
    /// </summary>
    /// <typeparam name="TAggregate"></typeparam>
    public class AggregateActor<TAggregate> : EventSourcedActor<TAggregate> where TAggregate : EventSourcing.Aggregate
    {
        private readonly IActorRef _customHandlersActor;
        private readonly ProcessEntry _domainEventProcessEntry;
        private readonly ProcessEntry _domainEventProcessFailEntry;
        private readonly IPublisher _publisher;

        private readonly IAggregateCommandsHandler<TAggregate> _aggregateCommandsHandler;
        private readonly List<IActorRef> _commandCompletedWaiters = new List<IActorRef>();
        private AggregateCommandExecutionContext<TAggregate> ExecutionContext { get; } = new AggregateCommandExecutionContext<TAggregate>();

        public AggregateActor(IAggregateCommandsHandler<TAggregate> handler,
                              IActorRef schedulerActorRef,
                              IPublisher publisher,
                              ISnapshotsPersistencePolicy snapshotsPersistencePolicy,
                              IConstructAggregates aggregateConstructor,
                              IActorRef customHandlersActor) : base(aggregateConstructor, snapshotsPersistencePolicy)
        {
            _aggregateCommandsHandler = handler;
            _publisher = publisher;
            _customHandlersActor = customHandlersActor;
            _domainEventProcessEntry = new ProcessEntry(Self.Path.Name, SimpleAggregateActorConstants.PublishingEvent, SimpleAggregateActorConstants.CommandExecutionCreatedAnEvent);
            _domainEventProcessFailEntry = new ProcessEntry(Self.Path.Name, SimpleAggregateActorConstants.CreatedFault, SimpleAggregateActorConstants.CommandRaisedAnError);
            State.Persist = PersistEventPack(Self);
            Behavior.Become(AwaitingCommandBehavior, nameof(AwaitingCommandBehavior));
        }
        protected virtual void AwaitingCommandBehavior()
        {
            DefaultBehavior();
            Command<NotifyOnCommandComplete>(n =>
                                             {
                                                 _commandCompletedWaiters.Add(Sender);
                                                 Sender.Tell(NotifyOnCommandCompletedAck.Instance);
                                             });

            Command<IMessageMetadataEnvelop<ICommand>>(m =>
                                                       {
                                                           var cmd = m.Message;
                                                           Monitor.Increment(nameof(CQRS.Command));
                                                           ExecutionContext.Command = cmd;
                                                           ExecutionContext.CommandMetadata = m.Metadata;

                                                           _aggregateCommandsHandler.ExecuteAsync(State, 
                                                                                                  cmd,
                                                                                                  PersistEventPack(Self))
                                                                                    .ContinueWith(t => ProducedEventsPersisted.Instance)
                                                                                    .PipeTo(Self);

                                                           Behavior.Become(ProcessingCommandBehavior, nameof(ProcessingCommandBehavior));
                                                       });
        }

        private PersistenceDelegate PersistEventPack(IActorRef self)
        {
            return agr =>
                   {
                       ExecutionContext.ProducedState = (TAggregate)agr;
                       return self.Ask<EventsPersisted>(new PersistEvents(agr.GetDomainEvents()));
                   };
        }

        private void ProcessingCommandBehavior()
        {
            var commandMetadata = ExecutionContext.CommandMetadata;
            var producedEventsMetadata = commandMetadata.CreateChild(Id, _domainEventProcessEntry);

            //just for catching Failures on events persist
            Command<PersistEvents>(e =>
                                   {
                                       Monitor.Increment(nameof(EventPersistingInProgress));
                                       var command = ExecutionContext.Command;
                                       var domainEvents = e.Events;

                                       //dirty hack, but we know nobody will modify domain events before us 
                                       foreach (var evt in domainEvents)
                                           evt.SagaId = command.SagaId;

                                       ExecutionContext.MessagesToProject.AddRange(domainEvents);

                                       if (!domainEvents.Any())
                                       {
                                           Log.Warning("Aggregate {id} is saving zero events", PersistenceId);
                                       }

                                       PersistAll(domainEvents,
                                           persistedEvent =>
                                           {
                                               try
                                               {
                                                   ExecutionContext.ProducedState.MarkPersisted(persistedEvent);
                                               }
                                               catch (Exception ex)
                                               {
                                                   Log.Error(SimpleAggregateActorConstants.ErrorOnEventApplyText, Id, command);
                                                   PublishError(command, commandMetadata, ex);
                                                   //intentionally drop all pending commands and messages
                                                   //and don't wait end of projection builders processing as
                                                   //state is corrupted
                                                   Context.Stop(Self);
                                                   return;
                                               }

                                               NotifyPersistenceWatchers(persistedEvent);
                                               TrySaveSnapshot(ExecutionContext.ProducedState);

                                               if (State.HasUncommitedEvents)
                                                   return;

                                               Sender.Tell(EventsPersisted.Instance);
                                           });
                                   });
            //aggregate raised an error during command execution
            Command<Status.Failure>(f => PublishError(ExecutionContext.Command, commandMetadata, f.Cause).PipeTo(Self));

            Command<ProducedEventsPersisted>(newState =>
                                             {
                                                 Log.Debug("{Aggregate} received a {@command}", PersistenceId, newState);

                                                 ExecutionContext.MessagesToProject
                                                                 .Select(e => Project(e, producedEventsMetadata)).
                                                                  ToChain().
                                                                  ContinueWith(t =>
                                                                               {
                                                                                   //Publish produced messages
                                                                                   foreach (var e in ExecutionContext.MessagesToProject)
                                                                                       _publisher.Publish(e, producedEventsMetadata);
                                                                                   return CommandExecuted.Instance;
                                                                               });
                                             });

            Command<CommandExecuted>(c =>
                                     {
                                         Log.Debug("{Aggregate} received a {@command}", PersistenceId, c);

                                         //finish command execution
                                         State = ExecutionContext.ProducedState;
                                         ExecutionContext.Clear();
                                         ((IAggregate) State).ClearUncommittedEvents();

                                         Behavior.Unbecome();
                                         Stash.UnstashAll();
                                         //notify waiters
                                         foreach (var waiter in _commandCompletedWaiters)
                                             waiter.Tell(new CommandCompleted(ExecutionContext.Command.Id));
                                     });

            Command<IMessageMetadataEnvelop<ICommand>>(o => StashMessage(o));

            Command<GracefullShutdownRequest>(o => StashMessage(o));

            DefaultBehavior();
        }

        private Task<AllHandlersCompleted> PublishError(ICommand command, IMessageMetadata commandMetadata, Exception exception)
        {
            var producedFaultMetadata = commandMetadata.CreateChild(command.Id, _domainEventProcessFailEntry);

            var fault = Fault.NewGeneric(command, exception, command.SagaId, typeof(TAggregate));
            Log.Error(exception, "{Aggregate} raised an error {@Exception} while executing {@Command}", PersistenceId, exception, command);

            return Project(fault, producedFaultMetadata).
                ContinueWith(t =>
                             {
                                 _publisher.Publish(fault, producedFaultMetadata);
                                 return t.Result;
                             });
        }

        protected override void TerminatingBehavior()
        {
            Command<IMessageMetadataEnvelop<ICommand>>(c =>
                                                       {
                                                           Self.Tell(CancelShutdownRequest.Instance);
                                                           StashMessage(c);
                                                       });
            base.TerminatingBehavior();
        }

        private Task<AllHandlersCompleted> Project(object evt, IMessageMetadata commandMetadata)
        {
            var envelop = new MessageMetadataEnvelop<Project>(new Project(evt), commandMetadata);
            return _customHandlersActor.Ask<AllHandlersCompleted>(envelop);
        }
    }
}