namespace zFramework.Hotfix.Toolkit
{
    using UnityEditor;
    using UnityEngine;
    using static AssemblyHotfixManager;
    public class HotfixedConfigWindow : EditorWindow
    {
        private Editor editor;
        [MenuItem("Tools/Hotfixed")]
        static void OpenWindow()
        {
            var window = GetWindow<HotfixedConfigWindow>("Dll 热更处理配置工具", true);

            window.editor = Editor.CreateEditor(AssemblyHotfixManager.Instance);
            window.minSize = new Vector2(360, 300);
        }
        private void OnGUI()
        {
            Editor.CreateCachedEditor(AssemblyHotfixManager.Instance, typeof(AssemblyHotfixManagerEditor), ref editor);
            GUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var bt_left = GUILayout.Button(BT_LT,EditorStyles.miniButtonLeft);
                var bt_right = GUILayout.Button(BT_RT,EditorStyles.miniButtonRight);
                if (bt_left)
                {

                }
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.VerticalScope())
            {
                editor?.OnInspectorGUI();
                GUILayout.FlexibleSpace();
                using (var hr = new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.FlexibleSpace();
                    var color = GUI.color;
                    GUI.color = new Color32(127, 214, 253, 255);
                    if (GUILayout.Button(new GUIContent("Force Build Assembly", "强制构建所有Player Assemblies！"), GUILayout.Height(36)))
                    {
                        ForceLoadAssemblies();
                    }
                    GUI.color = color;
                    GUILayout.FlexibleSpace();
                }
            }
        }
        GUIContent BT_LT = new GUIContent("Editor","编辑器下使用的配置");
        GUIContent BT_RT = new GUIContent("Runtime","运行时使用的配置");
    }
}