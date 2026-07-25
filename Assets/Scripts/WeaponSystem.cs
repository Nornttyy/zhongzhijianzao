using UnityEngine;

namespace DoNotOpen.Prototype
{
    /// <summary>
    /// Handles the first weapon in the game. The wooden sword has a short
    /// right-click swing now; its hit window is kept in one place so enemies
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
        private Texture2D swordTexture;
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
        }

        public void SelectHotbarItem(string itemId)
        {
            selectedWeaponId = itemId == "wood_sword" ? itemId : string.Empty;
        }

        private void Update()
        {
            if (player == null || world == null || player.IsInputLocked || world.IsInCave)
            {
                return;
            }

            if (selectedWeaponId == "wood_sword" && Input.GetMouseButtonDown(1))
            {
                TryAttack();
            }
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
