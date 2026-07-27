using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float AutosaveInterval = 1.5f;
        private TopDownPlayer player;
        private ForestTreeSystem trees;
        private bool saveReady;
        private float nextAutosaveTime;

        [Serializable]
        private sealed class ForestSaveData
        {
            public int version = 1;
            public float playerX;
            public float playerY;
            public int wood;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SaveGameData(string json);

        [DllImport("__Internal")]
        private static extern string LoadGameData();
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeExists()
        {
            if (FindFirstObjectByType<PrototypeBootstrap>() == null)
            {
                new GameObject("Forest Survival Bootstrap").AddComponent<PrototypeBootstrap>();
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.antiAliasing = 0;

            Texture2D worldTexture = LoadPixelTexture("PixelArt/world-tiles");
            Texture2D playerTexture = LoadPixelTexture("PixelArt/player-idle");
            Texture2D caveEntranceTexture = LoadPixelTexture("PixelArt/cave-entrance");
            Texture2D treeTexture = LoadPixelTexture("PixelArt/forest-tree");
            Font pixelFont = Resources.Load<Font>("Fonts/ark-pixel-12px");
            if (worldTexture == null || playerTexture == null ||
                caveEntranceTexture == null || treeTexture == null || pixelFont == null)
            {
                Debug.LogError("森林求生素材加载失败，请确认 forest-tree.png 等素材存在。");
                return;
            }

            Camera camera = BuildCamera();
            player = BuildPlayer(playerTexture);

            ProceduralWorld world = gameObject.AddComponent<ProceduralWorld>();
            world.Initialize(worldTexture, caveEntranceTexture, player);
            player.World = world;

            trees = gameObject.AddComponent<ForestTreeSystem>();
            trees.Initialize(world, player, treeTexture);
            player.Trees = trees;

            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Initialize(player.transform, world.MapBounds);

            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(world, player, pixelFont, trees);

            LoadSavedGame();
            saveReady = true;
            nextAutosaveTime = Time.time + AutosaveInterval;
        }

        private void Update()
        {
            if (!saveReady || Time.time < nextAutosaveTime)
            {
                return;
            }

            SaveCurrentGame();
            nextAutosaveTime = Time.time + AutosaveInterval;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveCurrentGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveCurrentGame();
            }
        }

        private void LoadSavedGame()
        {
            string json;
#if UNITY_WEBGL && !UNITY_EDITOR
            json = LoadGameData();
#else
            json = PlayerPrefs.GetString("zhongzhijianzao-forest-save-v1", string.Empty);
#endif
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                ForestSaveData data = JsonUtility.FromJson<ForestSaveData>(json);
                if (data == null || player == null)
                {
                    return;
                }

                player.Teleport(new Vector2(data.playerX, data.playerY));
                if (trees != null)
                {
                    trees.SetWoodCount(data.wood);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("森林存档读取失败，将使用新的营地：" + exception.Message);
            }
        }

        private void SaveCurrentGame()
        {
            if (!saveReady || player == null || trees == null)
            {
                return;
            }

            ForestSaveData data = new ForestSaveData
            {
                playerX = player.transform.position.x,
                playerY = player.transform.position.y,
                wood = trees.WoodCount
            };
            string json = JsonUtility.ToJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveGameData(json);
#else
            PlayerPrefs.SetString("zhongzhijianzao-forest-save-v1", json);
            PlayerPrefs.Save();
#endif
        }

        public void SaveGameNow()
        {
            SaveCurrentGame();
        }

        public void SelectHotbarItem(string itemId)
        {
        }

        public void BuyItem(string itemId)
        {
        }

        public void SellItem(string itemId)
        {
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
            camera.backgroundColor = new Color32(35, 65, 43, 255);
            return camera;
        }

        private static TopDownPlayer BuildPlayer(Texture2D playerTexture)
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.transform.position = new Vector3(1f, -1f, 0f);

            Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = playerObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.2f;
            collider.offset = new Vector2(0f, 0.22f);

            GameObject visual = new GameObject("Player Art");
            visual.transform.SetParent(playerObject.transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(
                playerTexture,
                new Rect(0f, 0f, playerTexture.width, playerTexture.height),
                new Vector2(0.5f, 0.08f),
                12f);
            renderer.sortingOrder = 320;

            TopDownPlayer controller = playerObject.AddComponent<TopDownPlayer>();
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
