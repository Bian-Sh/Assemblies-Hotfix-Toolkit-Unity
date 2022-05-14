//#define UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
using UnityEngine;
namespace zFramework.Hotfix.Toolkit
{
    #region Inspector Draw
    [CustomEditor(typeof(AssemblyHotfixManager))]
    public class AssemblyHotfixManagerEditor : Editor
    {
        string HuatuoVersionPath = default;
        string url = @"https://github.com/focus-creative-games/huatuo_upm";
        GUIStyle style;
        private void OnEnable()
        {
            HuatuoVersionPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, ".huatuo");
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
        [Header("热更文件测试模式："), ReadOnly, Tooltip("瞅了一眼，还是可以实现的，但暂时不支持。")]
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
        /// </summary>
        internal class AssemblyFilterHandler : IFilterBuildAssemblies
        {
            int IOrderedCallback.callbackOrder => 0;
            string[] IFilterBuildAssemblies.OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
            {
                var hotfixAssemblies = Instance.assemblies.Select(v => v.Dll).ToList();
                return assemblies.Where(ass => hotfixAssemblies.All(dll => !ass.EndsWith(dll, StringComparison.OrdinalIgnoreCase))).ToArray();
            }
        }
        #endregion

        #region Post Build Handler for IL2cpp
        internal class PostBuildHandler :
#if UNITY_ANDROID
         UnityEditor.Android.IPostGenerateGradleAndroidProject
#else
         IPostprocessBuildWithReport
#endif
        {
            public int callbackOrder => 0;
#if UNITY_ANDROID
            public void OnPostGenerateGradleAndroidProject(string path) => AddBackHotFixAssembliesToJson(path);
#else
            public void OnPostprocessBuild(BuildReport report) => AddBackHotFixAssembliesToJson(report.summary.outputPath);
#endif
            private void AddBackHotFixAssembliesToJson(string path)
            {
                /*
                  il2cpp: ScriptingAssemblies.json 文件中记录了所有的dll名称，此列表在游戏启动时自动加载，
                  不在此列表中的dll在资源反序列化时无法被找到其类型,因此 OnFilterAssemblies 中移除的条目需要再加回来
                  mono: 无须添加回来
                 */
                var targetgroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                var backend = PlayerSettings.GetScriptingBackend(targetgroup);
                if (backend != ScriptingImplementation.Mono2x)
                {
                    string[] files = Directory.GetFiles(Path.GetDirectoryName(path), "ScriptingAssemblies.json", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        string content = File.ReadAllText(file);
                        var data = JsonUtility.FromJson<ScriptingAssemblies>(content);
                        var assemblies = Instance.assemblies.Select(v => v.Dll);
                        foreach (string name in assemblies)
                        {
                                data.names.Add(name);
                                data.types.Add(16); // user dll type
                        }
                        content = JsonUtility.ToJson(data);
                        File.WriteAllText(file, content);
                    }
                }
            }
            [Serializable]
            public class ScriptingAssemblies
            {
                public List<string> names;
                public List<int> types;
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
            return ReturnCode.Success;
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
