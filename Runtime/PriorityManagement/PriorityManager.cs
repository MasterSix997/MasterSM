using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        
        private readonly Dictionary<TStateId, int> _stateIndices = new();

        public readonly List<ChangeOrderEvent> OnChangeOrderEvent = new();
        
        public int StatesCount => StatesOrder.Count;
        
        [HideInCallstack]
        public void AddState(in TStateId stateId, in StatePriority<TStateId> priority)
        {
            if (_stateIndices.ContainsKey(stateId))
                throw new Exception($"State with id '{stateId}' already exists");
            
            var index = ResolveOrder(stateId, priority);
            StatesOrder.Insert(index, stateId);
            Priorities.Insert(index, priority);
            
            UpdateIndicesAfterInsert(index);
            _stateIndices[stateId] = index;
        }

        [HideInCallstack]
        public void AddState(in TStateId stateId, in IPriorityResolver<TStateId> resolver)
        {
            var priority = new StatePriority<TStateId>(resolver);
            AddState(stateId, priority);
        }

        public void RemoveState(in TStateId stateId)
        {
            if (!_stateIndices.TryGetValue(stateId, out var index))
                return;
            
            StatesOrder.RemoveAt(index);
            Priorities.RemoveAt(index);
            
            _stateIndices.Remove(stateId);
            UpdateIndicesAfterRemove(index);
        }

        private int ResolveOrder(TStateId stateId, in StatePriority<TStateId> priority)
        {
            for (var i = 0; i <= StatesCount; i++)
            {
                var result = priority.Resolver.CanInsertHere(this, i, stateId);
                
                if (result == ResolverResult.Insert)
                {
                    return i;
                }
                else if (result == ResolverResult.Skip)
                {
                    continue;
                }
                // If it is unknown, continue the verification with next resolvers
            }

            // If no resolver has a definitive opinion, insert at the end
            return StatesCount;
        }

        private void UpdateIndicesAfterInsert(int insertedIndex)
        {
            foreach (var key in _stateIndices.Keys.ToList())
            {
                var currentIndex = _stateIndices[key];
                if (currentIndex >= insertedIndex)
                {
                    _stateIndices[key] = currentIndex + 1;
                }
            }
            IncreaseStateIndex(insertedIndex);
        }
        
        private void UpdateIndicesAfterRemove(int removedIndex)
        {
            foreach (var key in _stateIndices.Keys.ToList())
            {
                var currentIndex = _stateIndices[key];
                if (currentIndex > removedIndex)
                {
                    _stateIndices[key] = currentIndex - 1;
                }
            }
            DecreaseStateIndex(removedIndex);
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
                if (value > index)
                    changeOrder.NewValue(value - 1);
            }
        }

        public TStateId IdFrom(int idIndex) => StatesOrder[idIndex];
        
        public StatePriority<TStateId> PriorityFrom(int idIndex) => Priorities[idIndex];
        
        public StatePriority<TStateId> PriorityFrom(in TStateId stateId)
        {
            if (_stateIndices.TryGetValue(stateId, out var index))
            {
                return Priorities[index];
            }
            
            throw new KeyNotFoundException($"Estado {stateId} não encontrado no gerenciador de prioridades.");
        }
        
        public int IndexFrom(in TStateId stateId)
        {
            if (_stateIndices.TryGetValue(stateId, out var index))
            {
                return index;
            }

            throw new Exception($"No state with id '{stateId}' present");
        }
        
        /// <summary>
        /// Recalculate the entire order of states (useful if resolvers change their behavior)
        /// </summary>
        public void RecalculateOrder()
        {
            var tempStates = new List<(TStateId id, StatePriority<TStateId> priority)>();
            
            for (var i = 0; i < StatesCount; i++)
            {
                tempStates.Add((StatesOrder[i], Priorities[i]));
            }
            
            StatesOrder.Clear();
            Priorities.Clear();
            _stateIndices.Clear();
            
            foreach (var (id, priority) in tempStates)
            {
                AddState(id, priority);
            }
        }
        
        public string DebugOrder()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Priority-sorted states:");
            
            for (var i = 0; i < StatesCount; i++)
            {
                var state = StatesOrder[i];
                var priority = Priorities[i];
                
                sb.AppendLine($"{i}: {state} - Resolver: {priority.Resolver.Description}");
            }
            
            return sb.ToString();
        }
    }
}