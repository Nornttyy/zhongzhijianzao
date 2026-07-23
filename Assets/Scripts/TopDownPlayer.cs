using UnityEngine;

namespace DoNotOpen.Prototype
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownPlayer : MonoBehaviour
    {
        private const float MovementRadius = 0.2f;
        private const float BoundaryMargin = 0.22f;

        public float Speed { get; set; } = 3.6f;
        public float SwimSpeedMultiplier { get; set; } = 0.45f;
        public Transform VisualRoot { get; private set; }
        public SpriteRenderer PlayerSprite { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.down;
        public ProceduralWorld World { get; set; }
        public bool IsSwimming { get; private set; }

        private Rigidbody2D body;
        private Vector2 movement;
        private Vector2 spawnPosition;
        private Vector3 visualOrigin;
        private Vector3 visualScale = Vector3.one;
        private float bouncePhase;
        private float moveBlend;
        private SpriteMask swimMask;
        private Texture2D swimMaskTexture;
        private Sprite swimMaskSprite;
        private ParticleSystem waterSplash;
        private ParticleSystemRenderer waterSplashRenderer;
        private Material waterSplashMaterial;
        private float nextSplashTime;

        public void ConfigureVisual(Transform visualRoot, SpriteRenderer playerSprite)
        {
            VisualRoot = visualRoot;
            PlayerSprite = playerSprite;
            visualOrigin = visualRoot.localPosition;
            visualScale = visualRoot.localScale;
            CreateSwimMask();
            CreateWaterSplash();
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

            if (World != null)
            {
                SetSwimming(World.IsWaterAt(body.position));
            }

            UpdateBounce();
            UpdateWaterSplash();

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
                if (waterSplashRenderer != null)
                {
                    waterSplashRenderer.sortingOrder = PlayerSprite.sortingOrder + 1;
                }
            }
        }

        private void CreateSwimMask()
        {
            swimMaskTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Swim Upper Body Mask Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            swimMaskTexture.SetPixel(0, 0, Color.white);
            swimMaskTexture.Apply();

            swimMaskSprite = Sprite.Create(
                swimMaskTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            swimMaskSprite.name = "Swim Upper Body Mask Sprite";

            GameObject maskObject = new GameObject("Swim Upper Body Mask");
            maskObject.transform.SetParent(VisualRoot, false);
            maskObject.transform.localPosition = new Vector3(0f, 0.78f, 0f);
            maskObject.transform.localScale = new Vector3(1.05f, 0.72f, 1f);

            swimMask = maskObject.AddComponent<SpriteMask>();
            swimMask.sprite = swimMaskSprite;
            swimMask.alphaCutoff = 0.1f;
            swimMask.isCustomRangeActive = true;
            swimMask.frontSortingLayerID = PlayerSprite.sortingLayerID;
            swimMask.frontSortingOrder = short.MaxValue;
            swimMask.backSortingLayerID = PlayerSprite.sortingLayerID;
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

            ParticleSystem.EmissionModule emission = waterSplash.emission;
            emission.enabled = false;

            waterSplashRenderer = splashObject.GetComponent<ParticleSystemRenderer>();
            waterSplashRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            waterSplashRenderer.sortingLayerID = PlayerSprite.sortingLayerID;
            waterSplashMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                name = "Blue Water Splash Material",
                mainTexture = Texture2D.whiteTexture
            };
            waterSplashRenderer.sharedMaterial = waterSplashMaterial;
        }

        private void SetSwimming(bool swimming)
        {
            if (IsSwimming == swimming)
            {
                return;
            }

            IsSwimming = swimming;
            if (PlayerSprite != null)
            {
                PlayerSprite.maskInteraction = swimming
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
                        Random.Range(-0.32f, 0.32f),
                        Random.Range(0.34f, 0.45f),
                        0f),
                    velocity = new Vector3(
                        Random.Range(-0.85f, 0.85f),
                        Random.Range(0.45f, 1.05f),
                        0f),
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
            float movementSpeed = IsSwimming ? Speed * SwimSpeedMultiplier : Speed;
            Vector2 next = body.position + movement * (movementSpeed * Time.fixedDeltaTime);
            if (World != null)
            {
                Vector2 horizontal = new Vector2(next.x, body.position.y);
                if (World.CanStandAt(horizontal, MovementRadius))
                {
                    body.position = horizontal;
                }

                Vector2 vertical = new Vector2(body.position.x, next.y);
                if (World.CanStandAt(vertical, MovementRadius))
                {
                    body.position = vertical;
                }

                body.position = World.ClampToBounds(body.position, BoundaryMargin);
                SetSwimming(World.IsWaterAt(body.position));
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

        private void OnDestroy()
        {
            if (waterSplashMaterial != null)
            {
                Destroy(waterSplashMaterial);
            }
            if (swimMaskSprite != null)
            {
                Destroy(swimMaskSprite);
            }
            if (swimMaskTexture != null)
            {
                Destroy(swimMaskTexture);
            }
        }
    }
}
