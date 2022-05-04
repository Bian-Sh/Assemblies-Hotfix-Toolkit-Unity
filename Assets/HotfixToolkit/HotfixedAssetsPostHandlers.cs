namespace zFramework.Hotfix.Toolkit
{
    using UnityEngine;
    using UnityEditor;
    using static HotfixConfiguration;
    using System.IO;
    using UnityEditor.Callbacks;
    using System;
    using System.Collections.Generic;

    public static class HotfixedAssetsPostHandlers
    {
        /// <summary>
        ///1.  编译时处理流程
        ///<br>当编译发生时，如果需要热更的程序集发生了改变则自动转移 dll 到目标文件夹</br>
        /// </summary>
        [DidReloadScripts]
        static void OnComileFinished() => SyncAssemblyRawData(false);

        /// <summary>
        /// 2. 打包后处理
        /// <br>PC 的 dll 热更只需替换 dll 即可，打包完成直接删除热更dll 以及清单条目即可</br>
        /// </summary>
        [PostProcessBuild(1)]
        public static void Processing(BuildTarget target, string pathToBuiltProject)
        {
            //1.  获取 app 根节点
            string app_path = pathToBuiltProject.Substring(0, pathToBuiltProject.LastIndexOf("/"));
            //2. 获取 运行时程序集 清单
            string json_path = Path.Combine(app_path, $"{Application.productName}_Data", "ScriptingAssemblies.json");
            Assemblies catalog = null;
            if (File.Exists(json_path))
            {
                catalog = JsonUtility.FromJson<Assemblies>(File.ReadAllText(json_path));
            }
            //3. 获取 运行时 程序集存储文件夹
            string dstpath = Path.Combine(app_path, $"{Application.productName}_Data", "Managed");//拼接 dll 存放的文件夹路径

            // 4. 移除对应 dll 条目
            foreach (var item in Instance.assemblies)
            {
                if (item.assembly)
                {
                    var path = Path.Combine(dstpath, item.Dll);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        int index = catalog?.names?.FindIndex(v => string.Compare(v, item.Dll) == 0) ?? -1;
                        if (index != -1)
                        {
                            catalog.names.RemoveAt(index);
                            catalog.types.RemoveAt(index);
                        }
                        Debug.Log($"{nameof(HotfixedAssetsPostHandlers)}: 删除热更 dll <color=green>{item.Dll} </color>！");
                    }
                }
            }
            // 5. 存回 json 文件
            File.WriteAllText(json_path, JsonUtility.ToJson(catalog, false));
            Debug.Log($"{nameof(HotfixedAssetsPostHandlers)}: 热更DLL打包后处理执行完毕 ！");
        }
    }

    [Serializable]
    public class Assemblies
    {
        public List<string> names;
        public List<int> types;
    }

}

