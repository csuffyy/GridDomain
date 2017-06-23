﻿using System;
using System.Collections.Generic;
using System.Linq;
using GridDomain.EventSourcing;
using Ploeh.AutoFixture;

namespace GridDomain.Tests.Common
{
    public class AggregateTest<TAggregate> where TAggregate : Aggregate
    {
        protected readonly AggregateFactory aggregateFactory = new AggregateFactory();
        protected TAggregate Aggregate;

        protected static Fixture Data { get; } = new Fixture();
        protected DomainEvent[] GivenEvents { get; set; }

        protected virtual IEnumerable<DomainEvent> Given()
        {
            yield break;
        }

        protected virtual void Init()
        {
            Aggregate = (TAggregate) aggregateFactory.Build(typeof(TAggregate), Guid.NewGuid(), null);
            GivenEvents = Given().ToArray();
            Aggregate.ApplyEvents(GivenEvents);
        }
    }
}