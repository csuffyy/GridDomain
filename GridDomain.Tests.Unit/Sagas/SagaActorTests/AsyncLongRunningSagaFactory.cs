using System;
using GridDomain.EventSourcing.Sagas;
using GridDomain.EventSourcing.Sagas.InstanceSagas;
using GridDomain.Tests.Unit.BalloonDomain.Events;
using Serilog;

namespace GridDomain.Tests.Unit.Sagas.SagaActorTests
{
    public class AsyncLongRunningSagaFactory : ISagaCreator<TestState>,
                                               ISagaCreator<TestState, BalloonCreated>
    {
        private readonly ILogger _log;

        public AsyncLongRunningSagaFactory(ILogger log)
        {
            _log = log;
        }

        public ISaga<TestState> Create(TestState message)
        {
            return new Saga<TestState>(new AsyncLongRunningProcess(), message, _log);
        }

        public ISaga<TestState> CreateNew(BalloonCreated message, Guid? id = null)
        {
            return Create(new TestState(id ?? message.SagaId, nameof(AsyncLongRunningProcess.Initial)));
        }
    }
}