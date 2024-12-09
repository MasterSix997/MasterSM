using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace MasterSM
{
    public interface IStateMachine
    {
        
    }

    public class LayerMachine<TStateId, TStateMachine> : IStateMachine
        where TStateMachine : BehaviourMachine<TStateId, TStateMachine>
    {
        public TStateMachine Machine { get; private set; }
        public int LayerIndex { get; private set; }
        
        [CanBeNull] public IState<TStateId, TStateMachine> CurrentState { get; private set; }
        [CanBeNull] public IState<TStateId, TStateMachine> PreviousState { get; private set; }

        protected Dictionary<TStateId, IState<TStateId, TStateMachine>> States = new();
        protected List<TStateId> StatesOrder = new();
        protected int CurrentIndex;

        public LayerMachine(TStateMachine machine, int layerIndex)
        {
            Machine = machine;
            LayerIndex = layerIndex;
        }

        public void AddState(IState<TStateId, TStateMachine> state)
        {
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
                index = i + 1;
            }
            StatesOrder.Insert(index, state.Id);

            if (index <= CurrentIndex)
            {
                CurrentIndex++;
            }
        }
        
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
        
        public void ChangeState(TStateId newState)
        {
            if (newState == null)
            {
                PreviousState = CurrentState;
                CurrentState = null;
                ExitPreviousState();
                return;
            }
            
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
        
        public void EnterNewState()
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

            if (PreviousState.Extensions == null) return;
            foreach (var extension in PreviousState.Extensions.Where(extension => extension.enabled))
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

            var current = CurrentState;
            
            CurrentState.OnUpdate();

            if (current != CurrentState)
                return;

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled))
                extension.OnUpdate();
        }
        
        public void OnFixedUpdate()
        {
            if (CurrentState == null)
                return;
            
            var current = CurrentState;
            
            CurrentState.OnFixedUpdate();

            if (current != CurrentState)
                return;

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled))
                extension.OnFixedUpdate();
        }
    }

    public abstract class BehaviourMachine<TStateId, TStateMachine> : MonoBehaviour, IStateMachine
        where TStateMachine : BehaviourMachine<TStateId, TStateMachine>
    {
        public IState<TStateId, TStateMachine> CurrentState { get; private set; }
        public IState<TStateId, TStateMachine> PreviousState { get; private set; }
        
        public List<LayerMachine<TStateId, TStateMachine>> Layers { get; private set; } = new();
        
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

        public LayerMachine<TStateId, TStateMachine> AddLayer()
        {
            var layerIndex = Layers.Count;
            Layers.Add(new LayerMachine<TStateId, TStateMachine>((TStateMachine)this, layerIndex));
            return Layers[layerIndex];
        }
        
        public void RemoveLayer(int layerIndex)
        {
            if (layerIndex < 0)
                layerIndex = 0;

            if (layerIndex >= Layers.Count)
            {
                throw new IndexOutOfRangeException("Layer index is out of range");
            }

            var layerState = Layers[layerIndex].CurrentState;
            if (layerState != null)
                Layers[layerIndex].RemoveState(layerState.Id);
            Layers.RemoveAt(layerIndex);
        }

        public LayerMachine<TStateId, TStateMachine> GetLayer(int layerIndex)
        {
            if (layerIndex < 0)
                layerIndex = 0;

            if (layerIndex >= Layers.Count)
            {
                throw new IndexOutOfRangeException("Layer index is out of range");
            }

            return Layers[layerIndex];
        }
        
        public void AddStateInLayer(int layerIndex, TStateId id, IState<TStateId, TStateMachine> state, int priority = 0)
        {
            if (layerIndex < 0)
                layerIndex = 0;

            if (layerIndex >= Layers.Count)
            {
                throw new IndexOutOfRangeException("Layer index is out of range");
            }
            
            AddStateInLayer(Layers[layerIndex], id, state, priority);
        }
        
        public void AddStateInLayer(LayerMachine<TStateId, TStateMachine> layer, TStateId id, IState<TStateId, TStateMachine> state, int priority = 0)
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
            
            layer.AddState(state);
        }
        
        public void RemoveStateInLayer(int layerIndex, TStateId id)
        {
            if (layerIndex < 0)
                layerIndex = 0;

            if (layerIndex >= Layers.Count)
            {
                throw new IndexOutOfRangeException("Layer index is out of range");
            }
            
            Layers[layerIndex].RemoveState(id);
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

            if (PreviousState.Extensions == null) return;
            foreach (var extension in PreviousState.Extensions.Where(extension => extension.enabled))
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
            
            foreach (var layer in Layers)
            {
                layer.OnCreated();
                layer.EnterNewState();
            }
        }

        protected virtual void Update()
        {
            OnUpdate();
            
            foreach (var layer in Layers)
            {
                layer.OnUpdate();
            }
        }

        protected virtual void FixedUpdate()
        {
            OnFixedUpdate();
            
            foreach (var layer in Layers)
            {
                layer.OnFixedUpdate();
            }
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
            
            var current = CurrentState;
            
            CurrentState.OnUpdate();

            if (current != CurrentState)
                return;

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled))
                extension.OnUpdate();
        }
        
        public void OnFixedUpdate()
        {
            if (CurrentState == null)
                return;
            
            var current = CurrentState;
            
            CurrentState.OnFixedUpdate();

            if (current != CurrentState)
                return;

            if (CurrentState.Extensions == null) return;
            foreach (var extension in CurrentState.Extensions.Where(extension => extension.enabled))
                extension.OnFixedUpdate();
        }
    }
}