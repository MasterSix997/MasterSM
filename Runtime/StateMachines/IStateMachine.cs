using System;
using System.Collections.Generic;
using MasterSM.PriorityManagement;

namespace MasterSM
{
    public interface IStateMachine
    {
        
    }

    public interface IStateMachineImpl<TStateId, TStateMachine> : IStateMachine
        where TStateMachine : IStateMachine
    {
        public TStateId CurrentId { get; }
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, StatePriority<TStateId> statePriority);
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int priority = 0);
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int group, int priority);
        public void AddState(in IState<TStateId, TStateMachine> state, StatePriority<TStateId> priority);
        public void AddState(StateGroup<TStateId, TStateMachine> group);
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