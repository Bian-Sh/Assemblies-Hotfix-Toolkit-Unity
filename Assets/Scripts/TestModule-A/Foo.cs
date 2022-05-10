using UnityEngine;
using UnityEngine.UI;
namespace zFramework.Hotfix.Examples
{
    public class Foo : MonoBehaviour
    {
        public Button button;
        public Button close_button;
        public Dropdown dropdown;
        public Text text;
        void Start()
        {
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
            index +=9999;
            //index +=1555;//index +=33333这俩互换不会触发 aa content update ?
            text.text = $"{index}";
        }

        public static void MainFunc() => Debug.Log($"{nameof(Foo)}: Enter Main Function");
    }
}
