//#define UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    [SingletonParam(container)]
    public partial class AssemblyHotfixManager : ScriptableObjectSingleton<AssemblyHotfixManager>
    {
        #region Fields
        [Header("热更 DLL 存储的文件展名："), ReadOnly]
        public string fileExtension = ".bytes";

        [Header("Dll 转存文件夹"), FolderValidate]
        public DefaultAsset folder;

        [Header("需要热更的程序集定义文件：")]
        public List<HotfixAssemblyInfo> assemblies;

        const string container = "AssemblyHotfixToolkit";

        private bool IsFolderValid => folder && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));
        private static IEnumerable<AssemblyName> references;
        private static SimpleAssemblyInfo info = new SimpleAssemblyInfo();
        #endregion

        #region Scriptable Life 
        private void OnEnable()
        {
            if (!IsFolderValid)
            {
                var path = $"Assets/{container}/Addressable";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    AssetDatabase.Refresh();
                }
                folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
                EditorUtility.SetDirty(this);
            }
            references = AppDomain.CurrentDomain.GetAssemblies()
                                                                  .Where(v => v.FullName.Contains("Assembly-CSharp"))
                                                                  .SelectMany(v => v.GetReferencedAssemblies());
        }
        #endregion

        #region Assemblies Validate
        /// <summary>
        /// 校验是否被 Assembly-CSharp 等相关的程序集引用
        /// </summary>
        /// <param name="name">需要校验的程序集名称</param>
        /// <returns></returns>
        public static bool IsUsedByAssemblyCSharp(AssemblyDefinitionAsset asset)
        {
            EditorJsonUtility.FromJsonOverwrite(asset.text, info);
            return references.Any(v => v.Name.Equals(info.name));
        }

        /// <summary>
        ///  校验程序集是否重复
        /// </summary>
        /// <param name="name">需要校验的程序集名称</param>
        /// <returns></returns>
        public static bool IsAssemblyDuplicated(AssemblyDefinitionAsset asset) => Instance.assemblies.Count(v => v.assembly && v.assembly == asset) > 1;

        /// <summary>
        /// 校验程序集是否为 编辑器 程序集
        /// </summary>
        /// <param name="asset">需要校验的 程序集名称</param>
        /// <returns></returns>
        public static bool IsEditorAssembly(AssemblyDefinitionAsset asset)
        {
            EditorJsonUtility.FromJsonOverwrite(asset.text, info);
            return null != info.includePlatforms && info.includePlatforms.Length == 1 && info.includePlatforms[0] == "Editor";
        }

        public static string GetAssemblyName(AssemblyDefinitionAsset asset)
        {
            if (asset)
            {
                EditorJsonUtility.FromJsonOverwrite(asset.text, info);
                return info.name;
            }
            else
            {
                return string.Empty;
            }
        }
        public new static void SetDirty() 
        {
            EditorUtility.SetDirty(Instance);
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
                var hotfixAssemblies = Instance.assemblies.Select(v => $"{v.assembly.name}.dll").ToList();
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
                        var assemblies = Instance.assemblies.Select(v => $"{v.assembly.name}.dll");
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
            foreach (var item in Instance.assemblies)
            {
                if (Instance.IsFolderValid && item.assembly)    // 如果配置正确则尝试转存储文件
                {
                    var output = Path.Combine(AssetDatabase.GetAssetPath(Instance.folder), $"{item.assembly.name}{Instance.fileExtension}");
                    FileUtil.ReplaceFile(Path.Combine(src, $"{item.assembly.name}.dll"), output);

                    AssetDatabase.Refresh();
                    var asset = AssetDatabase.LoadMainAssetAtPath(output) as TextAsset;
                    item.bytesAsset = asset;
                    EditorUtility.SetDirty(Instance);
                }
                else
                {
                    Debug.LogError($"{nameof(AssemblyHotfixManager)}: 请先完善 Assembly Hotfix Manager 配置项！");
                    return ReturnCode.Exception;
                }
            }
            UpdateHotfixConfiguration();
            return ReturnCode.Success;
        }


        //todo: 自动构建文件夹，自动转变资产为 AA资产，自动构建 AA Group ，自动打组
        private static void UpdateHotfixConfiguration()
        {

        }

        [Serializable]
        public class SimpleAssemblyInfo
        {
            public string name;
            public string[] includePlatforms;
        }
        #endregion
    }
}
