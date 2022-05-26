using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Reflection;
using System.Linq;

namespace zFramework.Hotfix.Toolkit
{
    [SingletonParam("AssemblyHotfixToolkit", addressable = true)]
    public class HotfixAssembliesData : ScriptableObjectSingleton<HotfixAssembliesData>
    {
        public List<AssetReference> assemblies;
        public IEnumerator LoadAssemblyAsync()
        {
            List<Assembly> arr = new List<Assembly>();
            foreach (var item in assemblies)
            {
                var handler = item.LoadAssetAsync<TextAsset>();
                yield return handler;
                if (handler.Status == AsyncOperationStatus.Succeeded)
                {
                    var asm = AppDomain.CurrentDomain.Load(handler.Result.bytes);
                    arr.Add(asm);
                }
                item.ReleaseAsset();
            }
            ExecuteCustomFunction(arr);
        }

        private void ExecuteCustomFunction(List<Assembly> arr)
        {
            var attrs = arr.SelectMany(v => v.GetTypes())
                                                 .SelectMany(v => v.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                                                 .Select(v => new { attr = v.GetCustomAttribute<InitializeOnAssemblyLoadAttribute>(), m = v })
                                                 .Where(v => v.attr != null)
                                                 .OrderBy(v => v.attr.priority)
                                                 .Select(v => v.m);
            foreach (var item in attrs)
            {
                item.Invoke(null, null);
            }
        }

        public async Task LoadAssemblyTAPAsync()
        {
            List<Assembly> arr = new List<Assembly>();
            foreach (var item in assemblies)
            {
                var task = item.LoadAssetAsync<TextAsset>().Task;
                try
                {
                    var data = await task;
                    var asm = AppDomain.CurrentDomain.Load(data.bytes);
                    arr.Add(asm);
                    item.ReleaseAsset();
                }
                catch (Exception e)
                {
                    Debug.LogError($"{nameof(HotfixAssembliesData)}: Task 执行中出现错误！see more ↓\n{e}");
                }
            }
            ExecuteCustomFunction(arr);
        }
    }
}
