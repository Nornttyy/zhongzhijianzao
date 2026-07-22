using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownPlayer : MonoBehaviour
    {
        public float Speed { get; set; } = 3.8f;
        public Transform VisualRoot { get; set; }
        public SpriteRenderer PlayerSprite { get; set; }

        private Rigidbody2D body;
        private Vector2 movement;
        private Vector2 spawnPosition;
        private Vector3 visualOrigin;
        private Vector3 visualScale;
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

            if (VisualRoot != null)
            {
                visualOrigin = VisualRoot.localPosition;
                visualScale = VisualRoot.localScale;
            }
        }

        private void Update()
        {
            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);

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
