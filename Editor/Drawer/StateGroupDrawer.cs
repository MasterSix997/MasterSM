using MasterSM.Editor.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterSM.Editor.Drawer
{
    [CustomPropertyDrawer(typeof(StateGroup<,>))]
    public class StateGroupDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
    
            var statesProperty = property.FindPropertyRelative("states");
            if (statesProperty == null || statesProperty.arraySize == 0)
            {
                root.Add(new Label("Empty state list..."));
                return root;
            }

            var headerContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.4f),
                    width = Length.Percent(100),
                    paddingTop = 5,
                    paddingLeft = 5,
                    paddingRight = 5,
                    marginTop = 5,
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5
                }
            };
            root.Add(headerContainer);
            var headerLabel = new Label("State Group")
            {
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                }
            };
            headerContainer.Add(headerLabel);
            var basePriority = new PropertyField(property.FindPropertyRelative("basePriority"));
            headerContainer.Add(basePriority);
            
            var statesView = new StatesTabView(statesProperty)
            {
                viewDataKey = property.propertyPath
            };
            root.Add(statesView);
    
            for (int i = 0; i < statesProperty.arraySize; i++)
            {
                var stateProperty = statesProperty.GetArrayElementAtIndex(i);
                var name = stateProperty.type.Split('<')[^1][..^1];
                statesView.AddState(stateProperty, name, true);
            }
            return root;
        }
    }
}