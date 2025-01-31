using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace MasterSM.Editor
{
    public static class EditorUtils
    {
        public static bool HasStateAttribute(SerializedProperty property, out string stateName)
        {
            stateName = null;
            var fieldInfo = GetFieldInfo(property);
            if (fieldInfo == null)
            {
                return false;
            }

            var attributes = fieldInfo.GetCustomAttributes(typeof(StateTabAttribute), false);
            if (attributes.Length == 0)
            {
                return false;
            }

            var attribute = (StateTabAttribute)attributes[0];
            stateName = attribute.Name;
            return true;
        }
        
        public static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var propertyPath = property.propertyPath;

            var type = property.serializedObject.targetObject.GetType();
            FieldInfo fieldInfo = null;

            foreach (var part in propertyPath.Split('.'))
            {
                if (type == null) break;

                fieldInfo = type.GetField(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = fieldInfo?.FieldType;
            }

            return fieldInfo;
        }
        
        public static Type GetFieldType(SerializedProperty property)
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

        public static object GetParentObject(SerializedProperty property)
        {
            string path = property.propertyPath;
            object obj = property.serializedObject.targetObject;
        
            var elements = path.Split('.');
            for (int i = 0; i < elements.Length - 1; i++) // Ignora o último (a própria propriedade)
            {
                var type = obj.GetType();
                var field = type.GetField(elements[i], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    obj = field.GetValue(obj);
                }
                else
                {
                    return null; // Não encontrou o campo, pode ser um problema de path
                }
            }
        
            return obj;
        }
        
        public static bool IsDerivedFrom(Type type, Type baseType, int depth = -1)
        {
            while (type != null && type != typeof(object) && depth--!= 0)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
                    return true;

                type = type.BaseType;
            }

            return false;
        }
        
        public static bool ImplementsInterface(Type type, Type interfaceType)
        {
            while (type != null && type != typeof(object))
            {
                if (type.GetInterfaces().Any(i => i == interfaceType || (i.IsGenericType && i.GetGenericTypeDefinition() == interfaceType)))
                    return true;

                type = type.BaseType;
            }

            return false;
        }
        
        public static bool IsSameTypeIgnoringGenericArguments(Type type1, Type type2)
        {
            if (type1 == type2)
                return true;

            if (!type1.IsGenericType || !type2.IsGenericType) return false;
            var definition1 = type1.GetGenericTypeDefinition();
            var definition2 = type2.GetGenericTypeDefinition();

            if (definition1 != definition2) return false;
            var arguments1 = type1.GetGenericArguments();
            var arguments2 = type2.GetGenericArguments();

            return arguments1.Length == arguments2.Length;
        }
    }
}