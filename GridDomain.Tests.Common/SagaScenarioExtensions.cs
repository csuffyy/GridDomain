using System.Threading.Tasks;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using KellermanSoftware.CompareNetObjects;

namespace GridDomain.Tests.Common
{
    public static class SagaScenarioExtensions
    {
        public static async Task<SagaScenario<TSaga, TData, TFactory>> CheckProducedCommands<TSaga, TData, TFactory>(this Task<SagaScenario<TSaga, TData, TFactory>> scenarioInProgress) where TSaga : Process<TData> where TData : class, ISagaState where TFactory : class, ISagaCreator<TData>
        {
            var scnearion = await scenarioInProgress;
            scnearion.CheckProducedCommands();
            return scnearion;
        }

        public static async Task<SagaScenario<TSaga, TData, TFactory>> CheckOnlyStateNameChanged<TSaga, TData, TFactory>(this Task<SagaScenario<TSaga, TData, TFactory>> scenarioInProgress, string stateName) where TSaga : Process<TData> where TData : class, ISagaState where TFactory : class, ISagaCreator<TData>
        {
            var scnearion = await scenarioInProgress;
            scnearion.CheckOnlyStateNameChanged(stateName);
            return scnearion;
        }

        public static async Task<SagaScenario<TSaga, TData, TFactory>> CheckProducedState<TSaga, TData, TFactory>(
            this Task<SagaScenario<TSaga, TData, TFactory>> scenarioInProgress, TData expectedState, CompareLogic logic = null) where TSaga : Process<TData> where TData : class, ISagaState where TFactory : class, ISagaCreator<TData>
        {
            var scnearion = await scenarioInProgress;
            scnearion.CheckProducedState(expectedState, logic);
            return scnearion;
        }
    }
}