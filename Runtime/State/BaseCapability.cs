using System;
using UnityEngine;

namespace MasterSM
{
    /// <summary>
    /// Base class for implementing capabilities in a state machine.
    /// </summary>
    /// <typeparam name="TStateId">Type of the state id.</typeparam>
    /// <typeparam name="TStateMachine">Type of the state machine.</typeparam>
    public class BaseCapability<TStateId, TStateMachine> : MonoBehaviour
        where TStateMachine : IStateMachine
    {
        /// <summary>
        /// The state machine that this capability belongs to.
        /// </summary>
        [Tooltip("The state machine that this capability belongs to")]
        public TStateMachine machine;

        protected virtual void Reset()
        {
            TryFindStateMachine();
        }
        
        private void TryFindStateMachine()
        {
            if (machine != null)
                return;
            
            machine = GetComponent<TStateMachine>();
            if (machine != null)
                return;

            machine = GetComponentInChildren<TStateMachine>();
            if (machine != null)
                return;
            
            machine = GetComponentInParent<TStateMachine>();
            if (machine != null)
                return;
        }
    }
}