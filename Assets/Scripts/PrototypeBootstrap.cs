using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float MapPixelsPerUnit = 15f;
        private const float PlayerPixelsPerUnit = 30f;

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
            QualitySettings.antiAliasing = 0;
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
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(15, 15, 23, 255);
        }

        private static void BuildApartment()
        {
            Texture2D mapTexture = Resources.Load<Texture2D>("PixelArt/apartment-map");
            if (mapTexture == null)
            {
                Debug.LogError("Pixel art map could not be loaded.");
                return;
            }

            ConfigurePixelTexture(mapTexture);

            GameObject map = new GameObject("Pixel Apartment Map");
            SpriteRenderer renderer = map.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(
                mapTexture,
                new Rect(0f, 0f, mapTexture.width, mapTexture.height),
                Vector2.one * 0.5f,
                MapPixelsPerUnit);
            renderer.sortingOrder = 0;

            GameObject collisions = new GameObject("Apartment Collisions");

            // Outer shell and the two sides of the central hallway.
            AddPixelCollider(collisions.transform, "North Wall", 12, 10, 296, 6);
            AddPixelCollider(collisions.transform, "South Wall", 12, 162, 296, 6);
            AddPixelCollider(collisions.transform, "West Wall", 12, 10, 6, 158);
            AddPixelCollider(collisions.transform, "East Wall", 302, 10, 6, 158);
            AddPixelCollider(collisions.transform, "Hall Left South", 134, 10, 6, 33);
            AddPixelCollider(collisions.transform, "Hall Left Centre", 134, 55, 6, 52);
            AddPixelCollider(collisions.transform, "Hall Left North", 134, 119, 6, 49);
            AddPixelCollider(collisions.transform, "Hall Right South", 180, 10, 6, 33);
            AddPixelCollider(collisions.transform, "Hall Right Centre", 180, 55, 6, 52);
            AddPixelCollider(collisions.transform, "Hall Right North", 180, 119, 6, 49);
            AddPixelCollider(collisions.transform, "Left Divider", 12, 84, 122, 6);
            AddPixelCollider(collisions.transform, "Right Divider", 186, 84, 122, 6);

            // Furniture colliders match the large, readable pixel silhouettes.
            AddPixelCollider(collisions.transform, "Sofa", 27, 24, 48, 17);
            AddPixelCollider(collisions.transform, "Living Table", 48, 53, 34, 16);
            AddPixelCollider(collisions.transform, "Cabinet", 113, 26, 11, 28);
            AddPixelCollider(collisions.transform, "Bed", 250, 23, 36, 48);
            AddPixelCollider(collisions.transform, "Nightstand", 226, 26, 15, 15);
            AddPixelCollider(collisions.transform, "Kitchen Counter", 24, 99, 90, 16);
            AddPixelCollider(collisions.transform, "Kitchen Table", 50, 126, 36, 24);
            AddPixelCollider(collisions.transform, "Bath", 252, 99, 35, 49);
            AddPixelCollider(collisions.transform, "Toilet", 207, 121, 20, 29);
        }

        private static void BuildPlayer()
        {
            Texture2D playerTexture = Resources.Load<Texture2D>("PixelArt/player-idle");
            if (playerTexture == null)
            {
                Debug.LogError("Pixel art player could not be loaded.");
                return;
            }

            ConfigurePixelTexture(playerTexture);

            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(0f, -3.65f, 0f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.29f;
            collider.offset = new Vector2(0f, 0.12f);

            GameObject visual = new GameObject("Single Frame Pixel Visual");
            visual.transform.SetParent(player.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(
                playerTexture,
                new Rect(0f, 0f, playerTexture.width, playerTexture.height),
                new Vector2(0.5f, 0.12f),
                PlayerPixelsPerUnit);
            renderer.sortingOrder = 20;

            TopDownPlayer controller = player.AddComponent<TopDownPlayer>();
            controller.Speed = 3.8f;
            controller.ConfigureVisual(visual.transform, renderer);
        }

        private static void ConfigurePixelTexture(Texture2D texture)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        private static void AddPixelCollider(
            Transform parent,
            string name,
            int x,
            int y,
            int width,
            int height)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);

            float centerX = (x + width * 0.5f - 160f) / MapPixelsPerUnit;
            float centerY = (90f - (y + height * 0.5f)) / MapPixelsPerUnit;
            item.transform.position = new Vector3(centerX, centerY, 0f);

            BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width / MapPixelsPerUnit, height / MapPixelsPerUnit);
        }
    }
}
