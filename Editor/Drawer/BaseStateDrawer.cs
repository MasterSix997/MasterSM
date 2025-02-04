using MasterSM.Editor.Elements;
using MasterSM.Editor.Utils;
using UnityEditor;
using UnityEngine.UIElements;
using Exception = System.Exception;

namespace MasterSM.Editor.Drawer
{
    // [CustomPropertyDrawer(typeof(BaseState<,>), true)]
    // [CustomPropertyDrawer(typeof(SubStateMachine<,,>), true)]
    [CustomPropertyDrawer(typeof(IState<,>), true)]
    internal class BaseStateDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            bool isFoldout;
            try
            {
                isFoldout = !EditorUtils.IsDerivedFrom(EditorUtils.GetParentObject(property).GetType(), typeof(BaseCapability<,>), 2);
            }
            catch (Exception)
            {
                isFoldout = true;
            }

            return new BaseStateContainer(property, isFoldout);
        }
    }
}