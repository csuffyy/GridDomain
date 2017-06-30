using System;
using GridDomain.CQRS.Messaging;
using GridDomain.CQRS.Messaging.MessageRouting;
using GridDomain.EventSourcing;
using GridDomain.Tests.Unit.EventsUpgrade.Domain.Commands;

namespace GridDomain.Tests.Unit.EventsUpgrade.Domain
{
    public class BalanceAggregatesCommandHandler : AggregateCommandsHandler<BalanceAggregate>,
                                                   IAggregateCommandsHandlerDescriptor

    {
        public BalanceAggregatesCommandHandler()
        {
            Map<ChangeBalanceCommand>((c, a) => a.ChangeState(c.Parameter));

            Map<CreateBalanceCommand>(c => new BalanceAggregate(c.AggregateId, c.Parameter));

            Map<ChangeBalanceInFuture>((c, a) => a.ChangeStateInFuture(c.RaiseTime, c.Parameter, c.UseLegacyEvent));
            this.MapFutureEvents();
        }
    }
}