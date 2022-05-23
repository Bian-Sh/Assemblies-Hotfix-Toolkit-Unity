using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;
using zFramework.Hotfix.Toolkit;

namespace zFramework.Hotfix.Examples
{
    public class GameLoader : MonoBehaviour
    {
        public HotfixAssembliesData assembliesData;
        public Button button;
#if UNITY_EDITOR
        [Header("加载热更逻辑："),Tooltip("勾选则从 ab 中加载程序集！")]
        public bool testLoad = false;
#endif
        static SceneInstance sceneInstance;
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

        IEnumerator OnLoad()
        {
            button.interactable = false;
#if UNITY_EDITOR
            if (testLoad)
#endif
            {
                // 需要先加载依赖,按一定的顺序加载
                yield return assembliesData.LoadAssemblyAsync();
            }
            var handler = Addressables.LoadSceneAsync("Hotfixed.unity", activateOnLoad: false);
            yield return handler;
            sceneInstance = handler.Result;
            button.interactable = true;
        }
    }

}
