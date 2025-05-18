using MasterSM.PriorityManagement;
using NUnit.Framework;

namespace MasterSM.Tests.Editor
{
    public class PriorityTests
    {
        private enum StateId
        {
            State1,
            State2,
            State3,
            State4,
            State5,
            State6,
            State7,
            State8,
            State10,
        }

        private PriorityManager<StateId> _priorityManager;
        
        [SetUp]
        public void SetUp()
        {
            _priorityManager = new PriorityManager<StateId>();
        }

        [Test]
        public void AddingStates_WithSimplePriorities_ShouldAddStates()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(1));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(2));
            _priorityManager.AddState(StateId.State4, new StatePriority<StateId>(3));
            
            Assert.AreEqual(_priorityManager.IdFrom(0), StateId.State4);
            Assert.AreEqual(_priorityManager.IdFrom(1), StateId.State3);
            Assert.AreEqual(_priorityManager.IdFrom(2), StateId.State2);
            Assert.AreEqual(_priorityManager.IdFrom(3), StateId.State1);
        }
        
        [Test]
        public void AddingStates_WithGroupPriorities_ShouldAddStates()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(0, 1));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(1, 1));
            _priorityManager.AddState(StateId.State4, new StatePriority<StateId>(1, 0));
            
            Assert.AreEqual(_priorityManager.IdFrom(0), StateId.State3);
            Assert.AreEqual(_priorityManager.IdFrom(1), StateId.State4);
            Assert.AreEqual(_priorityManager.IdFrom(2), StateId.State2);
            Assert.AreEqual(_priorityManager.IdFrom(3), StateId.State1);
        }
        
        [Test]
        public void AddingState_AfterAnotherState_ShouldAddState()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(0, 1));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(0, 2));
            _priorityManager.AddState(StateId.State4, new StatePriority<StateId>(1, 0));
            _priorityManager.AddState(StateId.State5, new StatePriority<StateId>(1, 1));
            _priorityManager.AddState(StateId.State6, new AfterStateResolver<StateId>(StateId.State4));
            
            Assert.AreEqual(_priorityManager.IdFrom(0), StateId.State5);
            Assert.AreEqual(_priorityManager.IdFrom(1), StateId.State4);
            Assert.AreEqual(_priorityManager.IdFrom(2), StateId.State6);
            Assert.AreEqual(_priorityManager.IdFrom(3), StateId.State3);
            Assert.AreEqual(_priorityManager.IdFrom(4), StateId.State2);
            Assert.AreEqual(_priorityManager.IdFrom(5), StateId.State1);
        }

        [Test]
        public void AddingStates_WithNegativePriorities_ShouldAddStates()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(1));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(-1));
            _priorityManager.AddState(StateId.State4, new StatePriority<StateId>(-2));
            
            Assert.AreEqual(_priorityManager.IdFrom(0), StateId.State2);
            Assert.AreEqual(_priorityManager.IdFrom(1), StateId.State1);
            Assert.AreEqual(_priorityManager.IdFrom(2), StateId.State3);
            Assert.AreEqual(_priorityManager.IdFrom(3), StateId.State4);
        }
        
        [Test]
        public void AddingStates_WithNegativeGroupPriority_ShouldAddStates()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(1));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(-1, 0));
            _priorityManager.AddState(StateId.State4, new StatePriority<StateId>(-1, 1));
            
            Assert.AreEqual(_priorityManager.IdFrom(0), StateId.State2);
            Assert.AreEqual(_priorityManager.IdFrom(1), StateId.State1);
            Assert.AreEqual(_priorityManager.IdFrom(2), StateId.State4);
            Assert.AreEqual(_priorityManager.IdFrom(3), StateId.State3);
        }
    }
}