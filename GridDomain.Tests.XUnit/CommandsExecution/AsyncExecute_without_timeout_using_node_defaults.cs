using System;
using System.Threading.Tasks;
using GridDomain.CQRS;
using GridDomain.Tests.Common;
using GridDomain.Tests.XUnit.BalloonDomain;
using GridDomain.Tests.XUnit.BalloonDomain.Commands;
using GridDomain.Tests.XUnit.BalloonDomain.Events;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.XUnit.CommandsExecution
{
    public class AsyncExecute_without_timeout_using_node_defaults : NodeTestKit
    {
        public AsyncExecute_without_timeout_using_node_defaults(ITestOutputHelper output) : base(output, CreateFixture()) {}

        private static NodeTestFixture CreateFixture()
        {
            return new NodeTestFixture(new BalloonContainerConfiguration(),
                                       new BalloonRouteMap(),
                                       TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public async Task SyncExecute_throw_exception_according_to_node_default_timeout()
        {
            await Node.Prepare(new PlanTitleWriteCommand(1000, Guid.NewGuid()))
                      .Expect<BalloonTitleChanged>()
                      .Execute()
                      .ShouldThrow<TimeoutException>();
        }
    }
}