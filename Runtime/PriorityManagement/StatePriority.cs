using System.Collections.Generic;

namespace MasterSM.PriorityManagement
{
    public interface IPriorityResolver<TStateId>
    {
        public bool CanInsertHere(PriorityManager<TStateId> context, int index);
    }

    public readonly struct AfterStateResolver<TStateId> : IPriorityResolver<TStateId>
    {
        private readonly TStateId _after;

        public AfterStateResolver(TStateId after)
        {
            _after = after;
        }

        public bool CanInsertHere(PriorityManager<TStateId> context, int index)
        {
            return index > 0 && context.IdFrom(index - 1).Equals(_after);
        }
    }
    
    public readonly struct StatePriority<TStateId>
    {
        public readonly int Group;
        public readonly int Priority;
        public readonly IPriorityResolver<TStateId> Resolver;

        private StatePriority(int group, int priority, IPriorityResolver<TStateId> resolver)
        {
            Group = group;
            Priority = priority;
            Resolver = resolver;
        }
        
        public StatePriority(int group, int priority)
        {
            Group = group;
            Priority = priority;
            Resolver = null;
        }
        
        public StatePriority(int priority)
        {
            Priority = priority;
            Group = 0;
            Resolver = null;
        }
        
        public StatePriority(IPriorityResolver<TStateId> resolver)
        {
            Resolver = resolver;
            Group = 0;
            Priority = 0;
        }
    }
}