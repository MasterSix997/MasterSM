using System;
using System.Reflection;

namespace MasterSM.Editor.Utils
{
    internal struct ReflectionEvent
    {
        private readonly object _target;
        private readonly EventInfo _eventInfo;
        private Delegate _handler;
        private bool _subscribed;
        
        public ReflectionEvent(object target, EventInfo eventInfo, Delegate handler)
        {
            _target = target;
            _eventInfo = eventInfo;
            _handler = handler;
            _subscribed = false;
        }
        
        public void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            _eventInfo.AddEventHandler(_target, _handler);
        }
        
        public void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            _eventInfo.RemoveEventHandler(_target, _handler);
        }
    }
}