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
        public string assemblyAssetKey = "zFramework.Hotfix.Examples.bytes";
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
#if UNITY_EDITOR
            if (Toolkit.HotfixConfiguration.Instance.testLoad)
#endif
            {
                button.interactable = false;
                handler = Addressables.LoadAssetAsync<TextAsset>(assemblyAssetKey);
                var data = await handler.Task as TextAsset;
                if (data)
                {
                    var asm = AppDomain.CurrentDomain.Load(data.bytes);
                    MethodInfo method = asm.GetType("Foo").GetMethod("MainFunc");
                    method.Invoke(null, null);
                }
            }

            handler_scene = Addressables.LoadSceneAsync("Hotfixed.unity", activateOnLoad: false);
            sceneInstance = (SceneInstance)await handler_scene.Task;
            button.interactable = true;
        }
    }

}
