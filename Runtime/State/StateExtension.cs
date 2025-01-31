using System;

namespace MasterSM
{
    /// <summary>
    /// Abstract class for extending the functionality of a state.
    /// </summary>
    /// <typeparam name="TStateId"></typeparam>
    /// <typeparam name="TStateMachine"></typeparam>
    [Serializable]
    public abstract class StateExtension<TStateId, TStateMachine> 
        where TStateMachine : IStateMachine
    {
        /// <summary>
        /// The state machine that this extension belongs to.
        /// </summary>
        public TStateMachine Machine { get; set; }
        
        /// <summary>
        /// Whether this extension is enabled or not.
        /// </summary>
        public bool enabled = true;
        
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.OnCreated"/>
        /// </summary>
        /// <param name="state">The state that this extension belongs to.</param>
        public virtual void OnCreated(IState<TStateId, TStateMachine> state) { }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.OnEnter"/>
        /// </summary>
        public virtual void OnEnter() { }
        /// <summary>
        /// <inheritdoc cref="IState{TStateId,TStateMachine}.OnExit"/>
        /// </summary>
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
    }
}