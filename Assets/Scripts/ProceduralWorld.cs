using System.Collections.Generic;
using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class ProceduralWorld : MonoBehaviour
    {
        public const int WorldWidth = 100000;
        public const int WorldHeight = 100000;
        public const int ChunkSize = 16;

        private const int LoadRadius = 1;
        private const int AtlasCellSize = 12;
        private const float WaterFrameDuration = 0.55f;
        private const float DecorationChance = 0.11f;
        private const int SwimEntryWaterSamples = 7;
        private const int SwimStayWaterSamples = 3;

        private static readonly Vector2[] SwimSampleOffsets =
        {
            Vector2.zero,
            new Vector2(-0.28f, 0f),
            new Vector2(0.28f, 0f),
            new Vector2(0f, -0.28f),
            new Vector2(0f, 0.28f),
            new Vector2(-0.2f, -0.2f),
            new Vector2(-0.2f, 0.2f),
            new Vector2(0.2f, -0.2f),
            new Vector2(0.2f, 0.2f)
        };

        public enum GroundType
        {
            Grass,
            Stone,
            Water,
            Sand
        }

        public int Seed { get; private set; } = 271828;
        public Bounds MapBounds { get; private set; }

        private readonly Dictionary<Vector2Int, GeneratedWorldChunk> loadedChunks =
            new Dictionary<Vector2Int, GeneratedWorldChunk>();

        private readonly List<Vector2Int> removalBuffer = new List<Vector2Int>();
        private Texture2D atlas;
        private Material worldMaterial;
        private TopDownPlayer player;
        private Vector2Int currentChunk = new Vector2Int(int.MinValue, int.MinValue);
        private Sprite[] decorationSprites;
        private Sprite[] mineralSprites;
        private bool useSecondWaterFrame;
        private float nextWaterFrameTime;

        public void Initialize(Texture2D worldAtlas, TopDownPlayer controlledPlayer)
        {
            atlas = worldAtlas;
            player = controlledPlayer;
            MapBounds = new Bounds(
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(WorldWidth, WorldHeight, 1f));

            Shader spriteShader = Shader.Find("Sprites/Default");
            worldMaterial = new Material(spriteShader)
            {
                name = "Procedural World Pixel Material",
                mainTexture = atlas
            };

            decorationSprites = CreateAtlasSprites(1, 0, 7, "Decoration");
            mineralSprites = CreateAtlasSprites(2, 2, 5, "Mineral");
            nextWaterFrameTime = Time.time + WaterFrameDuration;
            RefreshChunks(true);
        }

        private void Update()
        {
            RefreshChunks(false);
            if (Time.time >= nextWaterFrameTime)
            {
                useSecondWaterFrame = !useSecondWaterFrame;
                nextWaterFrameTime = Time.time + WaterFrameDuration;
                RefreshAnimatedTileUvs();
            }
        }

        public Vector2Int WorldToTile(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x + 0.5f),
                Mathf.FloorToInt(position.y + 0.5f));
        }

        public bool CanStandAt(Vector2 position, float radius)
        {
            return ContainsPosition(position) &&
                   ContainsPosition(position + Vector2.left * radius) &&
                   ContainsPosition(position + Vector2.right * radius) &&
                   ContainsPosition(position + Vector2.up * radius) &&
                   ContainsPosition(position + Vector2.down * radius);
        }

        public bool IsWaterAt(Vector2 position)
        {
            Vector2Int tile = WorldToTile(position);
            return GetGround(tile.x, tile.y) == GroundType.Water;
        }

        public bool ShouldSwimAt(Vector2 position, bool currentlySwimming)
        {
            int waterSamples = 0;
            for (int i = 0; i < SwimSampleOffsets.Length; i++)
            {
                if (IsWaterAt(position + SwimSampleOffsets[i]))
                {
                    waterSamples++;
                }
            }

            // 进入水中时要求脚下大部分范围都在水里，避免刚碰到岸边就开始游泳。
            if (!currentlySwimming)
            {
                return waterSamples >= SwimEntryWaterSamples;
            }

            // 离开水面时保留一点缓冲，防止状态在岸边反复切换。
            return IsWaterAt(position) && waterSamples >= SwimStayWaterSamples;
        }

        public Vector2 ClampToBounds(Vector2 position, float margin)
        {
            return new Vector2(
                Mathf.Clamp(position.x, MapBounds.min.x + margin, MapBounds.max.x - margin),
                Mathf.Clamp(position.y, MapBounds.min.y + margin, MapBounds.max.y - margin));
        }

        public GroundType GetGround(int worldX, int worldY)
        {
            // 出生点附近保持为草地，方便玩家一开始活动。
            if (Mathf.Abs(worldX) <= 7 && Mathf.Abs(worldY) <= 7)
            {
                return GroundType.Grass;
            }

            if (IsWaterNoise(worldX, worldY))
            {
                return GroundType.Water;
            }

            // 水边的一圈陆地变成沙地，组成自然的沙滩。
            if (IsWaterNoise(worldX + 1, worldY) ||
                IsWaterNoise(worldX - 1, worldY) ||
                IsWaterNoise(worldX, worldY + 1) ||
                IsWaterNoise(worldX, worldY - 1))
            {
                return GroundType.Sand;
            }

            float stone = FractalNoise(worldX * 0.032f, worldY * 0.032f, Seed + 307);
            if (stone > 0.68f)
            {
                return GroundType.Stone;
            }

            return GroundType.Grass;
        }

        private bool ContainsPosition(Vector2 position)
        {
            return MapBounds.Contains(new Vector3(position.x, position.y, 0f));
        }

        private void RefreshChunks(bool force)
        {
            if (player == null || atlas == null)
            {
                return;
            }

            Vector2Int nextChunk = new Vector2Int(
                Mathf.FloorToInt(player.transform.position.x / ChunkSize),
                Mathf.FloorToInt(player.transform.position.y / ChunkSize));
            if (!force && nextChunk == currentChunk)
            {
                return;
            }

            currentChunk = nextChunk;
            for (int y = -LoadRadius; y <= LoadRadius; y++)
            {
                for (int x = -LoadRadius; x <= LoadRadius; x++)
                {
                    Vector2Int coordinate = currentChunk + new Vector2Int(x, y);
                    if (!loadedChunks.ContainsKey(coordinate) && ChunkIntersectsWorld(coordinate))
                    {
                        loadedChunks.Add(coordinate, CreateChunk(coordinate));
                    }
                }
            }

            removalBuffer.Clear();
            foreach (KeyValuePair<Vector2Int, GeneratedWorldChunk> entry in loadedChunks)
            {
                Vector2Int delta = entry.Key - currentChunk;
                if (Mathf.Abs(delta.x) > LoadRadius || Mathf.Abs(delta.y) > LoadRadius)
                {
                    removalBuffer.Add(entry.Key);
                }
            }

            foreach (Vector2Int coordinate in removalBuffer)
            {
                GeneratedWorldChunk chunk = loadedChunks[coordinate];
                loadedChunks.Remove(coordinate);
                Destroy(chunk.gameObject);
            }
        }

        private GeneratedWorldChunk CreateChunk(Vector2Int coordinate)
        {
            GameObject chunkObject = new GameObject("World Chunk " + coordinate.x + ", " + coordinate.y);
            chunkObject.transform.SetParent(transform, false);
            chunkObject.transform.position = new Vector3(
                coordinate.x * ChunkSize,
                coordinate.y * ChunkSize,
                0f);

            Mesh mesh = BuildChunkMesh(coordinate);
            MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = chunkObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = worldMaterial;
            renderer.sortingOrder = 0;

            GeneratedWorldChunk chunk = chunkObject.AddComponent<GeneratedWorldChunk>();
            chunk.Mesh = mesh;
            chunk.Coordinate = coordinate;
            PopulateDecorations(chunkObject.transform, coordinate);
            return chunk;
        }

        private Mesh BuildChunkMesh(Vector2Int coordinate)
        {
            int tileCount = ChunkSize * ChunkSize;
            Vector3[] vertices = new Vector3[tileCount * 4];
            Vector2[] uv = new Vector2[tileCount * 4];
            int[] triangles = new int[tileCount * 6];
            int tileIndex = 0;

            for (int localY = 0; localY < ChunkSize; localY++)
            {
                for (int localX = 0; localX < ChunkSize; localX++)
                {
                    int worldX = coordinate.x * ChunkSize + localX;
                    int worldY = coordinate.y * ChunkSize + localY;
                    GroundType ground = IsInsideWorld(worldX, worldY)
                        ? GetGround(worldX, worldY)
                        : GroundType.Water;
                    Vector2Int atlasTile = GetAtlasTile(ground, worldX, worldY);
                    WriteTile(
                        vertices,
                        uv,
                        triangles,
                        tileIndex,
                        localX,
                        localY,
                        atlasTile.x,
                        atlasTile.y);
                    tileIndex++;
                }
            }

            Mesh mesh = new Mesh { name = "Generated Chunk Mesh " + coordinate.x + ", " + coordinate.y };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void WriteTile(
            Vector3[] vertices,
            Vector2[] uv,
            int[] triangles,
            int tileIndex,
            int x,
            int y,
            int atlasColumn,
            int atlasRow)
        {
            int vertex = tileIndex * 4;
            float left = x - 0.5f;
            float right = x + 0.5f;
            float bottom = y - 0.5f;
            float top = y + 0.5f;
            vertices[vertex] = new Vector3(left, bottom, 0f);
            vertices[vertex + 1] = new Vector3(left, top, 0f);
            vertices[vertex + 2] = new Vector3(right, top, 0f);
            vertices[vertex + 3] = new Vector3(right, bottom, 0f);

            WriteTileUv(uv, tileIndex, atlasColumn, atlasRow);

            int triangle = tileIndex * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 1;
            triangles[triangle + 2] = vertex + 2;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
        }

        private void WriteTileUv(
            Vector2[] uv,
            int tileIndex,
            int atlasColumn,
            int atlasRow)
        {
            // 从像素中心取样，避免相邻 12×12 方块互相渗色形成接缝。
            int vertex = tileIndex * 4;
            float insetX = 0.5f / atlas.width;
            float insetY = 0.5f / atlas.height;
            float uMin = atlasColumn * AtlasCellSize / (float)atlas.width + insetX;
            float uMax = (atlasColumn + 1) * AtlasCellSize / (float)atlas.width - insetX;
            float vMin =
                (atlas.height - (atlasRow + 1) * AtlasCellSize) / (float)atlas.height + insetY;
            float vMax =
                (atlas.height - atlasRow * AtlasCellSize) / (float)atlas.height - insetY;
            uv[vertex] = new Vector2(uMin, vMin);
            uv[vertex + 1] = new Vector2(uMin, vMax);
            uv[vertex + 2] = new Vector2(uMax, vMax);
            uv[vertex + 3] = new Vector2(uMax, vMin);
        }

        private Vector2Int GetAtlasTile(GroundType ground, int worldX, int worldY)
        {
            switch (ground)
            {
                case GroundType.Stone:
                    return new Vector2Int(5, 0);
                case GroundType.Water:
                    return useSecondWaterFrame
                        ? new Vector2Int(1, 2)
                        : new Vector2Int(6, 0);
                case GroundType.Sand:
                    return new Vector2Int(0, 2);
                default:
                    // 两种草地颜色相同，只用不同斑点位置减少重复感。
                    return Hash01(worldX, worldY, Seed + 919) < 0.5f
                        ? new Vector2Int(0, 0)
                        : new Vector2Int(1, 0);
            }
        }

        private Vector2[] BuildChunkUvs(Vector2Int coordinate)
        {
            Vector2[] uv = new Vector2[ChunkSize * ChunkSize * 4];
            int tileIndex = 0;
            for (int localY = 0; localY < ChunkSize; localY++)
            {
                for (int localX = 0; localX < ChunkSize; localX++)
                {
                    int worldX = coordinate.x * ChunkSize + localX;
                    int worldY = coordinate.y * ChunkSize + localY;
                    GroundType ground = IsInsideWorld(worldX, worldY)
                        ? GetGround(worldX, worldY)
                        : GroundType.Water;
                    Vector2Int atlasTile = GetAtlasTile(ground, worldX, worldY);
                    WriteTileUv(uv, tileIndex, atlasTile.x, atlasTile.y);
                    tileIndex++;
                }
            }

            return uv;
        }

        private void RefreshAnimatedTileUvs()
        {
            foreach (KeyValuePair<Vector2Int, GeneratedWorldChunk> entry in loadedChunks)
            {
                if (entry.Value.Mesh != null)
                {
                    entry.Value.Mesh.uv = BuildChunkUvs(entry.Key);
                }
            }
        }

        private void PopulateDecorations(Transform chunk, Vector2Int coordinate)
        {
            if (decorationSprites == null || decorationSprites.Length == 0)
            {
                return;
            }

            for (int localY = 0; localY < ChunkSize; localY++)
            {
                for (int localX = 0; localX < ChunkSize; localX++)
                {
                    int worldX = coordinate.x * ChunkSize + localX;
                    int worldY = coordinate.y * ChunkSize + localY;
                    if (!IsInsideWorld(worldX, worldY) ||
                        GetGround(worldX, worldY) != GroundType.Grass ||
                        Hash01(worldX, worldY, Seed + 4001) >= DecorationChance)
                    {
                        continue;
                    }

                    int decorationIndex = Mathf.Min(
                        decorationSprites.Length - 1,
                        Mathf.FloorToInt(
                            Hash01(worldX, worldY, Seed + 5101) * decorationSprites.Length));
                    GameObject decoration = new GameObject(
                        "Decoration " + worldX + ", " + worldY);
                    decoration.transform.SetParent(chunk, false);
                    decoration.transform.localPosition = new Vector3(localX, localY, 0f);

                    SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
                    renderer.sprite = decorationSprites[decorationIndex];
                    renderer.sortingOrder = 20;
                }
            }
        }

        private Sprite[] CreateAtlasSprites(
            int atlasRow,
            int firstColumn,
            int count,
            string spriteName)
        {
            Sprite[] sprites = new Sprite[count];
            float y = atlas.height - (atlasRow + 1) * AtlasCellSize;
            for (int i = 0; i < count; i++)
            {
                int column = firstColumn + i;
                sprites[i] = Sprite.Create(
                    atlas,
                    new Rect(
                        column * AtlasCellSize,
                        y,
                        AtlasCellSize,
                        AtlasCellSize),
                    new Vector2(0.5f, 0.5f),
                    AtlasCellSize);
                sprites[i].name = spriteName + " " + i;
            }

            return sprites;
        }

        public int MineralSpriteCount
        {
            get { return mineralSprites == null ? 0 : mineralSprites.Length; }
        }

        public Sprite GetMineralSprite(int index)
        {
            if (mineralSprites == null || mineralSprites.Length == 0)
            {
                return null;
            }

            return mineralSprites[Mathf.Clamp(index, 0, mineralSprites.Length - 1)];
        }

        private bool IsWaterNoise(int worldX, int worldY)
        {
            return FractalNoise(worldX * 0.026f, worldY * 0.026f, Seed + 101) > 0.635f;
        }

        private static bool ChunkIntersectsWorld(Vector2Int coordinate)
        {
            int minX = coordinate.x * ChunkSize;
            int minY = coordinate.y * ChunkSize;
            int maxX = minX + ChunkSize - 1;
            int maxY = minY + ChunkSize - 1;
            return maxX >= -WorldWidth / 2 && minX < WorldWidth / 2 &&
                   maxY >= -WorldHeight / 2 && minY < WorldHeight / 2;
        }

        private static bool IsInsideWorld(int x, int y)
        {
            return x >= -WorldWidth / 2 && x < WorldWidth / 2 &&
                   y >= -WorldHeight / 2 && y < WorldHeight / 2;
        }

        private static float FractalNoise(float x, float y, int seed)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float total = 0f;
            for (int octave = 0; octave < 4; octave++)
            {
                sum += ValueNoise(x, y, seed + octave * 997) * amplitude;
                total += amplitude;
                x *= 2f;
                y *= 2f;
                amplitude *= 0.5f;
            }

            return sum / total;
        }

        private static float ValueNoise(float x, float y, int seed)
        {
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float tx = Smooth(x - x0);
            float ty = Smooth(y - y0);
            float a = Hash01(x0, y0, seed);
            float b = Hash01(x0 + 1, y0, seed);
            float c = Hash01(x0, y0 + 1, seed);
            float d = Hash01(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private static float Smooth(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static float Hash01(int x, int y, int seed)
        {
            unchecked
            {
                uint hash = (uint)x * 374761393u + (uint)y * 668265263u + (uint)seed * 2246822519u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                hash ^= hash >> 16;
                return (hash & 0x00ffffffu) / 16777215f;
            }
        }

        private void OnDestroy()
        {
            DestroySprites(decorationSprites);
            DestroySprites(mineralSprites);
            if (worldMaterial != null)
            {
                Destroy(worldMaterial);
            }
        }

        private static void DestroySprites(Sprite[] sprites)
        {
            if (sprites == null)
            {
                return;
            }

            foreach (Sprite sprite in sprites)
            {
                if (sprite != null)
                {
                    Destroy(sprite);
                }
            }
        }
    }

    public sealed class GeneratedWorldChunk : MonoBehaviour
    {
        public Mesh Mesh { get; set; }
        public Vector2Int Coordinate { get; set; }

        private void OnDestroy()
        {
            if (Mesh != null)
            {
                Destroy(Mesh);
            }
        }
    }
}
