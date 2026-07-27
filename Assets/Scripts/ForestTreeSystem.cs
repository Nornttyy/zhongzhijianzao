using System.Collections.Generic;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DoNotOpen.Prototype
{
    public sealed class ForestTreeSystem : MonoBehaviour
    {
        private const int TreeCount = 34;
        private const float SpawnRadius = 22f;
        private const float PlayerTreeDistance = 1.45f;
        private const float TrunkHalfWidth = 0.32f;
        private const float TrunkHalfHeight = 0.26f;
        private const float PixelPerUnit = 12f;

        private readonly List<ForestTree> trees = new List<ForestTree>();
        private ProceduralWorld world;
        private TopDownPlayer player;
        private Sprite treeSprite;
        private Texture2D treeTexture;
        private int woodCount;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void NotifyShopItem(string itemId, int count);
#endif

        public int WoodCount { get { return woodCount; } }

        public void SetWoodCount(int count)
        {
            woodCount = Mathf.Max(0, count);
            NotifyWoodCount();
        }

        public void Initialize(
            ProceduralWorld generatedWorld,
            TopDownPlayer controlledPlayer,
            Texture2D texture)
        {
            world = generatedWorld;
            player = controlledPlayer;
            treeTexture = texture;
            if (treeTexture == null || world == null)
            {
                return;
            }

            treeTexture.filterMode = FilterMode.Point;
            treeTexture.wrapMode = TextureWrapMode.Clamp;
            treeSprite = Sprite.Create(
                treeTexture,
                new Rect(0f, 0f, treeTexture.width, treeTexture.height),
                new Vector2(0.5f, 0f),
                PixelPerUnit);
            treeSprite.name = "Forest Tree";
            SpawnTrees();
        }

        public bool CanStandAt(Vector2 position, float radius)
        {
            for (int i = trees.Count - 1; i >= 0; i--)
            {
                ForestTree tree = trees[i];
                if (tree == null)
                {
                    trees.RemoveAt(i);
                    continue;
                }

                Vector2 trunkCenter = tree.Position + Vector2.up * 0.25f;
                if (Mathf.Abs(position.x - trunkCenter.x) <= TrunkHalfWidth + radius &&
                    Mathf.Abs(position.y - trunkCenter.y) <= TrunkHalfHeight + radius)
                {
                    return false;
                }
            }

            return true;
        }

        public string GetInteractionHint(Vector2 playerPosition)
        {
            return FindNearbyTree(playerPosition) != null
                ? "左键砍树，获得木材"
                : string.Empty;
        }

        public void TryChop(ForestTree tree)
        {
            if (tree == null || player == null || player.IsInputLocked ||
                Vector2.Distance(player.transform.position, tree.Position) > PlayerTreeDistance)
            {
                return;
            }

            woodCount += 3;
            NotifyWoodCount();
            trees.Remove(tree);
            Destroy(tree.gameObject);
        }

        private void Update()
        {
            if (player == null || player.IsInputLocked || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            ForestTree nearby = FindNearbyTree(player.transform.position);
            if (nearby != null)
            {
                TryChop(nearby);
            }
        }

        private ForestTree FindNearbyTree(Vector2 position)
        {
            ForestTree closest = null;
            float closestDistance = PlayerTreeDistance;
            for (int i = trees.Count - 1; i >= 0; i--)
            {
                ForestTree tree = trees[i];
                if (tree == null)
                {
                    trees.RemoveAt(i);
                    continue;
                }

                float distance = Vector2.Distance(position, tree.Position);
                if (distance <= closestDistance)
                {
                    closest = tree;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private void SpawnTrees()
        {
            Random.State previousState = Random.state;
            Random.InitState(world.Seed + 77231);
            Vector2[] starterPositions =
            {
                new Vector2(4f, 2f),
                new Vector2(-4f, 2f),
                new Vector2(5f, -3f),
                new Vector2(-5f, -3f)
            };
            foreach (Vector2 starterPosition in starterPositions)
            {
                if (trees.Count >= TreeCount)
                {
                    break;
                }

                TrySpawnTree(starterPosition);
            }

            int attempts = 0;
            while (trees.Count < TreeCount && attempts++ < TreeCount * 18)
            {
                Vector2 position = new Vector2(
                    Random.Range(-SpawnRadius, SpawnRadius),
                    Random.Range(-SpawnRadius, SpawnRadius));
                TrySpawnTree(position);
            }

            Random.state = previousState;
        }

        private bool TrySpawnTree(Vector2 position)
        {
            Vector2Int tile = world.WorldToTile(position);
            if (world.GetGround(tile.x, tile.y) != ProceduralWorld.GroundType.Grass ||
                position.magnitude < 3.5f ||
                HasNearbyTree(position, 1.8f))
            {
                return false;
            }

            GameObject treeObject = new GameObject("Forest Tree");
            treeObject.transform.SetParent(transform, false);
            treeObject.transform.position = new Vector3(position.x, position.y, 0f);
            SpriteRenderer renderer = treeObject.AddComponent<SpriteRenderer>();
            renderer.sprite = treeSprite;
            renderer.sortingOrder = ProceduralWorld.GetSurfaceSortingOrder(position.y) + 2;

            CircleCollider2D collider = treeObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.34f;
            collider.offset = new Vector2(0f, 0.25f);

            ForestTree tree = treeObject.AddComponent<ForestTree>();
            tree.Initialize(position, this);
            trees.Add(tree);
            return true;
        }

        private bool HasNearbyTree(Vector2 position, float distance)
        {
            float distanceSquared = distance * distance;
            foreach (ForestTree tree in trees)
            {
                if (tree != null && (tree.Position - position).sqrMagnitude < distanceSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDestroy()
        {
            if (treeSprite != null)
            {
                Destroy(treeSprite);
            }
        }

        private void NotifyWoodCount()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NotifyShopItem("wood", woodCount);
#endif
        }
    }

    public sealed class ForestTree : MonoBehaviour
    {
        public Vector2 Position { get; private set; }
        private ForestTreeSystem owner;

        public void Initialize(Vector2 position, ForestTreeSystem treeOwner)
        {
            Position = position;
            owner = treeOwner;
        }

        private void OnMouseDown()
        {
            if (owner != null)
            {
                owner.TryChop(this);
            }
        }
    }
}
