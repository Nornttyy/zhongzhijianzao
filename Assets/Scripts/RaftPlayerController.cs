using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class RaftPlayerController : MonoBehaviour
    {
        private const float MoveSpeed = 2.7f;
        private const float SwimSpeedMultiplier = 0.45f;
        // The raft is made from two 1-unit planks by three 1-unit planks.
        // Swimming starts when the player's centre crosses the visible edge,
        // so more than half of the body is actually in the water.
        private const float RaftHalfWidth = 0.96f;
        private const float RaftHalfHeight = 1.44f;
        private const float BoundaryMargin = 0.35f;

        private RaftController raft;
        private Bounds oceanBounds;
        private bool hasOceanBounds;
        private bool onRaft = true;
        private bool inputLocked;
        private Vector2 movement;
        private Vector3 visualOrigin;
        private Vector3 visualScale = Vector3.one;
        private float bouncePhase;
        private float moveBlend;
        private SpriteRenderer playerRenderer;
        private Transform visualRoot;
        private SpriteMask swimMask;
        private Texture2D swimMaskTexture;
        private Sprite swimMaskSprite;
        private ParticleSystem waterSplash;
        private ParticleSystemRenderer waterSplashRenderer;
        private Material waterSplashMaterial;
        private float nextSplashTime;

        public bool IsInputLocked { get { return inputLocked; } }
        public bool IsSwimming { get { return !onRaft; } }
        public Vector2 WorldPosition { get { return transform.position; } }
        public Vector2 LocalPosition { get { return transform.localPosition; } }

        public void Initialize(
            RaftController raftOwner,
            Transform playerVisualRoot,
            SpriteRenderer renderer)
        {
            raft = raftOwner;
            visualRoot = playerVisualRoot;
            playerRenderer = renderer;
            visualOrigin = visualRoot.localPosition;
            visualScale = visualRoot.localScale;
            CreateSwimMask();
            CreateWaterSplash();
        }

        public void SetOceanBounds(Bounds bounds)
        {
            oceanBounds = bounds;
            hasOceanBounds = bounds.size.sqrMagnitude > 0.01f;
        }

        public void SetInputLocked(string locked)
        {
            inputLocked = string.Equals(locked, "true", System.StringComparison.OrdinalIgnoreCase);
            movement = Vector2.zero;
        }

        public void SetLocalPosition(Vector2 position)
        {
            if (onRaft)
            {
                transform.localPosition = ClampOnRaft(position);
            }
        }

        private void Update()
        {
            if (inputLocked)
            {
                movement = Vector2.zero;
                UpdateBounce();
                UpdateWaterSplash();
                return;
            }

            movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            movement = Vector2.ClampMagnitude(movement, 1f);
            if (playerRenderer != null && Mathf.Abs(movement.x) > 0.01f)
            {
                playerRenderer.flipX = movement.x < 0f;
            }

            UpdateBounce();
            UpdateWaterSplash();
        }

        private void LateUpdate()
        {
            if (!inputLocked)
            {
                float speed = IsSwimming ? MoveSpeed * SwimSpeedMultiplier : MoveSpeed;
                if (onRaft)
                {
                    transform.localPosition = ClampOnRaft(
                        (Vector2)transform.localPosition + movement * (speed * Time.deltaTime));
                    if (!IsInsideRaft(transform.localPosition))
                    {
                        EnterWater();
                    }
                }
                else
                {
                    Vector2 next = (Vector2)transform.position + movement * (speed * Time.deltaTime);
                    transform.position = ClampToOcean(next);
                    TryReturnToRaft();
                }
            }
        }

        private void EnterWater()
        {
            Vector3 worldPosition = transform.position;
            transform.SetParent(null, true);
            transform.position = worldPosition;
            onRaft = false;
            SetSwimming(true);
        }

        private void TryReturnToRaft()
        {
            if (raft == null)
            {
                return;
            }

            Vector2 local = (Vector2)transform.position - raft.Position;
            if (!IsInsideRaft(local))
            {
                return;
            }

            transform.SetParent(raft.transform, false);
            transform.localPosition = ClampOnRaft(local);
            onRaft = true;
            SetSwimming(false);
        }

        private static bool IsInsideRaft(Vector2 position)
        {
            return Mathf.Abs(position.x) <= RaftHalfWidth &&
                Mathf.Abs(position.y) <= RaftHalfHeight;
        }

        private static Vector2 ClampOnRaft(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, -RaftHalfWidth - 0.06f, RaftHalfWidth + 0.06f),
                Mathf.Clamp(position.y, -RaftHalfHeight - 0.06f, RaftHalfHeight + 0.06f));
        }

        private Vector2 ClampToOcean(Vector2 position)
        {
            if (!hasOceanBounds)
            {
                return position;
            }

            return new Vector2(
                Mathf.Clamp(position.x, oceanBounds.min.x + BoundaryMargin, oceanBounds.max.x - BoundaryMargin),
                Mathf.Clamp(position.y, oceanBounds.min.y + BoundaryMargin, oceanBounds.max.y - BoundaryMargin));
        }

        private void UpdateBounce()
        {
            if (visualRoot == null)
            {
                return;
            }

            float targetBlend = movement.sqrMagnitude > 0.01f ? 1f : 0f;
            moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, Time.deltaTime * 8f);
            if (targetBlend > 0f)
            {
                bouncePhase += Time.deltaTime * (IsSwimming ? 11f : 15.5f);
            }

            float hop = (1f - Mathf.Cos(bouncePhase)) * 0.5f * moveBlend;
            float tilt = Mathf.Sin(bouncePhase) * 3f * moveBlend;
            visualRoot.localPosition = visualOrigin + Vector3.up * (hop * (IsSwimming ? 0.045f : 0.085f));
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);
            visualRoot.localScale = visualScale;
        }

        private void CreateSwimMask()
        {
            swimMaskTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Swimming Mask Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            swimMaskTexture.SetPixel(0, 0, Color.white);
            swimMaskTexture.Apply();
            swimMaskSprite = Sprite.Create(swimMaskTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            GameObject maskObject = new GameObject("Swimming Upper Body Mask");
            maskObject.transform.SetParent(visualRoot, false);
            maskObject.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            maskObject.transform.localScale = new Vector3(1.1f, 0.68f, 1f);
            swimMask = maskObject.AddComponent<SpriteMask>();
            swimMask.sprite = swimMaskSprite;
            swimMask.alphaCutoff = 0.1f;
            swimMask.isCustomRangeActive = true;
            swimMask.frontSortingLayerID = playerRenderer.sortingLayerID;
            swimMask.frontSortingOrder = short.MaxValue;
            swimMask.backSortingLayerID = playerRenderer.sortingLayerID;
            swimMask.backSortingOrder = short.MinValue;
            swimMask.enabled = false;
        }

        private void CreateWaterSplash()
        {
            GameObject splashObject = new GameObject("Blue Water Splash");
            splashObject.transform.SetParent(transform, false);
            waterSplash = splashObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = waterSplash.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.4f;
            main.startSpeed = 0f;
            main.startSize = 0.11f;
            main.maxParticles = 48;
            waterSplashRenderer = splashObject.GetComponent<ParticleSystemRenderer>();
            waterSplashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            waterSplashRenderer.sortingOrder = 22;
            waterSplashMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Blue Water Splash Material",
                mainTexture = Texture2D.whiteTexture
            };
            waterSplashRenderer.sharedMaterial = waterSplashMaterial;
        }

        private void SetSwimming(bool swimming)
        {
            if (playerRenderer != null)
            {
                playerRenderer.maskInteraction = swimming
                    ? SpriteMaskInteraction.VisibleInsideMask
                    : SpriteMaskInteraction.None;
            }
            if (swimMask != null)
            {
                swimMask.enabled = swimming;
            }
            if (swimming)
            {
                EmitWaterSplash(9);
            }
        }

        private void UpdateWaterSplash()
        {
            if (!IsSwimming || movement.sqrMagnitude < 0.01f || Time.time < nextSplashTime)
            {
                return;
            }

            EmitWaterSplash(3);
            nextSplashTime = Time.time + 0.14f;
        }

        private void EmitWaterSplash(int count)
        {
            if (waterSplash == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                ParticleSystem.EmitParams splash = new ParticleSystem.EmitParams
                {
                    position = transform.position + new Vector3(
                        Random.Range(-0.32f, 0.32f), Random.Range(-0.25f, 0.25f), 0f),
                    velocity = new Vector3(Random.Range(-0.85f, 0.85f), Random.Range(0.1f, 0.65f), 0f),
                    startColor = Color.Lerp(
                        new Color32(58, 170, 224, 230),
                        new Color32(142, 232, 255, 245),
                        Random.value),
                    startLifetime = Random.Range(0.26f, 0.48f),
                    startSize = Random.Range(0.07f, 0.14f)
                };
                waterSplash.Emit(splash, 1);
            }
        }

        private void OnDestroy()
        {
            if (waterSplashMaterial != null) Destroy(waterSplashMaterial);
            if (swimMaskSprite != null) Destroy(swimMaskSprite);
            if (swimMaskTexture != null) Destroy(swimMaskTexture);
        }
    }
}
