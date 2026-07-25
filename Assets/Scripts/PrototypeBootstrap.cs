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
        private FarmingSystem farming;
        private ProceduralWorld world;
        private TopDownPlayer player;
        private WeaponSystem weapon;
        private bool saveReady;
        private float nextAutosaveTime;

        [Serializable]
        private sealed class SavedTileData
        {
            public int x;
            public int y;
            public bool wet;
        }

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
            public List<SavedTileData> tilledTiles =
                new List<SavedTileData>();
            public List<FarmingSystem.CropSaveData> crops =
                new List<FarmingSystem.CropSaveData>();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SaveGameData(string json);

        [DllImport("__Internal")]
        private static extern string LoadGameData();

        [DllImport("__Internal")]
        private static extern int GetSelectedWorldSeed();
#endif

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

            Texture2D worldTexture = LoadPixelTexture("PixelArt/world-tiles");
            Texture2D playerTexture = LoadPixelTexture("PixelArt/player-idle");
            Texture2D caveEntranceTexture = LoadPixelTexture("PixelArt/cave-entrance");
            Texture2D farmingTexture = LoadPixelTexture("PixelArt/shop-materials");
            Font pixelFont = Resources.Load<Font>("Fonts/ark-pixel-12px");

            if (worldTexture == null ||
                playerTexture == null ||
                caveEntranceTexture == null ||
                farmingTexture == null ||
                pixelFont == null)
            {
                Debug.LogError("One or more pixel-art resources could not be loaded.");
                return;
            }

            Camera camera = BuildCamera();
            player = BuildPlayer(playerTexture);

            world = gameObject.AddComponent<ProceduralWorld>();
#if UNITY_WEBGL && !UNITY_EDITOR
            world.SetSeed(GetSelectedWorldSeed());
#endif
            world.Initialize(worldTexture, caveEntranceTexture, player);
            player.World = world;

            BuildingSystem buildings = gameObject.AddComponent<BuildingSystem>();
            buildings.Initialize(player, world);
            player.Buildings = buildings;

            shop = gameObject.AddComponent<ShopSystem>();
            shop.Initialize(player);

            farming = gameObject.AddComponent<FarmingSystem>();
            farming.Initialize(player, world, shop, farmingTexture);

            weapon = gameObject.AddComponent<WeaponSystem>();
            weapon.Initialize(player, world, shop);

            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Initialize(player.transform, world.MapBounds);

            PrototypeHud hud = gameObject.AddComponent<PrototypeHud>();
            hud.Initialize(world, player, pixelFont);

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

                if (data.tilledTiles != null)
                {
                    foreach (SavedTileData savedTile in data.tilledTiles)
                    {
                        if (savedTile != null)
                        {
                            world.RestoreTilledTile(
                                new Vector2Int(savedTile.x, savedTile.y));
                            if (savedTile.wet)
                            {
                                world.RestoreWetTile(
                                    new Vector2Int(savedTile.x, savedTile.y));
                            }
                        }
                    }
                }

                if (data.crops != null)
                {
                    foreach (FarmingSystem.CropSaveData savedCrop in data.crops)
                    {
                        farming.RestoreCrop(savedCrop);
                    }
                }

                if (data.hasPlayerPosition)
                {
                    Vector2 savedPosition = new Vector2(data.playerX, data.playerY);
                    if (world.MapBounds.Contains(
                            new Vector3(savedPosition.x, savedPosition.y, 0f)) &&
                        world.CanStandAt(savedPosition, 0.2f))
                    {
                        player.Teleport(savedPosition);
                    }
                }

                farming.RefreshGrowth();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("存档读取失败，将使用新游戏：" + exception.Message);
            }
        }

        private void SaveCurrentGame()
        {
            if (!saveReady || player == null || world == null || shop == null || farming == null)
            {
                return;
            }

            GameSaveData data = new GameSaveData
            {
                coins = player.Coins,
                hasPlayerPosition = true,
                playerX = player.transform.position.x,
                playerY = player.transform.position.y,
                items = shop.CaptureItems(),
                crops = farming.CaptureCrops()
            };
            foreach (Vector2Int tile in world.CaptureTilledTiles())
            {
                data.tilledTiles.Add(new SavedTileData
                {
                    x = tile.x,
                    y = tile.y,
                    wet = world.IsWetAt(tile)
                });
            }

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
            if (farming != null)
            {
                farming.SelectHotbarItem(itemId);
            }
            if (weapon != null)
            {
                weapon.SelectHotbarItem(itemId);
            }
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
