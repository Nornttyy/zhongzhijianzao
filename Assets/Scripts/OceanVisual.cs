using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class OceanVisual : MonoBehaviour
    {
        private const float PixelsPerUnit = 16f;
        private const int TileSize = 16;
        private Sprite[] waterFrames;
        private SpriteRenderer[] tiles;
        private float nextFrameTime;
        private int frame;

        public Bounds OceanBounds { get; private set; }

        public void Initialize(Texture2D atlas)
        {
            OceanBounds = new Bounds(Vector3.zero, new Vector3(64f, 64f, 1f));
            waterFrames = new Sprite[4];
            for (int i = 0; i < waterFrames.Length; i++)
            {
                waterFrames[i] = CreateAtlasSprite(atlas, i, 0, "Water Frame " + (i + 1));
            }

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
                    renderer.sprite = waterFrames[0];
                    renderer.sortingOrder = -100;
                    tiles[index++] = renderer;
                }
            }
        }

        private void Update()
        {
            if (waterFrames == null || waterFrames.Length == 0 || Time.time < nextFrameTime)
            {
                return;
            }

            frame = (frame + 1) % waterFrames.Length;
            nextFrameTime = Time.time + 0.24f;
            if (tiles == null)
            {
                return;
            }

            foreach (SpriteRenderer tile in tiles)
            {
                if (tile != null)
                {
                    tile.sprite = waterFrames[frame];
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
