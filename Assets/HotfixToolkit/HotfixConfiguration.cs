using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;
using UnityEngine;
using UniAssembly = UnityEditor.Compilation.Assembly;
namespace zFramework.Hotfix.Toolkit
{
    #region Inspector Draw
    [CustomEditor(typeof(HotfixConfiguration))]
    public class HotfixedAssemblyConfigurationEditor : Editor
    {
        public override void OnInspectorGUI()
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
                if (GUILayout.Button(new GUIContent("Assemblies Force Build", "尽管我们会自动更新有改变的程序集，但不妨碍为你提供强制重建服务！")))
                {
                    HotfixConfiguration.SyncAssemblyRawData(true);
                }
            }
            if (disable)
            {
                EditorGUILayout.HelpBox("在编辑器播放、编译时不可进行修改！", MessageType.Info);
            }
        }
    }
    #endregion
    public class HotfixConfiguration : ScriptableObject
    {
        [Header("热更 DLL 存储的文件展名："),ReadOnly]
        public string fileExtension = ".bytes";
        [Header("热更文件测试模式：")]
        public bool testLoad = false;
        [Header("需要热更的程序集定义文件：")]
        public List<AssemblyData> assemblies = new List<AssemblyData>();

        #region 单例
        public static HotfixConfiguration Instance => LoadConfiguration();
        static HotfixConfiguration instance;
        private static HotfixConfiguration LoadConfiguration()
        {
            if (!instance)
            {
                var guids = AssetDatabase.FindAssets("HotfixConfiguration t:Script");
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                path = path.Substring(0, path.LastIndexOf('/'));
                path = Path.Combine(path, $"Data/Hotfix Configuration.asset");
                instance = AssetDatabase.LoadAssetAtPath<HotfixConfiguration>(path);
            }
            return instance;
        }
        #endregion

        public static void SyncAssemblyRawData(bool forceCompilie = false)
        {
            // 将要进入播放模式时会导致 Type 实例意外回收，同时避免编译导致的异常，故而规避之！
            if (EditorApplication.isPlayingOrWillChangePlaymode||!Instance) return;
            // 1.  dll 编译存放处
            var lib_dir = Path.Combine(Application.dataPath, "..", "Library\\ScriptAssemblies");
            foreach (var item in Instance.assemblies)
            {
                // 如果配置正确则开始尝试编译
                if (item.IsValid)
                {
                    var data = JsonUtility.FromJson<SimplifiedAssemblyData>(item.assembly.text);
                    var dll = Path.Combine(lib_dir, $"{data.name}.dll");
                    var temp = Path.Combine(Application.temporaryCachePath, $"{data.name}{Instance.fileExtension}");
                    var file = new FileInfo(dll);
                    if (file.Exists)
                    {
                        var lastWriteTime = file.LastWriteTime.Ticks;
                        if (forceCompilie || item.lastWriteTime < lastWriteTime)
                        {
                            item.lastWriteTime = lastWriteTime;

                            UniAssembly assembly = CompilationPipeline.GetAssemblies(AssembliesType.Player).FirstOrDefault(v => v.name == item.assembly.name);
                            if (null != assembly)
                            {
                                AssemblyBuilder builder = new AssemblyBuilder(temp, assembly.sourceFiles);
                                builder.compilerOptions.AllowUnsafeCode = item.AllowUnsafeCode;
                                BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                                builder.compilerOptions.ApiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(buildTargetGroup);
                                builder.additionalReferences = assembly.allReferences;
                                builder.flags = AssemblyBuilderFlags.None;
                                builder.referencesOptions = ReferencesOptions.UseEngineModules;
                                builder.buildTarget = EditorUserBuildSettings.activeBuildTarget;
                                builder.buildTargetGroup = buildTargetGroup;
                                builder.excludeReferences = new string[] { assembly.outputPath };
                                builder.additionalDefines = assembly.defines;
                                builder.compilerOptions = assembly.compilerOptions;
                                builder.buildFinished += OnBuildFinished;
                                if (!builder.Build())
                                {
                                    Debug.LogError($"{nameof(HotfixConfiguration)}: Assembly {item.Dll} Build Fail！");
                                }
                                else
                                {
                                    FileInfo fileinfo = new FileInfo(item.OutputPath);
                                    File.Copy(temp, fileinfo.FullName, true);
                                    if (fileinfo.Exists)
                                    {
                                        item.UpdateFacade();
                                        Debug.Log($"{nameof(HotfixConfiguration)}: {fileinfo.FullName}");
                                    }
                                    Debug.Log($"{nameof(HotfixConfiguration)} 热更程序集： <color=yellow>{fileinfo.Name} </color> 完成构建！");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"{nameof(HotfixConfiguration)}: 请先完善 Hotfix Configuration 配置项！");
                }
                EditorUtility.SetDirty(Instance);
                AssetDatabase.Refresh();
            }
        }

        private static void OnBuildFinished(string arg1, CompilerMessage[] arg2)
        {
            foreach (var msg in arg2)
            {
                if (msg.type == CompilerMessageType.Error)
                {
                    Debug.LogError(msg.message);
                }
                else if (msg.type == CompilerMessageType.Warning)
                {
                    Debug.LogWarning(msg.message);
                }
                else
                {
                    Debug.Log(msg.message);
                }
            }
        }

        [Serializable]
        public class AssemblyData 
        {
            [Header("热更的程序集")]
            public AssemblyDefinitionAsset assembly;
            [Header("Dll 转存文件夹"), FolderValidate]
            public DefaultAsset folder;
            [SerializeField, Header("Dll 热更文件"), ReadOnly]
            TextAsset hotfixAssembly;
            [SerializeField, Header("Dll 最后更新时间"), ReadOnly]
            string lastUpdateTime;
            [HideInInspector]
            public long lastWriteTime;
            public string OutputPath => CombinePath();
            public string Dll => GetFileName();
            public bool IsValid => assembly && folder && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));
            public bool AllowUnsafeCode => CheckIfUnsafeCodeAllow();

            private bool CheckIfUnsafeCodeAllow()
            {
                var data = JsonUtility.FromJson<SimplifiedAssemblyData>(assembly.text);
                return data.allowUnsafeCode;
            }
            private string GetFileName()
            {
                var data = JsonUtility.FromJson<SimplifiedAssemblyData>(assembly.text);
                return $"{data.name}.dll";
            }
            private string CombinePath()
            {
                var data = JsonUtility.FromJson<SimplifiedAssemblyData>(assembly.text);
                return $"{AssetDatabase.GetAssetPath(folder)}/{data.name}{Instance.fileExtension}";
            }
            internal void UpdateFacade()
            {
                hotfixAssembly = AssetDatabase.LoadMainAssetAtPath(OutputPath) as TextAsset;
                lastUpdateTime = new DateTime(lastWriteTime).ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        [Serializable]
        public class SimplifiedAssemblyData
        {
            public string name;
            public bool allowUnsafeCode;
        }
    }
}
