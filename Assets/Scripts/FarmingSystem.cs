using System.Collections.Generic;
using UnityEngine;

namespace DoNotOpen.Prototype
{
    /// <summary>
    /// The first playable farming loop: buy a seed, plant it on the starter
    /// field, wait for the three pixel-art growth stages, then harvest it for
    /// coins. The field itself is part of the procedural world; crops are
    /// lightweight overlays so they do not alter the terrain mesh.
    /// </summary>
    public sealed class FarmingSystem : MonoBehaviour
    {
        private const float InteractionDistance = 2.45f;
        private const float GrowthStageDuration = 7f;
        private const int MatureStage = 2;

        private readonly Dictionary<Vector2Int, CropPlot> plots =
            new Dictionary<Vector2Int, CropPlot>();

        private TopDownPlayer player;
        private ProceduralWorld world;
        private ShopSystem shop;
        private Sprite[] wheatStages;
        private Sprite[] carrotStages;
        private string selectedSeed = "wheat_seed";

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
            if (itemId == "wheat_seed" || itemId == "carrot_seed")
            {
                selectedSeed = itemId;
            }
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
            Vector2 tilePosition = new Vector2(tile.x, tile.y);
            if (Vector2.Distance(player.transform.position, tilePosition) > InteractionDistance ||
                !world.IsFarmlandAt(tile))
            {
                return;
            }

            if (plots.TryGetValue(tile, out CropPlot existing))
            {
                if (existing.Stage >= MatureStage)
                {
                    Harvest(tile, existing);
                }

                return;
            }

            if (!shop.TryConsumeItem(selectedSeed))
            {
                shop.ShowFarmingFeedback("先去商店购买种子");
                return;
            }

            Sprite[] stages = selectedSeed == "carrot_seed" ? carrotStages : wheatStages;
            if (stages == null || stages.Length == 0)
            {
                return;
            }

            GameObject cropObject = new GameObject(
                (selectedSeed == "carrot_seed" ? "Carrot" : "Wheat") + " Crop " + tile);
            cropObject.transform.SetParent(transform, false);
            // The crop sprites are rooted at their bottom edge, so place that
            // edge on the bottom of the 1×1 farmland tile.
            cropObject.transform.position = new Vector3(tile.x, tile.y - 0.5f, 0f);
            SpriteRenderer renderer = cropObject.AddComponent<SpriteRenderer>();
            renderer.sprite = stages[0];
            renderer.sortingOrder = ProceduralWorld.GetSurfaceSortingOrder(tile.y) - 10;

            plots[tile] = new CropPlot
            {
                SeedId = selectedSeed,
                PlantedAt = Time.time,
                Stage = 0,
                Renderer = renderer
            };
            shop.ShowFarmingFeedback(selectedSeed == "carrot_seed" ? "胡萝卜已播种" : "小麦已播种");
        }

        private void UpdateGrowth()
        {
            foreach (CropPlot plot in plots.Values)
            {
                int nextStage = Mathf.Clamp(
                    Mathf.FloorToInt((Time.time - plot.PlantedAt) / GrowthStageDuration),
                    0,
                    MatureStage);
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
            int reward = plot.SeedId == "carrot_seed" ? 16 : 10;
            player.AddCoins(reward);
            if (plot.Renderer != null)
            {
                Destroy(plot.Renderer.gameObject);
            }

            plots.Remove(tile);
            shop.NotifyHarvest(plot.SeedId, reward);
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
            public float PlantedAt;
            public int Stage;
            public SpriteRenderer Renderer;
        }
    }
}
