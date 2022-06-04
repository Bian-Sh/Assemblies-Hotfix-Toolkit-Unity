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
        private static IEnumerable<string> asmdefs;
        #endregion

        #region ScriptableObject Life time
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
            //获取所有有 Assembly-CSharp 字眼的程序集
            references = AppDomain.CurrentDomain.GetAssemblies()
                                                                  .Where(v => v.FullName.Contains("Assembly-CSharp"))
                                                                  .SelectMany(v => v.GetReferencedAssemblies());

            //获取所有非只读的，可能会引用热更程序集的 Runtime 程序集
            asmdefs = AssetDatabase.FindAssets("t:asmdef")
                                         .Where(v =>
                                         {
                                             var path = AssetDatabase.GUIDToAssetPath(v);
                                             path = Path.GetFullPath(path);
                                             return !path.Contains("\\PackageCache\\") && !path.Contains("\\Editor\\");
                                         });
        }
        #endregion

        #region Assemblies Validate And Assistant
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
        /// <summary>
        /// 校验程序集是否被其他程序集所引用
        /// </summary>
        /// <param name="asset">检查对象程序集</param>
        /// <param name="guid">检查目标程序集</param>
        /// <returns>true :被引用，false 未被引用</returns>
        public static bool IsSomeAssemblyReferenced(AssemblyDefinitionAsset asset,string guid)
        {
            EditorJsonUtility.FromJsonOverwrite(asset.text, info);
            return null != info.references && info.references.Contains($"GUID:{guid}");
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
        /// <summary>
        /// 获取引用了 热更程序集 的程序集
        /// </summary>
        /// <param name="target">热更程序集</param>
        /// <returns></returns>
        public static AssemblyDefinitionAsset[] GetAssembliesRefed(AssemblyDefinitionAsset target)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long _))
            {
                var asms = asmdefs.Where(v => !Instance.assemblies.Exists(x =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(v);
                    var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                    return x.assembly == asset;
                }))
                .Where(v =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(v);
                    var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                    return !IsEditorAssembly(asset)&& IsSomeAssemblyReferenced(asset,guid);
                })
                .Select(v =>
                {
                    var path = AssetDatabase.GUIDToAssetPath(v);
                    return AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                }).ToArray();
                return asms;
            }
            else
            {
                throw new Exception("Unity 资产转 guid 失败!");
            }
        }

        /// <summary>
        /// 像热更程序集中添加需要热更的程序集
        /// </summary>
        /// <param name="asset"></param>
        public static void AddAssemblyData(AssemblyDefinitionAsset asset)
        {
            var index = Instance.assemblies.FindIndex(v => v.assembly == asset);
            if (index == -1)
            {
                Undo.RecordObject(Instance, "CaptureForSomeAssemblyLoaded");
                var data = new HotfixAssemblyInfo
                {
                    assembly = asset
                };
                Instance.assemblies.Add(data);
                EditorUtility.SetDirty(Instance);
            }
        }

        /// <summary>
        /// 把所有的校验都走一遍，用于卡最后打包
        /// </summary>
        /// <returns>false : 校验不通过，true ：校验通过</returns>
        public static bool ValidateAll()
        {
            Func<HotfixAssemblyInfo, bool> Validate = info =>
            IsEditorAssembly(info.assembly) ||
                         IsAssemblyDuplicated(info.assembly) ||
                         IsUsedByAssemblyCSharp(info.assembly) ||
                         GetAssembliesRefed(info.assembly).Length > 0;
            return !Instance.assemblies.Any(Validate);
        }


        public static  bool TryGetAssemblyBytesAsset(AssemblyDefinitionAsset asm ,out TextAsset asset)
        {
            var path = AssetDatabase.GetAssetPath(Instance.folder);
            EditorJsonUtility.FromJsonOverwrite(asm.text,info);
            var file = $"{path}/{info.name}{Instance.fileExtension}";
            asset = default;
            if (File.Exists(file))
            {
                Debug.Log($"{nameof(AssemblyHotfixManager)}: 2222");
                asset = AssetDatabase.LoadAssetAtPath<TextAsset>(file);
            }
            return asset;
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
                var info = new SimpleAssemblyInfo();
                var hotfixAssemblies = Instance.assemblies.Select(v =>
                {
                    EditorJsonUtility.FromJsonOverwrite(v.assembly.text, info);
                    return $"{info.name}.dll";
                }).ToList();
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

        public static ReturnCode PostScriptsCallbacks(IBuildParameters parameters, IBuildResults results)
        {
            if (ValidateAll())
            {
                return StoreHotfixAssemblies(parameters.ScriptOutputFolder);
            }
            else
            {
                Debug.LogError($"Hotfix Toolkit: 请先完善 Assembly Hotfix Manager 配置项！");
                return ReturnCode.Exception;
            }
        }

        #endregion

        #region Force Reload Assemblies
        public static void ForceLoadAssemblies()
        {
            if (ValidateAll())
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
            else
            {
                Debug.LogError($"Hotfix Toolkit: 请先完善 Assembly Hotfix Manager 配置项！");
            }
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
            public List<string> references;
        }
        #endregion
    }
}
