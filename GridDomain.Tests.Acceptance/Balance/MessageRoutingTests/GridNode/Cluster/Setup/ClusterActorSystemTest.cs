namespace GridDomain.Tests.Acceptance.Balance.MessageRoutingTests.GridNode.Cluster.Setup
{
    abstract class ClusterActorSystemTest : ActorSystemTest<ClusterMessage, ClusterActorSystemInfrastructure>
    {
        protected override ClusterActorSystemInfrastructure CreateInfrastructure()
        {
            return new ClusterActorSystemInfrastructure(new AutoTestAkkaConfiguration());
        }

        protected override IGivenCommands<ClusterMessage> GivenCommands()
        {
            return new GivenClusterTestMessages();
        }
    }
}