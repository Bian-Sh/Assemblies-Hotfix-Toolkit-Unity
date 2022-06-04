using System;
/// <summary>
/// 这个属性只能在 静态函数中使用
/// Only support for static method
/// </summary>
[AttributeUsage( AttributeTargets.Method,AllowMultiple =false,Inherited =false)]
public class InitializeOnAssemblyLoadAttribute : Attribute
{
    public int priority;//优先级
    public InitializeOnAssemblyLoadAttribute(int priority) => this.priority = priority;
}
