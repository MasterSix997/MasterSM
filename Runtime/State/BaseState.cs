using System;
using System.Collections.Generic;

namespace MasterSM
{
    /// <summary>
    /// Base class for a state in a state machine.
    /// </summary>
    /// <typeparam name="TStateId">Type of the state id.</typeparam>
    /// <typeparam name="TStateMachine">Type of the state machine.</typeparam>
    public abstract class BaseState<TStateId, TStateMachine> : IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.Id"/>
        /// </summary>
        public TStateId Id { get; private set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.Machine"/>
        /// </summary>
        [field: NonSerialized] public TStateMachine Machine { get; private set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.Priority"/>
        /// </summary>
        public int Priority { get; private set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.IsActive"/>
        /// </summary>
        public bool IsActive { get; set; }
        
        private bool _initialized;

        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.Initialized"/>
        /// </summary>
        bool IState<TStateId, TStateMachine>.Initialized => _initialized;
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.Enabled"/>
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.Extensions"/>
        /// </summary>
        public List<StateExtension<TStateId, TStateMachine>> Extensions { get; set; }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.Initialize"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="machine"></param>
        /// <param name="priority"></param>
        public void Initialize(TStateId id, TStateMachine machine, int priority)
        {
            Id = id;
            Machine = machine;
            Priority = priority;
            IsActive = false;
            Enabled = true;
            _initialized = true;
        }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.CanEnter"/>
        /// </summary>
        /// <returns></returns>
        public abstract bool CanEnter();

        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.CanExit"/>
        /// </summary>
        /// <returns>If this state can be exited, returns true. Otherwise, returns false.</returns>
        public virtual bool CanExit() => true;
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.OnCreated"/>
        /// </summary>
        public virtual void OnCreated() { }
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.OnEnter"/>
        /// </summary>
        public virtual void OnEnter() { }
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.OnExit"/>
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.OnUpdate"/>
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// <inheritdoc cref="IState{TStateId, TStateMachine}.OnFixedUpdate"/>
        /// </summary>
        public virtual void OnFixedUpdate() { }
    }
}
