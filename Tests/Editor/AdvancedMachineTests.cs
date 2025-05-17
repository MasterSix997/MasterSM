using System.Collections.Generic;
using MasterSM;
using NUnit.Framework;

namespace MasterSM.Tests.Editor
{
    public class AdvancedMachineTests
    {
        private enum State
        {
            Idle,
            Running,
            Jumping,
            Falling,
            Landing,
            Crouching
        }
        
        private class StateMachine : BaseMachine<State, StateMachine> { }
        
        private class TestState : BaseState<State, StateMachine>
        {
            public bool CanEnterResult = true;
            public bool CanExitResult = true;
            
            public int CreatedCallCount;
            public int EnterCallCount;
            public int ExitCallCount;
            public int UpdateCallCount;
            public int FixedUpdateCallCount;
            
            public override bool CanEnter() => CanEnterResult && Enabled;
            public override bool CanExit() => CanExitResult;
            
            public override void OnCreated()
            {
                base.OnCreated();
                CreatedCallCount++;
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                EnterCallCount++;
            }
            
            public override void OnExit()
            {
                base.OnExit();
                ExitCallCount++;
            }
            
            public override void OnUpdate()
            {
                base.OnUpdate();
                UpdateCallCount++;
            }
            
            public override void OnFixedUpdate()
            {
                base.OnFixedUpdate();
                FixedUpdateCallCount++;
            }
        }
        
        private StateMachine _machine;
        private Dictionary<State, TestState> _states;

        [SetUp]
        public void SetUp()
        {
            _machine = new StateMachine();
            _states = new Dictionary<State, TestState>
            {
                { State.Idle, new TestState() },
                { State.Running, new TestState() },
                { State.Jumping, new TestState() },
                { State.Falling, new TestState() },
                { State.Landing, new TestState() },
                { State.Crouching, new TestState() }
            };
            
            // Add states with different priorities
            _machine.AddState(State.Idle, _states[State.Idle], 0);
            _machine.AddState(State.Running, _states[State.Running], 1);
            _machine.AddState(State.Jumping, _states[State.Jumping], 2);
            _machine.AddState(State.Falling, _states[State.Falling], 3);
            _machine.AddState(State.Landing, _states[State.Landing], 4);
            _machine.AddState(State.Crouching, _states[State.Crouching], 5);
        }
        
        [Test]
        public void EnableDisableStates_ShouldAffectTransitions()
        {
            // Enable only two states
            foreach (var state in _states.Values)
            {
                state.Enabled = false;
                state.CanEnterResult = true;
            }
            
            _states[State.Idle].Enabled = true;
            _states[State.Running].Enabled = true;
            
            // Create machine
            _machine.OnCreated();
            
            // Only Running should be active (higher priority)
            Assert.AreEqual(State.Running, _machine.CurrentId);
            
            // Disable Running
            _states[State.Running].Enabled = false;
            _machine.OnUpdate();
            
            // Idle should become active
            Assert.AreEqual(State.Idle, _machine.CurrentId);
            
            // Enable a higher priority state
            _states[State.Jumping].Enabled = true;
            _machine.OnUpdate();
            
            // Jumping should become active
            Assert.AreEqual(State.Jumping, _machine.CurrentId);
        }
        
        [Test]
        public void PrioritySystem_ComplexTransitionScenario()
        {
            foreach (var state in _states.Values)
            {
                state.Enabled = true;
                state.CanEnterResult = false;
            }
            
            _states[State.Idle].CanEnterResult = true;
            _machine.OnCreated();
            
            Assert.AreEqual(State.Idle, _machine.CurrentId);
            
            _states[State.Running].CanEnterResult = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(State.Running, _machine.CurrentId);
            
            _states[State.Jumping].CanEnterResult = true;
            _states[State.Falling].CanEnterResult = true;
            _machine.OnUpdate();
            
            Assert.AreEqual(State.Falling, _machine.CurrentId);
            
            // Disable Falling, enable Landing
            _states[State.Falling].CanEnterResult = false;
            _states[State.Landing].CanEnterResult = true;
            _machine.OnUpdate();
            
            // Landing should be active
            Assert.AreEqual(State.Landing, _machine.CurrentId);
            
            // Lock Landing (can't exit)
            _states[State.Landing].CanExitResult = false;
            
            // Enable Crouching (higher priority)
            _states[State.Crouching].CanEnterResult = true;
            _machine.OnUpdate();
            
            // Landing should still be active (can't exit)
            Assert.AreEqual(State.Landing, _machine.CurrentId);
            
            // Unlock Landing
            _states[State.Landing].CanExitResult = true;
            _machine.OnUpdate();
            
            // Crouching should now be active
            Assert.AreEqual(State.Crouching, _machine.CurrentId);
        }
        
