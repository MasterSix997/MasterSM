using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterSM.Editor.InspectorEditor
{
    [CustomEditor(typeof(BehaviourMachine<,>), true)]
    [CanEditMultipleObjects]
    public class BehaviourMachineEditor : UnityEditor.Editor
    {
        // private bool _fieldsInitialized;
        private BaseStateValues _behaviourMachineValues;

        private VisualElement _debugContainer;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

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
            root.Add(headerContainer);

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
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            root.RemoveAt(1);

            if (Application.isPlaying && targets.Length == 1)
            {
                InitializeReflectionFields();
                _debugContainer = CreateDebugContainer();
                root.Add(_debugContainer);
            }

            return root;
        }
        
        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;
            
            InitializeReflectionFields();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            
            EditorApplication.update -= OnEditorUpdate;
        }
        
        private void OnEditorUpdate()
        {
            UpdateDebugContainer(_debugContainer);
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
                
                var baseMachineField = type.GetField("_baseMachine", BindingFlags.NonPublic | BindingFlags.Instance)!;
                var baseMachineInstance = baseMachineField.GetValue(target)!;

                _behaviourMachineValues = new BaseStateValues(baseMachineInstance, 2);
                UpdateValues(5);
            });
            // _fieldsInitialized = true;
        }

        private void UpdateValues(float deltaTime)
        {
            if (!_behaviourMachineValues.HasStates)
                return;
            
            _behaviourMachineValues.UpdateValues(deltaTime);
        }
        
        private void UpdateDebugContainer(VisualElement container)
        {
            if (!_behaviourMachineValues.HasStates)
                return;
            
            UpdateValues(Time.deltaTime);
            if (!_behaviourMachineValues.IsDirty)
                return;
            
            container.Clear();
            DrawBaseState(container, _behaviourMachineValues);
        }

        private VisualElement CreateDebugContainer()
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10,
                }
            };

            UpdateValues(Time.deltaTime);

            var statesLabel = new Label("States")
            {
                style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold }
            };
            container.Add(statesLabel);

            DrawBaseState(container, _behaviourMachineValues);
            return container;
        }

        private void DrawBaseState(VisualElement container, in BaseStateValues stateValues)
        {
            if (!_behaviourMachineValues.HasStates)
                return;
            
            if (stateValues.CachedStatesText.Length > 0)
            {
                for (var i = 0; i < stateValues.CachedStatesText.Length; i++)
                {
                    var color = GetStateColor(i, stateValues);
                    var drawBottomBorder = i == stateValues.CachedCurrentIndex;
                    var stateBox = new Label(stateValues.CachedStatesText[i]);
                    
                    var horizontalContainer = new VisualElement
                    {
                        style =
                        {
                            backgroundColor = new Color(0f, 0f, 0f, 0.3f),
                            alignContent = Align.Center,
                            flexDirection = FlexDirection.Row,
                            width = Length.Percent(100),
                            minHeight = 20,
                            paddingTop = 5,
                            paddingBottom = 5,
                            paddingLeft = 10,
                            paddingRight = 10,
                            marginBottom = 2,
                            borderTopLeftRadius = 0,
                            borderTopRightRadius = 4,
                            borderBottomLeftRadius = drawBottomBorder ? 4 : 0,
                            borderBottomRightRadius = 4,
                            borderLeftWidth = 1,
                            borderBottomWidth = drawBottomBorder ? 2 : 0,
                            borderLeftColor = color,
                            borderBottomColor = color,
                        }
                    };
                    horizontalContainer.Add(stateBox);
                    
                    if (!drawBottomBorder && i == stateValues.CachedPreviousIndex)
                    {
                        var previousContainer = new VisualElement
                        {
                            style =
                            {
                                width = Length.Percent(100),
                                marginBottom = 2,
                                marginLeft = -4,
                                paddingLeft = 2,
                                borderLeftWidth = 2,
                                borderLeftColor = Color.cyan,
                            }
                        };
                        horizontalContainer.style.marginBottom = 0;
                        previousContainer.Add(horizontalContainer);
                        container.Add(previousContainer);
                    }
                    else
                    {
                        container.Add(horizontalContainer);
                    }
                }
            }
            else
            {
                var noStateLabel = new Label("No states available.")
                {
                    style = { color = Color.gray }
                };
                container.Add(noStateLabel);
            }
            
            container.Add(new Label(Random.Range(0, 100).ToString()));
        }
        
        // private static Color GetStateColor(int stateIndex, int currentIndex, int previousIndex)
        // {
        //     if (stateIndex < currentIndex)
        //         return stateIndex == previousIndex ? new Color(0.5f, 1, 0.5f) : Color.yellow;
        //     if (stateIndex == currentIndex)
        //         return Color.green;
        //     return stateIndex == previousIndex ? new Color(1, 0.5f, 0.5f) : Color.red;
        // }
        
        private static Color GetStateColor(int stateIndex, in BaseStateValues stateValues)
        {
            if (stateValues.CachedStatesEnabled[stateIndex] == false)
                return Color.gray;
            if (stateIndex < stateValues.CachedCurrentIndex)
                return Color.yellow;
            if (stateIndex == stateValues.CachedCurrentIndex)
                return Color.green;
            return Color.red;
        }
    }
}
