using UnityEngine;

namespace DoNotOpen.Prototype
{
    /// <summary>
    /// Handles the first weapon in the game. The wooden sword has a short
    /// left-click swing now; its hit window is kept in one place so enemies
    /// can receive damage when they are added later.
    /// </summary>
    public sealed class WeaponSystem : MonoBehaviour
    {
        private const float AttackCooldown = 0.34f;
        private const float AttackDuration = 0.18f;
        private const float AttackDistance = 0.78f;

        private TopDownPlayer player;
        private ProceduralWorld world;
        private ShopSystem shop;
        private Texture2D itemSheet;
        private Sprite swordSprite;
        private SpriteRenderer heldToolRenderer;
        private Sprite heldToolSprite;
        private Texture2D swordTexture;
        private string selectedItemId = string.Empty;
        private string selectedWeaponId = string.Empty;
        private float nextAttackTime;

        public void Initialize(
            TopDownPlayer controlledPlayer,
            ProceduralWorld generatedWorld,
            ShopSystem itemShop,
            Texture2D itemTexture)
        {
            player = controlledPlayer;
            world = generatedWorld;
            shop = itemShop;
            itemSheet = itemTexture;
            swordSprite = CreateSwordSprite(itemSheet);
            CreateHeldToolRenderer();
        }

        public void SelectHotbarItem(string itemId)
        {
            selectedItemId = itemId ?? string.Empty;
            selectedWeaponId = itemId == "wood_sword" ? itemId : string.Empty;
            heldToolSprite = CreateHeldToolSprite(selectedItemId);
            if (heldToolRenderer != null)
            {
                heldToolRenderer.sprite = heldToolSprite;
                heldToolRenderer.enabled = heldToolSprite != null;
            }
        }

        private void Update()
        {
            UpdateHeldTool();
            if (player == null || world == null || player.IsInputLocked || world.IsInCave)
            {
                return;
            }

            if (selectedWeaponId == "wood_sword" && Input.GetMouseButtonDown(0))
            {
                TryAttack();
            }
        }

        private void LateUpdate()
        {
            UpdateHeldTool();
        }

        private void CreateHeldToolRenderer()
        {
            if (player == null || player.PlayerSprite == null)
            {
                return;
            }

            GameObject toolObject = new GameObject("Held Tool");
            // 挂在角色视觉层上，能跟随玩家的跳跃和轻微抖动，而不是只跟随碰撞体。
            Transform visualParent = player.VisualRoot != null
                ? player.VisualRoot
                : player.transform;
            toolObject.transform.SetParent(visualParent, false);
            heldToolRenderer = toolObject.AddComponent<SpriteRenderer>();
            heldToolRenderer.sortingLayerID = player.PlayerSprite.sortingLayerID;
            heldToolRenderer.enabled = false;
        }

        private void UpdateHeldTool()
        {
            if (heldToolRenderer == null || player == null || world == null)
            {
                return;
            }

            bool visible = heldToolSprite != null &&
                           !player.IsInputLocked &&
                           !world.IsInCave;
            heldToolRenderer.enabled = visible;
            if (!visible)
            {
                return;
            }

            Vector2 direction = player.Facing.sqrMagnitude > 0.01f
                ? player.Facing.normalized
                : Vector2.down;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Vector2 offset = direction * 0.56f + side * 0.10f;
            // 素材保持竖直，只改变位置；父级视觉层会带来玩家移动时的弹跳和轻微抖动。
            heldToolRenderer.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            heldToolRenderer.transform.localRotation = Quaternion.identity;
            heldToolRenderer.sortingOrder =
                ProceduralWorld.GetSurfaceSortingOrder(player.transform.position.y) + 12;
        }

        private Sprite CreateHeldToolSprite(string itemId)
        {
            if (itemSheet == null)
            {
                return null;
            }

            switch (itemId)
            {
                case "watering_can":
                    return CreateAtlasSprite(itemSheet, 0, 4, "Held Watering Can");
                case "hoe":
                    return CreateAtlasSprite(itemSheet, 0, 5, "Held Hoe");
                case "wood_sword":
                    return CreateAtlasSprite(itemSheet, 3, 0, "Held Wooden Sword");
                case "stone_sword":
                    return CreateAtlasSprite(itemSheet, 3, 1, "Held Stone Sword");
                case "wood_pickaxe":
                    return CreateAtlasSprite(itemSheet, 3, 5, "Held Wooden Pickaxe");
                case "stone_pickaxe":
                    return CreateAtlasSprite(itemSheet, 3, 6, "Held Stone Pickaxe");
                default:
                    return null;
            }
        }

