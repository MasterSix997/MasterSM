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
        
        // Dicionário para garantir acesso rápido via ID
        private readonly Dictionary<TStateId, int> _stateIndices = new();

        public readonly List<ChangeOrderEvent> OnChangeOrderEvent = new();
        public int StatesCount => StatesOrder.Count;
        
        [HideInCallstack]
        public void AddState(in TStateId stateId, in StatePriority<TStateId> priority)
        {
            // Remover primeiro se já existir (para garantir reordenação consistente)
            if (_stateIndices.ContainsKey(stateId))
            {
                RemoveState(stateId);
            }
            
            var index = ResolveOrder(stateId, priority);
            StatesOrder.Insert(index, stateId);
            Priorities.Insert(index, priority);
            
            // Atualizar índices no dicionário
            UpdateIndicesAfterInsert(index);
            
            // Registrar no dicionário
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
            {
                return; // Estado não encontrado
            }
            
            StatesOrder.RemoveAt(index);
            Priorities.RemoveAt(index);
            
            // Remover do dicionário
            _stateIndices.Remove(stateId);
            
            // Atualizar índices no dicionário
            UpdateIndicesAfterRemove(index);
        }

        private int ResolveOrder(TStateId stateId, in StatePriority<TStateId> priority)
        {
            // Para cada índice possível, verificar se o estado deve ser inserido aqui
            for (int i = 0; i <= StatesCount; i++)
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
                // Se for Unknown, continuar a verificação com próximos resolvedores
            }

            // Se nenhum resolvedor tem uma opinião definitiva, inserir no final
            return StatesCount;
        }

        private void UpdateIndicesAfterInsert(int insertedIndex)
        {
            // Atualizar índices no dicionário para todos os estados após o inserido
            foreach (var key in _stateIndices.Keys.ToList())
            {
                var currentIndex = _stateIndices[key];
                if (currentIndex >= insertedIndex)
                {
                    _stateIndices[key] = currentIndex + 1;
                }
            }
            
            // Atualizar eventos externos
            IncreaseStateIndex(insertedIndex);
        }
        
        private void UpdateIndicesAfterRemove(int removedIndex)
        {
            // Atualizar índices no dicionário para todos os estados após o removido
            foreach (var key in _stateIndices.Keys.ToList())
            {
                var currentIndex = _stateIndices[key];
                if (currentIndex > removedIndex)
                {
                    _stateIndices[key] = currentIndex - 1;
                }
            }
            
            // Atualizar eventos externos
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
            
            return -1; // Não encontrado
        }
        
        // Método para recalcular toda a ordem de estados (útil se os resolvedores mudarem de comportamento)
        public void RecalculateOrder()
        {
            var tempStates = new List<(TStateId id, StatePriority<TStateId> priority)>();
            
            // Armazenar todos os estados e prioridades
            for (int i = 0; i < StatesCount; i++)
            {
                tempStates.Add((StatesOrder[i], Priorities[i]));
            }
            
            // Limpar as listas
            StatesOrder.Clear();
            Priorities.Clear();
            _stateIndices.Clear();
            
            // Readicionar todos os estados
            foreach (var (id, priority) in tempStates)
            {
                AddState(id, priority);
            }
        }
        
        // Método utilitário para depuração
        public string DebugOrder()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Estados ordenados por prioridade:");
            
            for (int i = 0; i < StatesCount; i++)
            {
                var state = StatesOrder[i];
                var priority = Priorities[i];
                
                sb.AppendLine($"{i}: {state} - Resolvedor: {priority.Resolver.Description}");
            }
            
            return sb.ToString();
        }
    }
}