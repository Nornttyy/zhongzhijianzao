using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float PlayerPixelsPerUnit = 12f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeExists()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() == null)
            {
                new GameObject("Cozy Farm Bootstrap").AddComponent<PrototypeBootstrap>();
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.antiAliasing = 0;
            // 让独立运行版本直接铺满屏幕；网页版本的全屏由主界面的按钮触发。
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;

            Texture2D worldTexture = LoadPixelTexture("PixelArt/world-tiles");
            Texture2D playerTexture = LoadPixelTexture("PixelArt/player-idle");
            Texture2D caveEntranceTexture = LoadPixelTexture("PixelArt/cave-entrance");
            Font pixelFont = Resources.Load<Font>("Fonts/ark-pixel-12px");

            if (worldTexture == null ||
                playerTexture == null ||
                caveEntranceTexture == null ||
                pixelFont == null)
            {
                Debug.LogError("One or more pixel-art resources could not be loaded.");
                return;
            }

            Camera camera = BuildCamera();
            TopDownPlayer player = BuildPlayer(playerTexture);

            ProceduralWorld world = gameObject.AddComponent<ProceduralWorld>();
            world.Initialize(worldTexture, caveEntranceTexture, player);
            player.World = world;

            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Initialize(player.transform, world.MapBounds);

            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(world, player, pixelFont);
        }

        private static Camera BuildCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.SetPositionAndRotation(new Vector3(0f, -1f, -10f), Quaternion.identity);
            camera.orthographic = true;
            camera.orthographicSize = 5.65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(49, 64, 48, 255);
            return camera;
        }

        private static TopDownPlayer BuildPlayer(Texture2D playerTexture)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(1f, -1f, 0f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.offset = new Vector2(0f, 0.22f);

            GameObject visual = new GameObject("Player Art");
            visual.transform.SetParent(player.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(
                playerTexture,
                new Rect(0f, 0f, playerTexture.width, playerTexture.height),
                new Vector2(0.5f, 0.08f),
                PlayerPixelsPerUnit);
            renderer.sortingOrder = 320;

            TopDownPlayer controller = player.AddComponent<TopDownPlayer>();
            controller.Speed = 3.6f;
            controller.ConfigureVisual(visual.transform, renderer);
            return controller;
        }

        private static Texture2D LoadPixelTexture(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            return texture;
        }
    }
}
