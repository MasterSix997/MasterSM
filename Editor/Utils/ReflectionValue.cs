using System;
using System.Reflection;

namespace MasterSM.Editor.Utils
{
    internal struct ReflectionValue<T>
    {
        private readonly object _target;
        private readonly FieldInfo _fieldInfo;
        private readonly PropertyInfo _propertyInfo;
        
        private bool _initialized;
        private Func<T> _getter;
        
        public T Value { get; private set; }
        public bool IsDirty { get; set; }

        public ReflectionValue(object target, FieldInfo fieldInfo)
        {
            _target = target;
            _fieldInfo = fieldInfo;
            _propertyInfo = null;
            _initialized = false;
            _getter = null;
            Value = default;
            IsDirty = false;
            Initialize();
        }

        public ReflectionValue(object target, PropertyInfo propertyInfo)
        {
            _fieldInfo = null;
            _propertyInfo = propertyInfo;
            _target = target;
            _initialized = false;
            _getter = null;
            Value = default;
            IsDirty = false;
            Initialize();
        }

        public bool Initialize()
        {
            if (_initialized)
                return true;

            if (_fieldInfo != null)
            {
                _getter = EditorUtils.CreateFieldGetter<T>(_target, _fieldInfo);
                if (_getter == null)
                    return false;
                
                _initialized = true;
            }
            else if (_propertyInfo != null)
            {
                _getter = EditorUtils.CreatePropertyGetter<T>(_target, _propertyInfo);
                if (_getter == null)
                    return false;
                
                _initialized = true;
            }
            else
            {
                throw new InvalidOperationException("ReflectionValue must be initialized with a FieldInfo or PropertyInfo");
            }

            return true;
        }

        public void Update()
        {
            if (!_initialized)
                return;

            var newValue = _getter();
            IsDirty = !newValue.Equals(Value);
            Value = newValue;
        }
    }
}