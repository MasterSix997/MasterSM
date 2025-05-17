using System;
using MasterSM;
using NUnit.Framework;

namespace MasterSM.Tests.Editor
{
    public class BaseMachineTests
    {
        private enum State
        {
            State1,
            State2,
            State3
        }
        
        private class StateMachine : BaseMachine<State, StateMachine> { }
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
            _machine = new StateMachine();
            _state1 = new StateTests();
            _state2 = new StateTests();
            _state3 = new StateTests();
        }
        
        #region Creation Tests

        [Test]
        public void AddState_ShouldAddStateToMachine()
        {
            _machine.AddState(State.State1, _state1);
            _machine.AddState(State.State2, _state2);
            _machine.AddState(State.State3, _state3);
            
            Assert.AreEqual(3, _machine.States.Count);
            Assert.AreEqual(_state1, _machine.States[State.State1]);
            Assert.AreEqual(_state2, _machine.States[State.State2]);
            Assert.AreEqual(_state3, _machine.States[State.State3]);
        }
        
        [Test]
        public void RemoveState_ShouldRemoveStateFromMachine()
        {
            _machine.AddState(State.State1, _state1);
            _machine.AddState(State.State2, _state2);
            _machine.AddState(State.State3, _state3);
            
            _machine.RemoveState(State.State2);
            
            Assert.AreEqual(2, _machine.States.Count);
            Assert.AreEqual(_state1, _machine.States[State.State1]);
            Assert.AreEqual(_state3, _machine.States[State.State3]);
        }

        [Test]
        public void OnCreate_CurrentState_ShouldBeHighestPriorityWithCanEnterState()
        {
            _machine.AddState(State.State1, _state1);
            _machine.AddState(State.State2, _state2);
            _machine.AddState(State.State3, _state3);
            
            _state2.CanEnterToggle = true;
            
            _machine.OnCreated();
            
            Assert.AreEqual(_state2, _machine.CurrentState);
        }

        [Test]
        public void OnCreate_WhenNoStateCanEnter_CurrentState_ShouldBeNull()
        {
            _machine.AddState(State.State1, _state1);
            _machine.AddState(State.State2, _state2);
            _machine.AddState(State.State3, _state3);
            
            _machine.OnCreated();
            
            Assert.IsNull(_machine.CurrentState);
        }
        #endregion

