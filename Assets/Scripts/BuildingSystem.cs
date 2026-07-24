using System.Collections.Generic;
using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class BuildingSystem : MonoBehaviour
    {
        private const float PlacementDistance = 6f;
        private const float PixelPerUnit = 12f;

        private readonly Dictionary<string, int> ownedBuildings =
            new Dictionary<string, int>();
        private readonly List<PlacedBuilding> placedBuildings =
            new List<PlacedBuilding>();
        private readonly List<Texture2D> generatedTextures =
            new List<Texture2D>();
        private readonly List<Sprite> generatedSprites =
            new List<Sprite>();

        private TopDownPlayer player;
        private ProceduralWorld world;
        private string selectedBuildingId;

        public void Initialize(TopDownPlayer controlledPlayer, ProceduralWorld generatedWorld)
        {
            player = controlledPlayer;
            world = generatedWorld;
            ownedBuildings["training_camp"] = 0;
            ownedBuildings["wall"] = 0;
            ownedBuildings["gate"] = 0;
        }

        public void BuyBuilding(string buildingId)
        {
            int price = GetPrice(buildingId);
            if (price <= 0 || player == null || !player.TrySpendCoins(price))
            {
                return;
            }

            ownedBuildings[buildingId] = GetOwnedCount(buildingId) + 1;
            selectedBuildingId = buildingId;
        }

        public bool CanStandAt(Vector2 position, float radius)
        {
            foreach (PlacedBuilding building in placedBuildings)
            {
                if (Mathf.Abs(position.x - building.Position.x) <=
                        building.HalfExtents.x + radius &&
                    Mathf.Abs(position.y - building.Position.y) <=
                        building.HalfExtents.y + radius)
                {
                    return false;
                }
            }

            return true;
        }

        private void Update()
        {
            if (player == null || world == null || !player.enabled)
            {
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                TryPlaceSelectedBuilding();
            }
        }

        private void TryPlaceSelectedBuilding()
        {
            if (string.IsNullOrEmpty(selectedBuildingId) ||
                GetOwnedCount(selectedBuildingId) <= 0 ||
                Camera.main == null)
            {
                return;
            }

            Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int tile = world.WorldToTile(mouse);
            Vector2 position = new Vector2(tile.x, tile.y);
            if (Vector2.Distance(player.transform.position, position) > PlacementDistance ||
                world.IsWaterAt(position) ||
                !world.CanStandAt(position, 0.32f) ||
                !CanStandAt(position, GetHalfExtents(selectedBuildingId).x))
            {
                return;
            }

            CreateBuilding(selectedBuildingId, position);
            ownedBuildings[selectedBuildingId]--;
            if (ownedBuildings[selectedBuildingId] <= 0)
            {
                selectedBuildingId = null;
            }
        }

        private void CreateBuilding(string buildingId, Vector2 position)
        {
            GameObject buildingObject = new GameObject("Building " + buildingId);
            buildingObject.transform.position = position;
            buildingObject.transform.localScale = GetVisualSize(buildingId);

            SpriteRenderer renderer = buildingObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateBuildingSprite(buildingId);
            renderer.sortingOrder = ProceduralWorld.GetSurfaceSortingOrder(position.y) + 2;

            placedBuildings.Add(new PlacedBuilding
            {
                Id = buildingId,
                Position = position,
                HalfExtents = GetHalfExtents(buildingId),
                Object = buildingObject
            });
        }

        private Sprite CreateBuildingSprite(string buildingId)
        {
            Texture2D texture = new Texture2D(12, 12, TextureFormat.RGBA32, false)
            {
                name = "Generated " + buildingId + " Pixel Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color dark = new Color32(42, 48, 38, 255);
            Color main = buildingId == "wall"
                ? new Color32(112, 120, 108, 255)
                : buildingId == "gate"
                    ? new Color32(151, 91, 51, 255)
                    : new Color32(170, 112, 61, 255);

            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            if (buildingId == "wall")
            {
                Fill(texture, 0, 3, 11, 8, main);
                Fill(texture, 0, 3, 11, 3, dark);
                Fill(texture, 0, 8, 11, 8, dark);
                Fill(texture, 3, 5, 3, 6, dark);
                Fill(texture, 8, 5, 8, 6, dark);
            }
            else if (buildingId == "gate")
            {
                Fill(texture, 1, 1, 10, 10, dark);
                Fill(texture, 3, 2, 8, 10, main);
                Fill(texture, 5, 5, 6, 10, new Color32(91, 55, 39, 255));
                texture.SetPixel(7, 6, new Color32(244, 202, 91, 255));
            }
            else
            {
                Fill(texture, 2, 2, 9, 10, main);
                Fill(texture, 1, 1, 10, 3, new Color32(126, 55, 42, 255));
                Fill(texture, 3, 5, 8, 6, new Color32(215, 177, 93, 255));
                Fill(texture, 5, 7, 6, 10, dark);
            }

            texture.Apply();
            generatedTextures.Add(texture);
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 12f, 12f),
                new Vector2(0.5f, 0.5f),
                PixelPerUnit);
            sprite.name = "Generated " + buildingId + " Pixel Sprite";
            generatedSprites.Add(sprite);
            return sprite;
        }

        private static void Fill(Texture2D texture, int minX, int minY, int maxX, int maxY, Color color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static int GetPrice(string buildingId)
        {
            switch (buildingId)
            {
                case "training_camp":
                    return 80;
                case "wall":
                    return 10;
                case "gate":
                    return 25;
                default:
                    return 0;
            }
        }

        private static Vector2 GetHalfExtents(string buildingId)
        {
            switch (buildingId)
            {
                case "training_camp":
                    return new Vector2(0.75f, 0.65f);
                case "wall":
                    return new Vector2(0.55f, 0.2f);
                case "gate":
                    return new Vector2(0.6f, 0.3f);
                default:
                    return new Vector2(0.4f, 0.4f);
            }
        }

        private static Vector3 GetVisualSize(string buildingId)
        {
            switch (buildingId)
            {
                case "training_camp":
                    return new Vector3(1.8f, 1.6f, 1f);
                case "wall":
                    return new Vector3(1.25f, 0.5f, 1f);
                case "gate":
                    return new Vector3(1.35f, 0.8f, 1f);
                default:
                    return Vector3.one;
            }
        }

        private int GetOwnedCount(string buildingId)
        {
            return ownedBuildings.TryGetValue(buildingId, out int count) ? count : 0;
        }

        private void OnDestroy()
        {
            foreach (Sprite sprite in generatedSprites)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }

            foreach (Texture2D texture in generatedTextures)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        private sealed class PlacedBuilding
        {
            public string Id;
            public Vector2 Position;
            public Vector2 HalfExtents;
            public GameObject Object;
        }
    }
}
