namespace zFramework.Hotfix.Toolkit
{
    using System;
    using UnityEditor;
    using UnityEngine;
    using static AssemblyHotfixManager;
    public class HotfixedConfigWindow : EditorWindow
    {
        private Editor editor;
        [MenuItem("Tools/Hotfixed")]
        static void OpenWindow()
        {
            var window = GetWindow<HotfixedConfigWindow>(true, "Dll 热更处理配置工具", true);
            window.editor = Editor.CreateEditor(Instance);
        }
        Vector2 pos;
        private void OnGUI()
        {
            Editor.CreateCachedEditor(Instance, typeof(AssemblyHotfixManagerEditor), ref editor);
            using (var scroll = new EditorGUILayout.ScrollViewScope(pos))
            {
                pos = scroll.scrollPosition;
                editor?.OnInspectorGUI();
            }
            using (var hr = new EditorGUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.FlexibleSpace();
                var color = GUI.color;
                GUI.color = new Color32(127,214,253,255);
                if (GUILayout.Button(new GUIContent("Force Build Assembly", "强制构建所有Player Assemblies！"), GUILayout.Height(36)))
                {
                    ForceLoadAssemblies();
                }
                GUI.color = color;
                GUILayout.FlexibleSpace();
            }
        }
    }
}