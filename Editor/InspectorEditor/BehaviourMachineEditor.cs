using MasterSM.Editor.Elements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterSM.Editor.InspectorEditor
{
    [CustomEditor(typeof(BehaviourMachine<,>), true)]
    [CanEditMultipleObjects]
    internal class BehaviourMachineEditor : UnityEditor.Editor
    {
        private const string HeaderContainerName = "HeaderContainer"; 
        private const string HeaderTitleName = "HeaderTitle"; 
        private const string DefaultInspectorContainerName = "DefaultInspectorContainer";
        private const string LayersContainerName = "LayersContainer";

        private const string BaseMachineField = "_baseMachine";
        private const string OnLayerAddedEvent = "OnLayerAdded";
        private const string OnLayerRemovedEvent = "OnLayerRemoved";

        [SerializeField] private VisualTreeAsset visualTree;
        
        private VisualElement _headerContainer;
        private Label _headerTitle;
        private VisualElement _defaultInspectorContainer;
        private VisualElement _layersContainer;
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            visualTree.CloneTree(root);
            
            _headerContainer = root.Q<VisualElement>(HeaderContainerName);
            _headerTitle = _headerContainer.Q<Label>(HeaderTitleName);
            
            _defaultInspectorContainer = root.Q<VisualElement>(DefaultInspectorContainerName);
            _layersContainer = root.Q<VisualElement>(LayersContainerName);
            
            InspectorElement.FillDefaultInspector(_defaultInspectorContainer, serializedObject, this);
            _defaultInspectorContainer.RemoveAt(1); //script field
            
            if (Application.isPlaying)
                EditorApplication.update += DelayedInit;
            else
                _layersContainer.style.display = DisplayStyle.None;
            
            return root;
        }

        private void DelayedInit()
        {
            EditorApplication.update -= DelayedInit;
            _layersContainer.Add(new MachineWithLayers(target));
        }
    }
}
