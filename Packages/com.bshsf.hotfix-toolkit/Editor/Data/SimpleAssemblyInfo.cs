using System;
using System.Collections.Generic;

namespace zFramework.Hotfix.Toolkit
{
    [Serializable]
    public class SimpleAssemblyInfo
    {
        public string name;
        public string[] includePlatforms;
        public List<string> references;
        public override string ToString() => this.name;
    }
}
