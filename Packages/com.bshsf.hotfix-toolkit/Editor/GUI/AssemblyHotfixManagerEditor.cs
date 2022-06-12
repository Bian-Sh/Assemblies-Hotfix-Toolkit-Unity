namespace zFramework.Hotfix.Toolkit
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;
    using static GlobalConfiguration;

    [CustomEditor(typeof(AssemblyHotfixManager))]
    public class AssemblyHotfixManagerEditor : Editor
    {
        string HuatuoVersionPath = default;
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
            if (list.count > 0)
            {
                var item = list.serializedProperty.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(item);
            }
            return 0;
        }
        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (list.count > 0)
            {
                var item = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.x += 8;
                rect.width -= 3;
                EditorGUI.PropertyField(rect, item);
            }
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
                var label = EditorGUIUtility.TrTextContentWithIcon($"请安装 <a url=\"{HuatuoRepo}\"> Huatuo</a> 以支持代码后端为 IL2CPP 的程序集热更！", MessageType.Warning);
                EditorGUILayout.LabelField(label, style);
                var rect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                if (GUI.Button(rect, new GUIContent("", "点击访问 Huatuo 安装器托管仓库"), GUIStyle.none))
                {
                    Application.OpenURL(HuatuoRepo);
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
                                GUIContent content = default;
                                if (iterator.name == "folder")
                                {
                                    content = folder_content;
                                }
                                else if (iterator.name == "groupName")
                                {
                                    content = groupName_content;
                                }
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    var labelWidth = EditorGUIUtility.labelWidth;
                                    //EditorGUIUtility.labelWidth = 10;
                                    EditorGUILayout.LabelField(content, EditorStyles.boldLabel, GUILayout.Width(160));
                                    //EditorGUIUtility.labelWidth = labelWidth;
                                    EditorGUILayout.PropertyField(iterator, GUIContent.none);
                                }
                                EditorGUILayout.Space(6);
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
        GUIContent folder_content = new GUIContent("DLL 转存文件夹：", "用于存储需要热更的程序集的转存（.bytes）文件。");
        GUIContent groupName_content = new GUIContent("默认可寻址组名：", "用于生成默认的AA Group 以存储程序集转存 （.bytes） 文件。");
    }
}
