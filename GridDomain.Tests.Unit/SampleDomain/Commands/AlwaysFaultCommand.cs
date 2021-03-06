using System;
using GridDomain.CQRS;

namespace GridDomain.Tests.Unit.SampleDomain.Commands
{
    public class AlwaysFaultCommand : Command
    {
        public AlwaysFaultCommand(Guid aggregateId)
        {
            AggregateId = aggregateId;
        }

        public Guid AggregateId { get; }
    }
}