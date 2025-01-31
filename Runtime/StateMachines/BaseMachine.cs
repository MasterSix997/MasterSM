using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace MasterSM
{
    /// <summary>
    /// Base class for a state machine.
    /// </summary>
    /// <typeparam name="TStateId">The type of the state identifier.</typeparam>
    /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
    public class BaseMachine<TStateId, TStateMachine> : IStateMachineImpl<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public TStateMachine Machine { get; internal set; }
        
        [CanBeNull] public IState<TStateId, TStateMachine> CurrentState { get; private set; }
        [CanBeNull] public IState<TStateId, TStateMachine> PreviousState { get; private set; }

        public readonly Dictionary<TStateId, IState<TStateId, TStateMachine>> States = new();
        public readonly List<TStateId> StatesOrder = new();
        internal int CurrentIndex = -1;
        internal int PreviousIndex = -1;
        internal bool Created;

        /// <summary>
        /// Adds a state to the state machine.
        /// </summary>
        /// <param name="id">The state identifier.</param>
        /// <param name="state">The state to add.</param>
        /// <param name="priority">The priority of the state.</param>
        public void AddState(TStateId id, IState<TStateId, TStateMachine> state, int priority = 0)
        {
            if (!state.Initialized)
            {
                state.Initialize(id, Machine, priority);
                state.Extensions ??= GetExtensionsInState(state);
                if (state.Extensions != null)
                {
                    foreach (var extension in state.Extensions)
                        extension.Machine = Machine;
                }

                if (Created)
                {
                    ExecuteEventOnState(StateEvent.OnCreated, false, state);
                    // state.OnCreated();
                    // if (state.Extensions != null)
                    // {
                    //     foreach (var extension in state.Extensions)
                    //         extension.OnCreated(state);
                    // }
                }
            }
            
            if (!States.TryAdd(state.Id, state))
                return;
            
            // Add state to order list
            var index = 0;
            for (var i = 0; i < StatesOrder.Count; i++)
            {
                if (state.Priority > States[StatesOrder[i]].Priority)
                {
                    index = i;
                    break;
                }
                // todo: improve priority logic. 
                // - If two states have the same priority?
                //    - If a capability adds several states that need to have the states in ascending order?
                //    - Add one state in front of the other easily
                //    - Deals with the removal of states correctly
                
                // else if (state.Priority == States[StatesOrder[i]].Priority)
                // {
                //     throw new ArgumentException("Cannot add state with the same priority as an existing state.");
                // }
                
                index = i + 1;
            }
            StatesOrder.Insert(index, state.Id);

            if (index <= CurrentIndex)
            {
                CurrentIndex++;
            }
        }
        
        public void AddState(StateGroup<TStateId, TStateMachine> group)
        {
            var priority = group.basePriority;
            foreach (var state in group.GetStatesByLowestPriority())
            {
                AddState(state.id, state.state, priority++);
            }
        }
        
        /// <summary>
        /// Removes a state from the state machine.
        /// </summary>
        /// <param name="id">The state identifier.</param>
        public void RemoveState(TStateId id)
        {
            if (!States.TryGetValue(id, out var stateToRemove))
                return;
            
            if (stateToRemove == CurrentState && CurrentState != null)
            {
                CurrentState.OnExit();
                CurrentState.IsActive = false;
                CurrentState = null;
            }

            States.Remove(id);
            StatesOrder.Remove(id);
            
            if (stateToRemove == CurrentState)
            {
                if (TryGetTransition(out var newState, StatesOrder.Count))
                {
                    ChangeState(newState.id, newState.index);
                }
                else
                {
                    CurrentState = null;
                }
            }

            if (stateToRemove == PreviousState)
            {
                PreviousState = null;
            }
        }

        /// <summary>
        /// Gets a state by its identifier.
        /// </summary>
        /// <param name="id">The state identifier.</param>
        /// <returns>The state</returns>
        /// <exception cref="ArgumentException">Throw exception if state with specified identifier is not found.</exception>
        public IState<TStateId, TStateMachine> GetState(TStateId id)
        {
            if (!States.TryGetValue(id, out var state))
                throw new ArgumentException($"State with id '{id}' not found.");
            
            return state;
        }
        
        /// <summary>
        /// Checks if a state with the specified identifier exists in the state machine.
        /// </summary>
        /// <param name="stateId">The state identifier.</param>
        /// <returns></returns>
        public bool HasState(TStateId stateId)
        {
            return States.ContainsKey(stateId);
        }
        
        /// <summary>
        /// Exits the current state and sets the machine to its initial state.
        /// This method is called when the machine is a submachine of another machine, and the submachine is being exited.
        /// It is not recommended to call this method directly, as it is usually called by the submachine itself.
        /// </summary>
        public void ExitMachine()
        {
            if (CurrentState == null)
                return;
            
            CurrentState.OnExit();
            CurrentState.IsActive = false;
            CurrentState = null;
        }
        
        /// <summary>
        /// Enters the current state of the machine.
        /// This method is called when the machine is a submachine of another machine, and the submachine is being entered.
        /// It is not recommended to call this method directly, as it is usually called by the submachine itself.
        /// </summary>
        public void EnterMachine()
        {
            if (CurrentState == null)
                return;
            
            CurrentState.IsActive = true;
            CurrentState.OnEnter();
        }

        /// <summary>
        /// Change the current state to a specified state.
        /// Making a transition between current to new state.
        /// </summary>
        /// <param name="newState">The new state identifier.</param>
        public void ChangeState(TStateId newState)
        {
            if (newState == null)
            {
                ChangeState(default, -1);
                return;
            }
            
            var index = StatesOrder.IndexOf(newState);
            ChangeState(newState, index);
        }
        
        private void ChangeState(TStateId newState, int index)
        {
            if (newState == null || index < 0 || index >= StatesOrder.Count)
            {
                PreviousState = CurrentState;
                PreviousIndex = CurrentIndex;
                CurrentState = null;
                CurrentIndex = -1;
                ExitPreviousState();
                return;
            }

            if (!SetNewState(newState, index)) return;
            ExitPreviousState();
            EnterNewState();
        }
        
        private bool SetNewState(TStateId newState, int index)
        {
            if((CurrentState != null && newState.Equals(CurrentState.Id)) || !States.TryGetValue(newState, out var state))
                return false;

            if (!state.Enabled)
                return false;
            
            PreviousIndex = CurrentIndex;
            CurrentIndex = index;
            
            PreviousState = CurrentState;
            CurrentState = state;
            return true;
        }
        
        private void EnterNewState()
        {
            if (CurrentState == null)
                return;
            
            CurrentState.IsActive = true;
            ExecuteEventOnState(StateEvent.OnEnter, true);
        }
        
        private void ExitPreviousState()
        {
            if (PreviousState == null)
                return;
            
            PreviousState.OnExit();
            PreviousState.IsActive = false;

            if (PreviousState.Extensions == null) return;
            foreach (var extension in PreviousState.Extensions.Where(extension => extension.enabled))
                extension.OnExit();
        }
        
        /// <summary>
        /// Changes the current state to the previous state.
        /// Making a transition between current to previous state.
        /// </summary>
        public void RevertToPreviousState()
        {
            if (PreviousState != null)
            {          
                ChangeState(PreviousState.Id, PreviousIndex);
            }
        }
        
        public bool TryGetTransition(out (TStateId id, int index) state, int maxIndex)
        {
            state = default;
            
            for (var i = 0; i < maxIndex; i++)
            {
                if (States[StatesOrder[i]].Enabled && States[StatesOrder[i]].CanEnter())
                {
                    state = (StatesOrder[i], i);
                    
                    return true;
                }
            }

            return false;
        }
        
        private void TestTransitions()
        {
            if (CurrentState == null)
            {
                if (TryGetTransition(out var state, States.Count))
                {
                    ChangeState(state.id, state.index);
                }
            }
            else
            {
                if (CurrentState.CanExit())
                {
                    var maxIndex = CurrentState.CanEnter() ? CurrentIndex : StatesOrder.Count;
                    if (TryGetTransition(out var state, maxIndex))
                    {
                        ChangeState(state.id, state.index);
                    }
                }
            }
        }

        public void OnCreated()
        {
            Created = true;
            if (States.Count == 0)
                return;
            
            foreach (var state in States.Values)
            {
                ExecuteEventOnState(StateEvent.OnCreated, false, state);
                // state.OnCreated();
                // if (state.Extensions == null) 
                //     continue;
                //
                // foreach (var extension in state.Extensions)
                //     extension.OnCreated(state);
            }

            if (TryGetTransition(out var newState, States.Count))
            {
                ChangeState(newState.id, newState.index);
                // CurrentIndex = newState.index;
                // CurrentState = States[StatesOrder[CurrentIndex]];
            }
        }

        public void OnUpdate()
        {
            TestTransitions();
            ExecuteEventOnState(StateEvent.OnUpdate, true);
        }

        public void OnFixedUpdate()
        {
            ExecuteEventOnState(StateEvent.OnFixedUpdate, true);
        }
        
        private enum StateEvent
        {
            OnCreated,
            OnEnter,
            OnExit,
            OnUpdate,
            OnFixedUpdate
        }

        private void ExecuteEventOnState(StateEvent stateEvent, bool isCurrentState, IState<TStateId, TStateMachine> state = null)
        {
            if (isCurrentState)
                state = CurrentState;
            
            if (state == null)
                return;
            
            switch (stateEvent)
            {
                case StateEvent.OnCreated:
                    state.OnCreated();
                    break;
                case StateEvent.OnEnter:
                    state.OnEnter();
                    break;
                case StateEvent.OnExit:
                    state.OnExit();
                    break;
                case StateEvent.OnUpdate:
                    state.OnUpdate();
                    break;
                case StateEvent.OnFixedUpdate:
                    state.OnFixedUpdate();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateEvent), stateEvent, null);
            }

            if (state.Extensions == null) 
                return;

            if (isCurrentState && state != CurrentState)
                return;
            
            foreach (var extension in state.Extensions.Where(extension => stateEvent == StateEvent.OnCreated || extension.enabled))
            {
                switch (stateEvent)
                {
                    case StateEvent.OnCreated:
                        extension.OnCreated(state);
                        break;
                    case StateEvent.OnEnter:
                        extension.OnEnter();
                        break;
                    case StateEvent.OnExit:
                        extension.OnExit();
                        break;
                    case StateEvent.OnUpdate:
                        extension.OnUpdate();
                        break;
                    case StateEvent.OnFixedUpdate:
                        extension.OnFixedUpdate();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(stateEvent), stateEvent, null);
                }
            }
        }
        
        [CanBeNull]
        private List<StateExtension<TStateId, TStateMachine>> GetExtensionsInState(IState<TStateId, TStateMachine> state)
        {
            var extensions = new List<StateExtension<TStateId, TStateMachine>>();
            var fields = state.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.FieldType.BaseType.IsGenericType && field.FieldType.BaseType.GetGenericTypeDefinition() == typeof(StateExtension<,>))
                {
                    var value = field.GetValue(state);

                    if (value != null)
                    {
                        extensions.Add(value as StateExtension<TStateId, TStateMachine>);
                    }
                }
            }
            return extensions.Count == 0 ? null : extensions;
        }
    }
}