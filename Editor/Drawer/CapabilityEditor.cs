using System;
using System.Reflection;
using MasterSM;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSM.Editor
{
    [CustomEditor(typeof(BaseCapability<,>), true)]
    [CanEditMultipleObjects]
    public class CapabilityEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var fieldsFoldout = new Foldout { text = "General Fields", value = true };
            root.Add(fieldsFoldout);

            var statesTabView = new TabView
            {
                style =
                {
                    marginTop = 10,
                },
                viewDataKey = serializedObject.targetObject.GetInstanceID() + "SelectedTabIndex",
            };
            root.Add(statesTabView);

            var tabViewHeader = statesTabView.Q<VisualElement>("unity-tab-view__header-container");
            tabViewHeader.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
            tabViewHeader.style.borderBottomWidth = 3;
            tabViewHeader.style.borderBottomColor = new Color(0, 0, 0, 0.35f);
            tabViewHeader.style.flexWrap = Wrap.Wrap;

            var iterator = serializedObject.GetIterator();
            iterator.Next(true);
            iterator.NextVisible(false); // Skip script field

            while (iterator.NextVisible(false))
            {
                var propertyType = GetFieldType(iterator);

                if (propertyType != null && IsDerivedFrom(propertyType, typeof(BaseState<,>)))
                {
                    var tabName = iterator.displayName;
                    if (tabName.EndsWith("State"))
                        tabName = tabName[..^5];
                    tabName = tabName.Trim();
                    
                    var stateTab = GetOrCreateTab(statesTabView, tabName);
                    statesTabView.Add(stateTab);

                    var stateContainer = new VisualElement();
                    stateTab.Add(stateContainer);

                    var stateField = new PropertyField(iterator.Copy());
                    stateContainer.Add(stateField);
                }
                else if (HasStateAttribute(iterator, out var stateName))
                {
                    var stateTab = GetOrCreateTab(statesTabView, stateName);
                    var stateField = new PropertyField(iterator.Copy());
                    stateTab.Add(stateField);
                }
                else
                {
                    var field = new PropertyField(iterator.Copy());
                    fieldsFoldout.Add(field);
                }
            }

            if (statesTabView.childCount == 0)
            {
                tabViewHeader.Add(new Label("Capability without states")
                {
                    style =
                    {
                        marginTop = 5,
                        marginLeft = 5,
                        marginBottom = 5,
                        unityFontStyleAndWeight = FontStyle.BoldAndItalic
                    }
                });
            }

            return root;
        }

        private Tab GetOrCreateTab(TabView tabView, string tabName)
        {
            var tab = tabView.Q<Tab>(tabName);
            if (tab == null)
            {
                tab = new Tab(tabName)
                {
                    name = tabName,
                    viewDataKey = serializedObject.targetObject.GetInstanceID() + "SelectedTabIndex" + tabName,
                };
                tabView.Add(tab);
            }
            return tab;
        }
        
        private static Type GetFieldType(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                var typeName = property.managedReferenceFieldTypename; // Nome qualificado do tipo
                if (!string.IsNullOrEmpty(typeName))
                {
                    return Type.GetType(typeName);
                }
            }
            else if (property.propertyType == SerializedPropertyType.Generic)
            {
                var fieldInfo = GetFieldInfo(property);
                return fieldInfo?.FieldType;
            }

            return null;
        }

        private static bool HasStateAttribute(SerializedProperty property, out string stateName)
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
        
        private static FieldInfo GetFieldInfo(SerializedProperty property)
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

        private bool IsDerivedFrom(Type type, Type baseType)
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

    // [CustomEditor(typeof(BaseCapability<,>), true)]
    // public class CapabilityEditor : UnityEditor.Editor
    // {
    //     private object _capability;
    //
    //     public override VisualElement CreateInspectorGUI()
    //     {
    //         var root = new VisualElement();
    //         
    //         // var fieldsFoldout = new Foldout { text = "General Fields", value = true };
    //         // root.Add(fieldsFoldout);
    //         var capabilityContainer = new VisualElement();
    //         root.Add(capabilityContainer);
    //
    //         var statesTabView = new TabView
    //         {
    //             style =
    //             {
    //                 marginTop = 10,
    //             }
    //         };
    //         root.Add(statesTabView);
    //         
    //         var tabViewHeader = statesTabView.Q<VisualElement>("unity-tab-view__header-container");
    //         tabViewHeader.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.4f);
    //         tabViewHeader.style.borderBottomWidth = 3;
    //         tabViewHeader.style.borderBottomColor = new Color(0, 0, 0, 0.35f);
    //         tabViewHeader.style.flexWrap = Wrap.Wrap;
    //
    //         var iterator = serializedObject.GetIterator();
    //         iterator.Next(true);
    //         //Skip script field
    //         iterator.NextVisible(false);
    //         CreateFields(iterator, capabilityContainer, statesTabView);
    //         
    //         return root;
    //     }
    //
    //     private void CreateFields(SerializedProperty iterator, VisualElement rootContainer, TabView statesTabView = null)
    //     {
    //         while (iterator.NextVisible(false))
    //         {
    //             var propertyType = GetFieldType(iterator);
    //
    //             // If the field is a state, create a tab for it
    //             if (propertyType != null && IsDerivedFrom(propertyType, typeof(BaseState<,>)))
    //             {
    //                 var stateTab = new Tab(iterator.displayName)
    //                 {
    //                     name = iterator.name
    //                 };
    //                 statesTabView?.Add(stateTab);
    //                 
    //                 var stateContainer = new VisualElement();
    //                 stateTab.Add(stateContainer);
    //
    //                 var stateIterator = iterator.Copy();
    //                 stateIterator.Next(true);
    //                 CreateFields(stateIterator, stateContainer, statesTabView);
    //             }
    //             else if (HasStateAttribute(iterator, out var stateName))
    //             {
    //                 // Get or create the state tab
    //                 var stateTab = statesTabView.Q<Tab>(stateName);
    //                 if (stateTab == null)
    //                 {
    //                     stateTab = new Tab(stateName)
    //                     {
    //                         name = stateName
    //                     };
    //                     statesTabView.Add(stateTab);
    //                     var stateContainer = new VisualElement();
    //                     stateTab.Add(stateContainer);
    //                 }
    //
    //                 // Add the field to the state tab
    //                 stateTab.Q<VisualElement>().Add(new PropertyField(iterator.Copy()));
    //             }
    //             // If the field is a state extension, create a foldout for it
    //             // else if (propertyType != null && IsDerivedFrom(propertyType, typeof(StateExtension<,>)))
    //             // {
    //             //     var extensionFoldout = new Foldout { text = iterator.displayName, value = true };
    //             //     rootContainer.Add(extensionFoldout);
    //             //     
    //             //     var extensionIterator = iterator.Copy();
    //             //     extensionIterator.Next(true);
    //             //     CreateFields(extensionIterator, extensionFoldout);
    //             // }
    //             else
    //             {
    //                 rootContainer.Add(new PropertyField(iterator.Copy()));
    //             }
    //         }
    //     }
    //     
    //     private Type GetFieldType(SerializedProperty property)
    //     {
    //         var targetObject = target.GetType();
    //         var field = targetObject.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //         return field?.FieldType;
    //     }
    //     
    //     private bool HasStateAttribute(SerializedProperty property, out string stateName)
    //     {
    //         var targetObject = target.GetType();
    //         var field = targetObject.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    //         if (field == null)
    //         {
    //             stateName = null;
    //             return false;
    //         }
    //         
    //         var attributes = field.GetCustomAttributes(typeof(StateTabAttribute), false);
    //         if (attributes.Length == 0)
    //         {
    //             stateName = null;
    //             return false;
    //         }
    //         
    //         var attribute = (StateTabAttribute)attributes[0];
    //         stateName = attribute.Name;
    //         return true;
    //     }
    //
    //     private bool IsDerivedFrom(Type type, Type baseType)
    //     {
    //         while (type != null && type != typeof(object))
    //         {
    //             if (type.IsGenericType && type.GetGenericTypeDefinition() == baseType)
    //                 return true;
    //
    //             type = type.BaseType;
    //         }
    //
    //         return false;
    //     }
    // }
}
