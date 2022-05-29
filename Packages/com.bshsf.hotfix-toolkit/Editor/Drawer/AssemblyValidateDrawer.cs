using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{

    [CustomPropertyDrawer(typeof(AssemblyValidateAttribute))]
    public class AssemblyValidateDrawer : PropertyDrawer
    {
        SimpleAssemblyInfo info = new SimpleAssemblyInfo();
        GUIStyle style;
        Rect rect = default;
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string message = string.Empty;
            if (property.objectReferenceValue)
            {
                var data = property.objectReferenceValue as AssemblyDefinitionAsset;
                EditorJsonUtility.FromJsonOverwrite(data.text, info);
                if (info.includePlatforms.Length == 1 && info.includePlatforms[0] == "Editor")
                {
                    message = "编辑器程序集不可热更";
                }
                ValidateReferences(info.name);
            }
            else
            {
                message = "↓指定热更程序集↓";
            }

            EditorGUI.PropertyField(position, property, label, true);
            if (!string.IsNullOrEmpty(message))
            {
                var pos = new Rect(position);
                pos.y = position.y - 20;
                if (style == null)
                {
                    style = new GUIStyle();
                    style.alignment = TextAnchor.MiddleRight;
                    style.normal.textColor = Color.red;
                }
                GUI.Label(pos, message, style);
            }
            rect = position;
        }

        private void ValidateReferences(string name)
        {
            //var assets = AssetDatabase.FindAssets();

        }

        [Serializable]
        public class SimpleAssemblyInfo
        {
            public string name;
            public string[] includePlatforms;
        }
    }
}