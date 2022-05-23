using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace zFramework.Hotfix.Toolkit
{
    [CustomEditor(typeof(HotfixAssembliesData))]
    public class HotfixAssembliesDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            //bool enable = GUI.enabled;
            //GUI.enabled = false;
            //GUI.enabled = enable;
            base.OnInspectorGUI();
            if (GUILayout.Button("载入热更程序集"))
            {
                LoadAssembliesReference(target);
            }
        }

        private void LoadAssembliesReference(UnityEngine.Object target)
        {
            AssetReference Selector(AssemblyData data)
            {
                var asset = new AssetReference();
                asset.SetEditorAsset(data.hotfixAssembly);
                return asset;
            }
            var data = target as HotfixAssembliesData;
            data.assemblies = AssemblyHotfixManager.Instance.assemblies.Select(Selector).ToList();
            EditorUtility.SetDirty(target);
        }

    }
}
