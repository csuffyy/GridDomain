using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Acceptance.EventsUpgrade.SampleDomain
{
    class EventA : DomainEvent
    {
        public IOrder Order { get; }

        public EventA(Guid sourceId, IOrder order) : base(sourceId)
        {
            Order = order;
        }
    }
}