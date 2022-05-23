using UnityEngine;

namespace zFramework.Hotfix
{
    public class TestModule
    {
        public void SomeFunction()
        {
            Debug.Log($"{nameof(TestModule)}:  Inside  SomeFunction£¬update ... ");
            InternalFunction();
        }

        private void InternalFunction() 
        {
            Debug.Log($"{nameof(TestModule)}:  Inside  InternalFunction .. ");
        }
    }
}
