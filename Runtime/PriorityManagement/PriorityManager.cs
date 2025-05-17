using System;
using System.Collections.Generic;
using System.Linq;

namespace MasterSM.PriorityManagement
{
    public struct ChangeOrderEvent
    {
        public Func<int> Get;
        public Action<int> NewValue;
    }
    
    public class PriorityManager<TStateId>
    {
        public readonly List<TStateId> StatesOrder = new();
        public readonly List<StatePriority<TStateId>> Priorities = new();

        public readonly List<ChangeOrderEvent> OnChangeOrderEvent = new();
        public int StatesCount => StatesOrder.Count;
        
        public void AddState(in TStateId stateId, in StatePriority<TStateId> priority)
        {
            var index = ResolveOrder(priority);
            StatesOrder.Insert(index, stateId);
            Priorities.Insert(index, priority);
            IncreaseStateIndex(index);
        }

        public void AddState(in TStateId stateId, in IPriorityResolver<TStateId> resolver)
        {
            var priority = new StatePriority<TStateId>(resolver);
            AddState(stateId, priority);
        }

        public void RemoveState(in TStateId stateId)
        {
            var index = IndexFrom(stateId);
            StatesOrder.RemoveAt(index);
            Priorities.RemoveAt(index);
            DecreaseStateIndex(index);
        }

        private int ResolveOrder(in StatePriority<TStateId> priority)
        {
            for (int i = 0; i < StatesOrder.Count; i++)
            {
                // Resolver System
                var resolverResult = priority.Resolver?.CanInsertHere(this, i);
                if (resolverResult.HasValue)
                {
                    if (resolverResult.Value)
                        return i;
                    continue;
                }
                
                // Normal resolver
                if (priority.Group < PriorityFrom(i).Group)
                    continue;

                if (priority.Priority < PriorityFrom(i).Priority)
                    continue;


                return i;
            }

            return StatesOrder.Count;
        }

        private void IncreaseStateIndex(int index)
        {
            foreach (var changeOrder in OnChangeOrderEvent)
            {
                var value = changeOrder.Get();
                if (value >= index)
                    changeOrder.NewValue(value + 1);
            }
        }
        
        private void DecreaseStateIndex(int index)
        {
            foreach (var changeOrder in OnChangeOrderEvent)
            {
                var value = changeOrder.Get();
                if (value >= index)
                    changeOrder.NewValue(value - 1);
            }
        }

        public TStateId IdFrom(int idIndex) => StatesOrder[idIndex];
        
        public StatePriority<TStateId> PriorityFrom(int idIndex) => Priorities[idIndex];
        public StatePriority<TStateId> PriorityFrom(in TStateId stateId) => Priorities[IndexFrom(stateId)];
        
        public int IndexFrom(in TStateId stateId) => StatesOrder.IndexOf(stateId);
    }
}