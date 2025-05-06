using System;
using MasterSM;
using NUnit.Framework;

namespace Tests.Editor
{
    public class StateExtensionTests
    {
        private enum State
        {
            State1,
            State2,
            State3
        }
        
        private class StateMachine : BaseMachine<State, StateMachine> { }

        private class TestStateExtension : StateExtension<State, StateMachine>
        {
            public bool CanEnterResult = true;
            public bool CanExitResult = true;
            
            public bool OnCreatedCalled;
            public bool OnEnterCalled;
            public bool OnExitCalled;
            public bool OnUpdateCalled;
            public bool OnFixedUpdateCalled;
            
            public override bool CanEnter() => CanEnterResult;
            public override bool CanExit() => CanExitResult;
            
            public override void OnCreated(IState<State, StateMachine> state)
            {
                base.OnCreated(state);
                OnCreatedCalled = true;
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                OnEnterCalled = true;
            }
            
            public override void OnExit()
            {
                base.OnExit();
                OnExitCalled = true;
            }
            
            public override void OnUpdate()
            {
                base.OnUpdate();
                OnUpdateCalled = true;
            }
            
            public override void OnFixedUpdate()
            {
                base.OnFixedUpdate();
                OnFixedUpdateCalled = true;
            }
        }
        
        private class StateWithExtension : BaseState<State, StateMachine>
        {
            public TestStateExtension Extension = new TestStateExtension();
            
            public bool CanEnterResult = true;
            public bool CanExitResult = true;
            
            public override bool CanEnter() => CanEnterResult;
            public override bool CanExit() => CanExitResult;
        }
        
        private class StateWithMultipleExtensions : BaseState<State, StateMachine>
        {
            public TestStateExtension Extension1 = new TestStateExtension();
            public TestStateExtension Extension2 = new TestStateExtension();
            public TestStateExtension Extension3 = new TestStateExtension();
            public override bool CanEnter() => true;
        }
        
        private StateMachine _machine;
        private StateWithExtension _stateWithExtension;
        private StateWithExtension _anotherStateWithExtension;

        [SetUp]
        public void SetUp()
        {
            _machine = new StateMachine();
            _stateWithExtension = new StateWithExtension();
            _anotherStateWithExtension = new StateWithExtension();
        }
        
        [Test]
        public void Extension_ShouldBeInitialized_WhenStateIsAdded()
        {
            _machine.AddState(State.State1, _stateWithExtension);
            
            Assert.IsNotNull(_stateWithExtension.Extensions);
            Assert.AreEqual(1, _stateWithExtension.Extensions.Count);
            Assert.AreEqual(_stateWithExtension.Extension, _stateWithExtension.Extensions[0]);
        }
        
        [Test]
        public void Extension_ShouldReceiveOnCreatedCall_WhenMachineIsCreated()
        {
            _machine.AddState(State.State1, _stateWithExtension);
            _stateWithExtension.CanEnterResult = true;
            
            _machine.OnCreated();
            
            Assert.IsTrue(_stateWithExtension.Extension.OnCreatedCalled);
        }
        
        [Test]
        public void Extension_ShouldPreventStateTransition_WhenCanEnterReturnsFalse()
        {
            _machine.AddState(State.State1, _stateWithExtension);
            _machine.AddState(State.State2, _anotherStateWithExtension, 1);
            
            _anotherStateWithExtension.CanEnterResult = true;
            _anotherStateWithExtension.Extension.CanEnterResult = false;
            
            _machine.OnCreated();
            
            Assert.AreEqual(_machine.CurrentState, _stateWithExtension);
        }
        
        [Test]
        public void Extension_ShouldPreventStateTransition_WhenCanExitReturnsFalse()
        {
            _machine.AddState(State.State1, _stateWithExtension);
            _machine.AddState(State.State2, _anotherStateWithExtension);
            
            _stateWithExtension.CanEnterResult = true;
            _stateWithExtension.Extension.CanEnterResult = true;
            
            _machine.OnCreated();
            
            Assert.AreEqual(_stateWithExtension, _machine.CurrentState);
            
            _stateWithExtension.Extension.CanExitResult = false;
            _anotherStateWithExtension.CanEnterResult = true;
            
            _machine.OnUpdate();
            
            // Should not transition because the extension prevents exit
            Assert.AreEqual(_stateWithExtension, _machine.CurrentState);
            Assert.IsFalse(_stateWithExtension.Extension.OnExitCalled);
        }
        
        [Test]
        public void Extension_ShouldReceiveLifecycleCalls_DuringStateTransitions()
        {
            _machine.AddState(State.State1, _stateWithExtension);
            _machine.AddState(State.State2, _anotherStateWithExtension, 1);
            
            _stateWithExtension.CanEnterResult = true;
            _anotherStateWithExtension.CanEnterResult = false;
            
            _machine.OnCreated();
            
            Assert.IsTrue(_stateWithExtension.Extension.OnCreatedCalled);
            Assert.IsTrue(_stateWithExtension.Extension.OnEnterCalled);
            
            _anotherStateWithExtension.CanEnterResult = true;
            
            _machine.OnUpdate();
            
            Assert.IsTrue(_stateWithExtension.Extension.OnExitCalled);
            Assert.IsTrue(_anotherStateWithExtension.Extension.OnEnterCalled);
        }
        
        [Test]
        public void Extension_ShouldReceiveUpdateCalls_WhenStateIsActive()
        {
            _machine.AddState(State.State1, _stateWithExtension);
            
            _stateWithExtension.CanEnterResult = true;
            
            _machine.OnCreated();
            _machine.OnUpdate();
            
            Assert.IsTrue(_stateWithExtension.Extension.OnUpdateCalled);
            
            _machine.OnFixedUpdate();
            
            Assert.IsTrue(_stateWithExtension.Extension.OnFixedUpdateCalled);
        }
        
        [Test]
        public void Extension_ShouldNotReceiveUpdateCalls_WhenDisabled()
        {
            _machine.AddState(State.State1, _stateWithExtension);
            
            _stateWithExtension.CanEnterResult = true;
            _stateWithExtension.Extension.enabled = false;
            
            _machine.OnCreated();
            _machine.OnUpdate();
            
            Assert.IsFalse(_stateWithExtension.Extension.OnUpdateCalled);
            
            _machine.OnFixedUpdate();
            
            Assert.IsFalse(_stateWithExtension.Extension.OnFixedUpdateCalled);
        }
        
        [Test]
        public void MultipleExtensions_ShouldAllBeInitialized()
        {
            var stateWithMultipleExtensions = new StateWithMultipleExtensions();
            _machine.AddState(State.State1, stateWithMultipleExtensions);
            
            Assert.IsNotNull(stateWithMultipleExtensions.Extensions);
            Assert.AreEqual(3, stateWithMultipleExtensions.Extensions.Count);
        }
    }
}