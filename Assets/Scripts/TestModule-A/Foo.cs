using System;
using UnityEngine;
using UnityEngine.UI;
namespace zFramework.Hotfix.Examples
{
    [Serializable]
    public class SomeData  
    {
        public string name;
        public int age;
        public Button button;
    }
    public class Foo : MonoBehaviour
    {
        public SomeData data; //这种可序列化的类如果本身就热更，那么此处将会为空，所以类似的用法，清使用 ScriptableObject 代替
        public SomeSerializable  serializable; // 同上，如果你不行，可以把第 23 行解除注释后测试哈
        public Button button;
        public Button close_button;
        public Dropdown dropdown;
        public Text text;
        void Start()
        {
            //Debug.Log($"{nameof(Foo)}:  SomeSerializable-data {serializable.somestring}");
            //Debug.Log($"{nameof(Foo)}:  {data.name}  - {data.age} - {data.button.name}");
            
            button.onClick.AddListener(uuu);
            close_button.onClick.AddListener(OnCloseBTClicked);
            dropdown.onValueChanged.AddListener(OnDropdownValuechanged);
        }

        private void OnDropdownValuechanged(int arg0)
        {
            text.text = dropdown.captionText.text;
        }

        private void OnCloseBTClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

        int index = 0;
        private void uuu()
        {
            index +=1;
            //index +=1555;//index +=33333这俩互换不会触发 aa content update ?
            text.text = $"{index}";
        }

        public static void MainFunc() => Debug.Log($"{nameof(Foo)}: Enter Main Function");
    }
}
