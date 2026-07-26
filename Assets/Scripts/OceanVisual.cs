using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class OceanVisual : MonoBehaviour
    {
        private const float PixelsPerUnit = 16f;
        private const int TileSize = 16;
        private Sprite waterSprite;
        private Texture2D waterTexture;
        private SpriteRenderer[] tiles;

        public Bounds OceanBounds { get; private set; }

        public void Initialize(Texture2D atlas)
        {
            OceanBounds = new Bounds(Vector3.zero, new Vector3(64f, 64f, 1f));
            waterTexture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false)
            {
                name = "Solid Ocean Water",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32 oceanColor = new Color32(37, 143, 183, 255);
            Color32[] pixels = new Color32[TileSize * TileSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = oceanColor;
            }
            waterTexture.SetPixels32(pixels);
            waterTexture.Apply(false, true);
            waterSprite = Sprite.Create(
                waterTexture,
                new Rect(0f, 0f, TileSize, TileSize),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            waterSprite.name = "Solid Water";

            int width = Mathf.CeilToInt(OceanBounds.size.x);
            int height = Mathf.CeilToInt(OceanBounds.size.y);
            tiles = new SpriteRenderer[width * height];
            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GameObject tileObject = new GameObject("Ocean Tile");
                    tileObject.transform.SetParent(transform, false);
                    tileObject.transform.localPosition = new Vector3(
                        OceanBounds.min.x + x + 0.5f,
                        OceanBounds.min.y + y + 0.5f,
                        1f);
                    SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = waterSprite;
                    renderer.sortingOrder = -100;
                    tiles[index++] = renderer;
                }
            }
        }

        private void OnDestroy()
        {
            if (waterSprite != null)
            {
                Destroy(waterSprite);
            }
            if (waterTexture != null)
            {
                Destroy(waterTexture);
            }
        }

        private static Sprite CreateAtlasSprite(Texture2D atlas, int column, int row, string name)
        {
            if (atlas == null || atlas.width < (column + 1) * TileSize || atlas.height < (row + 1) * TileSize)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(
                atlas,
                new Rect(column * TileSize, atlas.height - (row + 1) * TileSize, TileSize, TileSize),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            sprite.name = name;
            return sprite;
        }
    }
}
