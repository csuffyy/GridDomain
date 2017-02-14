using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GridDomain.Common;
using GridDomain.CQRS;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Tests.Acceptance.XUnit.EventsUpgrade;
using GridDomain.Tests.Framework;
using GridDomain.Tests.XUnit;
using GridDomain.Tests.XUnit.SampleDomain;
using GridDomain.Tests.XUnit.SampleDomain.Commands;
using GridDomain.Tests.XUnit.SampleDomain.Events;
using GridDomain.Tools.Repositories.AggregateRepositories;
using Xunit;
using Xunit.Abstractions;

namespace GridDomain.Tests.Acceptance.XUnit.Snapshots
{
    public class Aggregate_Should_save_snapshots_on_message_process_if_activity_is_low : NodeTestKit
    {
        public Aggregate_Should_save_snapshots_on_message_process_if_activity_is_low(ITestOutputHelper output)
            : base(output, new SampleDomainFixture().InitSampleAggregateEachMessageSnapshots()) {}

        [Fact]
        public async Task Given_timeout_only_default_policy()
        {
            var aggregateId = Guid.NewGuid();
            var initialParameter = 1;
            var cmd = new CreateSampleAggregateCommand(initialParameter, aggregateId);
            await Node.Prepare(cmd)
                      .Expect<SampleAggregateCreatedEvent>()
                      .Execute();

            //checking "time-out" rule for policy, snapshots should be saved once on second command
            await Task.Delay(1000);
            var changedParameter = 2;
            var changeSampleAggregateCommand = new ChangeSampleAggregateCommand(changedParameter, aggregateId);

            await Node.Prepare(changeSampleAggregateCommand)
                      .Expect<SampleAggregateChangedEvent>()
                      .Execute();

            await Task.Delay(TimeSpan.FromSeconds(1));

            var snapshots =
                await
                    new AggregateSnapshotRepository(Fixture.AkkaConfig.Persistence.JournalConnectionString,
                        Node.AggregateFromSnapshotsFactory).Load<SampleAggregate>(aggregateId);
            //Snapshots_should_be_saved_one_time()
            Assert.Equal(1, snapshots.Length);
            //Restored_aggregates_should_have_same_ids()
            Assert.True(snapshots.All(s => s.Aggregate.Id == aggregateId));
            //First_snapshot_should_have_parameters_from_second_command()
            Assert.Equal(changedParameter.ToString(),
                snapshots.First()
                          .Aggregate.Value);
            //All_snapshots_should_not_have_uncommited_events()
            Assert.Empty(snapshots.SelectMany(s => s.Aggregate.GetEvents()));
        }
    }
}