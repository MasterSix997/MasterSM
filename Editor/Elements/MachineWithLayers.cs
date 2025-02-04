using System;
using System.Reflection;
using MasterSM.Editor.Utils;
using UnityEngine.UIElements;

namespace MasterSM.Editor.Elements
{
    public class MachineWithLayers : VisualElement
    {
        private const string OnLayerAddedEvent = "OnLayerAdded";
        private const string OnLayerRemovedEvent = "OnLayerRemoved";
        
        private ReflectionEvent _onLayerAdded;
        private ReflectionEvent _onLayerRemoved;
        
        private readonly object _target;

        public MachineWithLayers(object target)
        {
            _target = target;
            
            var type = target.GetType().BaseType;
            if (type == null || !EditorUtils.ImplementsInterface(type, typeof(IMachineWithLayers<,>)))
                throw new ArgumentException("Target does not implement IMachineWithLayers<,> interface.");
            
            _onLayerAdded = new ReflectionEvent(target, type.GetEvent(OnLayerAddedEvent, BindingFlags.Instance | BindingFlags.Public), (Action)UpdateLayersContainer);
            _onLayerRemoved = new ReflectionEvent(target, type.GetEvent(OnLayerRemovedEvent, BindingFlags.Instance | BindingFlags.Public), (Action)UpdateLayersContainer);
            
            UpdateLayersContainer();
            
            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        private void OnAttachedToPanel(AttachToPanelEvent e)
        {
            _onLayerAdded.Subscribe();
            _onLayerRemoved.Subscribe();
        }
        
        private void OnDetachedFromPanel(DetachFromPanelEvent e)
        {
            _onLayerAdded.Unsubscribe();
            _onLayerRemoved.Unsubscribe();
        }
        
        private void UpdateLayersContainer()
        {
            Clear();
            var baseMachineContainer = MachineResolver.ResolveBase(_target);
            if (baseMachineContainer != null)
                Add(baseMachineContainer);
            
            var machineLayersContainer = MachineResolver.ResolveLayers(_target);
            if (machineLayersContainer != null)
                Add(machineLayersContainer);
        }
    }
}