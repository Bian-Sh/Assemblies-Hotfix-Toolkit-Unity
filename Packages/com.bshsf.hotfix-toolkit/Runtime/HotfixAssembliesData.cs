using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;

namespace zFramework.Hotfix.Toolkit
{
    [SingletonParam ("AssemblyHotfixToolkit",addressable =true)]
    public class HotfixAssembliesData : ScriptableObjectSingleton<HotfixAssembliesData>
    {
        public List<AssetReference> assemblies;
        public IEnumerator LoadAssemblyAsync()
        {
            foreach (var item in assemblies)
            {
                var handler = item.LoadAssetAsync<TextAsset>();
                yield return handler;
                AppDomain.CurrentDomain.Load(handler.Result.bytes);
            }
        }
        public async Task LoadAssemblyTAPAsync()
        {
            foreach (var item in assemblies)
            {
                var task = item.LoadAssetAsync<TextAsset>().Task;
                var data = await task;
                AppDomain.CurrentDomain.Load(data.bytes);
            }
        }
    }
}
