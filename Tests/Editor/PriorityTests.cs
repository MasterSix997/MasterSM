using System;
using System.Collections.Generic;
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

        [Test]
        public void AddingState_WithBeforeResolver_ShouldAddState()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(0, 1));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(1, 0));
            _priorityManager.AddState(StateId.State4, StatePriority<StateId>.Before(StateId.State2));
            
            Assert.AreEqual(StateId.State3, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State4, _priorityManager.IdFrom(1));
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(2));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(3));
        }

        [Test]
        public void AddingState_WithBoundaryResolver_First_ShouldAddStateAtStart()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(0, 1));
            _priorityManager.AddState(StateId.State3, StatePriority<StateId>.Boundary(true));
            
            Assert.AreEqual(StateId.State3, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(1));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(2));
        }

        [Test]
        public void AddingState_WithBoundaryResolver_Last_ShouldAddStateAtEnd()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(0, 1));
            _priorityManager.AddState(StateId.State3, StatePriority<StateId>.Boundary(false));
            
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(1));
            Assert.AreEqual(StateId.State3, _priorityManager.IdFrom(2));
        }

        [Test]
        public void AddingState_WithGroupBoundaryResolver_First_ShouldAddStateAtGroupStart()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(1, 0));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(1, 1));
            _priorityManager.AddState(StateId.State4, StatePriority<StateId>.GroupBoundary(1, true));
            
            Assert.AreEqual(StateId.State4, _priorityManager.IdFrom(0)); // State4 deve ser o primeiro do grupo 1
            Assert.AreEqual(StateId.State3, _priorityManager.IdFrom(1));
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(2));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(3));
        }

        [Test]
        public void AddingState_WithCompositeResolver_AllStrategy_ShouldRespectAllConditions()
        {
            var resolvers = new List<IPriorityResolver<StateId>>
            {
                new DefaultPriorityResolver<StateId>(1, 0),
                new AfterStateResolver<StateId>(StateId.State1)
            };
            
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(1, 1));
            _priorityManager.AddState(StateId.State3, StatePriority<StateId>.Composite(
                resolvers, 
                CompositeResolver<StateId>.ResolverCombination.All
            ));
            
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(1));
            Assert.AreEqual(StateId.State3, _priorityManager.IdFrom(2));
        }

        [Test]
        public void AddingState_WithRelativeGroupPosition_ShouldAddStateAtCorrectPosition()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(1, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(1, 1));
            _priorityManager.AddState(StateId.State3, new StatePriority<StateId>(1, 2));
            _priorityManager.AddState(StateId.State4, StatePriority<StateId>.GroupPosition(1, 1));
            
            Assert.AreEqual(StateId.State3, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State4, _priorityManager.IdFrom(1));
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(2));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(3));
        }

        [Test]
        public void AddingState_WithConditionalResolver_ShouldRespectCondition()
        {
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(0, 0));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(0, 1));
            
            var baseResolver = new DefaultPriorityResolver<StateId>(1, 0);
            Func<PriorityManager<StateId>,StateId,bool> condition = (manager, stateId) => 
                manager.StatesCount < 3; // Only apply if less than 3 states
                
            _priorityManager.AddState(StateId.State3, StatePriority<StateId>.Conditional(baseResolver, condition));
            
            Assert.AreEqual(StateId.State3, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(1));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(2));
        }

        [Test]
        public void RecalculateOrder_WhenResolversChange_ShouldReorderStates()
        {
            var resolver1 = new DefaultPriorityResolver<StateId>(0, 1);
            var resolver2 = new DefaultPriorityResolver<StateId>(0, 2);
            
            _priorityManager.AddState(StateId.State1, new StatePriority<StateId>(resolver1));
            _priorityManager.AddState(StateId.State2, new StatePriority<StateId>(resolver2));
            
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(1));

            // Change resolver1's priority to be higher than resolver2
            var newResolver1 = new DefaultPriorityResolver<StateId>(0, 3);
            _priorityManager.Priorities[_priorityManager.IndexFrom(StateId.State1)] = 
                new StatePriority<StateId>(newResolver1);
            
            _priorityManager.RecalculateOrder();
            
            Assert.AreEqual(StateId.State1, _priorityManager.IdFrom(0));
            Assert.AreEqual(StateId.State2, _priorityManager.IdFrom(1));
        }
    }
}