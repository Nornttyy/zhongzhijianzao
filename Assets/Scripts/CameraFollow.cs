using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        private Transform target;
        private Bounds bounds;
        private Camera viewCamera;

        public void Initialize(Transform followTarget, Bounds mapBounds)
        {
            target = followTarget;
            bounds = mapBounds;
            viewCamera = GetComponent<Camera>();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null || viewCamera == null)
            {
                return;
            }

            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            float x = ClampAxis(target.position.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
            float y = ClampAxis(target.position.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight);
            Vector3 desired = new Vector3(x, y, -10f);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-9f * Time.deltaTime));
        }

        private void SnapToTarget()
        {
            if (target != null)
            {
                transform.position = new Vector3(target.position.x, target.position.y, -10f);
            }
        }

        private static float ClampAxis(float value, float minimum, float maximum)
        {
            return minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : (minimum + maximum) * 0.5f;
        }
    }
}
