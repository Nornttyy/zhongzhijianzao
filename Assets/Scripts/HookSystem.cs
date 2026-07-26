using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class HookSystem : MonoBehaviour
    {
        private const float HookSpeed = 9f;
        private const float MaxDistance = 6f;
        private RaftController raft;
        private OceanResourceSystem resources;
        private Camera viewCamera;
        private GameObject hookObject;
        private SpriteRenderer hookRenderer;
        private LineRenderer rope;
        private Vector2 origin;
        private Vector2 target;
        private bool launched;
        private bool returning;
        private float travelled;

        public void Initialize(RaftController controlledRaft, OceanResourceSystem resourceSystem, Texture2D atlas)
        {
            raft = controlledRaft;
            resources = resourceSystem;
            viewCamera = Camera.main;

            hookObject = new GameObject("Hook");
            hookRenderer = hookObject.AddComponent<SpriteRenderer>();
            hookRenderer.sprite = CreateAtlasSprite(atlas, 0, 1, "Hook");
            hookRenderer.sortingOrder = 30;
            hookObject.SetActive(false);

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
            if (raft == null || resources == null || raft.IsInputLocked)
            {
                return;
            }

            if (!launched && Input.GetMouseButtonDown(0))
            {
                LaunchHook();
            }

            if (!launched)
            {
                return;
            }

            Vector2 current = hookObject.transform.position;
            Vector2 destination = returning ? origin : target;
            Vector2 next = Vector2.MoveTowards(current, destination, HookSpeed * Time.deltaTime);
            hookObject.transform.position = next;
            UpdateRope(next);

            if (!returning)
            {
                Collider2D hit = Physics2D.OverlapCircle(next, 0.3f);
                OceanResource resource = hit == null ? null : hit.GetComponent<OceanResource>();
                if (resource != null)
                {
                    resources.Collect(resource);
                    returning = true;
                }
                else if (Vector2.Distance(origin, next) >= travelled)
                {
                    returning = true;
                }
            }

            if (returning && Vector2.Distance(next, origin) < 0.05f)
            {
                launched = false;
                returning = false;
                hookObject.SetActive(false);
                rope.enabled = false;
            }
        }

        private void LaunchHook()
        {
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            Vector3 mouse = viewCamera.ScreenToWorldPoint(Input.mousePosition);
            origin = raft.Position;
            Vector2 direction = ((Vector2)mouse - origin).normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            target = origin + direction * MaxDistance;
            travelled = Vector2.Distance(origin, target);
            hookObject.transform.position = origin;
            hookObject.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            hookObject.SetActive(true);
            rope.enabled = true;
            launched = true;
            returning = false;
            UpdateRope(origin);
        }

        private void UpdateRope(Vector2 hookPosition)
        {
            rope.SetPosition(0, new Vector3(raft.Position.x, raft.Position.y, 0f));
            rope.SetPosition(1, new Vector3(hookPosition.x, hookPosition.y, 0f));
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
    }
}
