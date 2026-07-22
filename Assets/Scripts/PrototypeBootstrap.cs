using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const int VisionBlockerLayer = 8;

        private static Sprite squareSprite;
        private static Sprite circleSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeExists()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() == null)
            {
                new GameObject("Prototype Bootstrap").AddComponent<PrototypeBootstrap>();
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            BuildCamera();
            BuildApartment();
            BuildPlayer();
            gameObject.AddComponent<PrototypeHud>();
        }

        private static void BuildCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.022f, 0.032f);
        }

        private static void BuildApartment()
        {
            Color floor = new Color(0.15f, 0.18f, 0.21f);
            Color wall = new Color(0.36f, 0.43f, 0.46f);
            Color trim = new Color(0.55f, 0.36f, 0.24f);
            Color furniture = new Color(0.22f, 0.29f, 0.31f);

            CreateRect("Floor", Vector2.zero, new Vector2(22f, 14f), floor, 0, false);

            CreateWall("North Wall", new Vector2(0f, 6.75f), new Vector2(22.5f, 0.5f), wall);
            CreateWall("South Wall", new Vector2(0f, -6.75f), new Vector2(22.5f, 0.5f), wall);
            CreateWall("West Wall", new Vector2(-10.75f, 0f), new Vector2(0.5f, 14f), wall);
            CreateWall("East Wall", new Vector2(10.75f, 0f), new Vector2(0.5f, 14f), wall);

            // Two internal walls with generous door gaps make the occlusion easy to read.
            CreateWall("Hall Wall A", new Vector2(-2f, -5f), new Vector2(0.4f, 3.5f), wall);
            CreateWall("Hall Wall B", new Vector2(-2f, 0.5f), new Vector2(0.4f, 3f), wall);
            CreateWall("Hall Wall C", new Vector2(-2f, 5.25f), new Vector2(0.4f, 3f), wall);

            CreateWall("Bedroom Wall A", new Vector2(4f, -4.25f), new Vector2(0.4f, 5f), wall);
            CreateWall("Bedroom Wall B", new Vector2(4f, 3.75f), new Vector2(0.4f, 5.5f), wall);

            CreateWall("Upper Room Wall A", new Vector2(-7.75f, 2f), new Vector2(5.5f, 0.4f), wall);
            CreateWall("Upper Room Wall B", new Vector2(-3.25f, 2f), new Vector2(2.1f, 0.4f), wall);

            CreateRect("Front Door", new Vector2(-2f, 3.35f), new Vector2(0.22f, 1.45f), trim, 6, false)
                .AddComponent<ThreatPulse>();

            CreateRect("Sofa", new Vector2(-7.3f, -4.7f), new Vector2(2.8f, 1.05f), furniture, 2, false);
            CreateRect("Table", new Vector2(0.9f, -3.8f), new Vector2(1.6f, 1.6f), furniture, 2, false);
            CreateRect("Bed", new Vector2(7.25f, 4.45f), new Vector2(3.2f, 1.9f), furniture, 2, false);
            CreateRect("Breaker", new Vector2(10.35f, 1.7f), new Vector2(0.25f, 1.1f), trim, 6, false);

            CreateSilhouette("Unknown Figure A", new Vector2(-6.2f, 4.5f));
            CreateSilhouette("Unknown Figure B", new Vector2(1.2f, 4.8f));
            CreateSilhouette("Unknown Figure C", new Vector2(7.3f, -3.7f));
        }

        private static void BuildPlayer()
        {
            GameObject player = CreateCircle(
                "Player",
                new Vector2(-6.6f, -2.6f),
                0.74f,
                new Color(0.37f, 0.84f, 0.78f),
                20);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.48f;

            TopDownPlayer controller = player.AddComponent<TopDownPlayer>();
            controller.Speed = 4.6f;

            GameObject facing = CreateRect(
                "Facing",
                Vector2.zero,
                new Vector2(0.36f, 0.12f),
                new Color(1f, 0.82f, 0.36f),
                21,
                false);
            facing.transform.SetParent(player.transform, false);
            facing.transform.localPosition = new Vector3(0.48f, 0f, 0f);
            controller.FacingMarker = facing.transform;

            GameObject visionObject = new GameObject("Local Vision");
            visionObject.transform.SetParent(player.transform, false);
            VisionMask vision = visionObject.AddComponent<VisionMask>();
            vision.Radius = 6.1f;
            vision.ObstacleMask = 1 << VisionBlockerLayer;
        }

        private static void CreateSilhouette(string name, Vector2 position)
        {
            GameObject figure = CreateCircle(name, position, 0.68f, new Color(0.65f, 0.18f, 0.21f), 12);
            CreateRect(name + " Shadow", position + new Vector2(0f, -0.58f), new Vector2(0.75f, 0.65f),
                new Color(0.29f, 0.08f, 0.11f), 11, false);
            figure.AddComponent<SilhouetteSway>();
        }

        private static void CreateWall(string name, Vector2 position, Vector2 size, Color color)
        {
            GameObject wall = CreateRect(name, position, size, color, 5, true);
            wall.layer = VisionBlockerLayer;
        }

        private static GameObject CreateRect(
            string name,
            Vector2 position,
            Vector2 size,
            Color color,
            int sortingOrder,
            bool addCollider)
        {
            EnsureSprites();
            GameObject item = new GameObject(name);
            item.transform.position = position;
            item.transform.localScale = new Vector3(size.x, size.y, 1f);

            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (addCollider)
            {
                item.AddComponent<BoxCollider2D>();
            }

            return item;
        }

        private static GameObject CreateCircle(
            string name,
            Vector2 position,
            float size,
            Color color,
            int sortingOrder)
        {
            EnsureSprites();
            GameObject item = new GameObject(name);
            item.transform.position = position;
            item.transform.localScale = Vector3.one * size;

            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return item;
        }

        private static void EnsureSprites()
        {
            if (squareSprite != null && circleSprite != null)
            {
                return;
            }

            Texture2D square = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = "Runtime Square",
                filterMode = FilterMode.Point
            };
            square.SetPixel(0, 0, Color.white);
            square.Apply();
            squareSprite = Sprite.Create(square, new Rect(0f, 0f, 1f, 1f), Vector2.one * 0.5f, 1f);

            const int size = 32;
            Texture2D circle = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Runtime Circle",
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            Vector2 center = Vector2.one * (size - 1f) * 0.5f;
            float radius = size * 0.48f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            circle.SetPixels(pixels);
            circle.Apply();
            circleSprite = Sprite.Create(circle, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, size);
        }
    }
}
