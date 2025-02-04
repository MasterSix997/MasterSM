using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MasterSM
{
    /// <summary>
    /// A state that is defined inline in code.
    /// </summary>
    /// <typeparam name="TStateId">The type of the state identifier.</typeparam>
    /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
    public class InlineState<TStateId, TStateMachine> : IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public TStateId Id { get; set; }
        public TStateMachine Machine { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        private bool _initialized;
        bool IState<TStateId, TStateMachine>.Initialized => _initialized;
        public bool Enabled { get; set; }

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
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public bool StateCanEnter() => canEnter();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public bool StateCanExit() => canExit == null || canExit();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void StateOnCreated() => onCreated?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void StateOnEnter() => onEnter?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void StateOnExit() => onExit?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void StateOnUpdate() => onUpdate?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void StateOnFixedUpdate() => onFixedUpdate?.Invoke();
    }
}