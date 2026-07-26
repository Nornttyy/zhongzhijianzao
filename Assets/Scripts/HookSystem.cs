using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class HookSystem : MonoBehaviour
    {
        private const float MinReach = 1.4f;
        private const float MaxReach = 8f;
        private const float MaxChargeTime = 1.2f;
        private const float LaunchSpeed = 8.5f;
        private const float ReturnSpeed = 12f;

        private RaftPlayerController player;
        private OceanResourceSystem resources;
        private Camera viewCamera;
        private GameObject hookObject;
        private SpriteRenderer hookRenderer;
        private Rigidbody2D hookBody;
        private LineRenderer rope;
        private Vector2 origin;
        private Vector2 direction;
        private float chargeStart;
        private float reach;
        private bool charging;
        private bool launched;
        private bool returning;

        public void Initialize(RaftPlayerController controlledPlayer, OceanResourceSystem resourceSystem, Texture2D atlas)
        {
            player = controlledPlayer;
            resources = resourceSystem;
            viewCamera = Camera.main;

            hookObject = new GameObject("Hook");
            hookRenderer = hookObject.AddComponent<SpriteRenderer>();
            hookRenderer.sprite = CreateAtlasSprite(atlas, 0, 1, "Hook");
            hookRenderer.sortingOrder = 30;
            hookBody = hookObject.AddComponent<Rigidbody2D>();
            hookBody.gravityScale = 0f;
            hookBody.drag = 0f;
            hookBody.angularDrag = 0f;
            hookBody.freezeRotation = true;
            hookBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CircleCollider2D collider = hookObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.18f;
            hookBody.simulated = false;

            rope = gameObject.AddComponent<LineRenderer>();
            rope.positionCount = 2;
            rope.startWidth = 0.045f;
            rope.endWidth = 0.035f;
            rope.material = new Material(Shader.Find("Sprites/Default"));
            rope.startColor = new Color32(82, 53, 34, 255);
            rope.endColor = new Color32(82, 53, 34, 255);
            rope.sortingOrder = 25;
            rope.enabled = false;
        }

        private void Update()
        {
            if (player == null || resources == null)
            {
                return;
            }

            if (player.IsInputLocked)
            {
                if (charging)
                {
                    CancelHook();
                }
                return;
            }

            if (!charging && !launched && Input.GetMouseButtonDown(0))
            {
                BeginCharge();
            }

            if (charging)
            {
                UpdateChargePreview();
                if (Input.GetMouseButtonUp(0))
                {
                    LaunchHook();
                }
            }

            if (launched)
            {
                UpdateRope(hookBody.position);
            }
        }

        private void FixedUpdate()
        {
            if (!launched || hookBody == null)
            {
                return;
            }

            if (!returning)
            {
                float distance = Vector2.Distance(origin, hookBody.position);
                Collider2D hit = Physics2D.OverlapCircle(hookBody.position, 0.22f);
                OceanResource resource = hit == null ? null : hit.GetComponent<OceanResource>();
                if (resource != null)
                {
                    resources.Collect(resource);
                    BeginReturn();
                }
                else if (distance >= reach)
                {
                    BeginReturn();
                }
            }
            else
            {
                Vector2 toPlayer = player.WorldPosition - hookBody.position;
                hookBody.velocity = toPlayer.sqrMagnitude < 0.001f
                    ? Vector2.zero
                    : toPlayer.normalized * ReturnSpeed;
                if (toPlayer.magnitude < 0.14f)
                {
                    FinishHook();
                }
            }
        }

        private void BeginCharge()
        {
            charging = true;
            chargeStart = Time.time;
            origin = player.WorldPosition;
            direction = GetMouseDirection(origin);
            hookBody.simulated = false;
            hookObject.SetActive(true);
            rope.enabled = true;
            SetHookDirection(direction);
            UpdateRope(origin + direction * MinReach);
        }

        private void UpdateChargePreview()
        {
            origin = player.WorldPosition;
            direction = GetMouseDirection(origin);
            float held = Mathf.Clamp(Time.time - chargeStart, 0f, MaxChargeTime);
            reach = Mathf.Lerp(MinReach, MaxReach, held / MaxChargeTime);
            Vector2 preview = origin + direction * reach;
            hookObject.transform.position = preview;
            SetHookDirection(direction);
            UpdateRope(preview);
        }

        private void LaunchHook()
        {
            charging = false;
            launched = true;
            returning = false;
            origin = player.WorldPosition;
            direction = GetMouseDirection(origin);
            float held = Mathf.Clamp(Time.time - chargeStart, 0f, MaxChargeTime);
            reach = Mathf.Lerp(MinReach, MaxReach, held / MaxChargeTime);
            hookObject.transform.position = origin;
            hookBody.position = origin;
            hookBody.velocity = direction * LaunchSpeed;
            hookBody.simulated = true;
            SetHookDirection(direction);
            UpdateRope(origin);
        }

        private void BeginReturn()
        {
            returning = true;
            Vector2 toPlayer = player.WorldPosition - hookBody.position;
            hookBody.velocity = toPlayer.sqrMagnitude < 0.001f
                ? Vector2.zero
                : toPlayer.normalized * ReturnSpeed;
        }

        private void FinishHook()
        {
            charging = false;
            launched = false;
            returning = false;
            hookBody.velocity = Vector2.zero;
            hookBody.simulated = false;
            hookObject.SetActive(false);
            rope.enabled = false;
        }

        private void CancelHook()
        {
            FinishHook();
        }

        private Vector2 GetMouseDirection(Vector2 start)
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            Vector3 mouse = viewCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2 result = ((Vector2)mouse - start).normalized;
            return result.sqrMagnitude < 0.001f ? Vector2.right : result;
        }

        private void SetHookDirection(Vector2 hookDirection)
        {
            float angle = Mathf.Atan2(hookDirection.y, hookDirection.x) * Mathf.Rad2Deg;
            hookRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdateRope(Vector2 hookPosition)
        {
            Vector2 playerPosition = player.WorldPosition;
            rope.SetPosition(0, new Vector3(playerPosition.x, playerPosition.y, 0f));
            rope.SetPosition(1, new Vector3(hookPosition.x, hookPosition.y, 0f));
            Vector2 lineDirection = hookPosition - playerPosition;
            if (lineDirection.sqrMagnitude > 0.001f)
            {
                SetHookDirection(lineDirection.normalized);
            }
        }

        private static Sprite CreateAtlasSprite(Texture2D source, int column, int row, string name)
        {
            if (source == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                source,
                new Rect(column * 16f, source.height - (row + 1) * 16f, 16f, 16f),
                new Vector2(0.5f, 0.5f),
                16f);
            sprite.name = name;
            return sprite;
        }

        private void OnDestroy()
        {
            if (rope != null && rope.material != null)
            {
                Destroy(rope.material);
            }
        }
    }
}
