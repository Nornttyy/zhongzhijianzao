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
        private const int AtlasCellSize = 32;
        public enum GroundType
        {
            Grass,
            GrassFlowers,
            Stone,
            Water
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

        private static readonly int[] TileColumns = { 0, 1, 4, 6 };

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

            RefreshChunks(true);
        }

        private void Update()
        {
            RefreshChunks(false);
        }

        public Vector2Int WorldToTile(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x + 0.5f),
                Mathf.FloorToInt(position.y + 0.5f));
        }

        public bool CanStandAt(Vector2 position, float radius)
        {
            if (!MapBounds.Contains(new Vector3(position.x, position.y, 0f)))
            {
                return false;
            }

            return IsWalkable(WorldToTile(position)) &&
                   IsWalkable(WorldToTile(position + Vector2.left * radius)) &&
                   IsWalkable(WorldToTile(position + Vector2.right * radius)) &&
                   IsWalkable(WorldToTile(position + Vector2.up * radius)) &&
                   IsWalkable(WorldToTile(position + Vector2.down * radius));
        }

        public Vector2 ClampToBounds(Vector2 position, float margin)
        {
            return new Vector2(
                Mathf.Clamp(position.x, MapBounds.min.x + margin, MapBounds.max.x - margin),
                Mathf.Clamp(position.y, MapBounds.min.y + margin, MapBounds.max.y - margin));
        }

        public GroundType GetGround(int worldX, int worldY)
        {
            // Keep the initial clearing comfortable and free of blocking water.
            if (Mathf.Abs(worldX) <= 7 && Mathf.Abs(worldY) <= 7)
            {
                return Hash01(worldX, worldY, Seed + 19) < 0.16f
                    ? GroundType.GrassFlowers
                    : GroundType.Grass;
            }

            float water = FractalNoise(worldX * 0.026f, worldY * 0.026f, Seed + 101);
            if (water > 0.635f)
            {
                return GroundType.Water;
            }

            float stone = FractalNoise(worldX * 0.032f, worldY * 0.032f, Seed + 307);
            if (stone > 0.68f)
            {
                return GroundType.Stone;
            }

            return Hash01(worldX, worldY, Seed + 919) < 0.18f
                ? GroundType.GrassFlowers
                : GroundType.Grass;
        }

        private bool IsWalkable(Vector2Int tile)
        {
            if (tile.x < MapBounds.min.x || tile.x > MapBounds.max.x ||
                tile.y < MapBounds.min.y || tile.y > MapBounds.max.y)
            {
                return false;
            }

            return GetGround(tile.x, tile.y) != GroundType.Water;
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
                    WriteTile(vertices, uv, triangles, tileIndex, localX, localY, TileColumns[(int)ground]);
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
            int atlasColumn)
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

            // Sample from texel centres so neighbouring atlas cells never bleed
            // into one another when the camera moves between screen pixels.
            float insetX = 0.5f / atlas.width;
            float insetY = 0.5f / atlas.height;
            float uMin = atlasColumn * AtlasCellSize / (float)atlas.width + insetX;
            float uMax = (atlasColumn + 1) * AtlasCellSize / (float)atlas.width - insetX;
            float vMin = (atlas.height - AtlasCellSize) / (float)atlas.height + insetY;
            float vMax = 1f - insetY;
            uv[vertex] = new Vector2(uMin, vMin);
            uv[vertex + 1] = new Vector2(uMin, vMax);
            uv[vertex + 2] = new Vector2(uMax, vMax);
            uv[vertex + 3] = new Vector2(uMax, vMin);

            int triangle = tileIndex * 6;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 1;
            triangles[triangle + 2] = vertex + 2;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 2;
            triangles[triangle + 5] = vertex + 3;
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
            if (worldMaterial != null)
            {
                Destroy(worldMaterial);
            }
        }
    }

    public sealed class GeneratedWorldChunk : MonoBehaviour
    {
        public Mesh Mesh { get; set; }

        private void OnDestroy()
        {
            if (Mesh != null)
            {
                Destroy(Mesh);
            }
        }
    }
}
