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
        Vector2 pos;
        private void OnEnable()
        {
            HuatuoVersionPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, ".huatuo");
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("assemblies"), true, false, true, true);
            list.drawElementCallback = OnDrawElement;
            list.elementHeightCallback = OnGetElementHeight;
            list.onRemoveCallback = OnRemoveCallback;
        }

        #region Reorderable Drawer
        GUIContent header = new GUIContent("需要热更的程序集");
        private void OnRemoveCallback(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            list.serializedProperty.serializedObject.ApplyModifiedProperties();

            AssemblyHotfixManager.AssembliesBinaryHandler();
        }
        private float OnGetElementHeight(int index)
        {
            var item = list.serializedProperty.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(item);
        }
        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var item = list.serializedProperty.GetArrayElementAtIndex(index);
            rect.x += 8;
            rect.width -= 3;
            EditorGUI.PropertyField(rect, item);
        }
        #endregion



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
                        if (iterator.name == "assemblies")
                        {
                            GUILayout.Space(8);
                            iterator.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(iterator.isExpanded, header);
                            EditorGUILayout.EndFoldoutHeaderGroup();
                            var rect = GUILayoutUtility.GetLastRect();
                            rect.x = rect.width - (EditorGUIUtility.hierarchyMode ? 20f : 33f);
                            rect.width = 36;
                            EditorGUI.DelayedIntField(rect, list.count);
                            if (iterator.isExpanded)
                            {
                                using (var scroll = new EditorGUILayout.ScrollViewScope(pos))
                                {
                                    pos = scroll.scrollPosition;
                                    using (var check = new EditorGUI.ChangeCheckScope())
                                    {
                                        list.DoLayoutList();
                                        if (check.changed)
                                        {
                                            this.serializedObject.ApplyModifiedProperties();
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                EditorGUILayout.PropertyField(iterator);
                                if (check.changed)
                                {
                                    this.serializedObject.ApplyModifiedProperties();
                                }
                            }
                        }
                    }
                }
                if (disable)
                {
                    EditorGUILayout.HelpBox("在编辑器播放、编译时不可进行修改！", MessageType.Info);
                }
            }
        }
    }
}
