using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MasterSM
{
    public interface IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public TStateId Id { get; }
        public TStateMachine Machine { get; }
        public int Priority { get; }
        public bool IsActive { get; set; }
        public bool Initialized { get; }
        public bool Enabled { get; set; }
        [CanBeNull] public List<StateExtension<TStateId, TStateMachine>> Extensions { get; set; }
        
        public void Initialize(TStateId id, TStateMachine machine, int priority);
        
        public bool CanEnter();

        public bool CanExit()
        {
            return true; 
        }
        
        public void OnCreated() { }
        
        public void OnEnter() { }
        
        public void OnExit() { }

        public void OnUpdate() { }
        
        public void OnFixedUpdate() { }
    }

    public abstract class BaseState<TStateId, TStateMachine> : IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        public TStateId Id { get; set; }
        public TStateMachine Machine { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        
        private bool _initialized;

        bool IState<TStateId, TStateMachine>.Initialized => _initialized;

        public bool Enabled { get; set; }
        public List<StateExtension<TStateId, TStateMachine>> Extensions { get; set; }

        public void Initialize(TStateId id, TStateMachine machine, int priority)
        {
            Id = id;
            Machine = machine;
            Priority = priority;
            IsActive = false;
            Enabled = true;
            _initialized = true;
        }

        public abstract bool CanEnter();

        public virtual bool CanExit() => true;
        
        public virtual void OnCreated() { }
        
        public virtual void OnEnter() { }
        
        public virtual void OnExit() { }

        public virtual void OnUpdate() { }

        public virtual void OnFixedUpdate() { }
    }

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
        public bool CanEnter() => canEnter();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public bool CanExit() => canExit == null || canExit();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnCreated() => onCreated?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnEnter() => onEnter?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnExit() => onExit?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnUpdate() => onUpdate?.Invoke();
        /// <summary>
        /// Don't call this method, it's called by the state machine.
        /// </summary>
        public void OnFixedUpdate() => onFixedUpdate?.Invoke();
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class StateTabAttribute : Attribute
    {
        public string Name { get; }

        public StateTabAttribute(string name)
        {
            Name = name;
        }
    }
}
