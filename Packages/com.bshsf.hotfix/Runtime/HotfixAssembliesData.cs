using System;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using System.Linq;

namespace zFramework.Hotfix.Toolkit
{
    public class HotfixAssembliesData : ScriptableObject
    {
        #region 单例,仅在 Editor 下使用，方便使用嘛
#if UNITY_EDITOR
        public static HotfixAssembliesData Instance => LoadConfiguration();
        static HotfixAssembliesData instance;

        //[InitializeOnLoadMethod]    static void CreatOne() =>LoadConfiguration();// 用于首次生成asset
        private static HotfixAssembliesData LoadConfiguration()
        {
            if (!instance)
            {
                var guids = AssetDatabase.FindAssets($"{nameof(HotfixAssembliesData)} t:Script");
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                path = path.Substring(0, path.LastIndexOf("/Runtime/"));
                var file = $"{path}/Data/{ObjectNames.NicifyVariableName(nameof(HotfixAssembliesData))}.asset";
                instance = AssetDatabase.LoadAssetAtPath<HotfixAssembliesData>(file);
                if (!instance)
                {
                    instance = CreateInstance(nameof(HotfixAssembliesData)) as HotfixAssembliesData;
                    AssetDatabase.CreateAsset(instance, file);
                    AssetDatabase.Refresh();
                }
            }
            return instance;
        }
#endif
        #endregion
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
