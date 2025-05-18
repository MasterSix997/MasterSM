using System;
using System.Collections.Generic;
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
        
        private int DefaultTiebreaker(TStateId a, TStateId b)
        {
            throw new Exception($"State '{a.ToString()}' has the same priority as '{b.ToString()}'");
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
            {
                return ResolverResult.Insert; // If it's the last index, insert here
            }
            
            var currentPriority = context.PriorityFrom(index);
            
            if (currentPriority.Resolver is not DefaultPriorityResolver<TStateId>)
            {
                return ResolverResult.Unknown; // Delegate to other resolvers
            }
            
            var currentResolver = (DefaultPriorityResolver<TStateId>)currentPriority.Resolver;
            
            if (_group > currentResolver._group)
            {
                return ResolverResult.Insert;
            }
            else if (_group < currentResolver._group)
            {
                return ResolverResult.Skip;
            }
            
            if (_priority > currentResolver._priority)
            {
                return ResolverResult.Insert;
            }
            else if (_priority < currentResolver._priority)
            {
                return ResolverResult.Skip;
            }
            
            int tiebreakerResult = _tiebreaker(stateId, context.IdFrom(index));
            if (tiebreakerResult > 0)
            {
                return ResolverResult.Insert;
            }
            
            return ResolverResult.Skip;
        }
    }

    /// <summary>
    /// Resolver that ensures a state is placed after another specific state
    /// </summary>
    public class AfterStateResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly TStateId _after;
        public int ResolverPriority => 100; // High priority
        public string Description => $"AfterStateResolver(After: {_after})";

        public AfterStateResolver(TStateId after)
        {
            _after = after;
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (index > 0 && context.IdFrom(index - 1).Equals(_after))
            {
                return ResolverResult.Insert;
            }
            if (index > 0 && context.IdFrom(index - 1).Equals(stateId))
            {
                return ResolverResult.Skip; // Avoid inserting after itself
            }
            
            return ResolverResult.Unknown; // No opinion for other indices
        }
    }
    
    /// <summary>
    /// Resolver that ensures a state is placed before another specific state
    /// </summary>
    public class BeforeStateResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly TStateId _before;
        public int ResolverPriority => 100; // High priority
        public string Description => $"BeforeStateResolver(Before: {_before})";

        public BeforeStateResolver(TStateId before)
        {
            _before = before;
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            if (index < context.StatesCount && context.IdFrom(index).Equals(_before))
            {
                return ResolverResult.Insert;
            }
            if (index < context.StatesCount && context.IdFrom(index).Equals(stateId))
            {
                return ResolverResult.Skip; // Avoid inserting before itself
            }
            
            return ResolverResult.Unknown; // No opinion for other indices
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
            {
                return index == 0 ? ResolverResult.Insert : ResolverResult.Skip;
            }
            else
            {
                return index == context.StatesCount ? ResolverResult.Insert : ResolverResult.Skip;
            }
        }
    }
    
    /// <summary>
    /// Resolver that ensures a state is placed at the beginning or end of a specific group
    /// </summary>
    public class GroupBoundaryResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly int _group;
        private readonly bool _isFirst;  // true = first in the group, false = last in the group
        public int ResolverPriority => 150; // High priority, but below the global BoundaryResolver
        public string Description => _isFirst 
            ? $"GroupBoundaryResolver(Group: {_group}, First)" 
            : $"GroupBoundaryResolver(Group: {_group}, Last)";

        public GroupBoundaryResolver(int group, bool isFirst)
        {
            _group = group;
            _isFirst = isFirst;
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            bool isValidPosition = false;
            
            if (_isFirst)
            {
                if (index == 0)
                {
                    isValidPosition = context.StatesCount == 0 || IsStateInGroup(context, 0, _group);
                }
                else if (index < context.StatesCount)
                {
                    isValidPosition = !IsStateInGroup(context, index - 1, _group) && 
                                     IsStateInGroup(context, index, _group);
                }
                else
                {
                    isValidPosition = !IsStateInGroup(context, index - 1, _group);
                }
            }
            else
            {
                if (index >= context.StatesCount)
                {
                    isValidPosition = index > 0 && IsStateInGroup(context, index - 1, _group);
                }
                else
                {
                    isValidPosition = index > 0 && 
                                     IsStateInGroup(context, index - 1, _group) && 
                                     !IsStateInGroup(context, index, _group);
                }
            }
            
            return isValidPosition ? ResolverResult.Insert : ResolverResult.Skip;
        }
        
        private bool IsStateInGroup(PriorityManager<TStateId> context, int index, int targetGroup)
        {
            var priority = context.PriorityFrom(index);
            if (priority.Resolver is DefaultPriorityResolver<TStateId> defaultResolver)
            {
                var groupField = typeof(DefaultPriorityResolver<TStateId>).GetField("_group", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (groupField != null)
                {
                    int group = (int)groupField.GetValue(defaultResolver);
                    return group == targetGroup;
                }
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Resolver that combines multiple resolvers with different combination strategies
    /// </summary>
    public class CompositeResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly List<IPriorityResolver<TStateId>> _resolvers;
        private readonly ResolverCombination _combination;
        
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
            _resolvers = resolvers;
            _combination = combination;
            ResolverPriority = resolverPriority;
        }

        public ResolverResult CanInsertHere(PriorityManager<TStateId> context, int index, TStateId stateId)
        {
            _resolvers.Sort((a, b) => b.ResolverPriority.CompareTo(a.ResolverPriority));
            
            if (_combination == ResolverCombination.First)
            {
                foreach (var resolver in _resolvers)
                {
                    var result = resolver.CanInsertHere(context, index, stateId);
                    if (result != ResolverResult.Unknown)
                    {
                        return result;
                    }
                }
                
                return ResolverResult.Unknown;
            }
            else if (_combination == ResolverCombination.All)
            {
                bool hasUnknown = false;
                
                foreach (var resolver in _resolvers)
                {
                    var result = resolver.CanInsertHere(context, index, stateId);
                    
                    if (result == ResolverResult.Skip)
                    {
                        return ResolverResult.Skip;
                    }
                    
                    if (result == ResolverResult.Unknown)
                    {
                        hasUnknown = true;
                    }
                }
                
                return hasUnknown ? ResolverResult.Unknown : ResolverResult.Insert;
            }
            else if (_combination == ResolverCombination.Any)
            {
                bool hasInsert = false;
                bool allUnknown = true;
                
                foreach (var resolver in _resolvers)
                {
                    var result = resolver.CanInsertHere(context, index, stateId);
                    
                    if (result == ResolverResult.Insert)
                    {
                        hasInsert = true;
                    }
                    
                    if (result != ResolverResult.Unknown)
                    {
                        allUnknown = false;
                    }
                }
                
                if (allUnknown)
                {
                    return ResolverResult.Unknown;
                }
                
                return hasInsert ? ResolverResult.Insert : ResolverResult.Skip;
            }
            else if (_combination == ResolverCombination.Majority)
            {
                int insertCount = 0;
                int skipCount = 0;
                int unknownCount = 0;
                
                foreach (var resolver in _resolvers)
                {
                    var result = resolver.CanInsertHere(context, index, stateId);
                    
                    if (result == ResolverResult.Insert)
                    {
                        insertCount++;
                    }
                    else if (result == ResolverResult.Skip)
                    {
                        skipCount++;
                    }
                    else
                    {
                        unknownCount++;
                    }
                }
                
                if (unknownCount == _resolvers.Count)
                {
                    return ResolverResult.Unknown;
                }
                
                int definiteOpinions = insertCount + skipCount;
                if (definiteOpinions > 0 && insertCount > skipCount)
                {
                    return ResolverResult.Insert;
                }
                
                return ResolverResult.Skip;
            }
            
            return ResolverResult.Unknown;
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
            {
                return _resolver.CanInsertHere(context, index, stateId);
            }
            
            return ResolverResult.Unknown;
        }
    }
    
    /// <summary>
    /// Resolver that places a state at a specific position relative to the start of its group
    /// </summary>
    public class RelativeGroupPositionResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly int _group;
        private readonly int _offsetFromStart;
        
        /// <summary>
        /// Gets the priority value for ordering between resolvers
        /// </summary>
        public int ResolverPriority => 80;

        /// <summary>
        /// Gets the resolver description for debugging purposes
        /// </summary>
        public string Description => $"RelativeGroupPositionResolver(Group: {_group}, Offset: {_offsetFromStart})";

        /// <summary>
        /// Initializes a new instance of the <see cref="RelativeGroupPositionResolver{TStateId}"/> class
        /// </summary>
        /// <param name="group">The group identifier</param>
        /// <param name="offsetFromStart">The position offset from the start of the group</param>
        public RelativeGroupPositionResolver(int group, int offsetFromStart)
        {
            _group = group;
            _offsetFromStart = offsetFromStart;
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
            int groupStartIndex = -1;
            for (int i = 0; i < context.StatesCount; i++)
            {
                if (IsStateInGroup(context, i, _group))
                {
                    if (groupStartIndex == -1 || (i > 0 && !IsStateInGroup(context, i - 1, _group)))
                    {
                        groupStartIndex = i;
                        break;
                    }
                }
            }
            
            if (groupStartIndex == -1)
            {
                return ResolverResult.Unknown;
            }
            
            int targetPosition = groupStartIndex + _offsetFromStart;
            
            if (index == targetPosition)
            {
                return ResolverResult.Insert;
            }
            
            return ResolverResult.Skip;
        }
        
        private bool IsStateInGroup(PriorityManager<TStateId> context, int index, int targetGroup)
        {
            var priority = context.PriorityFrom(index);
            if (priority.Resolver is DefaultPriorityResolver<TStateId> defaultResolver)
            {
                var groupField = typeof(DefaultPriorityResolver<TStateId>).GetField("_group", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (groupField != null)
                {
                    int group = (int)groupField.GetValue(defaultResolver);
                    return group == targetGroup;
                }
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Represents the priority configuration for a state
    /// </summary>
    /// <typeparam name="TStateId">The type of state identifier</typeparam>
    public partial class StatePriority<TStateId>
    {
        /// <summary>
        /// The resolver responsible for determining state priority
        /// </summary>
        public readonly IPriorityResolver<TStateId> Resolver;

        /// <summary>
        /// Gets the priority value within the group
        /// </summary>
        /// <exception cref="Exception">Thrown when the resolver doesn't implement IPriorityGroupProvider</exception>
        public int Priority
        {
            get
            {
                if (Resolver is IPriorityGroupProvider priorityResolver)
                    return priorityResolver.Priority;
                throw new Exception("Priority provider does not have 'priority' field");
            }
        }
        
        /// <summary>
        /// Gets the group identifier
        /// </summary>
        /// <exception cref="Exception">Thrown when the resolver doesn't implement IPriorityGroupProvider</exception>
        public int Group
        {
            get
            {
                if (Resolver is IPriorityGroupProvider priorityResolver)
                    return priorityResolver.Group;
                throw new Exception("Priority provider does not have 'group' field");
            }
        }

        /// <summary>
        /// Initializes a new instance with the specified resolver
        /// </summary>
        /// <param name="resolver">The priority resolver to use</param>
        public StatePriority(IPriorityResolver<TStateId> resolver)
        {
            Resolver = resolver;
        }
        
        /// <summary>
        /// Initializes a new instance with the specified group and priority
        /// </summary>
        /// <param name="group">The group identifier</param>
        /// <param name="priority">The priority value within the group</param>
        public StatePriority(int group, int priority)
        {
            Resolver = new DefaultPriorityResolver<TStateId>(group, priority);
        }
        
        /// <summary>
        /// Initializes a new instance with the specified priority in group 0
        /// </summary>
        /// <param name="priority">The priority value</param>
        public StatePriority(int priority)
        {
            Resolver = new DefaultPriorityResolver<TStateId>(0, priority);
        }

        /// <summary>
        /// Creates a default priority configuration with the specified group and priority
        /// </summary>
        public static StatePriority<TStateId> Default(int group, int priority)
        {
            return new StatePriority<TStateId>(new DefaultPriorityResolver<TStateId>(group, priority));
        }
        
        /// <summary>
        /// Creates a priority configuration with a custom tiebreaker function
        /// </summary>
        public static StatePriority<TStateId> WithTiebreaker(int group, int priority, Func<TStateId, TStateId, int> tiebreaker)
        {
            return new StatePriority<TStateId>(new DefaultPriorityResolver<TStateId>(group, priority, tiebreaker));
        }
        
        /// <summary>
        /// Creates a priority configuration that places the state after another specific state
        /// </summary>
        public static StatePriority<TStateId> After(TStateId afterState)
        {
            return new StatePriority<TStateId>(new AfterStateResolver<TStateId>(afterState));
        }
        
        /// <summary>
        /// Creates a priority configuration that places the state before another specific state
        /// </summary>
        public static StatePriority<TStateId> Before(TStateId beforeState)
        {
            return new StatePriority<TStateId>(new BeforeStateResolver<TStateId>(beforeState));
        }
        
        /// <summary>
        /// Creates a priority configuration that places the state at the beginning or end of the list
        /// </summary>
        /// <param name="isFirst">True to place at the beginning, false to place at the end</param>
        public static StatePriority<TStateId> Boundary(bool isFirst)
        {
            return new StatePriority<TStateId>(new BoundaryResolver<TStateId>(isFirst));
        }
        
        /// <summary>
        /// Creates a priority configuration that places the state at the beginning or end of a specific group
        /// </summary>
        /// <param name="group">The group identifier</param>
        /// <param name="isFirst">True to place at the beginning of the group, false to place at the end</param>
        public static StatePriority<TStateId> GroupBoundary(int group, bool isFirst)
        {
            return new StatePriority<TStateId>(new GroupBoundaryResolver<TStateId>(group, isFirst));
        }
        
        /// <summary>
        /// Creates a priority configuration that places the state at a specific position within its group
        /// </summary>
        /// <param name="group">The group identifier</param>
        /// <param name="offset">The position offset from the start of the group</param>
        public static StatePriority<TStateId> GroupPosition(int group, int offset)
        {
            return new StatePriority<TStateId>(new RelativeGroupPositionResolver<TStateId>(group, offset));
        }
        
        /// <summary>
        /// Creates a composite priority configuration with the specified resolvers and combination type
        /// </summary>
        public static StatePriority<TStateId> Composite(
            List<IPriorityResolver<TStateId>> resolvers, 
            CompositeResolver<TStateId>.ResolverCombination combinationType = CompositeResolver<TStateId>.ResolverCombination.First)
        {
            return new StatePriority<TStateId>(new CompositeResolver<TStateId>(resolvers, combinationType));
        }
        
        /// <summary>
        /// Creates a composite priority configuration with the specified combination type and resolvers
        /// </summary>
        public static StatePriority<TStateId> Composite(
            CompositeResolver<TStateId>.ResolverCombination combinationType,
            params IPriorityResolver<TStateId>[] resolvers)
        {
            return new StatePriority<TStateId>(new CompositeResolver<TStateId>(
                new List<IPriorityResolver<TStateId>>(resolvers), 
                combinationType));
        }
        
        /// <summary>
        /// Creates a conditional priority configuration that only applies when the specified condition is met
        /// </summary>
        /// <param name="resolver">The resolver to use when the condition is met</param>
        /// <param name="condition">The condition that must be met to apply the resolver</param>
        public static StatePriority<TStateId> Conditional(
            IPriorityResolver<TStateId> resolver,
            Func<PriorityManager<TStateId>, TStateId, bool> condition)
        {
            return new StatePriority<TStateId>(new ConditionalResolver<TStateId>(resolver, condition));
        }
    }
}
