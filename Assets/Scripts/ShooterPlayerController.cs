using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class ShooterPlayerController : MonoBehaviour
    {
        private const float MoveSpeed = 4.2f;
        private Rigidbody2D body;
        private SpriteRenderer renderer;
        private Sprite[] frames;
        private Vector2 movement;
        private float animationTimer;
        private int frameIndex;
        private bool inputLocked;

        public Vector2 Movement { get { return movement; } }

        public void Initialize(Rigidbody2D playerBody, SpriteRenderer playerRenderer, Sprite[] playerFrames)
        {
            body = playerBody;
            renderer = playerRenderer;
            frames = playerFrames;
        }

        public void SetInputLocked(string locked)
        {
            inputLocked = string.Equals(locked, "true", System.StringComparison.OrdinalIgnoreCase);
            movement = Vector2.zero;
        }

        private void Update()
        {
            if (inputLocked)
            {
                movement = Vector2.zero;
                if (renderer != null && frames != null && frames.Length > 0)
                {
                    renderer.sprite = frames[0];
                }
                return;
            }

            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);

            if (movement.sqrMagnitude < 0.01f || frames == null || frames.Length == 0)
            {
                frameIndex = 0;
                animationTimer = 0f;
                if (renderer != null)
                {
                    renderer.sprite = frames[0];
                }
                return;
            }

            animationTimer += Time.deltaTime;
            if (animationTimer >= 0.12f)
            {
                animationTimer = 0f;
                frameIndex = (frameIndex + 1) % frames.Length;
                renderer.sprite = frames[frameIndex];
            }
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            body.MovePosition(body.position + movement * (MoveSpeed * Time.fixedDeltaTime));
        }
    }
}
