using UnityEngine;

namespace zFramework
{
    public class RoteMe : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(Vector3.up,3,Space.Self);
        }
    }
}
