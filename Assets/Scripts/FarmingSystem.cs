using System.Collections.Generic;
using UnityEngine;

namespace DoNotOpen.Prototype
{
    /// <summary>
    /// The first playable farming loop: use the hoe to till grass, plant a
    /// seed, wait for the three pixel-art growth stages, then harvest it.
    /// Crops are lightweight overlays so they do not alter the terrain mesh.
    /// </summary>
    public sealed class FarmingSystem : MonoBehaviour
    {
        [System.Serializable]
        public sealed class CropSaveData
        {
            public int x;
            public int y;
            public string seedId;
            public float elapsed;
        }

        private const float InteractionDistance = 2.45f;
        private const float GrowthStageDuration = 7f;
        private const float DryGrowthMultiplier = 0.35f;
        private const int MatureStage = 2;

        private readonly Dictionary<Vector2Int, CropPlot> plots =
            new Dictionary<Vector2Int, CropPlot>();

        private TopDownPlayer player;
        private ProceduralWorld world;
        private ShopSystem shop;
        private Sprite[] wheatStages;
        private Sprite[] carrotStages;
        private string selectedItemId = "wheat_seed";

        public void Initialize(
            TopDownPlayer controlledPlayer,
            ProceduralWorld generatedWorld,
            ShopSystem itemShop,
            Texture2D cropSheet)
        {
            player = controlledPlayer;
            world = generatedWorld;
            shop = itemShop;
            if (cropSheet != null)
            {
                cropSheet.filterMode = FilterMode.Point;
                cropSheet.wrapMode = TextureWrapMode.Clamp;
                wheatStages = CreateSprites(cropSheet, 1, 0, 3, "Wheat Crop");
                carrotStages = CreateSprites(cropSheet, 1, 3, 3, "Carrot Crop");
            }
        }

        // Called from the web hotbar when the player presses 1–9.
        public void SelectHotbarItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                selectedItemId = string.Empty;
                return;
            }

            if (itemId == "wheat_seed" ||
                itemId == "carrot_seed" ||
                itemId == "watering_can" ||
                itemId == "hoe")
            {
                selectedItemId = itemId;
                return;
            }

