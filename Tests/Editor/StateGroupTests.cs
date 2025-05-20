using System;
using System.Collections.Generic;
using System.Linq;
using MasterSM;
using NUnit.Framework;

namespace MasterSM.Tests.Editor
{
    public class StateGroupTests
    {
        private enum State
        {
            State1,
            State2,
            State3,
            State4,
            State5
        }
        
        private class StateMachine : BaseMachine<State, StateMachine> { }
        
        private class TestState : BaseState<State, StateMachine>
        {
            public bool CanEnterResult = true;
            
            public override bool CanEnter() => CanEnterResult;
        }
        
        private StateMachine _machine;
        private Dictionary<State, TestState> _states;
        private StateGroup<State, StateMachine> _stateGroup;

        [SetUp]
        public void SetUp()
        {
            _machine = new StateMachine();
            _states = new Dictionary<State, TestState>
            {
                { State.State1, new TestState() },
                { State.State2, new TestState() },
                { State.State3, new TestState() },
                { State.State4, new TestState() },
                { State.State5, new TestState() }
            };
            
            _stateGroup = new StateGroup<State, StateMachine>();
        }
        
        [Test]
        public void AddingStateGroup_ShouldAddAllStatesInGroup_WithCorrectPriorities()
        {
            // Add states to group
            _stateGroup.AddState(State.State1, _states[State.State1]);
            _stateGroup.AddState(State.State2, _states[State.State2]);
            _stateGroup.AddState(State.State3, _states[State.State3]);
            
            // Set base priority
            _stateGroup.basePriority = 10;
            
            // Add group to machine
            _machine.AddState(_stateGroup);
            
            // Check that states were added with correct priorities
            Assert.AreEqual(3, _machine.States.Count);
            
            // State priorities should be basePriority, basePriority+1, basePriority+2
            Assert.AreEqual(10, _machine.PriorityManager.PriorityFrom(State.State1).Priority);
            Assert.AreEqual(11, _machine.PriorityManager.PriorityFrom(State.State2).Priority);
            Assert.AreEqual(12, _machine.PriorityManager.PriorityFrom(State.State3).Priority);
        }
        
        [Test]
        public void StateGroup_ShouldRespectStateOrder_WhenPerformingTransitions()
        {
            _stateGroup.AddState(State.State1, _states[State.State1]);
            _stateGroup.AddState(State.State2, _states[State.State2]);
            _stateGroup.AddState(State.State3, _states[State.State3]);
            
            // Set all states to be enterable
            foreach (var state in _states.Values)
            {
                state.CanEnterResult = true;
            }
            
            _machine.AddState(_stateGroup);
            _machine.OnCreated();
            
            // Highest priority state should be active
            Assert.AreEqual(State.State3, _machine.CurrentId);
        }
        
        [Test]
        public void AddingMultipleStateGroups_ShouldMaintainCorrectPriorities()
        {
            var group1 = new StateGroup<State, StateMachine> { basePriority = 10 };
            var group2 = new StateGroup<State, StateMachine> { basePriority = 20 };
            
            group1.AddState(State.State1, _states[State.State1]);
            group1.AddState(State.State2, _states[State.State2]);
            
            group2.AddState(State.State3, _states[State.State3]);
            group2.AddState(State.State4, _states[State.State4]);
            
            _machine.AddState(group1);
            _machine.AddState(group2);
            
            // Check priorities
            Assert.AreEqual(10, _machine.PriorityManager.PriorityFrom(State.State1).Priority);
            Assert.AreEqual(11, _machine.PriorityManager.PriorityFrom(State.State2).Priority);
            Assert.AreEqual(20, _machine.PriorityManager.PriorityFrom(State.State3).Priority);
            Assert.AreEqual(21, _machine.PriorityManager.PriorityFrom(State.State4).Priority);
            
            // Enable all states
            foreach (var state in _states.Values)
            {
                state.CanEnterResult = true;
            }
            
            // Create machine and check highest priority state
            _machine.OnCreated();
            Assert.AreEqual(State.State4, _machine.CurrentId);
        }
        
        [Test]
        public void GetStatesByLowestPriority_ShouldReturnStatesInCorrectOrder()
        {
            // Add states to group in mixed order
            _stateGroup.AddState(State.State3, _states[State.State3]);
            _stateGroup.AddState(State.State1, _states[State.State1]);
            _stateGroup.AddState(State.State2, _states[State.State2]);
            
            // Get ordered states
            var orderedStates = _stateGroup.GetStates().ToArray();
            
            // Check order (should be the order they were added)
            Assert.AreEqual(State.State3, orderedStates[0].id);
            Assert.AreEqual(State.State1, orderedStates[1].id);
            Assert.AreEqual(State.State2, orderedStates[2].id);
        }
        
        [Test]
        public void RemovingState_FromStateGroup_ShouldUpdateMachineProperly()
        {
            _stateGroup.AddState(State.State1, _states[State.State1]);
            _stateGroup.AddState(State.State2, _states[State.State2]);
            _stateGroup.AddState(State.State3, _states[State.State3]);
            
            foreach (var state in _states.Values)
            {
                state.CanEnterResult = true;
            }

            _machine.AddState(_stateGroup);
            _machine.OnCreated();
            
            // Remove middle state
            _machine.RemoveState(State.State2);
            
            // Check remaining states
            Assert.AreEqual(2, _machine.States.Count);
            Assert.IsTrue(_machine.HasState(State.State1));
            Assert.IsTrue(_machine.HasState(State.State3));
            Assert.IsFalse(_machine.HasState(State.State2));
            
            // Current state should adjust
            Assert.AreEqual(State.State3, _machine.CurrentId);
        }
    }
}