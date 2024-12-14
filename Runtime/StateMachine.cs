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

    public class BaseMachine<TStateId, TStateMachine> : IStateMachine
        where TStateMachine : IStateMachine
    {
        public TStateMachine Machine { get; set; }
        public int LayerIndex { get; set; }
        
        [CanBeNull] public IState<TStateId, TStateMachine> CurrentState { get; private set; }
        [CanBeNull] public IState<TStateId, TStateMachine> PreviousState { get; private set; }

        public Dictionary<TStateId, IState<TStateId, TStateMachine>> States = new();
        public List<TStateId> StatesOrder = new();
        public int CurrentIndex = -1;
        public int PreviousIndex = -1;
        public bool Created;

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
                    state.OnCreated();
                    if (state.Extensions != null)
                    {
                        foreach (var extension in state.Extensions)
                            extension.OnCreated(state);
                    }
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
        
        public void EnterNewState()
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
            
            foreach (var extension in state.Extensions.Where(extension => extension.enabled))
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

    public abstract class BehaviourMachine<TStateId, TStateMachine> : MonoBehaviour, IStateMachine
        where TStateMachine : BehaviourMachine<TStateId, TStateMachine>
    {
        private bool _initialized;
        private readonly BaseMachine<TStateId, TStateMachine> _baseMachine = new();
        public IState<TStateId, TStateMachine> CurrentState => _baseMachine.CurrentState;
        public IState<TStateId, TStateMachine> PreviousState => _baseMachine.PreviousState;
        
        public List<BaseMachine<TStateId, TStateMachine>> Layers { get; } = new();
        
        protected Dictionary<TStateId, IState<TStateId, TStateMachine>> States => _baseMachine.States;
        protected List<TStateId> StatesOrder => _baseMachine.StatesOrder;
        protected int CurrentIndex => _baseMachine.CurrentIndex;
        
        // Capabilities
        protected readonly Dictionary<Type, BaseCapability<TStateId, TStateMachine>> Capabilities = new();
        
        public void Initialize()
        {
            if (_initialized)
                return;
            
            _baseMachine.Machine = (TStateMachine)this;
            _initialized = true;
        }
        
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
            Initialize();
            _baseMachine.AddState(id, state, priority);
        }

        public void RemoveState(TStateId id)
        {
            _baseMachine.RemoveState(id);
        }

        public BaseMachine<TStateId, TStateMachine> AddLayer()
        {
            Initialize();
            var layerIndex = Layers.Count;
            Layers.Add(new BaseMachine<TStateId, TStateMachine> { LayerIndex = layerIndex, Machine = (TStateMachine)this });
            return Layers[layerIndex];
        }
        
        public void RemoveLayer(int layerIndex)
        {
            layerIndex = GetCorrectLayerIndex(layerIndex);

            var layerState = Layers[layerIndex].CurrentState;
            if (layerState != null)
                Layers[layerIndex].RemoveState(layerState.Id);
            Layers.RemoveAt(layerIndex);
        }

        public BaseMachine<TStateId, TStateMachine> GetLayer(int layerIndex)
        {
            layerIndex = GetCorrectLayerIndex(layerIndex);
            return Layers[layerIndex];
        }
        
        private int GetCorrectLayerIndex(int layerIndex)
        {
            if (layerIndex < 0)
                return 0;

            if (layerIndex >= Layers.Count)
            {
                throw new IndexOutOfRangeException("Layer index is out of range");
            }
            
            return layerIndex;
        }

        public void ChangeState(TStateId newState)
        {
            _baseMachine.ChangeState(newState);
        }
        
        public void RevertToPreviousState()
        {
            _baseMachine.RevertToPreviousState();
        }
        
        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Start()
        {
            _baseMachine.OnCreated();
            _baseMachine.EnterNewState();
            
            foreach (var layer in Layers)
            {
                layer.OnCreated();
                layer.EnterNewState();
            }
        }

        protected virtual void Update()
        {
            _baseMachine.OnUpdate();
            
            foreach (var layer in Layers)
            {
                layer.OnUpdate();
            }
        }

        protected virtual void FixedUpdate()
        {
            _baseMachine.OnFixedUpdate();
            
            foreach (var layer in Layers)
            {
                layer.OnFixedUpdate();
            }
        }
    }
}