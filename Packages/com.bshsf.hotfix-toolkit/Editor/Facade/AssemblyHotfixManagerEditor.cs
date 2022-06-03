using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
namespace zFramework.Hotfix.Toolkit
{
    [CustomEditor(typeof(AssemblyHotfixManager))]
    public class AssemblyHotfixManagerEditor : Editor
    {
        string HuatuoVersionPath = default;
        string url = @"https://github.com/focus-creative-games/huatuo_upm";
        GUIStyle style;
        ReorderableList list;
        private void OnEnable()
        {
            HuatuoVersionPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, ".huatuo");
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("assemblies"), true, true, true, true);
         //   list.
        }
        public override void OnInspectorGUI()
        {
            var targetgroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var backend = PlayerSettings.GetScriptingBackend(targetgroup);
            var is_Huatuo_Installed = File.Exists(HuatuoVersionPath);
            if (backend != ScriptingImplementation.Mono2x && !is_Huatuo_Installed)
            {
                if (style == null)
                {
                    style = new GUIStyle(EditorStyles.helpBox);
                    style.wordWrap = true;
                    style.richText = true;
                }
                var label = EditorGUIUtility.TrTextContentWithIcon($"请安装 <a url=\"{url}\"> Huatuo</a> 以支持代码后端为 IL2CPP 的程序集热更！", MessageType.Warning);
                EditorGUILayout.LabelField(label, style);
                var rect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                if (GUI.Button(rect, new GUIContent("", "点击访问 Huatuo 安装器托管仓库"), GUIStyle.none))
                {
                    Application.OpenURL(url);
                }
            }
            else
            {
                var disable = EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling;
                using (var dsa = new EditorGUI.DisabledGroupScope(disable))
                {
                    this.serializedObject.Update();
                    var iterator = this.serializedObject.GetIterator();
                    // go to child
                    iterator.NextVisible(true);
                    // skip name
                    iterator.Next(false);
                    // skip EditorClassIdentifier
                    iterator.Next(false);
                    // 遍历每一个属性并绘制
                    while (iterator.Next(false))
                    {
                     //   if (iterator.name != "assemblies")
                        {
                            EditorGUILayout.PropertyField(iterator);
                        }
                    }
                    //list.DoLayoutList();
                    this.serializedObject.ApplyModifiedProperties();
                }
                if (disable)
                {
                    EditorGUILayout.HelpBox("在编辑器播放、编译时不可进行修改！", MessageType.Info);
                }
            }
        }
    }
}
