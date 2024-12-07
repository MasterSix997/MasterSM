using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AdvancedSM.Editor
{
    [CustomPropertyDrawer(typeof(StateExtension<,>), true)]
    public class StateExtensionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            
            var foldout = new Foldout
            {
                text = $"{property.displayName} ({property.type})",
                style =
                {
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0, 0, 0, 1),
                    borderLeftWidth = 1,
                    borderLeftColor = new Color(0, 0, 0, 1),
                },
                viewDataKey = $"{property.serializedObject.targetObject.GetInstanceID()}_{property.propertyPath}",
            };
            
            foldout.Q<Toggle>().style.marginLeft = 0;

            var foldoutArrow = foldout.Q<VisualElement>("unity-checkmark");
            var header = foldoutArrow.parent;
            header.style.height = 25;
            header.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f, 0.4f);
            header.style.borderBottomWidth = 2;
            header.style.borderBottomColor = new Color(0, 0, 0, 0.35f);
            
            var text = header.Q<Label>();
            text.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            header.Remove(foldoutArrow);

            var toggleEnabled = new Toggle
            {
                style =
                {
                    marginLeft = 5,
                    marginRight = 5,
                }
            };
            toggleEnabled.BindProperty(property.FindPropertyRelative("enabled"));
            header.Insert(0, toggleEnabled);
            
            root.Add(foldout);
            
            if (!property.Next(true))
            {
                foldout.Add(new Label("Empty extension..."));
                return root;
            }
            
            var minDepth = property.depth;
            while (property.NextVisible(false))
            {
                if (property.depth < minDepth)
                    break;
                
                foldout.Add(new PropertyField(property));
            }

            if (foldout.childCount == 0)
            {
                foldout.Add(new Label("Empty extension..."));
            }
            
            return root;
        }
    }
}