using System;
using System.Threading.Tasks;
using GridDomain.CQRS;
using AggregateChangedEventNotification = GridDomain.Tests.Unit.SampleDomain.ProjectionBuilders.AggregateChangedEventNotification;

namespace GridDomain.Tests.XUnit.CommandsExecution
{
  
    public class When_execute_command_Then_produced_event_are_available_for_projection_builders: SampleDomainCommandExecutionTests
    {

       [Fact]
        public async Task Async_method_should_produce_messages_for_projection_builders()
        {
            var cmd = new AsyncMethodCommand(42, Guid.NewGuid());

            await Node.Prepare(cmd)
                          .Expect<AggregateChangedEventNotification>()
                          .Execute();
        }

       [Fact]
        public async Task Sync_method_should_produce_messages_for_projection_builders()
        {
            var cmd = new LongOperationCommand(42, Guid.NewGuid());

            await Node.Prepare(cmd)
                          .Expect<AggregateChangedEventNotification>()
                          .Execute();
        }
    }
}