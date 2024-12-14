using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace MasterSM.Editor.Drawer
{
    public struct BaseStateValues
    {
        private readonly Func<int> _currentIndexGetter;
        private readonly Func<int> _previousIndexGetter;
        private readonly Func<IList> _statesOrderGetter;
        private readonly Func<IDictionary> _statesGetter;
            
        public int CachedCurrentIndex { get; private set; }
        public int CachedPreviousIndex { get; private set; }
        public string[] CachedStatesText { get; private set; }
        public bool[] CachedStatesEnabled { get; private set; }
            
        private readonly float _refreshRate;
        private float _currentTime;
        
        public bool IsDirty { get; private set; }

        public BaseStateValues(object target, float refreshRate = 2)
        {
            CachedCurrentIndex = -1;
            CachedPreviousIndex = -1;
            CachedStatesText = Array.Empty<string>();
            CachedStatesEnabled = Array.Empty<bool>();
            _refreshRate = refreshRate;
            _currentTime = 0;
            IsDirty = true;

            var type = target.GetType();
                
            var currentIndexField = type.GetField("CurrentIndex", BindingFlags.Public | BindingFlags.Instance)!;
            var previousIndexField = type.GetField("PreviousIndex", BindingFlags.Public | BindingFlags.Instance)!;
            var statesOrderField = type.GetField("StatesOrder", BindingFlags.Public | BindingFlags.Instance)!;
            var statesField = type.GetField("States", BindingFlags.Public | BindingFlags.Instance)!;
                
            _currentIndexGetter = CreateFieldGetter<int>(target, currentIndexField);
            _previousIndexGetter = CreateFieldGetter<int>(target, previousIndexField);
            _statesOrderGetter = CreateFieldGetter<IList>(target, statesOrderField);
            _statesGetter = CreateFieldGetter<IDictionary>(target, statesField);
        }
            
        public static Func<T> CreateFieldGetter<T>(object objectTarget, FieldInfo fieldInfo)
        {
            var instanceParam = Expression.Constant(objectTarget);
            var fieldAccess = Expression.Field(instanceParam, fieldInfo);
            var lambda = Expression.Lambda<Func<T>>(fieldAccess);
            return lambda.Compile();
        }

        public static Func<T> CreatePropertyGetter<T>(object objectTarget, PropertyInfo propertyInfo)
        {
            var instanceParam = Expression.Constant(objectTarget);
            var fieldAccess = Expression.Property(instanceParam, propertyInfo);
            var lambda = Expression.Lambda<Func<T>>(fieldAccess);
            return lambda.Compile();
        }

        public void UpdateValues(float deltaTime)
        {
            _currentTime += deltaTime;

            var currentIndex = _currentIndexGetter();
            var previousIndex = _previousIndexGetter();
            IsDirty = currentIndex != CachedCurrentIndex || previousIndex != CachedPreviousIndex;
            CachedCurrentIndex = currentIndex;
            CachedPreviousIndex = previousIndex;

            if (_currentTime < _refreshRate)
                return;

            _currentTime -= _refreshRate;
                
            var statesOrder = _statesOrderGetter();
            
            var statesText = statesOrder.Cast<object>().Select(s => s.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries).Last()).ToArray();
            IsDirty |= statesText.Length != CachedStatesText.Length || !statesText.SequenceEqual(CachedStatesText);
            CachedStatesText = statesText;
            
            var states = _statesGetter();
            var statesEnabled = new bool[states.Count];
            for (var i = 0; i < statesOrder.Count; i++)
            {
                var state = states[statesOrder[i]];
                var enabledField = state.GetType().GetProperty("Enabled", BindingFlags.Public | BindingFlags.Instance);
                if (enabledField == null)
                    continue;
                statesEnabled[i] = enabledField.GetValue(state) as bool? ?? true;
            }
            IsDirty |= statesEnabled.Length != CachedStatesEnabled.Length || !statesEnabled.SequenceEqual(CachedStatesEnabled);
            CachedStatesEnabled = statesEnabled;
        }
    }
}