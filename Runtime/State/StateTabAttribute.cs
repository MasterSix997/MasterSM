using System;

namespace MasterSM
{
    /// <summary>
    /// Attribute used to mark fields as state tabs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class StateTabAttribute : Attribute
    {
        public string Name { get; }

        public StateTabAttribute(string name)
        {
            Name = name;
        }
    }
}