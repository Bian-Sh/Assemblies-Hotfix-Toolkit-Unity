using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    public class ConfigurationPresetSelector : PresetSelectorReceiver
    {
        private Object m_Target;

        private Preset m_InitialValue;
        private EditorWindow window;

        internal void Init(Object target, EditorWindow window)
        {
            m_Target = target;
            this.window = window;
            m_InitialValue = new Preset(target);
        }

        //
        // 摘要:
        //     Applies the Preset to each target. If Preset is null, this method reverts the
        //     value of each target.
        //
        // 参数:
        //   selection:
        public override void OnSelectionChanged(Preset selection)
        {
            if (selection != null)
            {
                Undo.RecordObject(m_Target, "Apply Preset " + selection.name);
                selection.ApplyTo(m_Target);
            }
            else
            {
                Undo.RecordObject(m_Target, "Cancel Preset");
                m_InitialValue.ApplyTo(m_Target);
            }
           window.Repaint();
        }

        //
        // 摘要:
        //     Applies the current selection and then destroys itself.
        //
        // 参数:
        //   selection:
        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            Object.DestroyImmediate(this);
        }
    }
}