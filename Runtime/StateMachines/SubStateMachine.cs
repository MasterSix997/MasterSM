using System;
using System.Collections.Generic;

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
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TParentMachine}.Priority"/>
        /// </summary>
        public int Priority { get; private set; }
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
        protected List<TStateId> StatesOrder => _baseMachine.StatesOrder;
        protected int CurrentIndex => _baseMachine.CurrentIndex;
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.Initialize"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="machine"></param>
        /// <param name="priority"></param>
        public void Initialize(TStateId id, TParentMachine machine, int priority)
        {
            Id = id;
            Machine = machine;
            Priority = priority;
            IsActive = false;
            Initialized = true;
            Enabled = true;
            
            _baseMachine.Machine = (TStateMachine)(IStateMachine)this;
        }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.CanEnter"/>
        /// </summary>
        /// <returns></returns>
        public abstract bool CanEnter();
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.CanExit"/>
        /// </summary>
        /// <returns>If true, the state can exit. If false, the state cannot exit.</returns>
        public virtual bool CanExit() => true;

        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.OnCreated"/><br/>
        /// Call this on the implementation e.g. base.OnCreated();
        /// </summary>
        public virtual void OnCreated()
        {
            _baseMachine.OnCreated();
            // _baseMachine.EnterNewState();
            
            foreach (var layer in Layers.Values)
            {
                layer.OnCreated();
                // layer.EnterNewState();
            }
        }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.OnEnter"/>
        /// Calls this on the implementation e.g. base.OnEnter();
        /// </summary>
        public virtual void OnEnter()
        {
            _baseMachine.EnterMachine();
        }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.OnExit"/>
        /// Calls this on the implementation e.g. base.OnExit();
        /// </summary>
        public virtual void OnExit()
        {
            _baseMachine.ExitMachine();
        }

        /// <summary>
        /// Calls this on the implementation e.g. base.OnUpdate();
        /// </summary>
        public virtual void OnUpdate()
        {
            _baseMachine.OnUpdate();
            
            foreach (var layer in Layers.Values)
                layer.OnUpdate();
        }

        /// <summary>
        /// Calls this on the implementation e.g. base.OnFixedUpdate();
        /// </summary>
        public virtual void OnFixedUpdate()
        {
            _baseMachine.OnFixedUpdate();
            
            foreach (var layer in Layers.Values)
                layer.OnFixedUpdate();
        }

        /// <summary>
        /// <inheritdoc cref="BaseMachine{TStateId,TStateMachine}.AddState"/>
        /// </summary>
        /// <param name="id">The identifier of the state.</param>
        /// <param name="state">The state instance.</param>
        /// <param name="priority">The priority of the state.</param>
        public void AddState(TStateId id, IState<TStateId, TStateMachine> state, int priority = 0) => _baseMachine.AddState(id, state, priority);
        
        public void AddState(StateGroup<TStateId, TStateMachine> group)
        {
            var priority = group.basePriority;
            foreach (var state in group.GetStatesByLowestPriority())
            {
                AddState(state.id, state.state, priority++);
            }
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
        /// <exception cref="ArgumentException">Thrown if a layer with the same identifier already exists.</exception>
        public BaseMachine<TStateId, TStateMachine> AddLayer(object layerId)
        {
            if (Layers.ContainsKey(layerId))
                throw new ArgumentException($"Layer with Id {layerId} already exists.");
            
            var layer = new BaseMachine<TStateId, TStateMachine>
            {
                Machine = (TStateMachine)(IStateMachine)this
            };
            Layers.Add(layerId, layer);
            return layer;
        }

        /// <summary>
        /// Removes a layer from the state machine.
        /// </summary>
        /// <param name="layerId">The identifier of the layer to remove.</param>
        /// <exception cref="ArgumentException">Thrown if the layer does not exist.</exception>
        public void RemoveLayer(object layerId)
        {
            if(!Layers.TryGetValue(layerId, out var layer))
                throw new ArgumentException($"Layer with Id {layerId} does not exist.");

            if (layer.CurrentState != null)
                layer.ChangeState(default);
            
            Layers.Remove(layerId);
        }

        /// <summary>
        /// Gets a layer by its identifier.
        /// </summary>
        /// <param name="layerId">The identifier of the layer.</param>
        /// <returns>The layer instance.</returns>
        /// <exception cref="ArgumentException">Thrown if the layer does not exist.</exception>
        public BaseMachine<TStateId, TStateMachine> GetLayer(object layerId)
        {
            if (!Layers.TryGetValue(layerId, out var layer))
                throw new ArgumentException($"Layer with Id {layerId} not found.");

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