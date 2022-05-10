using UnityEditor;
namespace zFramework.Hotfix.Toolkit
{
    public class HotfixedConfigWindow : EditorWindow
    {
        private Editor editor;
        [MenuItem("Tools/Hotfixed")]
        static void OpenWindow()
        {
            var window = GetWindow<HotfixedConfigWindow>(true, "Dll 热更处理配置工具", true);
            window.editor = Editor.CreateEditor(AssemblyHotfixManager.Instance);
        }
        private void OnGUI()
        {
            editor = editor ?? Editor.CreateEditor(AssemblyHotfixManager.Instance);
            editor.OnInspectorGUI();
        }
    }
}