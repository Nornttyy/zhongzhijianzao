using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class RaftHud : MonoBehaviour
    {
        private RaftController raft;
        private OceanResourceSystem resources;
        private Font pixelFont;
        private Texture2D atlas;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle panelStyle;
        private GUIStyle slotStyle;
        private GUIStyle slotCountStyle;
        private bool inventoryOpen;

        public void Initialize(
            RaftController controlledRaft,
            OceanResourceSystem resourceSystem,
            Font interfaceFont,
            Texture2D materialAtlas)
        {
            raft = controlledRaft;
            resources = resourceSystem;
            pixelFont = interfaceFont;
            atlas = materialAtlas;
            if (pixelFont != null)
            {
                pixelFont.RequestCharactersInTexture(
                    "海上生存WASD移动鼠标瞄准左键钩子木材塑料瓶木桶树叶绳卷数量背包关闭按E",
                    20,
                    FontStyle.Normal);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleInventory();
            }
        }

        private void ToggleInventory()
        {
            inventoryOpen = !inventoryOpen;
            if (raft != null && raft.Player != null)
            {
                raft.Player.SetInputLocked(inventoryOpen ? "true" : "false");
            }
        }

        private void OnGUI()
        {
            if (raft == null || resources == null)
            {
                return;
            }

            EnsureStyles();
            GUI.color = Color.white;
            GUI.Label(new Rect(24f, 20f, 240f, 30f), "海上生存", titleStyle);
            GUI.Label(new Rect(24f, 52f, 760f, 24f), "WASD 移动玩家 · 木筏随海流漂移 · 左键蓄力钩子 · E 背包", bodyStyle);

            string inventory = "木材 " + resources.GetCount("wood") +
                "   塑料瓶 " + resources.GetCount("plastic_bottle") +
                "   木桶 " + resources.GetCount("barrel") +
                "   树叶 " + resources.GetCount("leaf") +
                "   绳卷 " + resources.GetCount("rope");
            GUI.Label(new Rect(24f, 84f, 760f, 26f), inventory, bodyStyle);

            if (inventoryOpen)
            {
                DrawInventory();
            }
        }

        private void EnsureStyles()
        {
            if (bodyStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = pixelFont,
                fontSize = 24,
                normal = { textColor = new Color(0.88f, 0.96f, 0.90f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = pixelFont,
                fontSize = 15,
                normal = { textColor = new Color(0.90f, 0.96f, 0.92f) }
            };
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                font = pixelFont,
                fontSize = 18,
                alignment = TextAnchor.UpperCenter,
                normal =
                {
                    textColor = new Color(0.94f, 0.98f, 1f),
                    background = MakeSolidTexture(new Color(0.035f, 0.12f, 0.18f, 0.96f))
                }
            };
            slotStyle = new GUIStyle(GUI.skin.box)
            {
                font = pixelFont,
                fontSize = 12,
                alignment = TextAnchor.LowerCenter,
                normal =
                {
                    textColor = new Color(0.90f, 0.96f, 0.98f),
                    background = MakeSolidTexture(new Color(0.10f, 0.27f, 0.34f, 0.98f))
                }
            };
            slotCountStyle = new GUIStyle(GUI.skin.label)
            {
                font = pixelFont,
                fontSize = 13,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = Color.white }
            };
        }

        private void DrawInventory()
        {
            float panelWidth = Mathf.Min(560f, Screen.width - 48f);
            float panelHeight = 300f;
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);
            GUI.Box(panel, "背包", panelStyle);
            GUI.Label(
                new Rect(panel.x, panel.y + 40f, panel.width, 24f),
                "E 关闭 · 物品会按顺序收进物品栏",
                bodyStyle);

            string[] ids = { "wood", "plastic_bottle", "barrel", "leaf", "rope" };
            string[] names = { "木材", "塑料瓶", "木桶", "树叶", "绳卷" };
            int[] columns = { 1, 2, 3, 0, 2 };
            int[] rows = { 1, 1, 1, 2, 2 };
            const int columnsPerRow = 5;
            const float slotSize = 76f;
            float startX = panel.x + (panel.width - columnsPerRow * slotSize) * 0.5f;
            float startY = panel.y + 82f;
            for (int i = 0; i < ids.Length; i++)
            {
                float x = startX + (i % columnsPerRow) * slotSize;
                float y = startY + (i / columnsPerRow) * 84f;
                Rect slot = new Rect(x, y, slotSize - 6f, slotSize - 6f);
                GUI.Box(slot, names[i], slotStyle);
                DrawIcon(new Rect(slot.x + 20f, slot.y + 8f, 34f, 34f), columns[i], rows[i]);
                GUI.Label(
                    new Rect(slot.x + 3f, slot.y + slot.height - 23f, slot.width - 7f, 19f),
                    GetCountText(ids[i]),
                    slotCountStyle);
            }

            GUI.Label(
                new Rect(panel.x + 16f, panel.y + panel.height - 32f, panel.width - 32f, 22f),
                "物品栏：木材、塑料瓶、木桶、树叶、绳卷",
                bodyStyle);
        }

        private string GetCountText(string itemId)
        {
            return "×" + resources.GetCount(itemId);
        }

        private void DrawIcon(Rect target, int column, int row)
        {
            if (atlas == null)
            {
                return;
            }

            float width = atlas.width;
            float height = atlas.height;
            Rect uv = new Rect(
                column * 16f / width,
                (height - (row + 1) * 16f) / height,
                16f / width,
                16f / height);
            GUI.DrawTextureWithTexCoords(target, atlas, uv, true);
        }

        private static Texture2D MakeSolidTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
