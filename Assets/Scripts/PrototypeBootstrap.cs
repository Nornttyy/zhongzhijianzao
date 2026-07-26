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
        private const float AutosaveInterval = 1.5f;
        private RaftController raft;
        private OceanResourceSystem resources;
        private OceanVisual ocean;
        private bool saveReady;
        private float nextAutosaveTime;

        [Serializable]
        private sealed class GameSaveData
        {
            public int version = 3;
            public bool hasRaftPosition;
            public float raftX;
            public float raftY;
            public float playerLocalX;
            public float playerLocalY;
            public List<OceanResourceSystem.ItemSaveData> resources =
                new List<OceanResourceSystem.ItemSaveData>();
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
                new GameObject("Raft Survival Bootstrap").AddComponent<PrototypeBootstrap>();
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            QualitySettings.antiAliasing = 0;

            Texture2D raftAtlas = LoadPixelTexture("PixelArt/raft-materials");
            Texture2D playerTexture = LoadPixelTexture("PixelArt/player-idle");
            Font pixelFont = Resources.Load<Font>("Fonts/ark-pixel-12px");
            if (raftAtlas == null || playerTexture == null || pixelFont == null)
            {
                Debug.LogError("海上生存素材加载失败，请确认 raft-materials.png 和角色素材存在。");
                return;
            }

            Camera camera = BuildCamera();
            ocean = gameObject.AddComponent<OceanVisual>();
            ocean.Initialize(raftAtlas);

            raft = BuildRaft(raftAtlas, playerTexture);
            raft.SetMovementBounds(ocean.OceanBounds);

            resources = gameObject.AddComponent<OceanResourceSystem>();
            resources.Initialize(raft.transform, raftAtlas, ocean.OceanBounds);

            HookSystem hook = gameObject.AddComponent<HookSystem>();
            hook.Initialize(raft.Player, resources, raftAtlas);

            CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.Initialize(raft.Player.transform, ocean.OceanBounds);

            RaftHud hud = gameObject.AddComponent<RaftHud>();
            hud.Initialize(raft, resources, pixelFont);

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

                if (data.resources != null)
                {
                    resources.RestoreItems(data.resources);
                }

                if (data.hasRaftPosition)
                {
                    raft.Teleport(new Vector2(data.raftX, data.raftY));
                    raft.Player.SetLocalPosition(new Vector2(data.playerLocalX, data.playerLocalY));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("存档读取失败，将使用新木筏：" + exception.Message);
            }
        }

        private void SaveCurrentGame()
        {
            if (!saveReady || raft == null || resources == null)
            {
                return;
            }

            Vector2 position = raft.Position;
            GameSaveData data = new GameSaveData
            {
                hasRaftPosition = true,
                raftX = position.x,
                raftY = position.y,
                playerLocalX = raft.Player.LocalPosition.x,
                playerLocalY = raft.Player.LocalPosition.y,
                resources = resources.CaptureItems()
            };

            string json = JsonUtility.ToJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveGameData(json);
#else
            PlayerPrefs.SetString("zhongzhijianzao-save-v1", json);
            PlayerPrefs.Save();
#endif
        }

        // Web page entry points retained for compatibility with the existing UI.
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

        private static RaftController BuildRaft(Texture2D raftAtlas, Texture2D playerTexture)
        {
            GameObject raftObject = new GameObject("Player");
            raftObject.transform.position = Vector3.zero;

            Rigidbody2D body = raftObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            RaftController controller = raftObject.AddComponent<RaftController>();
            controller.Initialize(raftAtlas, playerTexture);
            return controller;
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

            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            camera.orthographic = true;
            camera.orthographicSize = 5.65f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(35, 118, 158, 255);
            return camera;
        }

        private static Texture2D LoadPixelTexture(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Repeat;
            }

            return texture;
        }
    }
}
