using System;
using UnityEngine.UI;

[Serializable]
public class SomeSerializable 
{
    public string somestring;
    public int someint;
    public Button somebt;

    public override string ToString()
    {
        return $" somestring {somestring} - someint {someint}  - somebt {somebt.name}";
    }
}
