using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float PlayerPixelsPerUnit = 12f;
        private const float AutosaveInterval = 1.5f;
        private ShopSystem shop;
        private ShopRoomSystem room;
        private TopDownPlayer player;
        private bool saveReady;
        private float nextAutosaveTime;

        [Serializable]
        private sealed class GameSaveData
        {
            public int version = 2;
            public int coins;
            public bool hasPlayerPosition;
            public float playerX;
            public float playerY;
            public List<ShopSystem.ItemSaveData> items =
                new List<ShopSystem.ItemSaveData>();
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
                new GameObject("Shop Bootstrap").AddComponent<PrototypeBootstrap>();
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.antiAliasing = 0;

            Texture2D playerTexture = LoadPixelTexture("PixelArt/player-idle");
            Texture2D roomTexture = LoadPixelTexture("PixelArt/apartment-map");
            Font pixelFont = Resources.Load<Font>("Fonts/ark-pixel-12px");

            if (roomTexture == null ||
                playerTexture == null ||
                pixelFont == null)
            {
                Debug.LogError("One or more pixel-art resources could not be loaded.");
                return;
            }

            Camera camera = BuildCamera();
            player = BuildPlayer(playerTexture);

            room = gameObject.AddComponent<ShopRoomSystem>();
            room.Initialize(roomTexture);
            player.SetMovementBounds(room.RoomBounds);

            shop = gameObject.AddComponent<ShopSystem>();
            shop.Initialize(player);

            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Initialize(player.transform, room.RoomBounds);

            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(null, player, pixelFont);

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
            string json = null;
#if UNITY_WEBGL && !UNITY_EDITOR
            json = LoadGameData();
#else
            json = PlayerPrefs.GetString("zhongzhijianzao-save-v1", string.Empty);
#endif
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                {
                    return;
                }

                player.SetCoins(data.coins);
                if (data.items != null)
                {
                    shop.RestoreItems(data.items);
                }

                if (data.hasPlayerPosition)
                {
                    Vector2 savedPosition = new Vector2(data.playerX, data.playerY);
                    if (room.RoomBounds.Contains(
                            new Vector3(savedPosition.x, savedPosition.y, 0f)))
                    {
                        player.Teleport(savedPosition);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("存档读取失败，将使用新游戏：" + exception.Message);
            }
        }

        private void SaveCurrentGame()
        {
            if (!saveReady || player == null || room == null || shop == null)
            {
                return;
            }

            GameSaveData data = new GameSaveData
            {
                coins = player.Coins,
                hasPlayerPosition = true,
                playerX = player.transform.position.x,
                playerY = player.transform.position.y,
                items = shop.CaptureItems()
            };

            string json = JsonUtility.ToJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveGameData(json);
#else
            PlayerPrefs.SetString("zhongzhijianzao-save-v1", json);
            PlayerPrefs.Save();
#endif
        }

        // Explicit entry points for the web page. Keeping these on the
        // bootstrap object avoids relying on SendMessage finding a sibling
        // component in an IL2CPP WebGL build.
        public void BuyItem(string itemId)
        {
            if (shop != null)
            {
                shop.BuyItem(itemId);
            }
        }

        public void SelectHotbarItem(string itemId)
        {
            // Kept as a no-op entry point while the shop UI is being
            // migrated. Older pages may still send this message.
        }

        public void SellItem(string itemId)
        {
            if (shop != null)
            {
                shop.SellItem(itemId);
            }
        }

        public void SaveGameNow()
        {
            SaveCurrentGame();
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
