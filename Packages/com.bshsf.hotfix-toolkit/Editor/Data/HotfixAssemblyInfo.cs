using System;
using UnityEditorInternal;
using UnityEngine;
namespace zFramework.Hotfix.Toolkit
{
    [Serializable]
    public class HotfixAssemblyInfo : ISerializationCallbackReceiver
    {
        [Header("热更的程序集"),AssemblyValidate()]
        public AssemblyDefinitionAsset assembly;
        [Header("热更转存文件"), ReadOnly]
        public TextAsset bytesAsset;

        //在Unity中，类类型字段在可序列化对象中永不为 null，故而声明：NonSerialized
        // 避免频繁的加载数据（因为data未参与序列化且调用前程序集 Reload 过，所以在本应用场景下能够保证加载的数据总是最新的）
        [NonSerialized]
        SimpleAssemblyInfo data; 
        SimpleAssemblyInfo Data => data ?? (data = JsonUtility.FromJson<SimpleAssemblyInfo>(assembly.text));
        public string Name => Data.name;
        public string Dll => $"{Data.name}.dll";
        public bool IsValid => assembly ;
        public bool IsEditorOnly=>null==Data||Data.includePlatforms.Length==1&& Data.includePlatforms[0]=="Editor";
        public bool AllowUnsafeCode => Data.allowUnsafeCode;

        public void OnBeforeSerialize() { }

        // Inspector 上的每一次修改都会触发一次这俩回调
        public void OnAfterDeserialize()
        {
            if (null == assembly)
            {
                bytesAsset = null;
            }
        }
        [Serializable]
        public class SimpleAssemblyInfo
        {
            public string name;
            public bool allowUnsafeCode;
            public string[] includePlatforms;
        }
    }
}
