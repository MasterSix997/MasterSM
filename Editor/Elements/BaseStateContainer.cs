using MasterSM.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterSM.Editor.Elements
{
    internal class BaseStateContainer : VisualElement
    {
        public BaseStateContainer() { }
        
        public BaseStateContainer(SerializedProperty property, bool isFoldout = false)
        {
            UpdateContainer(property, isFoldout);
        }
        
        public void UpdateContainer(SerializedProperty property, bool isFoldout = false)
        {
            Clear();
            Add(CreateUI(property, isFoldout));
        }

        private VisualElement CreateUI(SerializedProperty property, bool isFoldout)
        {
            var root = isFoldout? new Foldout
            {
                text = property.displayName,
                viewDataKey = property.propertyPath
            } : new VisualElement();
            
            var extensionsContainer = new VisualElement
            {
                name = "extensions-container",
                style = { marginTop = 10 }
            };
            extensionsContainer.Add(new Label("Extensions")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            });

            var minDepth = property.depth + 1;
        
            if (!property.Next(true))
            {
                root.Add(new Label("Empty state..."));
                return root;
            }
        
            do
            {
                if (property.depth < minDepth)
                    break;
            
                var field = new PropertyField(property);
                var fieldType = EditorUtils.GetFieldType(property);
            
                if (fieldType != null && EditorUtils.IsDerivedFrom(fieldType, typeof(StateExtension<,>)))
                    extensionsContainer.Add(field);
                else
                    root.Add(field);
            } while (property.NextVisible(false));

            if (root.childCount == 0)
            {
                root.Add(new Label("Empty state..."));
            }
        
            if (extensionsContainer.childCount > 1)
                root.Add(extensionsContainer);
        
            return root;
        }
    }
}