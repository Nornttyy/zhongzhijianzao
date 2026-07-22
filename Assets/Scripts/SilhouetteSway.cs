using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class SilhouetteSway : MonoBehaviour
    {
        private Vector3 origin;
        private float phase;

        private void Awake()
        {
            origin = transform.position;
            phase = transform.position.x * 0.71f + transform.position.y * 0.37f;
        }

        private void Update()
        {
            transform.position = origin + Vector3.up * (Mathf.Sin(Time.time * 1.2f + phase) * 0.04f);
        }
    }
}
