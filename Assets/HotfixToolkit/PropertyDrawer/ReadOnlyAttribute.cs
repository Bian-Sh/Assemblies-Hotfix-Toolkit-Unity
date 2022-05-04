using UnityEditor;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    /// <summary>
    /// 输入框只读
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label, true);
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}