            selectedItemId = string.Empty;
        }

        private void Update()
        {
            if (player == null || world == null || shop == null || player.IsInputLocked)
            {
                return;
            }

            if (!world.IsInCave && Input.GetMouseButtonDown(1))
            {
                HandleInteraction();
            }

            UpdateGrowth();
        }

        private void HandleInteraction()
        {
            Camera viewCamera = Camera.main;
            if (viewCamera == null)
            {
                return;
            }

            Vector2 clickPosition = viewCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int tile = world.WorldToTile(clickPosition);
            if (!IsWithinInteractionDistance(player.transform.position, tile))
            {
                return;
            }

            // 收割是对成熟作物的直接互动，不应被当前快捷栏里的锄头、
            // 水壶或木剑拦截。这样换着工具靠近作物时也能稳定收割。
            if (plots.TryGetValue(tile, out CropPlot crop) && IsMature(crop))
            {
                Harvest(tile, crop);
                return;
            }

            if (selectedItemId == "watering_can")
            {
                if (world.TryWaterAt(tile))
                {
                    shop.ShowFarmingFeedback("耕地已浇水，作物会正常生长");
                }

                return;
            }

            if (selectedItemId == "hoe")
            {
                if (world.TryTillAt(tile))
                {
                    shop.ShowFarmingFeedback("耕地已开垦，可以播种了");
                }

                return;
            }

            if ((selectedItemId != "wheat_seed" && selectedItemId != "carrot_seed") ||
                !world.IsFarmlandAt(tile))
            {
                return;
            }

            if (plots.TryGetValue(tile, out CropPlot existing))
            {
                return;
            }

            Sprite[] stages = selectedItemId == "carrot_seed" ? carrotStages : wheatStages;
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            if (!shop.TryConsumeItem(selectedItemId))
            {
                shop.ShowFarmingFeedback("先去商店购买种子");
                return;
            }

            CreateCrop(tile, selectedItemId, 0f, stages);
            shop.ShowFarmingFeedback(selectedItemId == "carrot_seed" ? "胡萝卜已播种" : "小麦已播种");
        }

        public List<CropSaveData> CaptureCrops()
        {
            List<CropSaveData> savedCrops = new List<CropSaveData>();
            foreach (KeyValuePair<Vector2Int, CropPlot> entry in plots)
            {
                CropPlot plot = entry.Value;
                savedCrops.Add(new CropSaveData
                {
                    x = entry.Key.x,
                    y = entry.Key.y,
                    seedId = plot.SeedId,
                    elapsed = Mathf.Max(0f, plot.GrowthElapsed)
                });
            }

            return savedCrops;
        }

        public void RestoreCrop(CropSaveData savedCrop)
        {
            if (savedCrop == null || string.IsNullOrEmpty(savedCrop.seedId))
            {
                return;
            }

            Vector2Int tile = new Vector2Int(savedCrop.x, savedCrop.y);
            if (!world.IsFarmlandAt(tile) || plots.ContainsKey(tile))
            {
                return;
            }

            Sprite[] stages = savedCrop.seedId == "carrot_seed" ? carrotStages : wheatStages;
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            CreateCrop(tile, savedCrop.seedId, Mathf.Max(0f, savedCrop.elapsed), stages);
        }

        public void RefreshGrowth()
        {
            UpdateGrowth();
        }

        private void CreateCrop(
            Vector2Int tile,
            string seedId,
            float elapsed,
            Sprite[] stages)
        {
            GameObject cropObject = new GameObject(
                (seedId == "carrot_seed" ? "Carrot" : "Wheat") + " Crop " + tile);
            cropObject.transform.SetParent(transform, false);
            // The crop sprites are rooted at their bottom edge, so place that
            // edge on the bottom of the 1×1 farmland tile.
            cropObject.transform.position = new Vector3(tile.x, tile.y - 0.5f, 0f);
            SpriteRenderer renderer = cropObject.AddComponent<SpriteRenderer>();
            float safeElapsed = Mathf.Max(0f, elapsed);
            int stage = GetStageForElapsed(safeElapsed);
            renderer.sprite = stages[stage];
            renderer.sortingOrder = ProceduralWorld.GetSurfaceSortingOrder(tile.y) - 10;

            plots[tile] = new CropPlot
            {
                SeedId = seedId,
                GrowthElapsed = safeElapsed,
                Stage = stage,
                Renderer = renderer
            };
        }

        private static bool IsMature(CropPlot plot)
        {
            return plot != null &&
                   (plot.Stage >= MatureStage ||
                    plot.GrowthElapsed >= GrowthStageDuration * MatureStage);
        }

        private static int GetStageForElapsed(float elapsed)
        {
            return Mathf.Clamp(
                Mathf.FloorToInt(elapsed / GrowthStageDuration),
                0,
                MatureStage);
        }

        private static bool IsWithinInteractionDistance(Vector2 playerPosition, Vector2Int tile)
        {
            // Measure to the nearest point of the 1×1 tile, not only its center.
            // This keeps edge clicks usable while preserving the interaction range.
            float horizontal = Mathf.Max(Mathf.Abs(playerPosition.x - tile.x) - 0.5f, 0f);
            float vertical = Mathf.Max(Mathf.Abs(playerPosition.y - tile.y) - 0.5f, 0f);
            return horizontal * horizontal + vertical * vertical <=
                   InteractionDistance * InteractionDistance;
        }

        private void UpdateGrowth()
        {
            foreach (KeyValuePair<Vector2Int, CropPlot> entry in plots)
            {
                CropPlot plot = entry.Value;
                float growthMultiplier = world.IsWetAt(entry.Key)
                    ? 1f
                    : DryGrowthMultiplier;
                plot.GrowthElapsed += Time.deltaTime * growthMultiplier;
                int nextStage = GetStageForElapsed(plot.GrowthElapsed);
                if (nextStage == plot.Stage)
                {
                    continue;
                }

                plot.Stage = nextStage;
                Sprite[] stages = plot.SeedId == "carrot_seed" ? carrotStages : wheatStages;
                if (plot.Renderer != null && stages != null && stages.Length > plot.Stage)
                {
                    plot.Renderer.sprite = stages[plot.Stage];
                }
                if (plot.Stage == MatureStage)
                {
                    shop.ShowFarmingFeedback(
                        plot.SeedId == "carrot_seed" ? "胡萝卜成熟了" : "小麦成熟了");
                }
            }
        }

        private void Harvest(Vector2Int tile, CropPlot plot)
        {
            if (plot.Renderer != null)
            {
                Destroy(plot.Renderer.gameObject);
            }

            world.DryFarmlandAt(tile);
            plots.Remove(tile);
            shop.AddHarvest(plot.SeedId);
        }

        private static Sprite[] CreateSprites(
            Texture2D texture,
            int row,
            int firstColumn,
            int count,
            string label)
        {
            Sprite[] sprites = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                int column = firstColumn + i;
                float y = texture.height - (row + 1) * 12f;
                sprites[i] = Sprite.Create(
                    texture,
                    new Rect(column * 12f, y, 12f, 12f),
                    new Vector2(0.5f, 0.02f),
                    12f);
                sprites[i].name = label + " " + i;
            }

            return sprites;
        }

        private sealed class CropPlot
        {
            public string SeedId;
            public float GrowthElapsed;
            public int Stage;
            public SpriteRenderer Renderer;
        }
    }
}
