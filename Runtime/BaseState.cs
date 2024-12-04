using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace AdvancedSM
{
    public interface IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public TStateId Id { get; set; }
        public TStateMachine Machine { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        [CanBeNull] public List<StateExtension<TStateId, TStateMachine>> Extensions { get; set; }
        
        public bool CanEnter();

        public bool CanExit()
        {
            return true;
        }
        
        public void OnCreated()
        {
            
        }
        
        public void OnEnter()
        {
            
        }
        
        public void OnExit()
        {
            
        }

        public void OnUpdate()
        {
            
        }
        
        public void OnFixedUpdate()
        {
            
        }
    }

    public abstract class BaseState<TStateId, TStateMachine> : IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public TStateId Id { get; set; }
        public TStateMachine Machine { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public List<StateExtension<TStateId, TStateMachine>> Extensions { get; set; }

        public abstract bool CanEnter();

        public virtual bool CanExit()
        {
            return true;
        }
        
        public virtual void OnCreated()
        {
            
        }
        
        public virtual void OnEnter()
        {
            
        }
        
        public virtual void OnExit()
        {
            
        }

        public virtual void OnUpdate()
        {
            
        }
        
        public virtual void OnFixedUpdate()
        {
            
        }
    }

    public class InlineState<TStateId, TStateMachine> : IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public TStateId Id { get; set; }
        public TStateMachine Machine { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }

        public List<StateExtension<TStateId, TStateMachine>> Extensions { get; set; } = null;
        
        public Func<bool> canEnter;
        [CanBeNull] public Func<bool> canExit;
        [CanBeNull] public Action onCreated;
        [CanBeNull] public Action onEnter;
        [CanBeNull] public Action onExit;
        [CanBeNull] public Action onUpdate;
        [CanBeNull] public Action onFixedUpdate;

        public InlineState(Func<bool> canEnter, Func<bool> canExit = null, Action onCreated = null, Action onEnter = null, Action onExit = null, Action onUpdate = null, Action onFixedUpdate = null)
        {
            this.canEnter = canEnter;
            this.canExit = canExit;
            this.onCreated = onCreated;
            this.onEnter = onEnter;
            this.onExit = onExit;
            this.onUpdate = onUpdate;
            this.onFixedUpdate = onFixedUpdate;
        }
        
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public bool CanEnter() => canEnter();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public bool CanExit() => canExit == null || canExit();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnCreated() => onCreated?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnEnter() => onEnter?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnExit() => onExit?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnUpdate() => onUpdate?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnFixedUpdate() => onFixedUpdate?.Invoke();
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class StateTabAttribute : Attribute
    {
        public string Name { get; }

        public StateTabAttribute(string name)
        {
            Name = name;
        }
    }
    // public abstract class BaseState<TStateId, TParentState, TStateMachine> : IState<TStateId, TStateMachine>
    //     where TParentState : BaseState<TStateId, TParentState, TStateMachine>
    //     where TStateMachine : IStateMachine<TStateId>
    // {
    //     public TStateMachine Machine { get; set; }
    //     protected TParentState ParentState { get; set; }
    //     public TStateId Id { get; set; }
    //     public BaseState<TStateId, TParentState, TStateMachine> CurrentState { get; private set; }
    //     public BaseState<TStateId, TParentState, TStateMachine> PreviousState { get; private set; }
    //     // private List<Transition> _transitions = new();
    //     public int Priority { get; set; }
    //
    //     private readonly Dictionary<TStateId, BaseState<TStateId, TParentState, TStateMachine>> _states = new();
    //     private readonly List<TStateId> _statesOrder = new();
    //     private int _currentIndex;
    //
    //     public void AddState(TStateId id, BaseState<TStateId, TParentState, TStateMachine> state, int priority = 0)
    //     {
    //         state.Id = id;
    //         state.Priority = priority;
    //         state.Machine = Machine;
    //         state.ParentState = (TParentState)this;
    //         if (!_states.TryAdd(id, state)) 
    //             return;
    //         
    //         var index = 0;
    //         for (var i = 0; i < _statesOrder.Count; i++)
    //         {
    //             if (_states[_statesOrder[i]].Priority > priority)
    //             {
    //                 index = i;
    //                 break;
    //             }
    //             index = i + 1;
    //         }
    //         _statesOrder.Insert(index, id);
    //     }
    //
    //     protected void SetInitialState(BaseState<TStateId, TParentState, TStateMachine> state)
    //     {
    //         CurrentState = state;
    //     }
    //
    //     public void ChangeState(TStateId newState)
    //     {
    //         SetNewState(newState);
    //         ExitPreviousState();
    //         EnterNewState();
    //     }
    //     
    //     private void SetNewState(TStateId newState)
    //     {
    //         if(newState.Equals(CurrentState.Id) || !_states.TryGetValue(newState, out var state))
    //            return;
    //            
    //         PreviousState = CurrentState;
    //         CurrentState = state;
    //     }
    //     
    //     private void EnterNewState()
    //     {
    //         CurrentState?.OnEnter();
    //     }
    //     
    //     private void ExitPreviousState()
    //     {
    //         PreviousState?.OnExit();
    //     }
    //     
    //     public void RevertToPreviousState()
    //     {
    //         if (PreviousState != null)
    //         {          
    //             ChangeState(PreviousState.Id);
    //         }
    //     }
    //     
    //     public bool TryGetTransition(out (TStateId, int) state, int maxIndex)
    //     {
    //         state = default;
    //         
    //         for (int i = 0; i < maxIndex; i++)
    //         {
    //             if (_states[_statesOrder[i]].CanEnter())
    //             {
    //                 state = (_statesOrder[i], i);
    //                 
    //                 return true;
    //             }
    //         }
    //
    //         return false;
    //     }
    //     
    //     //Virtual Methods
    //     
    //     public abstract bool CanEnter();
    //
    //     public virtual bool CanExit()
    //     {
    //         return true;
    //     }
    //     
    //     public virtual void OnCreated()
    //     {
    //         if (_states.Count == 0)
    //             return;
    //         
    //         foreach (var state in _states)
    //         {
    //             state.Value.OnCreated();
    //         }
    //
    //         TryGetTransition(out var newState, _states.Count);
    //         _currentIndex = newState.Item2;
    //         CurrentState = _states[_statesOrder[_currentIndex]];
    //     }
    //     
    //     public virtual void OnEnter()
    //     {
    //         CurrentState?.OnEnter();
    //     }
    //
    //     public virtual void OnUpdate()
    //     {
    //         if (CurrentState == null)
    //             return;
    //         
    //         if (CurrentState.CanExit())
    //         {
    //             var maxIndex = CurrentState.CanEnter() ? _currentIndex : _statesOrder.Count;
    //             if (TryGetTransition(out var state, maxIndex))
    //             {
    //                 _currentIndex = state.Item2;
    //                 ChangeState(state.Item1);
    //             }
    //         }
    //         
    //         CurrentState.OnUpdate();
    //     }
    //     
    //     public virtual void OnFixedUpdate()
    //     {
    //         CurrentState?.OnFixedUpdate();
    //     }
    //
    //     public virtual void OnExit()
    //     {
    //         CurrentState?.OnExit();
    //     }
    //
    //     
    // }
}
