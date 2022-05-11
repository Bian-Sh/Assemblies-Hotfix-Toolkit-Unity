using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using UnityEditorInternal;
using UnityEngine;
namespace zFramework.Hotfix.Toolkit
{
    #region Inspector Draw
    [CustomEditor(typeof(AssemblyHotfixManager))]
    public class AssemblyHotfixManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var targetgroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var backend = PlayerSettings.GetScriptingBackend(targetgroup);
            if (backend != ScriptingImplementation.Mono2x)
            {
                var content = EditorGUIUtility.TrTextContent("此 Assembly 热更方案仅支持 mono scripting backend ！");
                var rect = GUILayoutUtility.GetRect(content, EditorStyles.helpBox, GUILayout.Height(36));
                EditorGUI.HelpBox(rect, content.text, MessageType.Warning);
                var h = EditorGUIUtility.singleLineHeight;
                rect.y += rect.height / 2f - h / 2f;
                rect.x = rect.width - 40;
                rect.width = 50;
                rect.height = h;
                if (GUI.Button(rect, new GUIContent("fix", "点击将修改 scriptingbackend 为 il2cpp")))
                {
                    PlayerSettings.SetScriptingBackend(targetgroup, ScriptingImplementation.Mono2x);
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
                        EditorGUILayout.PropertyField(iterator);
                    }
                    this.serializedObject.ApplyModifiedProperties();
                }
                if (disable)
                {
                    EditorGUILayout.HelpBox("在编辑器播放、编译时不可进行修改！", MessageType.Info);
                }
            }
        }
    }
    #endregion
    public class AssemblyHotfixManager : ScriptableObject
    {
        #region Fields
        [Header("热更 DLL 存储的文件展名："), ReadOnly]
        public string fileExtension = ".bytes";
        [Header("热更文件测试模式：")]
        public bool testLoad = false;
        [Header("需要热更的程序集定义文件：")]
        public List<AssemblyData> assemblies;
        #endregion

        #region 单例
        public static AssemblyHotfixManager Instance => LoadConfiguration();
        static AssemblyHotfixManager instance;
        private static AssemblyHotfixManager LoadConfiguration()
        {
            if (!instance)
            {
                var guids = AssetDatabase.FindAssets($"{nameof(AssemblyHotfixManager)} t:Script");
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                path = path.Substring(0, path.LastIndexOf("/Editor/"));
                var file = $"{path}/Data/{ObjectNames.NicifyVariableName(nameof(AssemblyHotfixManager))}.asset";
                instance = AssetDatabase.LoadAssetAtPath<AssemblyHotfixManager>(file);
                if (!instance)
                {
                    instance = CreateInstance(nameof(AssemblyHotfixManager)) as AssemblyHotfixManager;
                    AssetDatabase.CreateAsset(instance, file);
                    AssetDatabase.Refresh();
                }
            }
            return instance;
        }
        #endregion

        #region Filter Assembly files when build application
        /// <summary>
        /// 所有热更新 dll在 Build 时需要剥离出来
        /// <br>并且不需要将 dll 名称写入到 ScriptingAssemblies.json 中</br>
        /// <br>这个动作仅适用于 mono 编译的 dll，IL2CPP 则不适用</br>
        /// </summary>
        internal class AssemblyFilterHandler : IFilterBuildAssemblies
        {
            int IOrderedCallback.callbackOrder => 0;
            string[] IFilterBuildAssemblies.OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                // 将热更dll从打包列表中移除
                var hotfixAssemblies = Instance.assemblies.Select(v => v.Dll).ToList();
                return assemblies.Where(ass => hotfixAssemblies.All(dll => !ass.EndsWith(dll, StringComparison.OrdinalIgnoreCase))).ToArray();
            }
        }
        #endregion

        #region Addressable Post Script Build Process
        [InitializeOnLoadMethod]
        static void InstallContentPipelineListener() => ContentPipeline.BuildCallbacks.PostScriptsCallbacks += PostScriptsCallbacks;
        public static ReturnCode PostScriptsCallbacks(IBuildParameters parameters, IBuildResults results) => StoreHotfixAssemblies(parameters.ScriptOutputFolder);

        #endregion

        #region Force Reload Assemblies
        public static void ForceLoadAssemblies()
        {
            var buildDir = Application.temporaryCachePath;
            var files = new DirectoryInfo(buildDir).GetFiles();
            foreach (var file in files)
            {
                FileUtil.DeleteFileOrDirectory(file.FullName);
            }
            var target = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(target);
            ScriptCompilationSettings scs = default;
            scs.group = group;
            scs.target = target;
            PlayerBuildInterface.CompilePlayerScripts(scs, buildDir);
            StoreHotfixAssemblies(buildDir);
        }
        #endregion

        #region Assistance Typs And Functions
        private static ReturnCode StoreHotfixAssemblies(string src)
        {
            var lib_dir = Path.Combine(Application.dataPath, "..", "Library\\ScriptAssemblies");
            foreach (var item in Instance.assemblies)
            {
                if (item.IsValid)    // 如果配置正确则尝试转存储文件
                {
                    var file = new FileInfo(Path.Combine(lib_dir, item.Dll));
                    var lastWriteTime = file.LastWriteTime.Ticks;
                    if (item.lastWriteTime < lastWriteTime)
                    {
                        item.lastWriteTime = lastWriteTime;
                        FileUtil.ReplaceFile(Path.Combine(src, item.Dll), item.OutputPath);
                        item.UpdateInformation();
                    }
                }
                else
                {
                    Debug.LogError($"{nameof(AssemblyHotfixManager)}: 请先完善 Hotfix Configuration 配置项！");
                    return ReturnCode.Exception;
                }
            }
            return ReturnCode.Exception;
        }

        [Serializable]
        public class AssemblyData : ISerializationCallbackReceiver
        {
            [Header("热更的程序集")]
            public AssemblyDefinitionAsset assembly;
            [Header("Dll 转存文件夹"), FolderValidate]
            public DefaultAsset folder;
#pragma warning disable IDE0052 // 删除未读的私有成员
            [SerializeField, Header("Dll 热更文件"), ReadOnly]
            TextAsset hotfixAssembly;
            [SerializeField, Header("Dll 最后更新时间"), ReadOnly]
            string lastUpdateTime;
#pragma warning restore IDE0052 // 删除未读的私有成员
            [NonSerialized]
            SimplifiedAssemblyData data; //在Unity中，类类型字段在可序列化对象中永不为 null，故而声明：NonSerialized
            // 避免频繁的加载数据（因为data未参与序列化且调用前脚本Reload过，所以在本应用场景下能够保证加载的数据总是最新的）
            SimplifiedAssemblyData Data => data ?? (data = JsonUtility.FromJson<SimplifiedAssemblyData>(assembly.text));
            [HideInInspector]
            public long lastWriteTime;
            public string OutputPath => $"{AssetDatabase.GetAssetPath(folder)}/{Data.name}{Instance.fileExtension}";
            public string Dll => $"{Data.name}.dll";
            public bool IsValid => assembly && folder && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));
            public bool AllowUnsafeCode => Data.allowUnsafeCode;
            internal void UpdateInformation()
            {
                AssetDatabase.Refresh();
                hotfixAssembly = AssetDatabase.LoadMainAssetAtPath(OutputPath) as TextAsset;
                lastUpdateTime = new DateTime(lastWriteTime).ToString("yyyy-MM-dd HH:mm:ss");
                EditorUtility.SetDirty(Instance);
            }
            public void OnBeforeSerialize() { }

            public void OnAfterDeserialize()
            {
                if (null == assembly)
                {
                    hotfixAssembly = null;
                    lastUpdateTime = string.Empty;
                }
            }
        }

        [Serializable]
        public class SimplifiedAssemblyData
        {
            public string name;
            public bool allowUnsafeCode;
        }
        #endregion

    }
}
