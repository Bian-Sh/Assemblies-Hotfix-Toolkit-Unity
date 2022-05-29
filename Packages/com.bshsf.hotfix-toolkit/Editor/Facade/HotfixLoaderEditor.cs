using UnityEditor;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    [CustomEditor(typeof(HotfixLoader))]
    public class HotfixLoaderEditor : Editor
    {
        SerializedProperty data;
        SerializedObject so;
        HotfixLoader loader;
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            if (null == data)
            {
                data = serializedObject.FindProperty("hotfixAssemblies");
            }
            if (!loader)
            {
                loader = target as HotfixLoader;
            }

            var rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.foldoutHeader);
            var f_rect = new Rect(rect);
            f_rect.width = EditorGUIUtility.labelWidth;
            data.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(f_rect, data.isExpanded, "");
            EditorGUI.EndFoldoutHeaderGroup();
            EditorGUI.PropertyField(rect, data);

            EditorGUI.indentLevel++;
            var editorAsset = loader.hotfixAssemblies.editorAsset;
            if (data.isExpanded && editorAsset)
            {
                if (null == so)
                {
                    so = new SerializedObject(editorAsset);
                }
                var asms = so.FindProperty("assemblies");
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    for (int i = 0; i < asms.arraySize; i++)
                    {
                        var prop = asms.GetArrayElementAtIndex(i);
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.PropertyField(prop);
                        }
                        rect = GUILayoutUtility.GetLastRect();
                        rect.x += EditorGUIUtility.labelWidth;
                        rect.y += 1;
                        rect.height -= 2;
                        rect.width -= rect.x;
                        if (Event.current.type == EventType.MouseDown)
                        {
                            if (rect.Contains(Event.current.mousePosition))
                            {
                                EditorGUIUtility.PingObject(editorAsset.assemblies[i].editorAsset);
                                Event.current.Use();
                            }
                        }
                    }
                }
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.HelpBox("todo：自动处理被加载的程序集顺序问题", MessageType.Info);
        }
    }
}
