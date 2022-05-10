using UnityEditor;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    /// <summary>
    /// 文件夹校验
    /// </summary>
    public class FolderValidateAttribute : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(FolderValidateAttribute))]
    public class FolderValidateAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var folder = property.objectReferenceValue as DefaultAsset;
            var path = AssetDatabase.GetAssetPath(folder);
            var valid = AssetDatabase.IsValidFolder(path);
            if (!valid)
            {
                property.objectReferenceValue = null;
                var pos = new Rect(position);
                pos.x = position.width - 80;
                pos.y = position.y - 20;
                var color = GUI.color;
                GUI.color = Color.red;
                GUI.Label(pos, "↓ 请指定文件夹 ↓");
                GUI.color = color;
            }
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}