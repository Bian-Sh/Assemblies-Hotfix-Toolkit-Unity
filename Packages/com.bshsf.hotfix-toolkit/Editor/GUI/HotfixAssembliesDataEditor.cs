using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    [CustomEditor(typeof(HotfixAssembliesData))]
    public class HotfixAssembliesDataEditor : Editor
    {
        ReorderableList list;
        private void OnEnable()
        {
            var arr = serializedObject.FindProperty("assemblies");
            list = new ReorderableList(serializedObject, arr, false, false, false, false)
            {
                elementHeightCallback = OnElementHeightCallback,
                drawElementCallback = OnDrawElements
            };
        }

        private void OnDrawElements(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = list.serializedProperty.GetArrayElementAtIndex(index);
            var width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 68f;
            EditorGUI.PropertyField(rect, item);
            EditorGUILayout.Space(8);
            EditorGUIUtility.labelWidth = width;
        }

        private float OnElementHeightCallback(int index)
        {
            if (list.count>0)
            {
                var item = list.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(item);
            }
            else
            {
                return EditorGUIUtility.singleLineHeight * 2;
            }
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);
            serializedObject.Update();

            GUILayout.Label("转存的 .bytes 文件：", EditorStyles.boldLabel);
            var iterator = this.serializedObject.GetIterator();
            iterator.NextVisible(true);
            iterator.Next(false);
            iterator.Next(false);
            bool enable = GUI.enabled;
            GUI.enabled = false;
            list?.DoLayoutList();
            GUI.enabled = enable;
            var asms = serializedObject.FindProperty("assemblies");
            for (int i = 0; i < asms.arraySize; i++)
            {
                var rect = GUILayoutUtility.GetLastRect();
                rect.x = EditorGUIUtility.currentViewWidth - (EditorGUIUtility.hierarchyMode ? 50 : 45);
                rect.y -= 12f;
                rect.y += (EditorGUIUtility.singleLineHeight + 1.5f) * (i + 1);
                rect.height = EditorGUIUtility.singleLineHeight;
                rect.width -= rect.x - (EditorGUIUtility.hierarchyMode ? 10 : -8);
                if (GUI.Button(rect, bt_content))
                {
                    var editorAsset = target as HotfixAssembliesData;
                    EditorGUIUtility.PingObject(editorAsset.assemblies[i].editorAsset);
                    Event.current.Use();
                }
            }
            EditorGUILayout.HelpBox(message, MessageType.Info);
        }
        GUIContent bt_content = new GUIContent("Ping", "高亮转存的程序集 .bytes 文件");
        string message = @"1. 本配置不可修改，工具会根据程序集依赖顺序自动进行排序。
2. 部分操作不会触发本页配置的自动排序，可以手动处理即可
3. Ctrl+Z、Ctrl+Y 、ReorderableList Header 右键 Delete 不触发自动排序";
    }
}
