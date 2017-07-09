using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Persistence;
using GridDomain.Common;
using GridDomain.CQRS.Messaging.Akka.Remote;
using GridDomain.EventSourcing;
using GridDomain.EventSourcing.CommonDomain;
using GridDomain.Node.Actors.EventSourced.Messages;
using GridDomain.Node.AkkaMessaging;

namespace GridDomain.Node.Actors.EventSourced
{

    public class EventSourcedActor<T> : ReceivePersistentActor where T : EventSourcing.Aggregate
    {
        private readonly List<IActorRef> _persistenceWatchers = new List<IActorRef>();
        private readonly ISnapshotsPersistencePolicy _snapshotsPolicy;
        private readonly TimeSpan _terminateWaitPeriod = TimeSpan.FromSeconds(1);
        protected readonly ActorMonitor Monitor;

        private int _terminateWaitCount = 3;

        protected readonly BehaviorStack Behavior;

        public EventSourcedActor(IConstructAggregates aggregateConstructor, ISnapshotsPersistencePolicy policy)
        {
            _snapshotsPolicy = policy;

            PersistenceId = Self.Path.Name;
            Id = AggregateActorName.Parse<T>(Self.Path.Name).Id;
            State = (T)aggregateConstructor.Build(typeof(T), Id, null);

            Monitor = new ActorMonitor(Context, typeof(T).Name);
            Behavior = new BehaviorStack(BecomeStacked, UnbecomeStacked);

            DefaultBehavior();

            Recover<DomainEvent>(e => { ((IAggregate)State).ApplyEvent(e); });

            Recover<SnapshotOffer>(offer =>
            {
                _snapshotsPolicy.MarkSnapshotApplied(offer.Metadata.SequenceNr);
                State = (T)aggregateConstructor.Build(typeof(T), Id, (IMemento)offer.Snapshot);
                RecoverFromSnapshot();
            });

            Recover<RecoveryCompleted>(message =>
            {
                Log.Debug("Recovery for actor {Id} is completed", PersistenceId);
                NotifyPersistenceWatchers(message);
            });
        }

        protected virtual void RecoverFromSnapshot()
        {
        }

        protected void DefaultBehavior()
        {
            Command<GracefullShutdownRequest>(req =>
                                              {
                                                  Log.Debug("{Actor} received shutdown request", PersistenceId);
                                                  Monitor.IncrementMessagesReceived();
                                                  Behavior.Become(TerminatingBehavior,nameof(TerminatingBehavior));
                                              });

            Command<CheckHealth>(s => Sender.Tell(new HealthStatus(s.Payload)));

            Command<NotifyOnPersistenceEvents>(c => SubscribePersistentObserver(c));

            Command<SaveSnapshotSuccess>(s =>
                                         {
                                             NotifyPersistenceWatchers(s);
                                             _snapshotsPolicy.MarkSnapshotSaved(s.Metadata.SequenceNr,
                                                                                BusinessDateTime.UtcNow);
                                         });
        }
        protected void StashMessage(object message)
        {
            Log.Debug("Aggregate {id} stashing message {message}� current behavior is {behavior}", PersistenceId, message, Behavior.Current);
            
            Stash.Stash();
        }

        private void SubscribePersistentObserver(NotifyOnPersistenceEvents c)
        {
            var waiter = c.Waiter ?? Sender;
            if (IsRecoveryFinished)
                waiter.Tell(RecoveryCompleted.Instance);

            _persistenceWatchers.Add(waiter);
            Sender.Tell(SubscribeAck.Instance);
        }

        protected Guid Id { get; }
        public override string PersistenceId { get; }
        public T State { get; protected set; }

        protected bool TrySaveSnapshot(IAggregate aggregate)
        {
            return _snapshotsPolicy.TrySave(() => SaveSnapshot(aggregate.GetSnapshot()),
                                            SnapshotSequenceNr,
                                            BusinessDateTime.UtcNow);
        }

        protected void NotifyPersistenceWatchers(object msg)
        {
            foreach (var watcher in _persistenceWatchers)
                watcher.Tell(new Persisted(msg));
        }

     

        protected virtual void TerminatingBehavior()
        {
            //for case when we in process of saving snapshot or events
            Command<DeleteSnapshotsSuccess>(s => StopNow(s));
            Command<DeleteSnapshotsFailure>(s => StopNow(s));
            Command<CancelShutdownRequest>(s =>
                                           {
                                               Behavior.Unbecome();
                                               Stash.UnstashAll();
                                               Log.Info("Aborting shutdown, will resume activity");
                                               Sender.Tell(ShutdownCanceled.Instance);
                                           });

            Command<GracefullShutdownRequest>(s =>
                                              {
                                                  if (_snapshotsPolicy.TryDelete(DeleteSnapshots))
                                                  {
                                                      Log.Debug("started snapshots delete");
                                                      return;
                                                  }

                                                  Log.Debug("Unsaved snapshots found");
                                                  if (--_terminateWaitCount < 0)
                                                      return;

                                                  Log.Debug("Will retry to delete snapshots in {time}, times left:{times}",
                                                            _terminateWaitPeriod,
                                                            _terminateWaitCount);

                                                  Context.System.Scheduler.ScheduleTellOnce(_terminateWaitPeriod,
                                                                                            Self,
                                                                                            GracefullShutdownRequest.Instance,
                                                                                            Self);
                                              });
            //start terminateion process
            Self.Tell(GracefullShutdownRequest.Instance);
        }

        protected override void Unhandled(object message)
        {
            Log.Warning("Actor {id} skipping message {message} because it was unhandled. \r\n {@behavior}.", 
                PersistenceId, message, Behavior);
            base.Unhandled(message);
        }

        private void StopNow(object s)
        {
            NotifyPersistenceWatchers(s);
            Context.Stop(Self);
        }

        protected override void OnPersistFailure(Exception cause, object @event, long sequenceNr)
        {
            Log.Error("Additional persistence diagnostics on fauilure {error} {actor} {event}", cause, Self.Path.Name, @event);
            base.OnPersistFailure(cause, @event, sequenceNr);
        }

        protected override void OnPersistRejected(Exception cause, object @event, long sequenceNr)
        {
            Log.Error("Additional persistence diagnostics on rejected {error} {actor} {event}", cause, Self.Path.Name, @event);
            base.OnPersistRejected(cause, @event, sequenceNr);
        }

        protected override void PreStart()
        {
            Monitor.IncrementActorStarted();
        }

        protected override void PostStop()
        {
            Monitor.IncrementActorStopped();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            Monitor.IncrementActorRestarted();
        }
    }
}