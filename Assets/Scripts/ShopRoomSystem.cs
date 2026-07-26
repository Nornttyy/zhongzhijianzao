using UnityEngine;

namespace DoNotOpen.Prototype
{
    /// <summary>
    /// Small, fixed room used by the shop-first game loop. The old procedural
    /// world is deliberately not created here: this room is the only playable
    /// space while the shop systems are being rebuilt.
    /// </summary>
    public sealed class ShopRoomSystem : MonoBehaviour
    {
        private const float PixelsPerUnit = 12f;
        private SpriteRenderer roomRenderer;

        public Bounds RoomBounds { get; private set; }

        public void Initialize(Texture2D roomTexture)
        {
            if (roomTexture == null)
            {
                return;
            }

            GameObject roomObject = new GameObject("Shop Interior");
            roomObject.transform.SetParent(transform, false);
            roomRenderer = roomObject.AddComponent<SpriteRenderer>();
            roomRenderer.sprite = Sprite.Create(
                roomTexture,
                new Rect(0f, 0f, roomTexture.width, roomTexture.height),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit);
            roomRenderer.sprite.name = "Shop Interior Sprite";
            roomRenderer.sortingOrder = 0;

            Vector2 roomSize = new Vector2(
                roomTexture.width / PixelsPerUnit,
                roomTexture.height / PixelsPerUnit);
            RoomBounds = new Bounds(Vector3.zero, new Vector3(roomSize.x, roomSize.y, 1f));
        }

        private void OnDestroy()
        {
            if (roomRenderer != null && roomRenderer.sprite != null)
            {
                Destroy(roomRenderer.sprite);
            }
        }
    }
}
