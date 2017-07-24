namespace GridDomain.ProcessManagers.Creation
{
    public interface IProcessManagerCreatorCatalog<TState> : IProcessManagerCreator<TState>,
                                                   IProcessManagerCreator<TState, object>
        where TState : IProcessState
    {
        bool CanCreateFrom(object message);
    }
}