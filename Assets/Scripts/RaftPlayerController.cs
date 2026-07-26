using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class RaftPlayerController : MonoBehaviour
    {
        private const float MoveSpeed = 2.7f;
        private const float HalfWidth = 0.42f;
        private const float HalfHeight = 1.35f;

        private Vector2 movement;
        private bool inputLocked;
        private SpriteRenderer playerRenderer;

        public bool IsInputLocked { get { return inputLocked; } }
        public Vector2 WorldPosition { get { return transform.position; } }
        public Vector2 LocalPosition { get { return transform.localPosition; } }

        public void Initialize(SpriteRenderer renderer)
        {
            playerRenderer = renderer;
        }

        public void SetInputLocked(string locked)
        {
            inputLocked = string.Equals(locked, "true", System.StringComparison.OrdinalIgnoreCase);
            movement = Vector2.zero;
        }

        public void SetLocalPosition(Vector2 position)
        {
            transform.localPosition = ClampPosition(position);
        }

        private void Update()
        {
            if (inputLocked)
            {
                movement = Vector2.zero;
                return;
            }

            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);
            if (playerRenderer != null && Mathf.Abs(movement.x) > 0.01f)
            {
                playerRenderer.flipX = movement.x < 0f;
            }
        }

        private void LateUpdate()
        {
            if (!inputLocked)
            {
                transform.localPosition = ClampPosition(
                    (Vector2)transform.localPosition + movement * (MoveSpeed * Time.deltaTime));
            }
        }

        private static Vector2 ClampPosition(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, -HalfWidth, HalfWidth),
                Mathf.Clamp(position.y, -HalfHeight, HalfHeight));
        }
    }
}
