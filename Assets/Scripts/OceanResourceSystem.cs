using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class OceanResourceSystem : MonoBehaviour
    {
        [Serializable]
        public sealed class ItemSaveData
        {
            public string id;
            public int count;
        }

        private sealed class ResourceDefinition
        {
            public string id;
            public int column;
            public int row;
        }

        private static readonly ResourceDefinition[] Definitions =
        {
            new ResourceDefinition { id = "wood", column = 1, row = 1 },
            new ResourceDefinition { id = "plastic_bottle", column = 2, row = 1 },
            new ResourceDefinition { id = "barrel", column = 3, row = 1 },
            new ResourceDefinition { id = "leaf", column = 0, row = 2 },
            new ResourceDefinition { id = "rope", column = 2, row = 2 }
        };

        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private readonly List<OceanResource> activeResources = new List<OceanResource>();
        private Transform raft;
        private Texture2D atlas;
        private Bounds bounds;
        private float nextSpawnTime;
        private int maximumResources = 18;

        public IReadOnlyList<OceanResource> ActiveResources { get { return activeResources; } }

        public void Initialize(Transform controlledRaft, Texture2D resourceAtlas, Bounds oceanBounds)
        {
            raft = controlledRaft;
            atlas = resourceAtlas;
            bounds = oceanBounds;
            foreach (ResourceDefinition definition in Definitions)
            {
                counts[definition.id] = 0;
            }

            for (int i = 0; i < 8; i++)
            {
                SpawnResource();
            }
        }

        public int GetCount(string itemId)
        {
            return counts.TryGetValue(itemId, out int count) ? count : 0;
        }

        public void AddResource(string itemId)
        {
            if (!counts.ContainsKey(itemId))
            {
                counts[itemId] = 0;
            }

            counts[itemId]++;
        }

        public List<ItemSaveData> CaptureItems()
        {
            List<ItemSaveData> savedItems = new List<ItemSaveData>();
            foreach (ResourceDefinition definition in Definitions)
            {
                savedItems.Add(new ItemSaveData
                {
                    id = definition.id,
                    count = GetCount(definition.id)
                });
            }

            return savedItems;
        }

        public void RestoreItems(List<ItemSaveData> savedItems)
        {
            if (savedItems == null)
            {
                return;
            }

            foreach (ItemSaveData item in savedItems)
            {
                if (item != null && !string.IsNullOrEmpty(item.id))
                {
                    counts[item.id] = Mathf.Max(0, item.count);
                }
            }
        }

        public void Collect(OceanResource resource)
        {
            if (resource == null || !activeResources.Remove(resource))
            {
                return;
            }

            AddResource(resource.ItemId);
            Destroy(resource.gameObject);
        }

        private void Update()
        {
            if (Time.time < nextSpawnTime || activeResources.Count >= maximumResources)
            {
                return;
            }

            nextSpawnTime = Time.time + 0.7f;
            SpawnResource();
        }

        private void SpawnResource()
        {
            if (raft == null || atlas == null || activeResources.Count >= maximumResources)
            {
                return;
            }

            ResourceDefinition definition = Definitions[UnityEngine.Random.Range(0, Definitions.Length)];
            Vector2 position = Vector2.zero;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                position = new Vector2(
                    UnityEngine.Random.Range(bounds.min.x + 1.5f, bounds.max.x - 1.5f),
                    UnityEngine.Random.Range(bounds.min.y + 1.5f, bounds.max.y - 1.5f));
                if (Vector2.Distance(position, raft.position) > 2.6f)
                {
                    break;
                }
            }

            GameObject resourceObject = new GameObject("Ocean Resource " + definition.id);
            resourceObject.transform.position = position;
            SpriteRenderer renderer = resourceObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateAtlasSprite(atlas, definition.column, definition.row, definition.id);
            renderer.sortingOrder = 2;
            CircleCollider2D collider = resourceObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.28f;
            OceanResource resource = resourceObject.AddComponent<OceanResource>();
            resource.Initialize(definition.id, this);
            activeResources.Add(resource);
        }

        private static Sprite CreateAtlasSprite(Texture2D source, int column, int row, string name)
        {
            Sprite sprite = Sprite.Create(
                source,
                new Rect(column * 16f, source.height - (row + 1) * 16f, 16f, 16f),
                new Vector2(0.5f, 0.5f),
                16f);
            sprite.name = name;
            return sprite;
        }
    }

    public sealed class OceanResource : MonoBehaviour
    {
        public string ItemId { get; private set; }
        private OceanResourceSystem owner;
        private float phase;
        private Vector3 startPosition;

        public void Initialize(string itemId, OceanResourceSystem resourceOwner)
        {
            ItemId = itemId;
            owner = resourceOwner;
            phase = UnityEngine.Random.value * Mathf.PI * 2f;
            startPosition = transform.position;
        }

        private void Update()
        {
            transform.position = startPosition +
                Vector3.up * (Mathf.Sin(Time.time * 1.7f + phase) * 0.02f);
        }

        private void OnMouseDown()
        {
            // The hook performs collection; direct clicks are intentionally ignored.
        }

        public void Collect()
        {
            if (owner != null)
            {
                owner.Collect(this);
            }
        }
    }
}
