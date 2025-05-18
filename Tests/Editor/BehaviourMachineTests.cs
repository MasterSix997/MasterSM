using MasterSM;
using NUnit.Framework;
using UnityEngine;

namespace MasterSM.Tests.Editor
{
    public class BehaviourMachineTests
    {
        private enum State
        {
            State1,
            State2,
            State3
        }

        private class StateMachine : BehaviourMachine<State, StateMachine>
        {
        }
        
        private class StateTests : BaseState<State, StateMachine>
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
        private StateTests _state1;
        private StateTests _state2;
        private StateTests _state3;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject();
            _machine = go.AddComponent<StateMachine>();
            _state1 = new StateTests();
            _state2 = new StateTests();
            _state3 = new StateTests();
        }

        [Test]
        public void AddState_ShouldAddStateToMachine()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);
            
            Assert.IsTrue(_machine.HasState(State.State1));
            Assert.IsTrue(_machine.HasState(State.State2));
            Assert.IsTrue(_machine.HasState(State.State3));
        }
        
        [Test]
        public void RemoveState_ShouldRemoveStateFromMachine()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);
            
            _machine.RemoveState(State.State2);
            
            Assert.IsTrue(_machine.HasState(State.State1));
            Assert.IsFalse(_machine.HasState(State.State2));
            Assert.IsTrue(_machine.HasState(State.State3));
        }

        [Test]
        public void ChangeState_ShouldChangeState()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);
            
            _machine.ChangeState(State.State2);

            Assert.AreEqual(_state2, _machine.CurrentState);
            Assert.IsTrue(_state2.Entered);
        }

        [Test]
        public void AddLayer_ShouldAddLayerToMachine()
        {
            var layer = _machine.AddLayer(State.State1);
            
            Assert.IsTrue(_machine.HasLayer(State.State1));
            Assert.NotNull(layer);
        }

        [Test]
        public void RemoveLayer_ShouldRemoveLayerFromMachine()
        {
            _machine.AddLayer(State.State1);
            _machine.RemoveLayer(State.State1);

            Assert.IsFalse(_machine.HasLayer(State.State1));
        }

        [Test]
        public void AddStateToLayer_ShouldAddStateToLayer()
        {
            var layer = _machine.AddLayer(State.State1);
            layer.AddState(State.State1, _state1, 0);
            
            Assert.IsTrue(layer.HasState(State.State1));
        }
    }
}