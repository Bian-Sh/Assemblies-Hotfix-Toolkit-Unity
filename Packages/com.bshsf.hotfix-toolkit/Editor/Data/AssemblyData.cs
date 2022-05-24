using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
namespace zFramework.Hotfix.Toolkit
{
    [Serializable]
    public class AssemblyData : ISerializationCallbackReceiver
    {
        [Header("热更的程序集")]
        public AssemblyDefinitionAsset assembly;
        [Header("Dll 转存文件夹"), FolderValidate]
        public DefaultAsset folder;
        [Header("Dll 热更文件"), ReadOnly]
        public TextAsset hotfixAssembly;

        //在Unity中，类类型字段在可序列化对象中永不为 null，故而声明：NonSerialized
        // 避免频繁的加载数据（因为data未参与序列化且调用前程序集 Reload 过，所以在本应用场景下能够保证加载的数据总是最新的）
        [NonSerialized]
        SimplifiedAssemblyData data; 
        SimplifiedAssemblyData Data => data ?? (data = JsonUtility.FromJson<SimplifiedAssemblyData>(assembly.text));
        public string OutputPath => $"{AssetDatabase.GetAssetPath(folder)}/{Data.name}{AssemblyHotfixManager.Instance.fileExtension}";
        public string Dll => $"{Data.name}.dll";
        public bool IsValid => assembly && folder && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder));
        public bool AllowUnsafeCode => Data.allowUnsafeCode;

        public void OnBeforeSerialize() { }

        // Inspector 上的每一次修改都会触发一次这俩回调
        public void OnAfterDeserialize()
        {
            if (null == assembly)
            {
                hotfixAssembly = null;
            }
        }
    }
}
