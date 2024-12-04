using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using MasterSM;
using UnityEngine;

namespace AdvancedSM
{
    public interface IStateMachine
    {
        
    }
    public abstract class BehaviourMachine<TStateId, TStateMachine> : MonoBehaviour, IStateMachine
        where TStateMachine : BehaviourMachine<TStateId, TStateMachine>
    {
        public IState<TStateId, TStateMachine> CurrentState { get; private set; }
        public IState<TStateId, TStateMachine> PreviousState { get; private set; }
        
        [NonSerialized] protected Dictionary<TStateId, IState<TStateId, TStateMachine>> States = new();
        [NonSerialized] protected List<TStateId> StatesOrder = new();
        [NonSerialized] protected int CurrentIndex;
        
        // Capabilities
        // base capabilites has <IStateMachine>
        [NonReorderable] protected readonly Dictionary<Type, BaseCapability<TStateId, TStateMachine>> Capabilities = new();
        
        public void RegisterCapability<T>(T capability) where T : BaseCapability<TStateId, TStateMachine>
        {
            Capabilities.Add(typeof(T), capability);
        }
        
        public T GetCapability<T>() where T : BaseCapability<TStateId, TStateMachine>
        {
            if (Capabilities.TryGetValue(typeof(T), out var capability))
            {
                return (T)capability;
            }

            return default;
        }
        
        public void AddState(TStateId id, IState<TStateId, TStateMachine> state, int priority = 0)
        {
            state.Id = id;
            state.Priority = priority;
            state.Machine = (TStateMachine)this;
            state.Extensions ??= GetExtensionsInState(state);
            if (state.Extensions != null)
            {
                foreach (var extension in state.Extensions)
                    extension.Machine = (TStateMachine)this;
            }
            if (!States.TryAdd(id, state))
                return;
            
            // Add state to order list
            var index = 0;
            for (var i = 0; i < StatesOrder.Count; i++)
            {
                if (priority > States[StatesOrder[i]].Priority)
                {
                    index = i;
                    break;
                }
                index = i + 1;
            }
            StatesOrder.Insert(index, id);

            if (index <= CurrentIndex)
            {
                CurrentIndex++;
            }
        }

        public void RemoveState(TStateId id)
        {
            if (!States.TryGetValue(id, out var stateToRemove))
                return;

            States.Remove(id);
            StatesOrder.Remove(id);
            
            if (stateToRemove == CurrentState)
            {
                if (TryGetTransition(out var newState, StatesOrder.Count))
                {
                    ChangeState(newState.Item1);
                    CurrentIndex = newState.Item2;
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
 
        public void ChangeState(TStateId newState)
        {
            SetNewState(newState);
            ExitPreviousState();
            EnterNewState();
        }
        
        private void SetNewState(TStateId newState)
        {
            if((CurrentState != null && newState.Equals(CurrentState.Id)) || !States.TryGetValue(newState, out var state))
               return;
               
            PreviousState = CurrentState;
            CurrentState = state;
        }
        
        private void EnterNewState()
        {
            if (CurrentState == null)
                return;
            
            CurrentState.IsActive = true;
            CurrentState.OnEnter();

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled)) 
                extension.OnEnter();
        }
        
        private void ExitPreviousState()
        {
            if (PreviousState == null)
                return;
            
            PreviousState.OnExit();
            PreviousState.IsActive = false;

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled))
                extension.OnExit();
        }
        
        public void RevertToPreviousState()
        {
            if (PreviousState != null)
            {          
                ChangeState(PreviousState.Id);
            }
        }
        
        public bool TryGetTransition(out (TStateId, int) state, int maxIndex)
        {
            state = default;
            
            for (int i = 0; i < maxIndex; i++)
            {
                if (States[StatesOrder[i]].CanEnter())
                {
                    state = (StatesOrder[i], i);
                    
                    return true;
                }
            }

            return false;
        }

        protected virtual void Start()
        {
            OnCreated();
            EnterNewState();
        }

        protected virtual void Update()
        {
            OnUpdate();
        }

        protected virtual void FixedUpdate()
        {
            OnFixedUpdate();
        }

        public void OnCreated()
        {
            if (States.Count == 0)
                return;
            
            foreach (var state in States.Values)
            {
                state.OnCreated();
                if (state.Extensions == null) 
                    continue;
                
                foreach (var extension in state.Extensions)
                    extension.OnCreated(state);
            }

            if (TryGetTransition(out var newState, States.Count))
            {
                CurrentIndex = newState.Item2;
                CurrentState = States[StatesOrder[CurrentIndex]];
            }
        }

        public void OnUpdate()
        {
            if (CurrentState == null)
            {
                if (TryGetTransition(out var state, States.Count))
                {
                    CurrentIndex = state.Item2;
                    ChangeState(state.Item1);
                }
            }
            else
            {
                if (CurrentState.CanExit())
                {
                    var maxIndex = CurrentState.CanEnter() ? CurrentIndex : StatesOrder.Count;
                    if (TryGetTransition(out var state, maxIndex))
                    {
                        CurrentIndex = state.Item2;
                        ChangeState(state.Item1);
                    }
                }
            }
            
            if (CurrentState == null)
                return;
            
            CurrentState.OnUpdate();

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled))
                extension.OnUpdate();
        }
        
        public void OnFixedUpdate()
        {
            if (CurrentState == null)
                return;
            
            CurrentState.OnFixedUpdate();

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled))
                extension.OnFixedUpdate();
        }
    }
}