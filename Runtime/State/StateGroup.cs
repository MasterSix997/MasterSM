using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MasterSM
{
    [Serializable]
    public class StateGroup<TStateId, TStateMachine> where TStateMachine : IStateMachine
    {
        public int basePriority;
        [SerializeReference] private List<IState<TStateId, TStateMachine>> states = new();
        [SerializeReference] private List<TStateId> stateIds = new();
        
        public IReadOnlyList<IState<TStateId, TStateMachine>> States => states;
    
        public StateGroup(params (TStateId id, IState<TStateId, TStateMachine> state)[] states)
        {
            foreach (var state in states)
                AddState(state.id, state.state);
        }
        
        public StateGroup(params IState<TStateId, TStateMachine>[] states)
        {
            if (typeof(TStateId) is not Type)
                return;
            
            foreach (var state in states)
                AddState((TStateId)(object)state.GetType(), state);
        }

        public void AddState(TStateId id, IState<TStateId, TStateMachine> state)
        {
            if (!states.Contains(state) && !stateIds.Contains(id))
            {
                states.Add(state);
                stateIds.Add(id);
            }
        }
    
        public void RemoveState(IState<TStateId, TStateMachine> state)
        {
            states.Remove(state);
        }

        public IEnumerable<(TStateId id, IState<TStateId, TStateMachine> state)> GetStatesByLowestPriority()
        {
            return GetStates();
            // for (var i = states.Count - 1; i >= 0; i--)
            // {
            //     yield return (stateIds[i], states[i]);
            // }
        }

        public IEnumerable<(TStateId id, IState<TStateId, TStateMachine> state)> GetStates()
        {
            return states.Select((t, i) => (stateIds[i], t));
        }
    }
}