using UnityEngine;
using zFramework.Hotfix;

namespace zFramework
{
    public class RoteMe : MonoBehaviour
    {
        private float speed = 5f;
        void Start() 
        {
            var tm = new TestModule();
            tm.SomeFunction();
        }
        private void Update()
        {
            transform.Rotate(Vector3.up,speed,Space.Self);
        }
    }
}
