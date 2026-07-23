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
        private const int SwimBodySampleColumns = 6;
        private const int SwimBodySampleRows = 6;
        private const int SwimBodyWaterThreshold = 19;
        private const float PlayerBodyBottom = -0.08f;
        private const float PlayerBodyWidth = 1f;
        private const float PlayerBodyHeight = 1f;
        private const float CavePixelsPerUnit = 12f;
        private const float CaveChunkChance = 0.45f;
        private const float CaveInteractionDistance = 2.35f;
        private const float CaveHintDistance = 3.6f;
        private const int CaveFloorWidth = 49;
        private const int CaveFloorHeight = 37;

        private static readonly Bounds CaveWalkBounds = new Bounds(
            Vector3.zero,
            new Vector3(47f, 35f, 1f));

        private static readonly Vector2 CaveSpawnPosition = new Vector2(0f, -15.75f);
        private static readonly Vector2 CaveExitBottom = new Vector2(0f, -18.35f);

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
        private readonly List<CaveEntranceMarker> loadedCaveEntrances =
            new List<CaveEntranceMarker>();

        private Texture2D atlas;
        private Material worldMaterial;
        private Material caveMaterial;
        private TopDownPlayer player;
        private Vector2Int currentChunk = new Vector2Int(int.MinValue, int.MinValue);
        private Sprite[] decorationSprites;
        private Sprite[] mineralSprites;
        private Sprite caveEntranceSprite;
        private GameObject caveRoot;
        private SpriteRenderer caveExitRenderer;
        private Vector2 surfaceReturnPosition;
        private Color surfaceCameraColor;
        private bool useSecondWaterFrame;
        private float nextWaterFrameTime;

        public bool IsInCave { get; private set; }

        public void Initialize(
            Texture2D worldAtlas,
            Texture2D caveEntranceTexture,
            TopDownPlayer controlledPlayer)
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

            caveMaterial = new Material(worldMaterial)
            {
                name = "Cave Pixel Material",
                color = new Color(0.68f, 0.72f, 0.68f, 1f)
            };

            decorationSprites = CreateAtlasSprites(1, 0, 7, "Decoration");
            mineralSprites = CreateAtlasSprites(2, 2, 5, "Mineral");
            caveEntranceSprite = Sprite.Create(
                caveEntranceTexture,
                new Rect(0f, 0f, caveEntranceTexture.width, caveEntranceTexture.height),
                new Vector2(0.5f, 0f),
                CavePixelsPerUnit);
            caveEntranceSprite.name = "Hand Drawn Cave Entrance";
            nextWaterFrameTime = Time.time + WaterFrameDuration;
            RefreshChunks(true);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                HandleCaveInteraction();
            }

            if (IsInCave)
            {
                return;
            }

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
            if (IsInCave)
            {
                return ContainsCavePosition(position, radius);
            }

            return ContainsPosition(position) &&
                   ContainsPosition(position + Vector2.left * radius) &&
                   ContainsPosition(position + Vector2.right * radius) &&
                   ContainsPosition(position + Vector2.up * radius) &&
                   ContainsPosition(position + Vector2.down * radius) &&
                   !IsBlockedByCaveEntrance(position, radius);
        }

        public bool IsWaterAt(Vector2 position)
        {
            if (IsInCave)
            {
                return false;
            }

            Vector2Int tile = WorldToTile(position);
            return GetGround(tile.x, tile.y) == GroundType.Water;
        }

        public bool ShouldSwimAt(Vector2 position)
        {
            if (IsInCave)
            {
                return false;
            }

            int waterSamples = 0;
            float horizontalStep = PlayerBodyWidth / SwimBodySampleColumns;
            float verticalStep = PlayerBodyHeight / SwimBodySampleRows;
            for (int row = 0; row < SwimBodySampleRows; row++)
            {
                float offsetY = PlayerBodyBottom + (row + 0.5f) * verticalStep;
                for (int column = 0; column < SwimBodySampleColumns; column++)
                {
                    float offsetX =
                        -PlayerBodyWidth * 0.5f + (column + 0.5f) * horizontalStep;
                    if (IsWaterAt(position + new Vector2(offsetX, offsetY)))
                    {
                        waterSamples++;
                        if (waterSamples >= SwimBodyWaterThreshold)
                        {
                            return true;
                        }
                    }
                }
            }

            // 角色身体共检测 36 个位置；至少 19 个在水上才算超过一半。
            return false;
        }

        public Vector2 ClampToBounds(Vector2 position, float margin)
        {
            if (IsInCave)
            {
                return new Vector2(
                    Mathf.Clamp(
                        position.x,
                        CaveWalkBounds.min.x + margin,
                        CaveWalkBounds.max.x - margin),
                    Mathf.Clamp(
                        position.y,
                        CaveWalkBounds.min.y + margin,
                        CaveWalkBounds.max.y - margin));
            }

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
            PopulateCaveEntrance(chunkObject.transform, coordinate);
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
                        IsCaveFootprintTile(worldX, worldY, coordinate) ||
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

        private void PopulateCaveEntrance(Transform chunk, Vector2Int coordinate)
        {
            if (caveEntranceSprite == null ||
                !TryGetCaveAnchor(coordinate, out Vector2Int worldAnchor))
            {
                return;
            }

            int chunkWorldX = coordinate.x * ChunkSize;
            int chunkWorldY = coordinate.y * ChunkSize;
            GameObject entranceObject = new GameObject(
                "Cave Entrance " + worldAnchor.x + ", " + worldAnchor.y);
            entranceObject.transform.SetParent(chunk, false);
            entranceObject.transform.localPosition = new Vector3(
                worldAnchor.x - chunkWorldX + 0.5f,
                worldAnchor.y - chunkWorldY - 0.5f,
                0f);

            SpriteRenderer renderer = entranceObject.AddComponent<SpriteRenderer>();
            renderer.sprite = caveEntranceSprite;
            renderer.sortingOrder =
                310 - Mathf.RoundToInt(entranceObject.transform.position.y * 10f);

            CaveEntranceMarker marker = entranceObject.AddComponent<CaveEntranceMarker>();
            marker.EntranceRenderer = renderer;
            loadedCaveEntrances.Add(marker);
        }

        private bool TryGetCaveAnchor(
            Vector2Int coordinate,
            out Vector2Int worldAnchor)
        {
            worldAnchor = default;
            if (Hash01(coordinate.x, coordinate.y, Seed + 8068) <
                1f - CaveChunkChance)
            {
                return false;
            }

            int localX = 2 + Mathf.FloorToInt(
                Hash01(coordinate.x, coordinate.y, Seed + 8066) * 12f);
            int localY = 2 + Mathf.FloorToInt(
                Hash01(coordinate.x, coordinate.y, Seed + 8067) * 12f);
            worldAnchor = new Vector2Int(
                coordinate.x * ChunkSize + localX,
                coordinate.y * ChunkSize + localY);

            return IsCaveGround(worldAnchor.x, worldAnchor.y) &&
                   IsCaveGround(worldAnchor.x + 1, worldAnchor.y) &&
                   IsCaveGround(worldAnchor.x, worldAnchor.y + 1) &&
                   IsCaveGround(worldAnchor.x + 1, worldAnchor.y + 1);
        }

        private bool IsCaveFootprintTile(
            int worldX,
            int worldY,
            Vector2Int chunkCoordinate)
        {
            if (!TryGetCaveAnchor(chunkCoordinate, out Vector2Int anchor))
            {
                return false;
            }

            return worldX >= anchor.x &&
                   worldX <= anchor.x + 1 &&
                   worldY >= anchor.y &&
                   worldY <= anchor.y + 1;
        }

        private bool IsCaveGround(int worldX, int worldY)
        {
            if (!IsInsideWorld(worldX, worldY))
            {
                return false;
            }

            GroundType ground = GetGround(worldX, worldY);
            return ground == GroundType.Grass || ground == GroundType.Stone;
        }

        private bool IsBlockedByCaveEntrance(Vector2 position, float radius)
        {
            for (int i = loadedCaveEntrances.Count - 1; i >= 0; i--)
            {
                CaveEntranceMarker entrance = loadedCaveEntrances[i];
                if (entrance == null)
                {
                    loadedCaveEntrances.RemoveAt(i);
                    continue;
                }

                if (!entrance.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector2 bottom = entrance.transform.position;
                if (position.x + radius > bottom.x - 0.9f &&
                    position.x - radius < bottom.x + 0.9f &&
                    position.y + radius > bottom.y + 0.05f &&
                    position.y - radius < bottom.y + 1.8f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCavePosition(Vector2 position, float radius)
        {
            return position.x - radius >= CaveWalkBounds.min.x &&
                   position.x + radius <= CaveWalkBounds.max.x &&
                   position.y - radius >= CaveWalkBounds.min.y &&
                   position.y + radius <= CaveWalkBounds.max.y;
        }

        private void HandleCaveInteraction()
        {
            Camera viewCamera = Camera.main;
            if (player == null || viewCamera == null)
            {
                return;
            }

            Vector2 clickPosition = viewCamera.ScreenToWorldPoint(Input.mousePosition);
            if (IsInCave)
            {
                if (caveExitRenderer != null &&
                    Vector2.Distance(player.transform.position, CaveExitInteractionPoint) <=
                    CaveInteractionDistance &&
                    RendererContainsClick(caveExitRenderer, clickPosition))
                {
                    ExitCave();
                }
                return;
            }

            for (int i = loadedCaveEntrances.Count - 1; i >= 0; i--)
            {
                CaveEntranceMarker entrance = loadedCaveEntrances[i];
                if (entrance == null)
                {
                    loadedCaveEntrances.RemoveAt(i);
                    continue;
                }

                if (!entrance.gameObject.activeInHierarchy ||
                    Vector2.Distance(player.transform.position, entrance.InteractionPoint) >
                    CaveInteractionDistance ||
                    !RendererContainsClick(entrance.EntranceRenderer, clickPosition))
                {
                    continue;
                }

                EnterCave(entrance);
                return;
            }
        }

        private static bool RendererContainsClick(
            SpriteRenderer renderer,
            Vector2 clickPosition)
        {
            if (renderer == null)
            {
                return false;
            }

            Bounds bounds = renderer.bounds;
            bounds.Expand(new Vector3(0.55f, 0.55f, 0f));
            return clickPosition.x >= bounds.min.x &&
                   clickPosition.x <= bounds.max.x &&
                   clickPosition.y >= bounds.min.y &&
                   clickPosition.y <= bounds.max.y;
        }

        private void EnterCave(CaveEntranceMarker entrance)
        {
            surfaceReturnPosition = player.transform.position;
            Camera viewCamera = Camera.main;
            if (viewCamera != null)
            {
                surfaceCameraColor = viewCamera.backgroundColor;
                viewCamera.backgroundColor = new Color32(18, 23, 22, 255);
            }

            IsInCave = true;
            SetSurfaceChunksActive(false);
            CreateCaveInterior();
            player.Teleport(CaveSpawnPosition);
        }

        public void ExitCave()
        {
            if (!IsInCave)
            {
                return;
            }

            if (caveRoot != null)
            {
                caveRoot.SetActive(false);
                Destroy(caveRoot);
                caveRoot = null;
                caveExitRenderer = null;
            }

            IsInCave = false;
            SetSurfaceChunksActive(true);
            Camera viewCamera = Camera.main;
            if (viewCamera != null)
            {
                viewCamera.backgroundColor = surfaceCameraColor;
            }

            player.Teleport(surfaceReturnPosition);
            RefreshChunks(true);
        }

        public string GetInteractionHint(Vector2 playerPosition)
        {
            if (IsInCave)
            {
                return Vector2.Distance(playerPosition, CaveExitInteractionPoint) <=
                       CaveInteractionDistance + 0.35f
                    ? "右键点击洞口离开"
                    : string.Empty;
            }

            for (int i = loadedCaveEntrances.Count - 1; i >= 0; i--)
            {
                CaveEntranceMarker entrance = loadedCaveEntrances[i];
                if (entrance == null)
                {
                    loadedCaveEntrances.RemoveAt(i);
                    continue;
                }

                if (entrance.gameObject.activeInHierarchy &&
                    Vector2.Distance(playerPosition, entrance.InteractionPoint) <=
                    CaveHintDistance)
                {
                    return "右键点击矿洞进入";
                }
            }

            return string.Empty;
        }

        private static Vector2 CaveExitInteractionPoint
        {
            get { return CaveExitBottom + Vector2.up * 0.65f; }
        }

        private void SetSurfaceChunksActive(bool active)
        {
            foreach (GeneratedWorldChunk chunk in loadedChunks.Values)
            {
                if (chunk != null)
                {
                    chunk.gameObject.SetActive(active);
                }
            }
        }

        private void CreateCaveInterior()
        {
            caveRoot = new GameObject("Cave Interior");
            caveRoot.transform.SetParent(transform, false);

            GameObject floorObject = new GameObject("Cave Floor");
            floorObject.transform.SetParent(caveRoot.transform, false);
            Mesh caveFloorMesh = BuildCaveFloorMesh();
            MeshFilter filter = floorObject.AddComponent<MeshFilter>();
            filter.sharedMesh = caveFloorMesh;
            MeshRenderer renderer = floorObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = caveMaterial;
            renderer.sortingOrder = 0;
            GeneratedWorldChunk cleanup = floorObject.AddComponent<GeneratedWorldChunk>();
            cleanup.Mesh = caveFloorMesh;

            GameObject exitObject = new GameObject("Cave Exit");
            exitObject.transform.SetParent(caveRoot.transform, false);
            exitObject.transform.localPosition = CaveExitBottom;
            caveExitRenderer = exitObject.AddComponent<SpriteRenderer>();
            caveExitRenderer.sprite = caveEntranceSprite;
            caveExitRenderer.sortingOrder =
                310 - Mathf.RoundToInt(CaveExitBottom.y * 10f);

            PopulateCaveMinerals(caveRoot.transform);
        }

        private Mesh BuildCaveFloorMesh()
        {
            int tileCount = CaveFloorWidth * CaveFloorHeight;
            Vector3[] vertices = new Vector3[tileCount * 4];
            Vector2[] uv = new Vector2[tileCount * 4];
            int[] triangles = new int[tileCount * 6];
            int tileIndex = 0;
            int halfWidth = CaveFloorWidth / 2;
            int halfHeight = CaveFloorHeight / 2;

            for (int y = 0; y < CaveFloorHeight; y++)
            {
                for (int x = 0; x < CaveFloorWidth; x++)
                {
                    WriteTile(
                        vertices,
                        uv,
                        triangles,
                        tileIndex,
                        x - halfWidth,
                        y - halfHeight,
                        5,
                        0);
                    tileIndex++;
                }
            }

            Mesh mesh = new Mesh { name = "Generated Cave Floor Mesh" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void PopulateCaveMinerals(Transform cave)
        {
            if (mineralSprites == null || mineralSprites.Length == 0)
            {
                return;
            }

            int halfWidth = CaveFloorWidth / 2;
            int halfHeight = CaveFloorHeight / 2;
            for (int y = -halfHeight + 2; y <= halfHeight - 2; y++)
            {
                for (int x = -halfWidth + 2; x <= halfWidth - 2; x++)
                {
                    if ((Mathf.Abs(x) <= 2 && y <= -11) ||
                        Hash01(x, y, Seed + 12101) >= 0.045f)
                    {
                        continue;
                    }

                    int mineralIndex = Mathf.Min(
                        mineralSprites.Length - 1,
                        Mathf.FloorToInt(
                            Hash01(x, y, Seed + 12102) * mineralSprites.Length));
                    GameObject mineral = new GameObject("Cave Mineral " + x + ", " + y);
                    mineral.transform.SetParent(cave, false);
                    mineral.transform.localPosition = new Vector3(x, y, 0f);
                    SpriteRenderer renderer = mineral.AddComponent<SpriteRenderer>();
                    renderer.sprite = mineralSprites[mineralIndex];
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
            if (caveEntranceSprite != null)
            {
                Destroy(caveEntranceSprite);
            }
            if (caveMaterial != null)
            {
                Destroy(caveMaterial);
            }
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

    public sealed class CaveEntranceMarker : MonoBehaviour
    {
        public SpriteRenderer EntranceRenderer { get; set; }

        public Vector2 InteractionPoint
        {
            get { return (Vector2)transform.position + Vector2.up * 0.65f; }
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
