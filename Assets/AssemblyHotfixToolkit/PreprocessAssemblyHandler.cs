using UnityEngine;
using static zFramework.Hotfix.Toolkit.HotfixConfiguration;
using UnityEditor.Callbacks;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace zFramework.Hotfix.Toolkit
{
    /// <summary>
    /// 用于监听 Addressable Build 事件，确保在打包 ab 前处理热更 dll 编译
    /// <br>插入自己的 BuildStep <see cref="BuildStep"/></br>
    /// </summary>
    public class PreprocessAssemblyHandler : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"{nameof(PreprocessAssemblyHandler)}:  开始打包 ab ");
            foreach (var item in report.steps)
            {
                Debug.Log($"{nameof(PreprocessAssemblyHandler)}: report.step = {item}");
            }
            foreach (var item in report.packedAssets)
            {
                Debug.Log($"{nameof(PreprocessAssemblyHandler)}: packedAssets = {item.name}");
                foreach (var file in item.contents)
                {
                    Debug.Log($"{nameof(PreprocessAssemblyHandler)}: packedAssets = {file.sourceAssetPath}");
                }
                Debug.Log($"--------");

            }
        }

        /// <summary>
        ///1.  编译时处理流程
        ///<br>当编译发生时，如果需要热更的程序集发生了改变则自动转移 dll 到目标文件夹</br>
        /// </summary>
        [DidReloadScripts]
        static void OnComileFinished() => SyncAssemblyRawData(false);
    }
}

