using System;
using System.Threading.Tasks;
using Automatonymous;

namespace GridDomain.EventSourcing.Sagas.InstanceSagas
{
    public class SagaStateAggregate<TState> : Aggregate where TState : ISagaState
    {
        public SagaStateAggregate(TState state): this(state.Id)
        {
            Emit(new SagaCreated<TState>(state, state.Id));
        }
        
        private SagaStateAggregate(Guid id) : base(id)
        {
        }

        public TState State { get; private set; }

        public void ReceiveMessage(TState sagaData, object message)
        {
            Emit(new SagaReceivedMessage<TState>(Id, sagaData, message));
        }

        public void Apply(SagaCreated<TState> e)
        {
            State = e.State;
            Id = e.SourceId;
        }

        public void Apply(SagaReceivedMessage<TState> e)
        {
            State = e.State;
        }
    }
}