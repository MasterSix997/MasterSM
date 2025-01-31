using MasterSM.Editor.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterSM.Editor.InspectorEditor
{
    [CustomEditor(typeof(BaseCapability<,>), true)]
    [CanEditMultipleObjects]
    public class CapabilityEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var fieldsFoldout = new Foldout
            {
                text = "Capability Fields", 
                value = true,
                style = { marginBottom = 10}
            };
            root.Add(fieldsFoldout);

            var statesTab = new StatesTabView
            {
                viewDataKey = serializedObject.targetObject.GetInstanceID().ToString()
            };
            root.Add(statesTab);

            var iterator = serializedObject.GetIterator();
            iterator.Next(true);
            iterator.NextVisible(false); // Skip script field
            
            var hasStateGroup = false;

            while (iterator.NextVisible(false))
            {
                var propertyType = EditorUtils.GetFieldType(iterator);

                if (propertyType != null && EditorUtils.ImplementsInterface(propertyType, typeof(IState<,>)))
                {
                    statesTab.AddState(iterator, iterator.displayName);
                }
                else if (EditorUtils.HasStateAttribute(iterator, out var stateName))
                {
                    statesTab.AddToStateTab(stateName, new PropertyField(iterator.Copy()));
                }
                else if (EditorUtils.IsSameTypeIgnoringGenericArguments(propertyType, typeof(StateGroup<,>)))
                {
                    hasStateGroup = true;
                    var field = new PropertyField(iterator.Copy());
                    root.Add(field);
                }
                else
                {
                    var field = new PropertyField(iterator.Copy());
                    fieldsFoldout.Add(field);
                }
            }

            if (hasStateGroup)
            {
                root.Remove(statesTab);

                if (statesTab.StateCount != 0)
                {
                    Debug.LogWarning("StateGroup found, but capability also has states. Please add states to the StateGroup.");
                }
            }
            
            return root;
        }
    }
}
