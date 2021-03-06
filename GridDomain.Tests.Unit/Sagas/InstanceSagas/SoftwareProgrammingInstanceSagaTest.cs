using GridDomain.Common;
using GridDomain.CQRS.Messaging;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Node.Configuration.Composition;
using GridDomain.Tests.Unit.CommandsExecution;
using GridDomain.Tests.Unit.Sagas.SoftwareProgrammingDomain.Events;

namespace GridDomain.Tests.Unit.Sagas.InstanceSagas
{
    public class SoftwareProgrammingInstanceSagaTest : SampleDomainCommandExecutionTests
    {
        protected override IMessageRouteMap CreateMap()
        {
            return new SoftwareProgrammingSagaRoutes();
        }

        protected override IContainerConfiguration CreateConfiguration()
        {
            var baseConf = base.CreateConfiguration();

            return new CustomContainerConfiguration(
                c => c.RegisterSaga<SoftwareProgrammingSaga,
                                    SoftwareProgrammingSagaData,
                                    SoftwareProgrammingSagaFactory,
                                    SleptWellEvent,
                                    GotTiredEvent>(SoftwareProgrammingSaga.Descriptor),

                c => c.Register(baseConf),
                c => c.RegisterAggregate<SagaDataAggregate<SoftwareProgrammingSagaData>,
                    SagaDataAggregateCommandsHandlerDummy<SoftwareProgrammingSagaData>>()
                );
        }

        protected SoftwareProgrammingInstanceSagaTest(bool inMemory = true) : base(inMemory)
        {
        }
    }
}