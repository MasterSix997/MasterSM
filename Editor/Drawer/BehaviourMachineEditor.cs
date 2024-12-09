using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace MasterSM.Editor.Drawer
{
    [CustomEditor(typeof(BehaviourMachine<,>), true)]
    [CanEditMultipleObjects]
    public class BehaviourMachineEditor : UnityEditor.Editor
    {
        private struct BaseStateValues
        {
            private readonly Func<int> _currentIndexGetter;
            private readonly Func<int> _previousIndexGetter;
            private readonly Func<IList> _statesOrderGetter;
            
            public int CachedCurrentIndex { get; private set; }
            public int CachedPreviousIndex { get; private set; }
            public string[] CachedStatesText { get; private set; }
            
            private readonly float _refreshRate;
            private float _currentTime;

            public BaseStateValues(object target, float refreshRate = 2)
            {
                // Initalize fields
                // _currentIndexGetter = null;
                // _statesOrderGetter = null;
                CachedCurrentIndex = -1;
                CachedPreviousIndex = -1;
                CachedStatesText = Array.Empty<string>();
                _refreshRate = refreshRate;
                _currentTime = 0;

                var type = target.GetType();
                
                var currentIndexField = type.GetField("CurrentIndex", BindingFlags.Public | BindingFlags.Instance)!;
                var previousIndexField = type.GetField("PreviousIndex", BindingFlags.Public | BindingFlags.Instance)!;
                var statesOrderField = type.GetField("StatesOrder", BindingFlags.Public | BindingFlags.Instance)!;
                
                _currentIndexGetter = CreateFieldGetter<int>(target, currentIndexField);
                _previousIndexGetter = CreateFieldGetter<int>(target, previousIndexField);
                _statesOrderGetter = CreateFieldGetter<IList>(target, statesOrderField);
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

                CachedCurrentIndex = _currentIndexGetter();
                CachedPreviousIndex = _previousIndexGetter();

                if (_currentTime < _refreshRate)
                    return;

                _currentTime -= _refreshRate;
                
                var statesOrder = _statesOrderGetter();
                CachedStatesText = statesOrder.Cast<object>().Select(s => s.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries).Last()).ToArray();
            }
        }
        
        private bool _fieldsInitialized;
        
        private BaseStateValues _behaviourMachineValues;
        private Func<IList> _layersGetter;
        private List<BaseStateValues> _layerValues = new();

        private float _currentTime;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            root.RemoveAt(0);
            
            // Header
            var headerContainer = new VisualElement
            {
                style =
                {
                    width = Length.Percent(100),
                    height = 20,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.2f),
                    marginBottom = 5,
                    alignItems = Align.Center,
                },
            };
            root.Insert(0, headerContainer);

            var titleText = "Behaviour Machine";
            if (serializedObject.targetObjects.Length == 1 || serializedObject.targetObjects.All(t => t.GetType() == target.GetType()))
                titleText = serializedObject.targetObject.GetType().Name;

            var title = new Label(titleText)
            {
                style =
                {
                    fontSize = 18,
                    unityFontStyleAndWeight = FontStyle.Bold,
                },
            };
            headerContainer.Add(title);

            if (Application.isPlaying && targets.Length == 1)
            {
                InitializeReflectionFields();
            
                var debugContainer = new IMGUIContainer(DrawBehaviourMachine);
                root.Add(debugContainer);
            }

            return root;
        }

        private async void InitializeReflectionFields()
        {
            if (targets.Length > 1)
            {
                Debug.LogError("Multiple state machines selected.");
                return;
            }
            await Task.Run(() =>
            {
                var type = target.GetType().BaseType!;
                
                var layersProperty = type.GetProperty("Layers", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetField);
                _layersGetter = BaseStateValues.CreatePropertyGetter<IList>(target, layersProperty);
                
                var baseMachineField = type.GetField("_baseMachine", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var baseMachineInstance = baseMachineField.GetValue(target)!;

                _behaviourMachineValues = new BaseStateValues(baseMachineInstance, 2);
                UpdateValues(5);
            });
            _fieldsInitialized = true;
        }

        private void UpdateValues(float deltaTime)
        {
            _behaviourMachineValues.UpdateValues(deltaTime);
            
            foreach (var layerValue in _layerValues)
                layerValue.UpdateValues(deltaTime);
            
            _currentTime += deltaTime;

            const float refreshRate = 5;
            if (_currentTime < refreshRate)
                return;
            
            _currentTime -= refreshRate;

            // TODO: Fix layer states don't draw
            var layers = _layersGetter();
            if (_layerValues.Count != layers.Count)
            {
                _layerValues.Clear();
                foreach (var layer in layers)
                {
                    _layerValues.Add(new BaseStateValues(layer, 5));
                }
                foreach (var layerValue in _layerValues)
                    layerValue.UpdateValues(5);
            }
        }

        private void DrawBehaviourMachine()
        {
            if (!_fieldsInitialized)
            {
                GUILayout.Label("Initializing fields...");
                return;
            }
            Profiler.BeginSample("DrawBehaviourMachine");
            EditorGUILayout.LabelField("States", EditorStyles.largeLabel);

            var defaultColor = GUI.color;
            
            UpdateValues(Time.deltaTime);

            DrawBaseState(_behaviourMachineValues);

            for (var i = 0; i < _layerValues.Count; i++)
            {
                GUI.color = defaultColor;
                
                var layerValue = _layerValues[i];
                EditorGUILayout.Separator();
                EditorGUILayout.LabelField($"Layer '{i}'");
                DrawBaseState(layerValue);
            }

            GUI.color = defaultColor;
            Profiler.EndSample();
        }

        private void DrawBaseState(BaseStateValues baseState)
        {
            if (baseState.CachedStatesText.Length > 0)
            {
                for (var i = 0; i < baseState.CachedStatesText.Length; i++)
                {
                    if (i < baseState.CachedCurrentIndex)
                    {
                        if (i == baseState.CachedPreviousIndex)
                            GUI.color = new Color(0.5f, 1, 0.5f);
                        else
                            GUI.color = Color.yellow;
                    }
                    else if (i == baseState.CachedCurrentIndex)
                        GUI.color = Color.green;
                    else
                    {
                        if (i == baseState.CachedPreviousIndex)
                            GUI.color = new Color(1, 0.5f, 0.5f);
                        else
                            GUI.color = Color.red;
                    }
                    
                    GUILayout.Box(baseState.CachedStatesText[i], GUILayout.ExpandWidth(true));
                }

                if (baseState.CachedCurrentIndex == -1)
                {
                    GUI.color = Color.blue;
                    GUILayout.Box("NONE", GUILayout.ExpandWidth(true));
                }
            }
            else
            {
                GUILayout.Label("No states available.");
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
