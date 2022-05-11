using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;
namespace zFramework.Hotfix.Examples
{
    public class GameLoader : MonoBehaviour
    {
        public string assemblyAssetKey = "zFramework.Hotfix.Examples.bytes";
        public string assemblyAssetKey_ref = "zFramework.Hotfix.Demo.bytes";
        public Button button;
        private void Start()
        {
            button.onClick.AddListener(OnButtonClicked);
            StartCoroutine(OnLoad());
        }

        private void OnButtonClicked()
        {
            if (sceneInstance.Scene.IsValid())
            {
                sceneInstance.ActivateAsync();
            }
        }

        static AsyncOperationHandle handler;
        static AsyncOperationHandle handler_scene;
        static SceneInstance sceneInstance;

        IEnumerator OnLoad()
        {
            button.interactable = false;
#if UNITY_EDITOR
            if (Toolkit.AssemblyHotfixManager.Instance.testLoad)
#endif
            {
                // 先加载依赖
                handler = Addressables.LoadAssetAsync<TextAsset>(assemblyAssetKey_ref);
                yield return handler;
                var data = handler.Result as TextAsset;
                AppDomain.CurrentDomain.Load(data.bytes);

                handler = Addressables.LoadAssetAsync<TextAsset>(assemblyAssetKey);
                yield return handler;
                data = handler.Result as TextAsset;
                var asm = AppDomain.CurrentDomain.Load(data.bytes);

                // 请注意，asm.GetType("Foo") 会导致逻辑卡死，后面要确认是卡在了哪儿。
                var type = asm.GetType("zFramework.Hotfix.Examples.Foo");
                MethodInfo method = type.GetMethod("MainFunc", BindingFlags.Static | BindingFlags.Public);
                method?.Invoke(null, null);
            }
            handler_scene = Addressables.LoadSceneAsync("Hotfixed.unity", activateOnLoad: false);
            yield return handler_scene;
            sceneInstance = (SceneInstance)handler_scene.Result;
            button.interactable = true;
        }
    }

}