        #region Transition Tests
        [Test]
        public void GreaterPriorityState_AllowsEnter_ShouldTransitionToGreaterPriorityState()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);
            
            _state1.CanEnterToggle = true;
            
            _machine.OnCreated();
            _state3.CanEnterToggle = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(_state3, _machine.CurrentState);
            Assert.AreEqual(_state1, _machine.PreviousState);
            Assert.IsTrue(_state1.Exited);
            Assert.IsTrue(_state3.Entered);
        }

        [Test]
        public void LowerPriorityState_AllowsEnter_ShouldNotPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);
            
            _state2.CanEnterToggle = true;
            
            _machine.OnCreated();
            _state1.CanEnterToggle = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(_state2, _machine.CurrentState);
            Assert.IsFalse(_state1.Exited);
            Assert.IsFalse(_state2.Exited);
            Assert.IsTrue(_state2.Entered);
        }
        
        [Test]
        public void NoChange_InEnterLogic_ShouldNotPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);
            
            _state1.CanEnterToggle = true;
            _state2.CanEnterToggle = true;
            _state3.CanEnterToggle = true;
            
            _machine.OnCreated();
            _machine.OnUpdate();
            
            Assert.AreEqual(_state3, _machine.CurrentState);
            Assert.IsTrue(_state3.Entered);
            Assert.IsFalse(_state3.Exited);
        }
        
        [Test]
        public void CurrentStateDoesAllowEnter_WhenAllStatesDoNotAllowEnter_ShouldNotPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);

            _state2.CanEnterToggle = true;
            
            _machine.OnCreated();
            _state2.CanEnterToggle = false;
            _machine.OnUpdate();
            
            Assert.AreEqual(_state2, _machine.CurrentState);
            Assert.IsTrue(_state2.Entered);
            Assert.IsFalse(_state2.Exited);
            Assert.IsFalse(_state1.Entered);
            Assert.IsFalse(_state3.Entered);
        }

        [Test]
        public void CurrentStateDoesAllowEnter_WhenLowerPriorityStateAllowsEnter_ShouldPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);

            _state2.CanEnterToggle = true;
            
            _machine.OnCreated();
            _state2.CanEnterToggle = false;
            _state1.CanEnterToggle = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(_state1, _machine.CurrentState);
            Assert.AreEqual(_state2, _machine.PreviousState);
            Assert.IsTrue(_state1.Entered);
            Assert.IsTrue(_state2.Exited);
            Assert.IsFalse(_state3.Entered);
        }

        [Test]
        public void CurrentStateDoesAllowEnter_WhenGreaterPriorityStateAllowsEnter_ShouldPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);

            _state2.CanEnterToggle = true;
            
            _machine.OnCreated();
            _state2.CanEnterToggle = false;
            _state3.CanEnterToggle = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(_state3, _machine.CurrentState);
            Assert.AreEqual(_state2, _machine.PreviousState);
            Assert.IsTrue(_state3.Entered);
            Assert.IsTrue(_state2.Exited);
            Assert.IsFalse(_state1.Entered);
        }

        [Test]
        public void CurrentStateDoesNotAllowExit_WhenGreaterPriorityStateAllowsEnter_ShouldNotPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);

            _state2.CanEnterToggle = true;
            _state2.CanExitToggle = false;
            
            _machine.OnCreated();
            _state3.CanEnterToggle = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(_state2, _machine.CurrentState);
            Assert.IsFalse(_state2.Exited);
            Assert.IsFalse(_state3.Entered);
        }

        [Test]
        public void CurrentStateDoesNotAllowExit_WhenLowerPriorityStateAllowsEnter_ShouldNotPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);

            _state2.CanEnterToggle = true;
            _state2.CanExitToggle = false;
            
            _machine.OnCreated();
            _state2.CanEnterToggle = false;
            _state1.CanEnterToggle = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(_state2, _machine.CurrentState);
            Assert.IsFalse(_state2.Exited);
            Assert.IsFalse(_state1.Entered);
        }
        
        [Test]
        public void ChangeState_ShouldPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);

            _state1.CanEnterToggle = true;
            _state2.CanEnterToggle = true;
            _state3.CanEnterToggle = true;
            
            _machine.OnCreated();
            _state2.CanEnterToggle = false;
            _state3.CanEnterToggle = false;
            _machine.ChangeState(State.State1);
            _machine.OnUpdate();
            
            Assert.AreEqual(_state1, _machine.CurrentState);
            Assert.AreEqual(_state3, _machine.PreviousState);
            Assert.IsTrue(_state1.Entered);
            Assert.IsTrue(_state3.Exited);
            Assert.IsFalse(_state2.Entered);
        }

        [Test]
        public void ChangeState_WhenNoStateCanEnter_ShouldPerformTransition()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            _machine.AddState(State.State3, _state3, 2);
            
            _machine.OnCreated();
            _machine.ChangeState(State.State1);
            _machine.OnUpdate();
            
            Assert.AreEqual(_state1, _machine.CurrentState);
            Assert.AreEqual(null, _machine.PreviousState);
            Assert.IsTrue(_state1.Entered);
            Assert.IsFalse(_state2.Entered);
            Assert.IsFalse(_state3.Entered);
        }

        [Test]
        public void ChangeState_ToUnlottedState_CurrentState_ShouldBeNull()
        {
            _machine.AddState(State.State1, _state1, 0);
            _machine.AddState(State.State2, _state2, 1);
            
            _state1.CanEnterToggle = true;
            
            _machine.OnCreated();
            _machine.ChangeState(State.State3);
            
            Assert.IsNull(_machine.CurrentState);
            Assert.AreEqual(_state1, _machine.PreviousState);
            Assert.IsTrue(_state1.Exited);
            Assert.IsFalse(_state3.Entered);
        }
        #endregion

        #region Execution Tests
        [Test]
        public void OnUpdate_WithNoState_ShouldNotThrowException()
        {
            _machine.OnCreated();
            _machine.OnUpdate();
            _machine.OnUpdate();
            
            Assert.Pass();
        }

        [Test]
        public void OnFixedUpdate_WithNoState_ShouldNotThrowException()
        {
            _machine.OnCreated();
            _machine.OnFixedUpdate();
            _machine.OnFixedUpdate();
            
            Assert.Pass();
        }

        [Test]
        public void GetInvalidState_ShouldThrowException()
        {
            Assert.Throws<ArgumentException>(() => _machine.GetState(State.State1));
        }
        #endregion
    }
}