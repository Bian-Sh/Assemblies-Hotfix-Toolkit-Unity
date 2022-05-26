using UnityEngine.AddressableAssets;

namespace zFramework.Hotfix.Examples
{
    /// <summary>
    /// 演示如何使用热更动态加载热更场景
    /// </summary>
    public class Init 
    {
        [InitializeOnAssemblyLoad(0)]
        static void LoadScene() => Addressables.LoadSceneAsync("Hotfixed.unity");
    }
}