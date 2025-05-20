using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MasterSM.Exceptions;
using MasterSM.PriorityManagement;

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

        public readonly PriorityManager<TStateId> PriorityManager = new();
        // public readonly List<TStateId> StatesOrder = new();
        internal int CurrentIndex = -1;
        internal int PreviousIndex = -1;
        internal bool Created;
        
        public TStateId CurrentId => CurrentState == null ? default : CurrentState.Id;
        
#if UNITY_EDITOR
        // Events for Custom Editor
        public event Action OnStateAdded;
        public event Action OnStateRemoved;
        public event Action OnCurrentStateChanged;
#endif

        public BaseMachine()
        {
            PriorityManager.OnChangeOrderEvent.Add(new ChangeOrderEvent
            {
                Get = () => CurrentIndex,
                NewValue = newValue => CurrentIndex = newValue
            });
            PriorityManager.OnChangeOrderEvent.Add(new ChangeOrderEvent
            {
                Get = () => PreviousIndex,
                NewValue = newValue => PreviousIndex = newValue
            });
        }
        
        /// <summary>
        /// Adds a state to the state machine using a priority resolver.
        /// </summary>
        /// <param name="id">The state identifier.</param>
        /// <param name="state">The state to add.</param>
        /// <param name="statePriority">The priority resolver of the state.</param>
        /// <exception cref="MasterSMException">If already have a state with the same id</exception>
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, StatePriority<TStateId> statePriority)
        {
            if (!state.Initialized)
            {
                state.Initialize(id, Machine);
                state.Extensions ??= GetExtensionsInState(state);
                if (state.Extensions != null)
                {
                    foreach (var extension in state.Extensions)
                        extension.Machine = Machine;
                }

                if (Created)
                {
                    ExecuteOnCreated(state);
                }
            }

            if (!States.TryAdd(state.Id, state))
                throw ExceptionCreator.StateIdAlreadyExists(id, state);
            
            PriorityManager.AddState(id, statePriority);
            
#if UNITY_EDITOR
            OnStateAdded?.Invoke();
#endif
        }

        /// <summary>
        /// Adds a state to the state machine, in group 0 with the specified priority.
        /// </summary>
        /// <param name="id">The state identifier.</param>
        /// <param name="state">The state to add.</param>
        /// <param name="priority">The priority of the state.</param>
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int priority)
        {
            AddState(id, state, new StatePriority<TStateId>(0, priority));
        }

        /// <summary>
        /// Adds a state to the state machine, with the specified group and priority.
        /// </summary>
        /// <param name="id">The state identifier.</param>
        /// <param name="state">The state to add.</param>
        /// <param name="group">The group of the state</param>
        /// <param name="priority">The priority of the state.</param>
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int group, int priority)
        {
            AddState(id, state, new StatePriority<TStateId>(group, priority));
        }
        
        /// <summary>
        /// Adds a state to the state machine omitting the id, If each state has a class as an identifier.
        /// </summary>
        /// <param name="state">The state to add.</param>
        /// <param name="priority">The priority provider of the state.</param>
        public void AddState(in IState<TStateId, TStateMachine> state, StatePriority<TStateId> priority)
        {
            if (typeof(TStateId) == typeof(Type))
                AddState((TStateId)(object)state.GetType(), state, priority);
            else if(typeof(TStateId) == typeof(string))
                AddState((TStateId)(object)state.GetType().Name, state, priority);
        }
        
        public void AddState(StateGroup<TStateId, TStateMachine> group)
        {
            var priority = group.basePriority;
            foreach (var state in group.GetStates())
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
            if (!States.Remove(id, out var stateToRemove))
                return;

            PriorityManager.RemoveState(id);
            
            if (stateToRemove == CurrentState)
            {
                if (TryGetTransition(out var newState, PriorityManager.StatesCount))
                {
                    ChangeState(newState.id, newState.index);
                }
                else if (CurrentState != null)
                {
                    ExecuteOnExit(CurrentState);
                    CurrentState.IsActive = false;
                    CurrentState = null;
                }
            }

            if (stateToRemove == PreviousState)
                PreviousState = null;
            
#if UNITY_EDITOR
            OnStateRemoved?.Invoke();
#endif
        }

        /// <summary>
        /// Gets a state by its identifier.
        /// </summary>
        /// <param name="id">The state identifier.</param>
        /// <returns>The state</returns>
        /// <exception cref="MasterSMException">Throw exception if state with specified identifier is not found.</exception>
        public IState<TStateId, TStateMachine> GetState(TStateId id)
        {
            if (!States.TryGetValue(id, out var state))
                throw ExceptionCreator.IdNotFound(id, "Getting state");
            
            return state;
        }
        
        /// <summary>
        /// Checks if a state with the specified identifier exists in the state machine.
        /// </summary>
        /// <param name="stateId">The state identifier.</param>
        /// <returns></returns>
        public bool HasState(TStateId stateId) => States.ContainsKey(stateId);
        
        /// <summary>
        /// Exits the current state and sets the machine to its initial state.
        /// This method is called when the machine is a submachine of another machine, and the submachine is being exited.
        /// It is not recommended to call this method directly, as it is usually called by the submachine itself.
        /// </summary>
        public void ExitMachine()
        {
            if (CurrentState == null)
                return;
            
            ExecuteOnExit(CurrentState);
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
            if (CurrentState is {IsActive: true})
                return;

            if (CurrentState is null)
            {
                TestTransitions();
                return;
            }
            
            CurrentState.IsActive = true;
            CurrentState.StateOnEnter();
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

            try
            {
                var index = PriorityManager.IndexFrom(newState);
                ChangeState(newState, index);
            }
            catch (MasterSMException e)
            {
                e.Context = "Changing state manually";
                throw;
            }
        }
        
        private void ChangeState(TStateId newState, int index)
        {
            if (newState == null || index < 0 || index >= PriorityManager.StatesCount)
            {
                PreviousState = CurrentState;
                PreviousIndex = CurrentIndex;
                CurrentState = null;
                CurrentIndex = -1;
                ExitPreviousState();
                
#if UNITY_EDITOR
                OnCurrentStateChanged?.Invoke();
#endif
                return;
            }

            if (!SetNewState(newState, index)) return;
            ExitPreviousState();
            EnterNewState();
#if UNITY_EDITOR
            OnCurrentStateChanged?.Invoke();
#endif
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
            ExecuteOnEnter(CurrentState);
        }
        
        private void ExitPreviousState()
        {
            if (PreviousState == null)
                return;
            
            ExecuteOnExit(PreviousState);
            PreviousState.IsActive = false;
            PreviousState.ExecuteOnExtensions(extension => extension.OnExit());
        }
        
        /// <summary>
        /// Changes the current state to the previous state.
        /// Making a transition between current to previous state.
        /// </summary>
        public void RevertToPreviousState()
        {
            if (PreviousState != null)
                ChangeState(PreviousState.Id, PreviousIndex);
        }

        private bool TryGetTransition(out (TStateId id, int index) state, int maxIndex)
        {
            state = default;
            
            for (var i = 0; i < maxIndex; i++)
            {
                if (CanEnter(i))
                {
                    state = (PriorityManager.IdFrom(i), i);
                    return true;
                }
            }

            return false;

            bool CanEnter(int i)
            {
                var id = PriorityManager.IdFrom(i);
                if (!States[id].Enabled || !States[id].StateCanEnter()) 
                    return false;
                
                foreach (var extension in States[id].EnabledExtensions())
                    if (!extension.CanEnter()) return false;
                    
                return true;
            }
        }
        
        private void TestTransitions()
        {
            if (CurrentState == null)
            {
                if (TryGetTransition(out var state, States.Count))
                    ChangeState(state.id, state.index);
            }
            else
            {
                if (!CurrentState.StateCanExit()) return;
                foreach (var extension in CurrentState.EnabledExtensions())
                    if (!extension.CanExit()) return;
                    
                var maxIndex = CurrentState.StateCanEnter() ? CurrentIndex : PriorityManager.StatesCount;
                if (TryGetTransition(out var state, maxIndex))
                    ChangeState(state.id, state.index);
            }
        }

        public void OnCreated(bool tryGetTransition = true)
        {
            Created = true;
            if (States.Count == 0)
                return;
            
            foreach (var state in States.Values)
                ExecuteOnCreated(state);

            if (!tryGetTransition) return;
            if (TryGetTransition(out var newState, States.Count))
                ChangeState(newState.id, newState.index);
        }

        public void OnUpdate()
        {
            TestTransitions();
            ExecuteOnUpdate(CurrentState);
        }

        public void OnFixedUpdate()
        {
            ExecuteOnFixedUpdate(CurrentState);
        }

        private void ExecuteOnCreated(IState<TStateId, TStateMachine> state)
        {
            if (state == null)
                return;

            state.StateOnCreated();
            state.ExecuteOnExtensions(extension => extension.OnCreated(state));
        }
        
        private void ExecuteOnEnter(IState<TStateId, TStateMachine> state)
        {
            if (state == null)
                return;

            state.StateOnEnter();
            state.ExecuteOnExtensions(extension => extension.OnEnter());
        }
        
        private void ExecuteOnExit(IState<TStateId, TStateMachine> state)
        {
            if (state == null)
                return;

            state.StateOnExit();
            state.ExecuteOnExtensions(extension => extension.OnExit());
        }
        
        private void ExecuteOnUpdate(IState<TStateId, TStateMachine> state)
        {
            if (state == null)
                return;

            state.StateOnUpdate();
            state.ExecuteOnExtensions(extension => extension.OnUpdate());
        }

        private void ExecuteOnFixedUpdate(IState<TStateId, TStateMachine> state)
        {
            if (state == null)
                return;
            
            state.StateOnFixedUpdate();
            state.ExecuteOnExtensions(extension => extension.OnFixedUpdate());
        }
        
        // [CanBeNull]
        // private static List<StateExtension<TStateId, TStateMachine>> GetExtensionsInState(IState<TStateId, TStateMachine> state)
        // {
        //     var extensions = new List<StateExtension<TStateId, TStateMachine>>();
        //     var fields = state.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //     foreach (var field in fields)
        //     {
        //         if (!field.FieldType.BaseType.IsGenericType || field.FieldType.BaseType.GetGenericTypeDefinition() != typeof(StateExtension<,>)) 
        //             continue;
        //         
        //         var value = field.GetValue(state);
        //         if (value != null)
        //             extensions.Add(value as StateExtension<TStateId, TStateMachine>);
        //     }
        //     return extensions.Count == 0 ? null : extensions;
        // }

        private static readonly Dictionary<Type, List<FieldInfo>> ExtensionFieldsCache = new();
        [CanBeNull]
        private static List<StateExtension<TStateId, TStateMachine>> GetExtensionsInState(IState<TStateId, TStateMachine> state)
        {
            var stateType = state.GetType();
    
            // Reflection cache by type
            if (!ExtensionFieldsCache.TryGetValue(stateType, out var fieldInfos))
            {
                fieldInfos = stateType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f => f.FieldType.BaseType?.IsGenericType == true && 
                                f.FieldType.BaseType.GetGenericTypeDefinition() == typeof(StateExtension<,>))
                    .ToList();
                ExtensionFieldsCache[stateType] = fieldInfos;
            }
    
            // use cached fields to obtain the instances
            var extensions = new List<StateExtension<TStateId, TStateMachine>>();
            foreach (var field in fieldInfos)
            {
                var value = field.GetValue(state);
                if (value != null)
                    extensions.Add(value as StateExtension<TStateId, TStateMachine>);
            }
    
            return extensions.Count == 0 ? null : extensions;
        }
    }
}