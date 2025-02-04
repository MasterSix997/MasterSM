using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MasterSM.Editor.Elements
{
    internal class StatesTabView : VisualElement
    {
        public TabView TabView { get; private set; }
        public VisualElement TabViewHeader { get; private set; }

        private bool _empty = true;
        [CanBeNull] private readonly SerializedProperty _property;
        public int StateCount => TabView.childCount;

        private MethodInfo _reorderTabReflection;

        public StatesTabView(SerializedProperty property = null)
        {
            _property = property;
            CreateVisualElements();
        }

        private void CreateVisualElements()
        {
            TabView = new TabView
            {
                viewDataKey = viewDataKey + "SelectedTabIndex",
            };
            Add(TabView);

            TabView.contentContainer.style.paddingLeft = 5;
            TabView.contentContainer.style.paddingRight = 2;

            TabViewHeader = TabView.Q<VisualElement>("unity-tab-view__header-container");
            TabViewHeader.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
            TabViewHeader.style.borderBottomWidth = 3;
            TabViewHeader.style.borderBottomColor = new Color(0, 0, 0, 0.35f);
            TabViewHeader.style.flexWrap = Wrap.Wrap;

            // if (_property is { arraySize: > 0 })
            // {
            //     var reorderButton = new Button(ShowReorderPopup)
            //     {
            //         text = "⚙️",
            //         tooltip = "Reorder states"
            //     };
            //
            //     TabViewHeader.Add(reorderButton);
            // }

            var emptyLabel = new Label("No defined states.")
            {
                name = "empty-label",
                style =
                {
                    marginTop = 5,
                    marginLeft = 5,
                    marginBottom = 5,
                    unityFontStyleAndWeight = FontStyle.BoldAndItalic
                }
            };
            TabViewHeader.Add(emptyLabel);
        }

        private void BindReflection()
        {
            _reorderTabReflection =
                typeof(TabView).GetMethod("ReorderTab", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_reorderTabReflection == null)
            {
                Debug.LogError("Failed to bind reflection for TabView.ReorderTab");
                return;
            }
        }

        public void AddState(SerializedProperty property, string stateName, bool isArrayElement = false)
        {
            var tabName = stateName;
            if (tabName.EndsWith("State"))
                tabName = tabName[..^5];
            tabName = tabName.Trim();

            var stateTab = GetOrCreateTab(tabName);

            var stateContainer = new VisualElement();
            stateTab.Add(stateContainer);

            if (isArrayElement)
            {
                var iterator = property.Copy();
                var endProperty = iterator.GetEndProperty();

                iterator.NextVisible(true); // Ir para a primeira propriedade real

                while (!SerializedProperty.EqualContents(iterator, endProperty))
                {
                    var field = new PropertyField(iterator);
                    stateContainer.Add(field);
                    iterator.NextVisible(false);
                }

                if (stateContainer.childCount == 0)
                {
                    stateContainer.Add(new Label("Empty state..."));
                }
            }
            else
            {
                var stateField = new PropertyField();
                stateField.BindProperty(property);
                stateContainer.Add(stateField);
            }
        }

        public void AddToStateTab(string stateName, VisualElement element)
        {
            var tab = GetOrCreateTab(stateName);
            tab.Add(element);
        }

        public void AddToStateTab(int index, VisualElement element)
        {
            var tab = TabView.ElementAt(index);
            tab?.Add(element);
        }

        private Tab GetOrCreateTab(string tabName)
        {
            if (_empty)
            {
                _empty = false;
                TabViewHeader.Remove(TabViewHeader.Q<Label>("empty-label"));
            }

            var tab = TabView.Q<Tab>(tabName);
            if (tab == null)
            {
                tab = new Tab(tabName)
                {
                    name = tabName,
                    viewDataKey = viewDataKey + "SelectedTabIndex" + tabName,
                };
                TabView.Add(tab);
            }

            return tab;
        }

        // private void ShowReorderPopup()
        // {
        //     var stateNames = TabView.Children().Select(tab => tab.name).ToList();
        //     var popup = new PriorityReorderablePopup(stateNames, OnReorder);
        //     PopupWindow.Show(new Rect(TabView.worldBound.x, TabView.worldBound.y, 200, 150), popup);
        // }
        //
        // private void OnReorder(int oldIndex, int newIndex)
        // {
        //     if (_property is null)
        //         return;
        //
        //     _property.MoveArrayElement(oldIndex, newIndex);
        //     _property.serializedObject.ApplyModifiedProperties();
        // }
        //
        // public class PriorityReorderablePopup : PopupWindowContent
        // {
        //     private List<string> _stateNames;
        //     private Action<int, int> _onReorder;
        //
        //     public PriorityReorderablePopup(List<string> stateNames, Action<int, int> onReorder)
        //     {
        //         _stateNames = stateNames;
        //         _onReorder = onReorder;
        //     }
        //
        //     public override Vector2 GetWindowSize()
        //     {
        //         return new Vector2(200, 150);
        //     }
        //
        //     public override VisualElement CreateGUI()
        //     {
        //         var root = new VisualElement();
        //
        //         var label = new Label("Selecione os estados que deseja reordenar:");
        //         root.Add(label);
        //
        //         var listView = new ListView(_stateNames, 20, MakeItem, BindItem)
        //         {
        //             selectionType = SelectionType.None,
        //             reorderable = true,
        //             headerTitle = "Reordenar Estados",
        //             showBorder = true,
        //             allowAdd = false,
        //             allowRemove = false,
        //             showAlternatingRowBackgrounds = AlternatingRowBackground.All
        //         };
        //         listView.itemIndexChanged += _onReorder;
        //         root.Add(listView);
        //
        //         return root;
        //     }
        //
        //     private VisualElement MakeItem()
        //     {
        //         return new Label();
        //     }
        //
        //     private void BindItem(VisualElement element, int index)
        //     {
        //         ((Label)element).text = _stateNames[index]; //TabView.Children().ElementAt(index).name;
        //     }
        // }
    }
}