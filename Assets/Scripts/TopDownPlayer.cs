using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownPlayer : MonoBehaviour
    {
        public float Speed { get; set; } = 3.6f;
        public Transform VisualRoot { get; private set; }
        public SpriteRenderer PlayerSprite { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.down;
        public ProceduralWorld World { get; set; }

        private Rigidbody2D body;
        private Vector2 movement;
        private Vector2 spawnPosition;
        private Vector3 visualOrigin;
        private Vector3 visualScale = Vector3.one;
        private float bouncePhase;
        private float moveBlend;

        public void ConfigureVisual(Transform visualRoot, SpriteRenderer playerSprite)
        {
            VisualRoot = visualRoot;
            PlayerSprite = playerSprite;
            visualOrigin = visualRoot.localPosition;
            visualScale = visualRoot.localScale;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;
        }

        private void Update()
        {
            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);

            if (movement.sqrMagnitude > 0.01f)
            {
                if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
                {
                    Facing = movement.x < 0f ? Vector2.left : Vector2.right;
                }
                else
                {
                    Facing = movement.y < 0f ? Vector2.down : Vector2.up;
                }
            }

            if (PlayerSprite != null && Mathf.Abs(movement.x) > 0.01f)
            {
                PlayerSprite.flipX = movement.x < 0f;
            }

            UpdateBounce();

            if (Input.GetKeyDown(KeyCode.R))
            {
                body.position = spawnPosition;
                body.linearVelocity = Vector2.zero;
            }
        }

        private void LateUpdate()
        {
            if (PlayerSprite != null)
            {
                PlayerSprite.sortingOrder = 320 - Mathf.RoundToInt(transform.position.y * 10f);
            }
        }

        private void UpdateBounce()
        {
            if (VisualRoot == null)
            {
                return;
            }

            float targetBlend = movement.sqrMagnitude > 0.01f ? 1f : 0f;
            moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, Time.deltaTime * 7f);
            if (targetBlend > 0f)
            {
                bouncePhase += Time.deltaTime * 10.5f;
            }

            float hop = (1f - Mathf.Cos(bouncePhase)) * 0.5f * moveBlend;
            float landing = (1f + Mathf.Cos(bouncePhase)) * 0.5f * moveBlend;
            VisualRoot.localPosition = visualOrigin + Vector3.up * (hop * 0.085f);
            VisualRoot.localScale = new Vector3(
                visualScale.x * (1f + landing * 0.035f),
                visualScale.y * (1f - landing * 0.045f),
                visualScale.z);
        }

        private void FixedUpdate()
        {
            Vector2 next = body.position + movement * (Speed * Time.fixedDeltaTime);
            if (World != null)
            {
                Vector2 horizontal = new Vector2(next.x, body.position.y);
                if (World.CanStandAt(horizontal, 0.24f))
                {
                    body.position = horizontal;
                }

                Vector2 vertical = new Vector2(body.position.x, next.y);
                if (World.CanStandAt(vertical, 0.24f))
                {
                    body.position = vertical;
                }

                body.position = World.ClampToBounds(body.position, 0.3f);
                body.linearVelocity = Vector2.zero;
                return;
            }

            body.linearVelocity = movement * Speed;
        }

        private void OnDisable()
        {
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
