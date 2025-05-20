using System;
using System.Collections.Generic;
using MasterSM.Exceptions;
using UnityEngine;

namespace MasterSM.PriorityManagement
{
    /// <summary>
    /// Interface for priority resolution in state management
    /// </summary>
    /// <typeparam name="TStateId">The type of state identifier</typeparam>
    public interface IPriorityResolver<TStateId>
    {
        /// <summary>
        /// Determines if a state can be inserted at a specific index
        /// </summary>
        /// <returns>
        /// - ResolverResult.Insert: Insert at this index <br/>
        /// - ResolverResult.Skip: Do not insert at this index <br/>
        /// - ResolverResult.Unknown: No opinion (delegate to next resolver) <br/>
        /// </returns>
        ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId);
        
        /// <summary>
        /// Priority value for ordering between resolvers (higher values are evaluated first)
        /// </summary>
        int ResolverPriority { get; }
        
        /// <summary>
        /// Resolver description for debugging purposes
        /// </summary>
        string Description { get; }
    }
    
    /// <summary>
    /// Represents the result of a priority resolution attempt
    /// </summary>
    public enum ResolverResult
    {
        /// <summary>Should insert at this position</summary>
        Insert,
        /// <summary>Should not insert at this position</summary>
        Skip,
        /// <summary>No opinion, delegate to next resolver</summary>
        Unknown
    }

    /// <summary>
    /// Interface for providers of group and priority information
    /// </summary>
    public interface IPriorityGroupProvider
    {
        /// <summary>Gets the group identifier</summary>
        int Group { get; }
        /// <summary>Gets the priority value within the group</summary>
        int Priority { get; }
    }

    /// <summary>
    /// Default resolver based on group and priority values
    /// </summary>
    public class DefaultPriorityResolver<TStateId> : IPriorityResolver<TStateId>, IPriorityGroupProvider
    {
        private readonly int _group;
        private readonly int _priority;
        private readonly Func<TStateId, TStateId, int> _tiebreaker;

        public int Group => _group;

        public int Priority => _priority;
        
        public int ResolverPriority => 0; // Low priority to be executed after other resolvers

        public string Description => $"DefaultPriorityResolver(Group: {_group}, Priority: {_priority})";

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPriorityResolver{TStateId}"/> class
        /// </summary>
        /// <param name="group">The group identifier</param>
        /// <param name="priority">The priority value</param>
        /// <param name="tiebreaker">Optional tiebreaker function</param>
        public DefaultPriorityResolver(int group, int priority, Func<TStateId, TStateId, int> tiebreaker = null)
        {
            _group = group;
            _priority = priority;
            _tiebreaker = tiebreaker ?? DefaultTiebreaker;
        }
        
        [HideInCallstack]
        private int DefaultTiebreaker(TStateId a, TStateId b)
        {
            throw ExceptionCreator.SamePriority(a, b, _priority);
        }

        /// <summary>
        /// Determines if a state can be inserted at a specific index
        /// </summary>
        /// <param name="context">The priority manager context</param>
        /// <param name="index">The index to check</param>
        /// <param name="stateId">The state identifier</param>
        /// <returns>The result of the resolution</returns>
        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (index >= context.StatesCount)
                return ResolverResult.Insert;
            
            var currentPriority = context.PriorityFrom(index);
            
            // Fast path: if not DefaultPriorityResolver, delegate immediately
            if (currentPriority.Resolver is not DefaultPriorityResolver<TStateId> currentResolver)
                return ResolverResult.Unknown;
            
            // Compare groups first - most significant difference
            var groupComparison = _group.CompareTo(currentResolver._group);
            if (groupComparison != 0)
                return groupComparison > 0 ? ResolverResult.Insert : ResolverResult.Skip;
            
            // Same group, compare priorities
            var priorityComparison = _priority.CompareTo(currentResolver._priority);
            if (priorityComparison != 0)
                return priorityComparison > 0 ? ResolverResult.Insert : ResolverResult.Skip;
            
            // Only use tiebreaker if absolutely necessary
            return _tiebreaker(stateId, context.IdFrom(index)) > 0 
                ? ResolverResult.Insert 
                : ResolverResult.Skip;
        }
    }

    /// <summary>
    /// Resolver that ensures a state is placed after another specific state
    /// </summary>
    public class AfterStateResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly TStateId _after;
        private readonly Dictionary<TStateId, int> _statePositionCache;
        private int _lastContextSize;
        
        public int ResolverPriority => 100;
        public string Description => $"AfterStateResolver(After: {_after})";

        public AfterStateResolver(TStateId after)
        {
            _after = after;
            _statePositionCache = new Dictionary<TStateId, int>();
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (context.StatesCount != _lastContextSize)
            {
                _statePositionCache.Clear();
                _lastContextSize = context.StatesCount;
            }

            // Check if we're trying to insert after ourselves
            if (stateId.Equals(_after))
                return ResolverResult.Skip;

            // Use cached position if available
            if (!_statePositionCache.TryGetValue(_after, out var targetPosition))
            {
                for (var i = 0; i < context.StatesCount; i++)
                {
                    var currentId = context.IdFrom(i);
                    if (currentId.Equals(_after))
                    {
                        targetPosition = i;
                        _statePositionCache[_after] = i;
                        break;
                    }
                }
            }

            return index == targetPosition + 1 ? ResolverResult.Insert : ResolverResult.Skip;
        }
    }
    
    /// <summary>
    /// Resolver that ensures a state is placed before another specific state
    /// </summary>
    public class BeforeStateResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly TStateId _before;
        private readonly Dictionary<TStateId, int> _statePositionCache;
        private int _lastContextSize;
        
        public int ResolverPriority => 100;
        public string Description => $"BeforeStateResolver(Before: {_before})";

        public BeforeStateResolver(TStateId before)
        {
            _before = before;
            _statePositionCache = new Dictionary<TStateId, int>();
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (context.StatesCount != _lastContextSize)
            {
                _statePositionCache.Clear();
                _lastContextSize = context.StatesCount;
            }

            // Check if we're trying to insert before ourselves
            if (stateId.Equals(_before))
                return ResolverResult.Skip;

            // Use cached position if available
            if (!_statePositionCache.TryGetValue(_before, out var targetPosition))
            {
                for (var i = 0; i < context.StatesCount; i++)
                {
                    var currentId = context.IdFrom(i);
                    if (currentId.Equals(_before))
                    {
                        targetPosition = i;
                        _statePositionCache[_before] = i;
                        break;
                    }
                }
            }

            return index == targetPosition ? ResolverResult.Insert : ResolverResult.Skip;
        }
    }
    
    /// <summary>
    /// Resolver that ensures a state is placed at the beginning or end of the list
    /// </summary>
    public class BoundaryResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly bool _isFirst;  // true = first in the list, false = last in the list
        public int ResolverPriority => 200; // Very high priority
        public string Description => _isFirst ? "BoundaryResolver(First)" : "BoundaryResolver(Last)";

        public BoundaryResolver(bool isFirst)
        {
            _isFirst = isFirst;
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (_isFirst)
                return index == 0 ? ResolverResult.Insert : ResolverResult.Skip;
            else
                return index == context.StatesCount ? ResolverResult.Insert : ResolverResult.Skip;
        }
    }
    
    /// <summary>
    /// Resolver that ensures a state is placed at the beginning or end of a specific group
    /// </summary>
    public class GroupBoundaryResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly int _group;
        private readonly bool _highPriority;
        private readonly Dictionary<int, bool> _groupCache = new();
        
        public int ResolverPriority => 150;
        public string Description => _highPriority 
            ? $"GroupBoundaryResolver(Group: {_group}, First)" 
            : $"GroupBoundaryResolver(Group: {_group}, Last)";

        public GroupBoundaryResolver(int group, bool highPriority)
        {
            _group = group;
            _highPriority = highPriority;
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            // Reset cache if context changed
            if (_groupCache.Count > 100)
                _groupCache.Clear();
            
            if (_highPriority)
            {
                // First position is special case
                if (index == 0)
                {
                    return (context.StatesCount == 0 || CheckIsInGroup(context, 0)) 
                        ? ResolverResult.Insert 
                        : ResolverResult.Skip;
                }
                
                // Check if this is the start of the group
                var prevInGroup = index > 0 && CheckIsInGroup(context, index - 1);
                var currentInGroup = index < context.StatesCount && CheckIsInGroup(context, index);
                
                return (!prevInGroup && currentInGroup) ? ResolverResult.Insert : ResolverResult.Skip;
            }
            else
            {
                // Last position in group
                if (index >= context.StatesCount)
                {
                    return (index > 0 && CheckIsInGroup(context, index - 1)) 
                        ? ResolverResult.Insert 
                        : ResolverResult.Skip;
                }
                
                var prevInGroup = index > 0 && CheckIsInGroup(context, index - 1);
                var currentInGroup = CheckIsInGroup(context, index);
                
                return (prevInGroup && !currentInGroup) ? ResolverResult.Insert : ResolverResult.Skip;
            }
        }
        
        private bool CheckIsInGroup(PriorityManager<TStateId> context, int index)
        {
            if (_groupCache.TryGetValue(index, out var cached))
                return cached;
            
            var result = IsStateInGroup(context, index, _group);
            _groupCache[index] = result;
            return result;
        }
        
        private bool IsStateInGroup(PriorityManager<TStateId> context, int index, int targetGroup)
        {
            var priority = context.PriorityFrom(index);
            return priority.Resolver is DefaultPriorityResolver<TStateId> defaultResolver && 
                   defaultResolver.Group == targetGroup;
        }
    }
    
    /// <summary>
    /// Resolver that combines multiple resolvers with different combination strategies
    /// </summary>
    public class CompositeResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly List<IPriorityResolver<TStateId>> _resolvers;
        private readonly ResolverCombination _combination;
        private bool _isResolved;
        
        public int ResolverPriority { get; }
        public string Description => $"CompositeResolver({string.Join(" + ", _resolvers.ConvertAll(r => r.Description))})";

        /// <summary>
        /// Defines how multiple resolvers should be combined
        /// </summary>
        public enum ResolverCombination
        {
            /// <summary>Uses the first non-Unknown result</summary>
            First,
            /// <summary>All resolvers must return Insert</summary>
            All,
            /// <summary>Any resolver returning Insert is sufficient</summary>
            Any,
            /// <summary>Majority of resolvers must return Insert</summary>
            Majority
        }

        public CompositeResolver(
            List<IPriorityResolver<TStateId>> resolvers, 
            ResolverCombination combination = ResolverCombination.First, 
            int resolverPriority = 50)
        {
            _resolvers = new List<IPriorityResolver<TStateId>>(resolvers);
            _combination = combination;
            ResolverPriority = resolverPriority;
            
            // Pre-sort resolvers by priority
            if (_combination != ResolverCombination.First) return;
            _resolvers.Sort((a, b) => b.ResolverPriority.CompareTo(a.ResolverPriority));
            _isResolved = true;
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (!_isResolved && _combination == ResolverCombination.First)
            {
                _resolvers.Sort((a, b) => b.ResolverPriority.CompareTo(a.ResolverPriority));
                _isResolved = true;
            }

            return _combination switch
            {
                ResolverCombination.First => ResolveFirst(context, index, stateId),
                ResolverCombination.All => ResolveAll(context, index, stateId),
                ResolverCombination.Any => ResolveAny(context, index, stateId),
                ResolverCombination.Majority => ResolveMajority(context, index, stateId),
                _ => ResolverResult.Unknown
            };
        }

        private ResolverResult ResolveFirst(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            foreach (var resolver in _resolvers)
            {
                var result = resolver.CanInsertHere(context, index, stateId);
                if (result != ResolverResult.Unknown)
                    return result;
            }
            return ResolverResult.Unknown;
        }

        private ResolverResult ResolveAll(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            var hasUnknown = false;
            
            foreach (var resolver in _resolvers)
            {
                var result = resolver.CanInsertHere(context, index, stateId);
                
                if (result == ResolverResult.Skip)
                    return ResolverResult.Skip;
                
                hasUnknown |= result == ResolverResult.Unknown;
            }
            
            return hasUnknown ? ResolverResult.Unknown : ResolverResult.Insert;
        }

        private ResolverResult ResolveAny(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            var hasInsert = false;
            var allUnknown = true;
            
            foreach (var resolver in _resolvers)
            {
                var result = resolver.CanInsertHere(context, index, stateId);
                
                if (result == ResolverResult.Insert)
                    return ResolverResult.Insert; // Early return on first insert
                
                allUnknown &= result == ResolverResult.Unknown;
            }
            
            return allUnknown ? ResolverResult.Unknown : ResolverResult.Skip;
        }

        private ResolverResult ResolveMajority(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            var insertCount = 0;
            var skipCount = 0;
            var unknownCount = 0;
            
            foreach (var resolver in _resolvers)
            {
                switch (resolver.CanInsertHere(context, index, stateId))
                {
                    case ResolverResult.Insert:
                        if (++insertCount > _resolvers.Count / 2)
                            return ResolverResult.Insert;
                        break;
                    case ResolverResult.Skip:
                        if (++skipCount > _resolvers.Count / 2)
                            return ResolverResult.Skip;
                        break;
                    case ResolverResult.Unknown:
                    default:
                        unknownCount++;
                        break;
                }
            }
            
            if (unknownCount == _resolvers.Count)
                return ResolverResult.Unknown;
            
            return insertCount > skipCount ? ResolverResult.Insert : ResolverResult.Skip;
        }
    }
    
    /// <summary>
    /// Resolver that only applies when a specific condition is met
    /// </summary>
    public class ConditionalResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly IPriorityResolver<TStateId> _resolver;
        private readonly Func<PriorityManager<TStateId>, TStateId, bool> _condition;
        
        /// <summary>
        /// Gets the priority value for ordering between resolvers
        /// </summary>
        public int ResolverPriority { get; }

        /// <summary>
        /// Gets the resolver description for debugging purposes
        /// </summary>
        public string Description => $"ConditionalResolver({_resolver.Description})";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConditionalResolver{TStateId}"/> class
        /// </summary>
        /// <param name="resolver">The resolver to use when the condition is met</param>
        /// <param name="condition">The condition that must be met to apply the resolver</param>
        /// <param name="resolverPriority">The priority value for this resolver</param>
        public ConditionalResolver(
            IPriorityResolver<TStateId> resolver, 
            Func<PriorityManager<TStateId>, TStateId, bool> condition,
            int resolverPriority = 75)
        {
            _resolver = resolver;
            _condition = condition;
            ResolverPriority = resolverPriority;
        }

        /// <summary>
        /// Determines if a state can be inserted at a specific index
        /// </summary>
        /// <param name="context">The priority manager context</param>
        /// <param name="index">The index to check</param>
        /// <param name="stateId">The state identifier</param>
        /// <returns>The result of the resolution</returns>
        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (_condition(context, stateId))
                return _resolver.CanInsertHere(context, index, stateId);
            
            return ResolverResult.Unknown;
        }
    }
    
    /// <summary>
    /// Resolver that only applies when a specific condition is met
    /// </summary>
    public class RelativeGroupPositionResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly int _group;
        private readonly int _offsetFromStart;
        private Dictionary<int, bool> _groupCache;
        private int? _lastGroupStartIndex;
        private int _lastContextSize;
        
        public int ResolverPriority => 80;
        public string Description => $"RelativeGroupPositionResolver(Group: {_group}, Offset: {_offsetFromStart})";

        public RelativeGroupPositionResolver(int group, int offsetFromStart)
        {
            _group = group;
            _offsetFromStart = offsetFromStart;
            _groupCache = new Dictionary<int, bool>();
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (context.StatesCount != _lastContextSize)
            {
                _lastGroupStartIndex = null;
                _lastContextSize = context.StatesCount;
                if (_groupCache.Count > 100)
                    _groupCache.Clear();
            }

            var groupStartIndex = _lastGroupStartIndex ?? FindGroupStartIndex(context);
            if (groupStartIndex == -1)
                return ResolverResult.Unknown;
            
            var targetPosition = groupStartIndex + _offsetFromStart;
            return index == targetPosition ? ResolverResult.Insert : ResolverResult.Skip;
        }
        
        private int FindGroupStartIndex(PriorityManager<TStateId> context)
        {
            for (var i = 0; i < context.StatesCount; i++)
            {
                if (CheckIsInGroup(context, i))
                {
                    if (i == 0 || !CheckIsInGroup(context, i - 1))
                    {
                        _lastGroupStartIndex = i;
                        return i;
                    }
                }
            }
            return -1;
        }
        
        private bool CheckIsInGroup(PriorityManager<TStateId> context, int index)
        {
            if (_groupCache.TryGetValue(index, out var cached))
                return cached;
            
            var priority = context.PriorityFrom(index);
            var result = priority.Resolver is DefaultPriorityResolver<TStateId> defaultResolver && 
                         defaultResolver.Group == _group;
            
            _groupCache[index] = result;
            return result;
        }
    }
}