using UnityEngine;
using UnityEngine.UI;
//[CreateAssetMenu]
public class SomeScriptableObject :ScriptableObject
{
    public string somestring;
    public int age;
    // Scene 中的对象不能编辑器赋值，用点技巧可以实现哦
    // 比如绑定到 Scene中 或者 可能的话，绘制到 inspector面板上
    // 但本例不做讨论~
    public Button button;
}
