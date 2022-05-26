using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace zFramework.Hotfix.Toolkit
{
    [DisallowMultipleComponent]
    public class HotfixLoader : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("加载热更逻辑：")]
        [Tooltip("勾选则从 ab 中加载程序集！")]
        public bool testLoad = false;
#endif
        public AssetReferenceHotfixAssemblis hotfixAssemblies;
        IEnumerator Start()
        {
#if UNITY_EDITOR
            if (testLoad)
#endif
            {
                //todo: 需要先加载依赖,按一定的顺序加载,希望全自动
                var handler = hotfixAssemblies.LoadAssetAsync();
                yield return handler;
                if (handler.Status == AsyncOperationStatus.Succeeded)
                {
                    var so = hotfixAssemblies.Asset as HotfixAssembliesData;
                    yield return so.LoadAssemblyAsync();
                }
                hotfixAssemblies.ReleaseAsset();
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (hotfixAssemblies == null)
            {
                if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(HotfixAssembliesData.Instance,out string guid,out long  id))
                {
                    hotfixAssemblies = new AssetReferenceHotfixAssemblis(guid);
                }
            }
            else if (!hotfixAssemblies.editorAsset)
            {
                hotfixAssemblies.SetEditorAsset(HotfixAssembliesData.Instance);
            }
        }
#endif

        [Serializable]
        public class AssetReferenceHotfixAssemblis : AssetReferenceT<HotfixAssembliesData>
        {
            public AssetReferenceHotfixAssemblis(string guid) : base(guid) { }
        }
    }
}