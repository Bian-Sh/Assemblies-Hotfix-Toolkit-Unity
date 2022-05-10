using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;
namespace zFramework.Hotfix.Examples
{
    public class GameLoader : MonoBehaviour
    {
        private string assemblyAssetKey = "zFramework.Hotfix.Examples.bytes";
        private string assemblyAssetKey_ref = "zFramework.Hotfix.Demo.bytes";
        public Button button;
        private void Start()
        {
            button.onClick.AddListener(OnButtonClicked);
            _ = OnLoad();
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

        async Task OnLoad()
        {
            button.interactable = false;
#if UNITY_EDITOR
            if (Toolkit.AssemblyHotfixManager.Instance.testLoad)
#endif
            {
                // 先加载依赖
                handler = Addressables.LoadAssetAsync<TextAsset>(assemblyAssetKey_ref);
                var data = await handler.Task as TextAsset;
                 AppDomain.CurrentDomain.Load(data.bytes);

                handler = Addressables.LoadAssetAsync<TextAsset>(assemblyAssetKey);
                data = await handler.Task as TextAsset;

                if (data)
                {
                    var asm = AppDomain.CurrentDomain.Load(data.bytes);
                    Debug.Log($"{nameof(GameLoader)}: {asm.FullName}");
                    // 请注意，asm.GetType("Foo") 会导致逻辑卡死，后面要确认是卡在了哪儿。
                    var type = asm.GetType("zFramework.Hotfix.Examples.Foo");
                    MethodInfo method = type.GetMethod("MainFunc", BindingFlags.Static | BindingFlags.Public);
                    method?.Invoke(null, null);
                }
                else
                {
                    Debug.LogError($"{nameof(GameLoader)}: Assembly Load Failed!");
                }
            }
            handler_scene = Addressables.LoadSceneAsync("Hotfixed.unity", activateOnLoad: false);
            sceneInstance = (SceneInstance)await handler_scene.Task;
            button.interactable = true;
        }
    }

}
