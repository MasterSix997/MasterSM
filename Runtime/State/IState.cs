using System.Collections.Generic;
using JetBrains.Annotations;

namespace MasterSM
{
    public interface IState<TStateId, TStateMachine>
        where TStateMachine : IStateMachine
    {
        /// <summary>
        /// The identifier of this state machine.
        /// </summary>
        public TStateId Id { get; }
        /// <summary>
        /// The parent state machine.
        /// </summary>
        public TStateMachine Machine { get; }
        /// <summary>
        /// The priority of this state machine.
        /// </summary>
        public int Priority { get; }
        /// <summary>
        /// Whether this state machine is currently active.
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Whether this state machine has been initialized.
        /// </summary>
        public bool Initialized { get; }
        /// <summary>
        /// Whether this state machine is enabled.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// A list of extensions for this state machine.
        /// The extensions are used to add functionality to the state.
        /// </summary>
        [CanBeNull] public List<StateExtension<TStateId, TStateMachine>> Extensions { get; set; }
        
        /// <summary>
        /// Initializes the state machine.
        /// This method is called by the state machine when it is added to the state machine list.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="machine"></param>
        /// <param name="priority"></param>
        public void Initialize(TStateId id, TStateMachine machine, int priority);
        
        /// <summary>
        /// Checks whether this state can be entered.
        /// This method is called by State Machine to try to make a transition to this state
        /// </summary>
        /// <returns>If this state can be entered, returns true. Otherwise, returns false.</returns>
        public bool CanEnter();

        /// <summary>
        /// Checks whether this state can be exited.
        /// This method is called by State Machine to see if you can leave this state, or if it is "locked".
        /// </summary>
        /// <returns></returns>
        public bool CanExit()
        {
            return true; 
        }
        
        /// <summary>
        /// This method is called by the State Machine when the state is created.
        /// </summary>
        public void OnCreated() { }
        
        /// <summary>
        /// This method is called by the State Machine when a transition from another state to this.
        /// </summary>
        public void OnEnter() { }
        
        /// <summary>
        /// This method is called by the State Machine when a transition from this state to another,
        /// or when the sub state machine is exited.
        /// </summary>
        public void OnExit() { }

        public void OnUpdate() { }
        
        public void OnFixedUpdate() { }
    }
}