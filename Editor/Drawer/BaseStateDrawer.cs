using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSM.Editor
{
    [CustomPropertyDrawer(typeof(BaseState<,>), true)]
    public class BaseStateDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            
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
                var fieldType = GetFieldType(property);
                
                if (fieldType != null && IsDerivedFrom(fieldType, typeof(StateExtension<,>)))
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
        
        private static Type GetFieldType(SerializedProperty property)
        {
            var targetType = property.serializedObject.targetObject.GetType();
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var fieldPath = property.propertyPath.Split('.');

            var currentType = targetType;
            foreach (var fieldName in fieldPath)
            {
                var fieldInfo = currentType.GetField(fieldName, bindingFlags);
                if (fieldInfo != null)
                {
                    currentType = fieldInfo.FieldType;
                }
                else
                    return null;
            }

            return currentType;
        }
        
        private static bool IsDerivedFrom(Type type, Type baseType)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                    return true;

                type = type.BaseType;
            }

            return false;
        }
    }
}