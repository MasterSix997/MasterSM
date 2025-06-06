﻿using System;
using System.Collections.Generic;
using MasterSM.Exceptions;
using MasterSM.PriorityManagement;

namespace MasterSM
{
    /// <summary>
    /// A sub state machine that can be used to create hierarchical state machines.
    /// </summary>
    /// <typeparam name="TStateId">The type of the state identifier.</typeparam>
    /// <typeparam name="TStateMachine">The type of this state machine.</typeparam>
    /// <typeparam name="TParentMachine">The type of the parent state machine.</typeparam>
    public abstract class SubStateMachine<TStateId, TParentMachine, TStateMachine> :
        IState<TStateId, TParentMachine>, IStateMachineImpl<TStateId, TStateMachine>, IMachineWithLayers<TStateId, TStateMachine>
        where TStateMachine : IStateMachine where TParentMachine : IStateMachine
    {
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TParentMachine}.Id"/>
        /// </summary>
        public TStateId Id { get; private set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TParentMachine}.Machine"/>
        /// </summary>
        [field: NonSerialized] public TParentMachine Machine { get; private set; }
        
        // public int Priority { get; private set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TParentMachine}.IsActive"/>
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TParentMachine}.Initialized"/>
        /// </summary>
        public bool Initialized { get; private set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TParentMachine}.Enabled"/>
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TParentMachine}.Extensions"/>
        /// </summary>
        public List<StateExtension<TStateId, TParentMachine>> Extensions { get; set; }
        
        private readonly BaseMachine<TStateId, TStateMachine> _baseMachine = new();
        public IState<TStateId, TStateMachine> CurrentState => _baseMachine.CurrentState;
        public IState<TStateId, TStateMachine> PreviousState => _baseMachine.PreviousState;

        private Dictionary<object, BaseMachine<TStateId, TStateMachine>> Layers { get; } = new();
        
        protected Dictionary<TStateId, IState<TStateId, TStateMachine>> States => _baseMachine.States;
        protected PriorityManager<TStateId> StatesOrder => _baseMachine.PriorityManager;
        protected int CurrentIndex => _baseMachine.CurrentIndex;
        
        public TStateId CurrentId => _baseMachine.CurrentId;
        
#if UNITY_EDITOR
        // Events for Custom Editor
        public event Action OnLayerAdded;
        public event Action OnLayerRemoved;
#endif
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.Initialize"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="machine"></param>
        /// <param name="priority"></param>
        public void Initialize(TStateId id, TParentMachine machine)//, int priority)
        {
            Id = id;
            Machine = machine;
            // Priority = priority;
            IsActive = false;
            Initialized = true;
            Enabled = true;
            
            _baseMachine.Machine = (TStateMachine)(IStateMachine)this;
        }
        
        public bool StateCanEnter() => CanEnter();
        public bool StateCanExit() => CanExit();

        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.StateOnCreated"/><br/>
        /// Call this on the implementation e.g. base.OnCreated();
        /// </summary>
        public void StateOnCreated()
        {
            OnCreated();
            _baseMachine.OnCreated(false);
            
            foreach (var layer in Layers.Values)
            {
                layer.OnCreated(false);
            }
            
        }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.StateOnEnter"/>
        /// Calls this on the implementation e.g. base.OnEnter();
        /// </summary>
        public void StateOnEnter()
        {
            _baseMachine.EnterMachine();
            OnEnter();
        }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.StateOnExit"/>
        /// Calls this on the implementation e.g. base.OnExit();
        /// </summary>
        public void StateOnExit()
        {
            _baseMachine.ExitMachine();
            OnExit();
        }

        /// <summary>
        /// Calls this on the implementation e.g. base.OnUpdate();
        /// </summary>
        public void StateOnUpdate()
        {
            _baseMachine.OnUpdate();
            
            foreach (var layer in Layers.Values)
                layer.OnUpdate();
            
            OnUpdate();
        }

        /// <summary>
        /// Calls this on the implementation e.g. base.OnFixedUpdate();
        /// </summary>
        public void StateOnFixedUpdate()
        {
            _baseMachine.OnFixedUpdate();
            
            foreach (var layer in Layers.Values)
                layer.OnFixedUpdate();
            
            OnFixedUpdate();
        }
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.StateCanEnter"/>
        /// </summary>
        /// <returns></returns>
        public abstract bool CanEnter();
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.StateCanExit"/>
        /// </summary>
        /// <returns>If true, the state can exit. If false, the state cannot exit.</returns>
        public virtual bool CanExit() => true;
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.StateOnCreated"/>
        /// </summary>
        public virtual void OnCreated() { }
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.StateOnEnter"/>
        /// </summary>
        public virtual void OnEnter() { }
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.StateOnExit"/>
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.StateOnUpdate"/>
        /// </summary>
        public virtual void OnUpdate() { }
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.StateOnFixedUpdate"/>
        /// </summary>
        public virtual void OnFixedUpdate() { }

        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.AddState(in TStateId,in MasterSM.IState{TStateId,TStateMachine},MasterSM.PriorityManagement.StatePriority{TStateId})"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="state"></param>
        /// <param name="statePriority"></param>
        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, StatePriority<TStateId> statePriority)
        {
            _baseMachine.AddState(id, state, statePriority);
        }

        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int priority = 0)
        {
            _baseMachine.AddState(id, state, priority);
        }

        public void AddState(in TStateId id, in IState<TStateId, TStateMachine> state, int group, int priority)
        {
            _baseMachine.AddState(id, state, group, priority);
        }

        public void AddState(in IState<TStateId, TStateMachine> state, StatePriority<TStateId> priority)
        {
            _baseMachine.AddState(state, priority);
        }

        public void AddState(StateGroup<TStateId, TStateMachine> group)
        {
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
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.ChangeState"/>
        /// </summary>
        /// <param name="newState">The identifier of the new state.</param>
        public void ChangeState(TStateId newState) => _baseMachine.ChangeState(newState);

        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.RevertToPreviousState"/>
        /// </summary>
        public void RevertToPreviousState() => _baseMachine.RevertToPreviousState();

        /// <summary>
        /// Adds a new layer to the state machine.
        /// The layers allow executing multiple state machines in parallel.
        /// </summary>
        /// <param name="layerId">The identifier of the layer.</param>
        /// <returns>Returns the new layer instance.</returns>
        /// <exception cref="MasterSMException">Thrown if a layer with the same identifier already exists.</exception>
        public BaseMachine<TStateId, TStateMachine> AddLayer(object layerId)
        {
            if (Layers.ContainsKey(layerId))
                throw ExceptionCreator.LayerIdAlreadyExists(layerId);
            
            var layer = new BaseMachine<TStateId, TStateMachine>
            {
                Machine = (TStateMachine)(IStateMachine)this
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
            
            Layers.Remove(layerId);

#if UNITY_EDITOR
            OnLayerRemoved?.Invoke();
#endif
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
    }
}