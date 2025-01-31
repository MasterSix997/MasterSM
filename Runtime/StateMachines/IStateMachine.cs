namespace MasterSM
{
    public interface IStateMachine
    {
        
    }

    public interface IStateMachineImpl<TStateId, TStateMachine> : IStateMachine
        where TStateMachine : IStateMachine
    {
        public void AddState(TStateId id, IState<TStateId, TStateMachine> state, int priority = 0);
        public void RemoveState(TStateId id);
        public void ChangeState(TStateId newState);
        public IState<TStateId, TStateMachine> GetState(TStateId id);
        public bool HasState(TStateId stateId);
        public void RevertToPreviousState();
    }

    public interface IMachineWithLayers<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public BaseMachine<TStateId, TStateMachine> AddLayer(object layerId);
        public void RemoveLayer(object layerId);
        public BaseMachine<TStateId, TStateMachine> GetLayer(object layerId);
        public bool HasLayer(object layerId);
    }
}