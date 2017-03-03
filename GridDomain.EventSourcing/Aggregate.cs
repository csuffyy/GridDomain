﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonDomain;
using CommonDomain.Core;
using GridDomain.EventSourcing.FutureEvents;

namespace GridDomain.EventSourcing
{

    public class Aggregate : AggregateBase,
                             IMemento
    {
        private static readonly AggregateFactory Factory = new AggregateFactory();

        private static readonly Action<DomainEvent> EmptyApply = e => { };
        private static readonly Action EmptyContinue = () => { };

        internal Action<Task<DomainEvent[]>, Action<DomainEvent>, Action, Aggregate> PersistEvents;
        private Func<DomainEvent, DomainEvent> _eventsEnricher = e => e;

        private readonly HashSet<DomainEvent> _eventToPersist = new HashSet<DomainEvent>();
        public IReadOnlyCollection<DomainEvent> EventToPersist => _eventToPersist;
        public void MarkPersisted(DomainEvent e)
        {
            _eventToPersist.Remove(e);
        }
        // Only for simple implementation 
        Guid IMemento.Id
        {
            get { return Id; }
            set { Id = value; }
        }

        int IMemento.Version
        {
            get { return Version; }
            set { Version = value; }
        }

        protected void Emit(DomainEvent e, Action<DomainEvent> onApply = null)
        {
            Emit(onApply, EmptyContinue, e);
        }
        protected void Emit(params DomainEvent[] e)
        {
            Emit(EmptyApply, EmptyContinue, e);

        }
        protected void Emit(Action afterAll, params DomainEvent[] e)
        {
            Emit(EmptyApply, afterAll, e);

        }

        protected void Emit(Action<DomainEvent> onApply, Action afterAll, params DomainEvent[] events)
        {
            Emit(Task.FromResult(events), onApply, afterAll);
        }

        protected void Emit<T>(Task<T> evtTask, Action<DomainEvent> onApply = null) where T : DomainEvent
        {
            Emit(evtTask.ContinueWith(t => new DomainEvent[] {t.Result}), onApply, EmptyContinue);
        }
        protected void Emit(Task<DomainEvent[]> evtTask, Action continuation = null, Action<DomainEvent> onApply = null)
        {
            Emit(evtTask, onApply, continuation);
        }

        protected void Emit(Task<DomainEvent[]> evtTask, Action<DomainEvent> onApply = null, Action continuation = null)
        {
            var task = evtTask.ContinueWith(t =>
                                            {
                                                var enrichedEvents = t.Result.Select(_eventsEnricher).ToArray();

                                                foreach (var e in enrichedEvents)
                                                    _eventToPersist.Add(e);

                                                return enrichedEvents;
                                            });

            PersistEvents(task, e => RaiseEvent(e,onApply), continuation ?? EmptyContinue, this);
        }

        private void RaiseEvent(DomainEvent e, Action<DomainEvent> onApply = null)
        {
            base.RaiseEvent(e);
            onApply?.Invoke(e);
        }

        public void RegisterEnricher(Func<DomainEvent, DomainEvent> enricher)
        {
            _eventsEnricher = enricher;
        }
        public void RegisterPersistence(Action<Task<DomainEvent[]>, Action<DomainEvent>, Action, Aggregate> persistDelegate)
        {
            PersistEvents = persistDelegate;
        }

        public static T Empty<T>(Guid id) where T : IAggregate
        {
            return Factory.Build<T>(id);
        }

        protected void Apply<T>(Action<T> action) where T : DomainEvent
        {
            Register(action);
        }

        protected override IMemento GetSnapshot()
        {
            return this;
        }

        #region AsyncMethods

        public IDictionary<Guid, FutureEventScheduledEvent> FutureEvents { get; } =
            new Dictionary<Guid, FutureEventScheduledEvent>();

        #endregion

        #region FutureEvents

        protected Aggregate(Guid id)
        {
            Id = id;
            Register<FutureEventScheduledEvent>(Apply);
            Register<FutureEventOccuredEvent>(Apply);
            Register<FutureEventCanceledEvent>(Apply);
        }

        public void RaiseScheduledEvent(Guid futureEventId, Guid futureEventOccuredEventId)
        {
            FutureEventScheduledEvent e;
            if (!FutureEvents.TryGetValue(futureEventId, out e))
                throw new ScheduledEventNotFoundException(futureEventId);

            Emit(e.Event);
            Emit(new FutureEventOccuredEvent(futureEventOccuredEventId, futureEventId, Id));
        }

        protected void Emit(DomainEvent @event, DateTime raiseTime, Guid? futureEventId = null)
        {
            Emit(new FutureEventScheduledEvent(futureEventId ?? Guid.NewGuid(), Id, raiseTime, @event));
        }

        protected void CancelScheduledEvents<TEvent>(Predicate<TEvent> criteia = null) where TEvent : DomainEvent
        {
            var eventsToCancel = FutureEvents.Values.Where(fe => fe.Event is TEvent);
            if (criteia != null)
                eventsToCancel = eventsToCancel.Where(e => criteia((TEvent) e.Event));

            var domainEvents = eventsToCancel.Select(e => new FutureEventCanceledEvent(e.Id, Id))
                                             .Cast<DomainEvent>()
                                             .ToArray();
            Emit(domainEvents);
        }

        private void Apply(FutureEventScheduledEvent e)
        {
            FutureEvents[e.Id] = e;
        }

        private void Apply(FutureEventOccuredEvent e)
        {
            DeleteFutureEvent(e.FutureEventId);
        }

        private void Apply(FutureEventCanceledEvent e)
        {
            DeleteFutureEvent(e.FutureEventId);
        }

        private void DeleteFutureEvent(Guid futureEventId)
        {
            FutureEventScheduledEvent evt;
            if (!FutureEvents.TryGetValue(futureEventId, out evt))
                return;
            FutureEvents.Remove(futureEventId);
        }

        #endregion
    }
}