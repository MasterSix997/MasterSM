﻿using System;
using System.Collections.Generic;
using MasterSM.Exceptions;
using MasterSM.PriorityManagement;
using UnityEngine;

namespace MasterSM
{
    /// <summary>
    /// Abstract base class for a behaviour-based state machine.
    /// </summary>
    /// <typeparam name="TStateId">The type of the state identifier.</typeparam>
    /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
    public abstract class BehaviourMachine<TStateId, TStateMachine> : 
        MonoBehaviour, IStateMachineImpl<TStateId, TStateMachine>, IMachineWithLayers<TStateId, TStateMachine>
        where TStateMachine : BehaviourMachine<TStateId, TStateMachine>
    {
        private bool _initialized;
        private readonly BaseMachine<TStateId, TStateMachine> _baseMachine = new();
        public IState<TStateId, TStateMachine> CurrentState => _baseMachine.CurrentState;
        public IState<TStateId, TStateMachine> PreviousState => _baseMachine.PreviousState;

        protected Dictionary<object, BaseMachine<TStateId, TStateMachine>> Layers { get; } = new();
        
        protected Dictionary<TStateId, IState<TStateId, TStateMachine>> States => _baseMachine.States;
        protected int CurrentIndex => _baseMachine.CurrentIndex;

        public TStateId CurrentId => _baseMachine.CurrentId;

        // Capabilities
        protected readonly Dictionary<Type, BaseCapability<TStateId, TStateMachine>> Capabilities = new();
        
#if UNITY_EDITOR
        // Events for Custom Editor
        public event Action OnLayerAdded;
        public event Action OnLayerRemoved;
#endif

        internal void Initialize()
        {
            if (_initialized)
                return;
            
            _baseMachine.Machine = (TStateMachine)this;
            _initialized = true;
        }
        
        /// <summary>
        /// Registers a capability to the state machine.
        /// This allows the states to access the capability instance.
        /// </summary>
        /// <param name="capability"></param>
        /// <typeparam name="T"></typeparam>
        public void RegisterCapability<T>(T capability) where T : BaseCapability<TStateId, TStateMachine>
        {
            Capabilities.Add(typeof(T), capability);
        }
        
        /// <summary>
        /// Gets a capability instance of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the capability.</typeparam>
        /// <returns>The capability instance.</returns>
        /// <exception cref="MasterSMException"></exception>
        public T GetCapability<T>() where T : BaseCapability<TStateId, TStateMachine>
        {
            if (!Capabilities.TryGetValue(typeof(T), out var capability))
                throw ExceptionCreator.CapabilityNotFound(typeof(T), "Getting capability");

            return (T)capability;
        }
        
        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.AddState(in TStateId,in MasterSM.IState{TStateId,TStateMachine},MasterSM.PriorityManagement.StatePriority{TStateId})"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <param name="statePriority"></param>
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, StatePriority<TStateId> statePriority)
        {
            Initialize();
            _baseMachine.AddState(id, state, statePriority);
        }

        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int priority = 0)
        {
            Initialize();
            _baseMachine.AddState(id, state, priority);
        }

        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int group, int priority)
        {
            Initialize();
            _baseMachine.AddState(id, state, group, priority);
        }

        public void AddState(in IState<TStateId, TStateMachine> state, StatePriority<TStateId> priority)
        {
            Initialize();
            _baseMachine.AddState(state, priority);
        }

        public void AddState(StateGroup<TStateId, TStateMachine> group)
        {
            Initialize();
            _baseMachine.AddState(group);
        }

        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.RemoveState"/>
        /// </summary>
        /// <param name="id">The identifier of the state to remove.</param>
        public void RemoveState(TStateId id) => _baseMachine.RemoveState(id);
        
        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.GetState"/>
        /// </summary>
        /// <param name="id">The identifier of the state.</param>
        /// <returns>The state instance.</returns>
        public IState<TStateId, TStateMachine> GetState(TStateId id) => _baseMachine.GetState(id);
        
        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.HasState"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool HasState(TStateId id) => _baseMachine.HasState(id);

        /// <summary>
        /// Adds a new layer to the state machine.
        /// The layers allow executing multiple state machines in parallel.
        /// </summary>
        /// <param name="layerId">The identifier of the layer.</param>
        /// <returns>Returns the new layer instance.</returns>
        /// <exception cref="MasterSMException">Thrown if a layer with the same identifier already exists.</exception>
        public BaseMachine<TStateId, TStateMachine> AddLayer(object layerId)
        {
            Initialize();
            if (Layers.ContainsKey(layerId))
                throw ExceptionCreator.LayerIdAlreadyExists(layerId);
            
            var layer = new BaseMachine<TStateId, TStateMachine>
            {
                Machine = (TStateMachine)this
            };
            Layers.Add(layerId, layer);

#if UNITY_EDITOR
            OnLayerAdded?.Invoke();
#endif
            return layer;
        }
        
        /// <summary>
        /// Removes a layer from the state machine.
        /// </summary>
        /// <param name="layerId">The identifier of the layer to remove.</param>
        /// <exception cref="MasterSMException">Thrown if the layer does not exist.</exception>
        public void RemoveLayer(object layerId)
        {
            if (!Layers.TryGetValue(layerId, out var layer))
                throw ExceptionCreator.LayerNotFound(layerId, "Removing layer");

            if (layer.CurrentState != null)
                layer.ChangeState(default);
            
#if UNITY_EDITOR
            OnLayerRemoved?.Invoke();
#endif
            
            Layers.Remove(layerId);
        }

        /// <summary>
        /// Gets a layer by its identifier.
        /// </summary>
        /// <param name="layerId">The identifier of the layer.</param>
        /// <returns>The layer instance.</returns>
        /// <exception cref="MasterSMException">Thrown if the layer does not exist.</exception>
        public BaseMachine<TStateId, TStateMachine> GetLayer(object layerId)
        {
            if (!Layers.TryGetValue(layerId, out var layer))
                throw ExceptionCreator.LayerNotFound(layerId, "Getting layer");

            return layer;
        }

        /// <summary>
        /// Checks if a layer with the specified identifier exists.
        /// </summary>
        /// <param name="layerId"></param>
        /// <returns></returns>
        public bool HasLayer(object layerId) => Layers.ContainsKey(layerId);

        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.ChangeState"/>
        /// </summary>
        /// <param name="newState">The identifier of the new state.</param>
        public void ChangeState(TStateId newState)
        {
            _baseMachine.ChangeState(newState);
        }
        
        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.RevertToPreviousState"/>
        /// </summary>
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
            _baseMachine.OnCreated(true);
            
            foreach (var layer in Layers.Values)
            {
                layer.OnCreated(true);
            }
        }

        protected virtual void Update()
        {
            _baseMachine.OnUpdate();
            
            foreach (var layer in Layers.Values)
                layer.OnUpdate();
        }

        protected virtual void FixedUpdate()
        {
            _baseMachine.OnFixedUpdate();
            
            foreach (var layer in Layers.Values)
                layer.OnFixedUpdate();
        }
    }
}