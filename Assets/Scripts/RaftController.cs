using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RaftController : MonoBehaviour
    {
        private const float MoveSpeed = 3.4f;
        private const float BoundaryMargin = 1.2f;
        private const float PixelsPerUnit = 16f;

        private Rigidbody2D body;
        private Vector2 movement;
        private Bounds movementBounds;
        private bool hasBounds;
        private bool inputLocked;
        private SpriteRenderer playerRenderer;

        public bool IsInputLocked { get { return inputLocked; } }
        public Vector2 Position { get { return body == null ? (Vector2)transform.position : body.position; } }

        public void Initialize(Texture2D atlas, Texture2D playerTexture)
        {
            body = GetComponent<Rigidbody2D>();
            BuildRaft(atlas);
            BuildPlayer(playerTexture);
        }

        public void SetMovementBounds(Bounds bounds)
        {
            movementBounds = bounds;
            hasBounds = bounds.size.sqrMagnitude > 0.01f;
            if (hasBounds && body != null)
            {
                body.position = ClampPosition(body.position);
                transform.position = body.position;
            }
        }

        public void SetInputLocked(string locked)
        {
            inputLocked = string.Equals(locked, "true", System.StringComparison.OrdinalIgnoreCase);
            movement = Vector2.zero;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void Teleport(Vector2 position)
        {
            if (body == null)
            {
                transform.position = position;
                return;
            }

            body.position = hasBounds ? ClampPosition(position) : position;
            transform.position = body.position;
            body.linearVelocity = Vector2.zero;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (inputLocked)
            {
                movement = Vector2.zero;
                return;
            }

            movement = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);

            if (playerRenderer != null && Mathf.Abs(movement.x) > 0.01f)
            {
                playerRenderer.flipX = movement.x < 0f;
            }
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            if (inputLocked)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 next = body.position + movement * (MoveSpeed * Time.fixedDeltaTime);
            body.position = hasBounds ? ClampPosition(next) : next;
            body.linearVelocity = Vector2.zero;
        }

        private Vector2 ClampPosition(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, movementBounds.min.x + BoundaryMargin, movementBounds.max.x - BoundaryMargin),
                Mathf.Clamp(position.y, movementBounds.min.y + BoundaryMargin, movementBounds.max.y - BoundaryMargin));
        }

        private void BuildRaft(Texture2D atlas)
        {
            Sprite plank = CreateAtlasSprite(atlas, 1, 2, "Raft Plank");
            if (plank == null)
            {
                return;
            }

            for (int y = -1; y <= 1; y++)
            {
                for (int x = 0; x < 2; x++)
                {
                    GameObject plankObject = new GameObject("Raft Plank");
                    plankObject.transform.SetParent(transform, false);
                    plankObject.transform.localPosition = new Vector3(
                        (x - 0.5f) * 0.95f,
                        y * 0.95f,
                        0f);
                    SpriteRenderer renderer = plankObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = plank;
                    renderer.sortingOrder = 10;
                }
            }
        }

        private void BuildPlayer(Texture2D playerTexture)
        {
            if (playerTexture == null)
            {
                return;
            }

            GameObject playerObject = new GameObject("Raft Player");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            playerRenderer = playerObject.AddComponent<SpriteRenderer>();
            playerRenderer.sprite = Sprite.Create(
                playerTexture,
                new Rect(0f, 0f, playerTexture.width, playerTexture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            playerRenderer.sortingOrder = 20;
        }

        private static Sprite CreateAtlasSprite(Texture2D atlas, int column, int row, string name)
        {
            if (atlas == null || atlas.width < (column + 1) * 16 || atlas.height < (row + 1) * 16)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                atlas,
                new Rect(column * 16f, atlas.height - (row + 1) * 16f, 16f, 16f),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            sprite.name = name;
            return sprite;
        }
    }
}
