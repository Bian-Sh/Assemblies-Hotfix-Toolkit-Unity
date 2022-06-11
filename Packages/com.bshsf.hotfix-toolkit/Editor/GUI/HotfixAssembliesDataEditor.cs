using UnityEditor;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    [CustomEditor(typeof(HotfixAssembliesData))]
    public class HotfixAssembliesDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            bool enable = GUI.enabled;
            GUI.enabled = false;
            base.OnInspectorGUI();
            GUI.enabled = enable;
            var asms = serializedObject.FindProperty("assemblies");
            if (asms.isExpanded)
            {
                for (int i = 0; i < asms.arraySize; i++)
                {
                    var prop = asms.GetArrayElementAtIndex(i);
                    var rect = GUILayoutUtility.GetLastRect();
                    rect.x = EditorGUIUtility.currentViewWidth - 50;
                    rect.y += 8;
                    rect.y += (EditorGUIUtility.singleLineHeight + 1) * (i + 1);
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.width -= rect.x - 10;
                    if (GUI.Button(rect, bt_content))
                    {
                        var editorAsset = target as HotfixAssembliesData;
                        EditorGUIUtility.PingObject(editorAsset.assemblies[i].editorAsset);
                        Event.current.Use();
                    }
                }
            }
            EditorGUILayout.HelpBox("本配置不可修改，程序集依赖顺序自动处理。", MessageType.Info);
            if (GUILayout.Button("Clear Data"))
            {
                var editorAsset = target as HotfixAssembliesData;
                editorAsset.assemblies.Clear();
            }
        }
        GUIContent bt_content = new GUIContent("Ping", "高亮转存的程序集 .bytes 文件");
    }
}
