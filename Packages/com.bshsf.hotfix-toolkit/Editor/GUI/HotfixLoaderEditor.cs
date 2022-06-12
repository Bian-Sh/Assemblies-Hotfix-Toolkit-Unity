namespace zFramework.Hotfix.Toolkit
{
using UnityEditor;
using UnityEngine;
    using static GlobalConfiguration;

    [CustomEditor(typeof(HotfixLoader))]
    public class HotfixLoaderEditor : Editor
    {
        SerializedProperty data;
        SerializedObject so;
        HotfixLoader loader;
        private GUIContent fixbtContent = new GUIContent("fix", "点击构建并加载程序集转存文件");

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
            var pick_rect = new Rect(rect);
            pick_rect.x = pick_rect.width - 4;
            pick_rect.width = 22;
            GUI.Button(pick_rect, new GUIContent("", "此配置自动分配且不可修改！"), EditorStyles.miniButtonRight);
            EditorGUI.PropertyField(rect, data);

            EditorGUI.indentLevel++;
            var editorAsset = loader.hotfixAssemblies.editorAsset;
            if (data.isExpanded && editorAsset)
            {
                if (null == so)
                {
                    so = new SerializedObject(editorAsset);
                }
                so.Update();
                var asms = so.FindProperty("assemblies");
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    if (asms.arraySize == 0)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button(fixbtContent, EditorStyles.miniButtonMid, GUILayout.Width(60)))
                            {
                                var state_a = AssemblyHotfixManager.ForceLoadAssemblies();
                                var state_b = AssemblyHotfixManager.AssembliesBinaryHandler();
                                if (state_a||state_b)
                                {
                                    Debug.LogError($"Hotfix Toolkit 配置异常请确认！");
                                    EditorApplication.ExecuteMenuItem(MenuNode);
                                }
                            }
                            GUILayout.FlexibleSpace();
                        }
                    }
                    else
                    {
                        for (int i = 0; i < asms.arraySize; i++)
                        {
                            var prop = asms.GetArrayElementAtIndex(i);
                            using (new EditorGUI.DisabledScope(true))
                            {
                                EditorGUILayout.PropertyField(prop, GUIContent.none);
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
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.HelpBox("本配置不可修改，程序集依赖顺序自动处理。", MessageType.Info);
        }
    }
}
