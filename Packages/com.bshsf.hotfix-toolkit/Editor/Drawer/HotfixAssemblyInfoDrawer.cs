using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    [CustomPropertyDrawer(typeof(HotfixAssemblyInfo))]
    public class HotfixAssemblyInfoDrawer : PropertyDrawer
    {
        static Dictionary<string, DrawerState> drawerState = new Dictionary<string, DrawerState>();
        SimpleAssemblyInfo info = new SimpleAssemblyInfo();
        internal class DrawerState
        {
            public GUIStyle style;
            public float height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight + 2;
            var indent = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;

            #region Init Related  Propertys
            var assemblis = property.FindPropertyRelative("assembly");



            var bytesAsset = property.FindPropertyRelative("bytesAsset");
            var asm = assemblis.objectReferenceValue as AssemblyDefinitionAsset;
            var bts = bytesAsset.objectReferenceValue as TextAsset;
            #endregion
            #region Store Drawer State
            var path = assemblis.propertyPath;
            if (!drawerState.TryGetValue(path, out var data))
            {
                data = new DrawerState();
                data.style = new GUIStyle(EditorStyles.foldout);
                drawerState[path] = data;
            }
            #endregion

            property.serializedObject.Update();
            EditorGUI.BeginProperty(position, label, property);

            #region Draw title
            string name = asm ? asm.name : "程序集未指定";
            var color = EditorStyles.foldout.normal.textColor;
            data.style.normal.textColor = asm ? color : Color.red;
            if (!EditorGUIUtility.hierarchyMode)
            {
                EditorGUI.indentLevel--;
            }
            EditorGUIUtility.labelWidth = position.width;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, new GUIContent(name), data.style);
            EditorGUIUtility.labelWidth = labelWidth;
            if (!EditorGUIUtility.hierarchyMode)
            {
                EditorGUI.indentLevel++;
            }
            #endregion
            var orign = position;
            if (property.isExpanded)
            {
                #region 绘制 程序集 定义文件字段
                position.y += position.height + 6;
                var field_rect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
                var message = string.Empty;
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.PropertyField(position, assemblis);

                    if (check.changed)
                    {
                        property.serializedObject.ApplyModifiedProperties();

                        #region Assembly 有效性校验

                        if (assemblis.objectReferenceValue is AssemblyDefinitionAsset asmdef &&asmdef)
                        {
                            #region 查重
                            var ahm = property.serializedObject.targetObject as AssemblyHotfixManager;
                            var isDuplicated = ahm.assemblies.Any(v => v.assembly && v.assembly.name == asmdef.name);
                            #endregion
                            if (!isDuplicated)
                            {
                                EditorJsonUtility.FromJsonOverwrite(asmdef.text, info);
                                var isEditorAssembly = null != info.includePlatforms && info.includePlatforms.Length == 1 && info.includePlatforms[0] == "Editor";
                                var isUsedByAssemblyCsharp = AssemblyHotfixManager.IsUsedByAssemblyCSharp(info.name);

                                if (isEditorAssembly)
                                {
                                    message = $"编辑器程序集不可热更！ ";
                                }
                                if (isUsedByAssemblyCsharp)
                                {
                                    message = $"被 Assembly-CSharp 相关程序集引用不可热更！ ";
                                }
                            }
                            else
                            {
                                message = $"已存在，无需重复添加！ ";
                            }
                        }
                        else
                        {
                                message = $"请指定需要热更的程序集！ ";
                        }
                        #endregion



                        #region 转存文件有效性验证：assembly 不为空且名称与 转存文件名称匹配
                        property.serializedObject.Update();

                        Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}:  some changed to {asm?.name} {message}");

                        if (bts && (!asm || !bts.name.Contains(asm.name)))
                        {
                            Undo.RecordObject(property.serializedObject.targetObject, "RemoveTypeMissmatchedBytesFile");
                            bytesAsset.objectReferenceValue = null;
                        }

                        #endregion

                    }
                }
                #endregion
                #region Draw Message
                Rect tip_rect = default;
                if (!string.IsNullOrEmpty(message))
                {
                    Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}: inside Draw Message {message}");
                    position.y += position.height + 4;
                    var height = EditorGUI.GetPropertyHeight(SerializedPropertyType.String, new GUIContent(message));
                    tip_rect = new Rect(position.x, position.y, position.width, height);
                    EditorGUI.HelpBox(tip_rect, message, MessageType.Warning);
                }
                #endregion


                #region 绘制 转存 dll 字段
                var enable = GUI.enabled;
                GUI.enabled = false;
                position.y += position.height + 4;
                EditorGUI.PropertyField(position, bytesAsset);
                GUI.enabled = enable;
                #endregion

            }
            drawerState[path].height = position.y - orign.y + EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var assemblis = property.FindPropertyRelative("assembly");
            drawerState.TryGetValue(assemblis.propertyPath, out var data);
            return data?.height??base.GetPropertyHeight(property,label);
        }

        [Serializable]
        public class SimpleAssemblyInfo
        {
            public string name;
            public string[] includePlatforms;
        }
    }
}
