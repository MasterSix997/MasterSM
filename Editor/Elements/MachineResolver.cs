using System;
using System.Collections;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterSM.Editor.Elements
{
    internal static class MachineResolver
    {
        private const string BaseMachineFieldName = "_baseMachine";
        private const string LayersPropertyName = "Layers";
        
        [CanBeNull]
        public static VisualElement ResolveBase(object target)
        {
            var baseMachineField = ResolveBaseMachine(target);
            if (baseMachineField == null)
                return null;
            
            var baseMachine = baseMachineField.GetValue(target);
            if (baseMachine == null)
                return null;

            var machineContainer = new BaseMachineContainer(baseMachine);
            return machineContainer;
        }

        [CanBeNull]
        private static FieldInfo ResolveBaseMachine(object target)
        {
            var baseType = target.GetType().BaseType;
            if (baseType == null)
            {
                Debug.LogError($"{target.GetType().Name} does not have a base type.");
                return null;
            }
            
            var baseMachine = baseType.GetField(BaseMachineFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (baseMachine == null)
            {
                Debug.LogError($"'{target.GetType().Name}: {baseType.Name}', does not have a field named '{BaseMachineFieldName}'.");
                return null;
            }
            
            return baseMachine;
        }

        [CanBeNull]
        public static BaseMachineContainer ResolveStateSubMachine(object target)
        {
            var baseType = target.GetType().BaseType;
            if (baseType == null || !EditorUtils.IsSameTypeIgnoringGenericArguments(baseType, typeof(SubStateMachine<,,>)))
            {
                return null;
            }
            
            var baseMachineField = ResolveBaseMachine(target);
            if (baseMachineField == null)
                return null;

            var baseMachine = baseMachineField.GetValue(target);
            if (baseMachine == null)
                return null;

            var machineContainer = new BaseMachineContainer(baseMachine);
            return machineContainer;
        }

        [CanBeNull]
        public static VisualElement ResolveLayers(object target)
        {
            var type = target.GetType();
            var layersProperty = type.GetProperty(LayersPropertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (layersProperty == null)
            {
                Debug.LogError($"'{type.Name}' does not have a field named '{LayersPropertyName}'.");
                return null;
            }

            if (layersProperty.GetValue(target) is not IDictionary layers)
            {
                Debug.LogError($"'{type.Name}.{LayersPropertyName}' is null.");
                return null;
            }

            var layersContainer = new VisualElement();
            foreach (DictionaryEntry layerEntry in layers)
            {
                if (!EditorUtils.IsSameTypeIgnoringGenericArguments(layerEntry.Value.GetType(), typeof(BaseMachine<,>)))
                {
                    Debug.LogError($"'{type.Name}.{LayersPropertyName}' contains an element of type '{layerEntry.Value.GetType().Name}' which is not a BaseMachine<,>.");
                    continue;
                }
            
                var layerContainer = new BaseMachineContainer(layerEntry.Value, layerEntry.Key.GetType().ToString());
                layersContainer.Add(layerContainer);
            }

            return layersContainer.childCount == 0 ? null : layersContainer;
        }
    }
}