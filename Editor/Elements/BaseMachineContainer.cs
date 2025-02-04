using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using MasterSM.Editor.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using Action = System.Action;

namespace MasterSM.Editor.Elements
{
    internal class BaseMachineContainer : VisualElement
    {
        private const string ClassBaseMachineContainer = "base-machine-container";
        private const string ClassSectionLabel = "section-label";
        
        private const string ClassStateContainer = "state-container";
        private const string ClassStateHeader = "state-header";
        private const string ClassCurrentState = "current-state";
        private const string ClassPreviousState = "previous-state";
        private const string ClassUnableEnterState = "unable-enter-state";
        
        private const string StatesDictionaryField = "States";
        private const string StatesOrderListField = "StatesOrder";
        private const string CurrentStateIndexProperty = "CurrentIndex";
        private const string PreviousStateIndexProperty = "PreviousIndex";
        private const string IsActiveProperty = "IsActive";
        private const string OnStateAddedEvent = "OnStateAdded";
        private const string OnStateRemovedEvent = "OnStateRemoved";
        private const string OnCurrentStateChanged = "OnCurrentStateChanged";
            
        private object _target;
        // private string _layerName = "States";

        private ReflectionValue<IDictionary> _statesDict; // IStateId, IState<TStateId, TStateMachine>
        private ReflectionValue<IList> _statesOrder; // TStateId
        private ReflectionValue<int> _currentState;
        private ReflectionValue<int> _previousState;
        private ReflectionEvent _onStateAdded;
        private ReflectionEvent _onStateRemoved;
        private ReflectionEvent _onCurrentStateChanged;
        
        private string[] _cachedStateTexts;
        private int _currentStateIndex = -1;
        private int _previousStateIndex = -1;
        
        private VisualElement _header;
        private Label _layerNameLabel;
        private VisualElement _statesContainer;

        public BaseMachineContainer(object target, string layerName) : this(target)
        {
            _layerNameLabel = new Label(layerName);
            _layerNameLabel.AddToClassList(ClassSectionLabel);
            _header.Add(_layerNameLabel);
        }
        
        public BaseMachineContainer(object target)
        {
            var baseMachine = target.GetType();
            _target = target;
            _statesDict = new ReflectionValue<IDictionary>(target, baseMachine.GetField(StatesDictionaryField, BindingFlags.Instance | BindingFlags.Public));
            _statesOrder = new ReflectionValue<IList>(target, baseMachine.GetField(StatesOrderListField, BindingFlags.Instance | BindingFlags.Public));
            _currentState = new ReflectionValue<int>(target, baseMachine.GetField(CurrentStateIndexProperty, BindingFlags.Instance | BindingFlags.NonPublic));
            _previousState = new ReflectionValue<int>(target, baseMachine.GetField(PreviousStateIndexProperty, BindingFlags.Instance | BindingFlags.NonPublic));
            _onStateAdded = new ReflectionEvent(target, baseMachine.GetEvent(OnStateAddedEvent, BindingFlags.Instance | BindingFlags.Public), (Action)UpdateAll);
            _onStateRemoved = new ReflectionEvent(target, baseMachine.GetEvent(OnStateRemovedEvent, BindingFlags.Instance | BindingFlags.Public), (Action)UpdateAll);
            _onCurrentStateChanged = new ReflectionEvent(target, baseMachine.GetEvent(OnCurrentStateChanged, BindingFlags.Instance | BindingFlags.Public), (Action)UpdateCurrentState);
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            AddToClassList(ClassBaseMachineContainer);
            
            CreateElements();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            _onStateAdded.Subscribe();
            _onStateRemoved.Subscribe();
            _onCurrentStateChanged.Subscribe();
            
            InitializeReflection();
            UpdateAll();
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _onStateAdded.Unsubscribe();
            _onStateRemoved.Unsubscribe();
            _onCurrentStateChanged.Unsubscribe();
        }

        private void CreateElements()
        {
            _header = new VisualElement();
            Add(_header);
            
            _statesContainer = new VisualElement();
            Add(_statesContainer);
        }

        private void InitializeReflection()
        {
            _statesDict.Update();
            _statesOrder.Update();
            _currentState.Update();
            _previousState.Update();
        }
        
        private void UpdateAll()
        {
            _currentState.Update();
            _previousState.Update();
            GenerateStatesText();
            CreateStatesFields();
            UpdateStateChanged();
            
            return;
            
            void GenerateStatesText()
            {
                _cachedStateTexts = _statesOrder.Value.Cast<object>().Select(x => x.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries).Last()).ToArray();
            }
        }

        private void UpdateCurrentState()
        {
            _currentState.Update();
            _previousState.Update();
            // CheckDirtFields();
            if (_currentState.IsDirty)
            {
                _currentState.IsDirty = false;
                UpdateStateChanged();
            }
        }

        // private void CheckDirtFields()
        // {
        //     if (_statesDict.IsDirty || _statesOrder.IsDirty)
        //     {
        //         CreateStatesFields();
        //         UpdateStateChanged();
        //         return;
        //     }
        //     if (_currentState.IsDirty)
        //         UpdateStateChanged();
        // }

        private void CreateStatesFields()
        {
            _statesContainer.Clear();
            _currentStateIndex = -1;
            _previousStateIndex = -1;

            // var statesArray = _statesDict.Value.Values.Cast<object>().ToArray();
            for (var i = 0; i < _cachedStateTexts.Length; i++)
            {
                var stateText = _cachedStateTexts[i];
                var stateContainer = new VisualElement();
                stateContainer.AddToClassList(ClassStateContainer);
                _statesContainer.Add(stateContainer);

                var header = new VisualElement();
                header.AddToClassList(ClassStateHeader);
                stateContainer.Add(header);

                header.Add(new Label(stateText));

                var state = _statesDict.Value[_statesOrder.Value[i]];
                if (EditorUtils.ImplementsInterface(state.GetType(), typeof(IMachineWithLayers<,>)))
                {
                    var subMachineContainer = new MachineWithLayers(state);
                    stateContainer.Add(subMachineContainer);
                }
            }
        }

        private void UpdateStateChanged()
        {
            var newCurrentStateIndex = _currentState.Value;
            
            if (newCurrentStateIndex == _currentStateIndex)
                return;
            
            if (_currentStateIndex >= 0 && _currentStateIndex < _statesContainer.childCount)
                _statesContainer[_currentStateIndex].RemoveFromClassList(ClassCurrentState);

            if (_previousStateIndex >= 0 && _currentStateIndex < _statesContainer.childCount)
                _statesContainer[_previousStateIndex].RemoveFromClassList(ClassPreviousState);
            
            if (newCurrentStateIndex >= _cachedStateTexts.Length)
            {
                Debug.LogError("New current state is invalid. this is a bug");
                return;
            }
            
            for (int i = 0; i < _statesContainer.childCount; i++)
            {
                _statesContainer[i].RemoveFromClassList(ClassUnableEnterState);
                
                if (i == newCurrentStateIndex)
                    _statesContainer[i].AddToClassList(ClassCurrentState);
                else if (i == _previousState.Value)
                    _statesContainer[i].AddToClassList(ClassPreviousState);
                else if (i > newCurrentStateIndex)
                    _statesContainer[i].AddToClassList(ClassUnableEnterState);
            }
            
            _currentStateIndex = newCurrentStateIndex;
            _previousStateIndex = _previousState.Value;
        }
    }
}