using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AdvancedSM;
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
        private string[] _cachedStatesText;
        private int _cachedCurrentIndex;
        private FieldInfo _statesOrderField;
        private FieldInfo _currentIndexField;
        private bool _fieldsInitialized;
        private Func<int> _currentIndexGetter;

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

                var debugContainer = new IMGUIContainer(DrawStates);
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
                var type = target.GetType();
        
                _statesOrderField = type.GetField("StatesOrder", BindingFlags.NonPublic | BindingFlags.Instance)!;
                _currentIndexField = type.GetField("CurrentIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
            
                var statesOrder = (IList)_statesOrderField.GetValue(target);
                _cachedStatesText = statesOrder.Cast<object>().Select(s => s.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries).Last()).ToArray();
                _currentIndexGetter = CreateFieldGetter<int>(target, _currentIndexField);

                return;

                Func<T> CreateFieldGetter<T>(object objectTarget, FieldInfo fieldInfo)
                {
                    var instanceParam = Expression.Constant(objectTarget);
                    var fieldAccess = Expression.Field(instanceParam, fieldInfo);
                    var lambda = Expression.Lambda<Func<T>>(fieldAccess);
                    return lambda.Compile();
                }
            });
            _fieldsInitialized = true;
        }

        private void DrawStates()
        {
            if (!_fieldsInitialized)
            {
                GUILayout.Label("Initializing fields...");
                return;
            }
            Profiler.BeginSample("DrawStates");
            EditorGUILayout.LabelField("States", EditorStyles.largeLabel);

            var defaultColor = GUI.color;
            _cachedCurrentIndex = _currentIndexGetter();

            if (_cachedStatesText.Length > 0)
            {
                for (var i = 0; i < _cachedStatesText.Length; i++)
                {
                    if (i < _cachedCurrentIndex)
                        GUI.color = Color.yellow;
                    else if (i == _cachedCurrentIndex)
                        GUI.color = Color.green;
                    else
                        GUI.color = Color.red;
                    
                    GUILayout.Box(_cachedStatesText[i], GUILayout.ExpandWidth(true));
                }
            }
            else
            {
                GUILayout.Label("No states available.");
            }

            GUI.color = defaultColor;
            Profiler.EndSample();
        }

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }
    }
}
