using System;
using GridDomain.EventSourcing;

namespace GridDomain.Tests.Unit.EventsUpgrade.Domain.Events
{
    public class BalanceChangedEvent_V0 : DomainEvent
    {
        public BalanceChangedEvent_V0(decimal amountChange,
                                      Guid sourceId,
                                      decimal koefficient = 1,
                                      DateTime? createdTime = null,
                                      Guid sagaId = new Guid()) : base(sourceId, sagaId: sagaId, createdTime: createdTime)
        {
            Koefficient = koefficient;
            AmountChange = amountChange;
            AmplifiedAmountChange = Koefficient * AmountChange;
        }

        public decimal AmountChange { get; }
        public decimal Koefficient { get; }
        public decimal AmplifiedAmountChange { get; }
    }
}