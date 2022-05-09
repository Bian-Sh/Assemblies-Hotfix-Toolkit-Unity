using UnityEditor;
using UnityEditor.Build;
using System.Linq;
using System;

namespace zFramework.Hotfix.Toolkit
{
    public class FilteredAssemblyBuilder : IFilterBuildAssemblies
    {
        /// <summary>
        /// 所有热更新 dll在 Build 时需要剥离出来
        /// <br>并且不需要将 dll 名称写入到 ScriptingAssemblies.json 中</br>
        /// <br>这个动作仅适用于 mono 编译的 dll，IL2CPP 则不适用</br>
        /// </summary>
        public int callbackOrder => 0;
        public string[] OnFilterAssemblies(BuildOptions buildOptions, string[] assemblies)
        {
            // 将热更dll从打包列表中移除
            var hotfixAssemblies = HotfixConfiguration.Instance.assemblies.Select(v => v.Dll).ToList();
            return assemblies.Where(ass => hotfixAssemblies.All(dll => !ass.EndsWith(dll, StringComparison.OrdinalIgnoreCase))).ToArray();
        }
    }
}