        private static Sprite CreateAtlasSprite(
            Texture2D texture,
            int row,
            int column,
            string spriteName)
        {
            if (texture == null || texture.width < (column + 1) * 12 ||
                texture.height < (row + 1) * 12)
            {
                return null;
            }

            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            float y = texture.height - (row + 1) * 12f;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(column * 12f, y, 12f, 12f),
                new Vector2(0.5f, 0.10f),
                12f);
            sprite.name = spriteName;
            return sprite;
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime)
            {
                return;
            }

            nextAttackTime = Time.time + AttackCooldown;
            CreateSwing();
            if (shop != null)
            {
                shop.ShowFarmingFeedback("木剑挥砍");
            }
        }

        private void CreateSwing()
        {
            GameObject swing = new GameObject("Wood Sword Swing");
            SpriteRenderer renderer = swing.AddComponent<SpriteRenderer>();
            renderer.sprite = swordSprite;
            renderer.sortingOrder = ProceduralWorld.GetSurfaceSortingOrder(
                player.transform.position.y) + 12;

            StartCoroutine(AnimateSwing(swing, renderer));
        }

        private System.Collections.IEnumerator AnimateSwing(
            GameObject swing,
            SpriteRenderer renderer)
        {
            Vector2 direction = player.Facing.sqrMagnitude > 0.01f
                ? player.Facing.normalized
                : Vector2.down;
            float directionAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float startAngle = directionAngle - 70f;
            float endAngle = directionAngle + 70f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float elapsed = 0f;

            while (elapsed < AttackDuration && swing != null)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / AttackDuration);
                float arc = Mathf.SmoothStep(0f, 1f, progress);
                Vector2 offset = direction *
                    (AttackDistance + Mathf.Sin(progress * Mathf.PI) * 0.16f) +
                    perpendicular * Mathf.Lerp(-0.3f, 0.3f, arc);
                swing.transform.position = player.transform.position +
                    new Vector3(offset.x, offset.y, 0f);
                swing.transform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(startAngle, endAngle, arc));
                Color color = renderer.color;
                color.a = 1f - progress * 0.2f;
                renderer.color = color;
                yield return null;
            }

            if (swing != null)
            {
                Destroy(swing);
            }
        }

        private Sprite CreateSwordSprite(Texture2D texture)
        {
            if (texture != null && texture.width >= 12 && texture.height >= 48)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                Sprite drawnSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 12f, 12f),
                    new Vector2(0.5f, 0.12f),
                    12f);
                drawnSprite.name = "Wooden Sword Pixel Sprite";
                return drawnSprite;
            }

            swordTexture = new Texture2D(12, 12, TextureFormat.RGBA32, false)
            {
                name = "Generated Wooden Sword Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color outline = new Color32(55, 38, 30, 255);
            Color blade = new Color32(219, 224, 190, 255);
            Color bladeLight = new Color32(255, 246, 194, 255);
            Color wood = new Color32(156, 88, 48, 255);

            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    swordTexture.SetPixel(x, y, clear);
                }
            }

            Fill(swordTexture, 5, 3, 6, 9, outline);
            Fill(swordTexture, 5, 4, 5, 9, bladeLight);
            Fill(swordTexture, 6, 4, 6, 8, blade);
            swordTexture.SetPixel(5, 10, outline);
            swordTexture.SetPixel(6, 10, bladeLight);
            Fill(swordTexture, 3, 2, 8, 3, outline);
            Fill(swordTexture, 4, 2, 7, 2, bladeLight);
            Fill(swordTexture, 5, 0, 6, 1, outline);
            Fill(swordTexture, 5, 1, 6, 1, wood);
            swordTexture.SetPixel(5, 0, wood);
            swordTexture.Apply();

            Sprite sprite = Sprite.Create(
                swordTexture,
                new Rect(0f, 0f, 12f, 12f),
                new Vector2(0.5f, 0.12f),
                12f);
            sprite.name = "Generated Wooden Sword Sprite";
            return sprite;
        }

        private static void Fill(
            Texture2D texture,
            int minX,
            int minY,
            int maxX,
            int maxY,
            Color color)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private void OnDestroy()
        {
            if (swordSprite != null)
            {
                Destroy(swordSprite);
            }

            if (swordTexture != null)
            {
                Destroy(swordTexture);
            }
        }
    }
}