        [Test]
        public void RevertToPreviousState_ShouldWork()
        {
            foreach (var state in _states.Values)
            {
                state.Enabled = true;
                state.CanEnterResult = true;
            }
            
            _machine.OnCreated();
            Assert.AreEqual(State.Crouching, _machine.CurrentId);
            _machine.ChangeState(State.Running);
            
            // Check state
            Assert.AreEqual(State.Running, _machine.CurrentId);
            Assert.AreEqual(State.Crouching, _machine.PreviousState.Id);
            
            _machine.RevertToPreviousState();
            
            // Should be back to Crouching
            Assert.AreEqual(State.Crouching, _machine.CurrentId);
            Assert.AreEqual(State.Running, _machine.PreviousState.Id);
        }
        
        [Test]
        public void LifecycleMethods_ShouldBeCalledCorrectly()
        {
            // Enable only Idle
            foreach (var state in _states.Values)
            {
                state.Enabled = false;
                state.CanEnterResult = true;
            }
            
            _states[State.Idle].Enabled = true;
            
            _machine.OnCreated();
            
            // Idle should have OnCreated and OnEnter called
            Assert.AreEqual(1, _states[State.Idle].CreatedCallCount);
            Assert.AreEqual(1, _states[State.Idle].EnterCallCount);
            Assert.AreEqual(0, _states[State.Idle].ExitCallCount);
            
            // Update several times
            for (int i = 0; i < 3; i++)
            {
                _machine.OnUpdate();
                _machine.OnFixedUpdate();
            }
            
            // Check update counts
            Assert.AreEqual(3, _states[State.Idle].UpdateCallCount);
            Assert.AreEqual(3, _states[State.Idle].FixedUpdateCallCount);
            
            // Enable Running
            _states[State.Running].Enabled = true;
            _machine.OnUpdate();
            
            // Idle should have exited, Running should have entered
            Assert.AreEqual(1, _states[State.Idle].ExitCallCount);
            Assert.AreEqual(1, _states[State.Running].EnterCallCount);
            
            // Update a few more times
            for (int i = 0; i < 2; i++)
            {
                _machine.OnUpdate();
                _machine.OnFixedUpdate();
            }
            
            // Check update counts again
            Assert.AreEqual(3, _states[State.Idle].UpdateCallCount); // No more updates
            Assert.AreEqual(3, _states[State.Idle].FixedUpdateCallCount);
            Assert.AreEqual(3, _states[State.Running].UpdateCallCount); // 1 from transition + 2 updates
            Assert.AreEqual(2, _states[State.Running].FixedUpdateCallCount);
        }
        
        [Test]
        public void ExitAndEnterMachine_ShouldWorkCorrectly()
        {
            foreach (var state in _states.Values)
            {
                state.Enabled = false;
            }
            
            _states[State.Idle].Enabled = true;
            _states[State.Idle].CanEnterResult = true;
            
            _machine.OnCreated();
            
            // Should be in Idle
            Assert.AreEqual(State.Idle, _machine.CurrentId);
            Assert.IsTrue(_states[State.Idle].IsActive);
            
            _machine.ExitMachine();
            
            // State should be inactive but still current
            Assert.AreEqual(State.Idle, _machine.CurrentId);
            Assert.IsFalse(_states[State.Idle].IsActive);
            Assert.AreEqual(1, _states[State.Idle].ExitCallCount);
            
            // Enter machine again
            _machine.EnterMachine();
            
            // State should be active again
            Assert.AreEqual(State.Idle, _machine.CurrentId);
            Assert.IsTrue(_states[State.Idle].IsActive);
            Assert.AreEqual(2, _states[State.Idle].EnterCallCount);
        }
        
        [Test]
        public void RemovingCurrentState_ShouldTransitionToNextValidState()
        {
            // Enable all states
            foreach (var state in _states.Values)
            {
                state.Enabled = true;
                state.CanEnterResult = true;
            }
            
            _machine.OnCreated();
            Assert.AreEqual(State.Crouching, _machine.CurrentId);
            
            _machine.RemoveState(State.Crouching);
            Assert.AreEqual(State.Landing, _machine.CurrentId);
            
            _machine.RemoveState(State.Landing);
            _machine.RemoveState(State.Falling);
            
            Assert.AreEqual(State.Jumping, _machine.CurrentId);
        }
    }
}