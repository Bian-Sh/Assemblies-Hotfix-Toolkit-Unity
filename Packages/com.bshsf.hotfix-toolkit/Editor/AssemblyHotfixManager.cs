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
using UnityEngine;
namespace zFramework.Hotfix.Toolkit
{
    [SingletonParam("AssemblyHotfixToolkit")]
    public partial class AssemblyHotfixManager : ScriptableObjectSingleton<AssemblyHotfixManager>
    {
        #region Fields
        [Header("热更 DLL 存储的文件展名："), ReadOnly]
        public string fileExtension = ".bytes";

        [Header("需要热更的程序集定义文件：")]
        public List<AssemblyData> assemblies;
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
                    FileUtil.ReplaceFile(Path.Combine(src, item.Dll), item.OutputPath);
                    UpdateInformation(item);
                }
                else
                {
                    Debug.LogError($"{nameof(AssemblyHotfixManager)}: 请先完善 Hotfix Configuration 配置项！");
                    return ReturnCode.Exception;
                }
            }
            return ReturnCode.Success;
        }

        private static void UpdateInformation(AssemblyData item)
        {
            AssetDatabase.Refresh();
            // todo :直接转为 可寻址对象
            var asset = AssetDatabase.LoadMainAssetAtPath(item.OutputPath) as TextAsset;
            if ( asset!= item.hotfixAssembly)
            {
                item.hotfixAssembly = asset;
                Debug.Log($"{nameof(AssemblyHotfixManager)}: asset 替换成功");
            }
            else
            {
                Debug.Log($"{nameof(AssemblyHotfixManager)}: asset 未发生赋值");
            }
            EditorUtility.SetDirty(Instance);
        }
        #endregion
    }
}
