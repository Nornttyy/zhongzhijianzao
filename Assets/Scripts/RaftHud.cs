using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class RaftHud : MonoBehaviour
    {
        private RaftController raft;
        private OceanResourceSystem resources;
        private Font pixelFont;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        public void Initialize(RaftController controlledRaft, OceanResourceSystem resourceSystem, Font interfaceFont)
        {
            raft = controlledRaft;
            resources = resourceSystem;
            pixelFont = interfaceFont;
            if (pixelFont != null)
            {
                pixelFont.RequestCharactersInTexture(
                    "海上生存WASD移动鼠标瞄准左键钩子木材塑料瓶木桶树叶绳卷数量",
                    20,
                    FontStyle.Normal);
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
            GUI.Label(new Rect(24f, 52f, 520f, 24f), "WASD 移动木筏 · 鼠标瞄准 · 左键发射钩子", bodyStyle);

            string inventory = "木材 " + resources.GetCount("wood") +
                "   塑料瓶 " + resources.GetCount("plastic_bottle") +
                "   木桶 " + resources.GetCount("barrel") +
                "   树叶 " + resources.GetCount("leaf") +
                "   绳卷 " + resources.GetCount("rope");
            GUI.Label(new Rect(24f, 84f, 760f, 26f), inventory, bodyStyle);
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
        }
    }
}
