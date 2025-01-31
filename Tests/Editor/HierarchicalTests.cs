using MasterSM;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class HierarchicalTests
    {
        private enum State
        {
            State1,
            State2,
            State3,
            Machine1,
            Machine2,
            Machine3
        }

        private class StateMachine : BehaviourMachine<State, StateMachine>
        {
            public int TestValue = 5;

            public float TestMethod() => Mathf.PI;

            public void OnCreated()
            {
                Awake();
                Start();
            }
        }

        private class SubMachine : SubStateMachine<State, StateMachine, SubMachine>
        {
            public bool CanEnterToggle;
            public bool CanExitToggle = true;
            
            public bool Entered { get; private set; }
            public bool Exited { get; private set; }
            
            public override bool CanEnter() => CanEnterToggle;
            public override bool CanExit() => CanExitToggle;
            
            public override void OnEnter() => Entered = true;
            public override void OnExit() => Exited = true;
        }
        
        private class StateTests : BaseState<State, SubMachine>
        {
            public bool CanEnterToggle;
            public bool CanExitToggle = true;
            
            public bool Entered { get; private set; }
            public bool Exited { get; private set; }
            
            public override bool CanEnter() => CanEnterToggle;
            public override bool CanExit() => CanExitToggle;
            
            public override void OnEnter() => Entered = true;
            public override void OnExit() => Exited = true;
        }
        
        private StateMachine _machine;
        private SubMachine _subMachine1;
        private SubMachine _subMachine2;
        private StateTests _state1;
        private StateTests _state2;
        private StateTests _state3;
        private StateTests _state4;
        private StateTests _state5;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject();
            _machine = go.AddComponent<StateMachine>();
            _subMachine1 = new SubMachine();
            _subMachine2 = new SubMachine();
            _state1 = new StateTests();
            _state2 = new StateTests();
            _state3 = new StateTests();
            _state4 = new StateTests();
            _state5 = new StateTests();
        }

        #region Insertion Tests
        [Test]
        public void AddStatesToMachine_ShouldAddSubMachines()
        {
            _machine.AddState(State.Machine1, _subMachine1, 0);
            _machine.AddState(State.Machine2, _subMachine2, 1);
            
            Assert.AreEqual(_subMachine1, _machine.GetState(State.Machine1));
            Assert.AreEqual(_subMachine2, _machine.GetState(State.Machine2));
        }
        
        [Test]
        public void AddStatesToSubMachine_ShouldAddStates()
        {
            _subMachine1.AddState(State.State1, _state1, 0);
            _subMachine1.AddState(State.State2, _state2, 1);
            
            Assert.AreEqual(_state1, _subMachine1.GetState(State.State1));
            Assert.AreEqual(_state2, _subMachine1.GetState(State.State2));
        }

        [Test]
        public void RemoveStateFromMachine_ShouldRemoveSubMachine()
        {
            _machine.AddState(State.Machine1, _subMachine1, 0);
            _machine.AddState(State.Machine2, _subMachine2, 1);
            
            _machine.RemoveState(State.Machine1);
            
            Assert.IsFalse(_machine.HasState(State.Machine1));
            Assert.AreEqual(_machine.GetState(State.Machine2), _subMachine2);
        }
        #endregion
    }
}