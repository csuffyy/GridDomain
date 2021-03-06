using System;
using GridDomain.EventSourcing.Sagas.StateSagas;

namespace GridDomain.Tests.Unit.Sagas.SagaRecycling.Saga
{
    public class State : SagaStateAggregate<States, Triggers>
    {
        private State(Guid id) : base(id)
        {
        }

        public State(Guid id, States state) : base(id, state)
        {
        }

        public void Finish()
        {
            RaiseEvent(new FinishedEvent(Id));
        }

        public void Apply(FinishedEvent evt)
        {

        }
    }
}