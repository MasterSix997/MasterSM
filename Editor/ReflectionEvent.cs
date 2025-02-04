using System;
using System.Reflection;

namespace MasterSM.Editor
{
    internal struct ReflectionEvent
    {
        private readonly object _target;
        private readonly EventInfo _eventInfo;
        private Delegate _handler;
        
        public ReflectionEvent(object target, EventInfo eventInfo, Delegate handler)
        {
            _target = target;
            _eventInfo = eventInfo;
            _handler = handler;
        }
        
        public void Subscribe()
        {
            _eventInfo.AddEventHandler(_target, _handler);
        }
        
        public void Unsubscribe()
        {
            _eventInfo.RemoveEventHandler(_target, _handler);
        }
    }
}