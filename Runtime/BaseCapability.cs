using UnityEngine;

namespace MasterSM
{
    public class BaseCapability<TStateId, TStateMachine> : MonoBehaviour
        where TStateMachine : IStateMachine
    {
        [Tooltip("The state machine that this capability belongs to")]
        public TStateMachine machine;
    }
}