using System;
using System.Collections.Generic;
using UnityEngine;

namespace MasterSM.PriorityManagement
{
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
        /// <param name="highPriority">True to place at the beginning of the group, false to place at the end</param>
        public static StatePriority<TStateId> GroupBoundary(int group, bool highPriority)
        {
            return new StatePriority<TStateId>(new GroupBoundaryResolver<TStateId>(group, highPriority));
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

