using System;

namespace AdvancedSM
{
    [Serializable]
    public abstract class StateExtension<TStateId, TStateMachine> 
        where TStateMachine : IStateMachine
    {
        public TStateMachine Machine { get; set; }
        // public TExtendedState State { get; set; }
        
        public bool enabled = true;
        
        public virtual void OnCreated(IState<TStateId, TStateMachine> state) { }
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        // public virtual void BeforeEnter() { }
        // public virtual void AfterEnter() { }
        // public virtual void BeforeExit() { }
        // public virtual void AfterExit() { }
        // public virtual void BeforeUpdate() { }
        // public virtual void AfterUpdate() { }
        // public virtual void BeforeFixedUpdate() { }
        // public virtual void AfterFixedUpdate() { }
    }
}