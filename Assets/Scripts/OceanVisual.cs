using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class OceanVisual : MonoBehaviour
    {
        private const float PixelsPerUnit = 16f;
        private const int TileSize = 16;
        private Sprite waterSprite;
        private SpriteRenderer[] tiles;

        public Bounds OceanBounds { get; private set; }

        public void Initialize(Texture2D atlas)
        {
            OceanBounds = new Bounds(Vector3.zero, new Vector3(64f, 64f, 1f));
            waterSprite = CreateAtlasSprite(atlas, 0, 0, "Water");

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
