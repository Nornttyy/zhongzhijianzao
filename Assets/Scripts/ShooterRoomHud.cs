using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class ShooterRoomHud : MonoBehaviour
    {
        private ShooterPlayerController player;
        private Font pixelFont;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        public void Initialize(ShooterPlayerController controlledPlayer)
        {
            player = controlledPlayer;
            pixelFont = Resources.Load<Font>("Fonts/ark-pixel-12px");
            if (pixelFont != null)
            {
                pixelFont.RequestCharactersInTexture("多人枪战WASD移动灰色房间", 22, FontStyle.Normal);
            }
        }

        private void OnGUI()
        {
            if (player == null)
            {
                return;
            }

            EnsureStyles();
            GUI.Label(new Rect(24f, 18f, 300f, 32f), "多人枪战", titleStyle);
            GUI.Label(new Rect(24f, 52f, 420f, 28f), "WASD 移动 · 灰色房间原型", bodyStyle);
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
                fontSize = 26,
                normal = { textColor = new Color(0.96f, 0.96f, 0.98f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = pixelFont,
                fontSize = 16,
                normal = { textColor = new Color(0.86f, 0.87f, 0.90f) }
            };
        }
    }
}
