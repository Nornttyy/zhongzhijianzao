using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private Texture2D generatedTexture;
        private Sprite generatedSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeExists()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() == null)
            {
                new GameObject("Shooter Room Bootstrap").AddComponent<PrototypeBootstrap>();
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.antiAliasing = 0;

            GameObject farmGrid = GameObject.Find("Farm Grid");
            if (farmGrid != null)
            {
                farmGrid.SetActive(false);
            }

            Camera camera = BuildCamera();
            Sprite[] playerFrames = LoadPlayerFrames();
            if (playerFrames == null || playerFrames.Length == 0)
            {
                Debug.LogError("多人枪战玩家动作帧加载失败。");
                return;
            }

            BuildRoom();
            ShooterPlayerController player = BuildPlayer(playerFrames);

            ShooterRoomHud hud = gameObject.AddComponent<ShooterRoomHud>();
            hud.Initialize(player);

            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        public void SaveGameNow()
        {
            // 存档界面沿用旧网页流程；枪战房间的存档系统稍后接入。
        }

        private Camera BuildCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(48, 48, 52, 255);
            return camera;
        }

        private void BuildRoom()
        {
            Transform room = new GameObject("Gray Shooter Room").transform;
            room.SetParent(transform, false);

            Sprite floorSprite = CreateSolidSprite("Room Floor", new Color32(117, 117, 122, 255));
            Sprite wallSprite = CreateSolidSprite("Room Wall", new Color32(46, 47, 53, 255));

            CreateRoomPart("Floor", room, floorSprite, new Vector3(0f, 0f, 1f), new Vector3(20f, 12f, 1f), -10, false);
            CreateRoomPart("Top Wall", room, wallSprite, new Vector3(0f, 6.45f, 0f), new Vector3(21f, 0.9f, 1f), -5, true);
            CreateRoomPart("Bottom Wall", room, wallSprite, new Vector3(0f, -6.45f, 0f), new Vector3(21f, 0.9f, 1f), -5, true);
            CreateRoomPart("Left Wall", room, wallSprite, new Vector3(-10.45f, 0f, 0f), new Vector3(0.9f, 12.9f, 1f), -5, true);
            CreateRoomPart("Right Wall", room, wallSprite, new Vector3(10.45f, 0f, 0f), new Vector3(0.9f, 12.9f, 1f), -5, true);

            // 四个低调的角柱，让房间边界在游戏中更容易辨认。
            Color32 cornerColor = new Color32(72, 73, 80, 255);
            Sprite cornerSprite = CreateSolidSprite("Room Corner", cornerColor);
            CreateRoomPart("Corner TL", room, cornerSprite, new Vector3(-10f, 6f, -0.1f), new Vector3(0.45f, 0.45f, 1f), -4, false);
            CreateRoomPart("Corner TR", room, cornerSprite, new Vector3(10f, 6f, -0.1f), new Vector3(0.45f, 0.45f, 1f), -4, false);
            CreateRoomPart("Corner BL", room, cornerSprite, new Vector3(-10f, -6f, -0.1f), new Vector3(0.45f, 0.45f, 1f), -4, false);
            CreateRoomPart("Corner BR", room, cornerSprite, new Vector3(10f, -6f, -0.1f), new Vector3(0.45f, 0.45f, 1f), -4, false);
        }

        private ShooterPlayerController BuildPlayer(Sprite[] frames)
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.transform.SetParent(transform, false);
            playerObject.transform.position = Vector3.zero;

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CapsuleCollider2D collider = playerObject.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Vertical;
            collider.size = new Vector2(0.48f, 0.82f);
            collider.offset = new Vector2(0f, -0.05f);

            GameObject visualObject = new GameObject("Player Visual");
            visualObject.transform.SetParent(playerObject.transform, false);
            SpriteRenderer renderer = visualObject.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 20;
            renderer.color = Color.white;

            ShooterPlayerController controller = playerObject.AddComponent<ShooterPlayerController>();
            controller.Initialize(body, renderer, frames);
            return controller;
        }

        private Sprite[] LoadPlayerFrames()
        {
            Texture2D texture = Resources.Load<Texture2D>("PixelArt/player-sprite-sheet-chibi-8f-blink-transparent");
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            const int columns = 8;
            const int rows = 2;
            int cellWidth = texture.width / columns;
            int cellHeight = texture.height / rows;
            Sprite[] frames = new Sprite[columns];
            // 生成图的部分帧边缘会带有相邻帧的一像素残留，切片时留出保护边，避免黄色/深色小点漏出来。
            const int bleedGuard = 2;
            float[] pivotX = { 0.683f, 0.671f, 0.671f, 0.680f, 0.674f, 0.667f, 0.664f, 0.615f };
            float[] pivotY = { 0.215f, 0.215f, 0.215f, 0.196f, 0.193f, 0.193f, 0.193f, 0.196f };
            for (int frame = 0; frame < columns; frame++)
            {
                // 第一排是男性角色；Unity 纹理原点在左下，所以 y 取上排。
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(frame * cellWidth + bleedGuard, cellHeight, cellWidth - bleedGuard, cellHeight),
                    new Vector2(pivotX[frame], pivotY[frame]),
                    256f);
                sprite.name = "Male Walk Frame " + frame;
                frames[frame] = sprite;
            }

            return frames;
        }

        private GameObject CreateRoomPart(
            string partName,
            Transform parent,
            Sprite sprite,
            Vector3 position,
            Vector3 scale,
            int sortingOrder,
            bool collider)
        {
            GameObject part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;

            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            if (collider)
            {
                BoxCollider2D box = part.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
            }

            return part;
        }

        private Sprite CreateSolidSprite(string name, Color32 color)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(new[] { color, color, color, color });
            texture.Apply(false, true);
            generatedTexture = texture;
            generatedSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 1f);
            generatedSprite.name = name;
            return generatedSprite;
        }

        private void OnDestroy()
        {
            if (generatedSprite != null)
            {
                Destroy(generatedSprite);
            }
            if (generatedTexture != null)
            {
                Destroy(generatedTexture);
            }
        }
    }
}
