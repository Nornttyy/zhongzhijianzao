using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private ProceduralWorld world;
        private TopDownPlayer player;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle coordinateStyle;
        private GUIStyle interactionStyle;

        public void Initialize(ProceduralWorld generatedWorld, TopDownPlayer controlledPlayer)
        {
            world = generatedWorld;
            player = controlledPlayer;
        }

        private void OnGUI()
        {
            if (world == null || player == null)
            {
                return;
            }

            EnsureStyles();

            GUI.Box(new Rect(18f, 18f, 282f, 102f), GUIContent.none);
            GUI.Label(new Rect(34f, 28f, 245f, 25f), "ZHONG ZHI JIAN ZAO", titleStyle);
            GUI.Label(new Rect(34f, 54f, 245f, 20f), "100,000 x 100,000  |  Seed " + world.Seed, bodyStyle);
            GUI.Label(new Rect(34f, 76f, 245f, 20f), "WASD / Arrows   Move", bodyStyle);
            GUI.Label(new Rect(34f, 96f, 245f, 20f), "R   Return to start", bodyStyle);

            Vector2Int tile = world.WorldToTile(player.transform.position);
            string coordinates = world.IsInCave
                ? "CAVE"
                : "X " + tile.x.ToString("N0") + "    Y " + tile.y.ToString("N0");
            Vector2 size = coordinateStyle.CalcSize(new GUIContent(coordinates));
            Rect panel = new Rect((Screen.width - size.x) * 0.5f - 14f, Screen.height - 56f, size.x + 28f, 38f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 7f, size.x, 24f), coordinates, coordinateStyle);

            string interaction = world.GetInteractionHint(player.transform.position);
            if (!string.IsNullOrEmpty(interaction))
            {
                Vector2 hintSize = interactionStyle.CalcSize(new GUIContent(interaction));
                float hintPanelWidth = Mathf.Max(hintSize.x + 40f, 360f);
                Rect hintPanel = new Rect(
                    (Screen.width - hintPanelWidth) * 0.5f,
                    Screen.height - 102f,
                    hintPanelWidth,
                    34f);
                GUI.Box(hintPanel, GUIContent.none);
                GUI.Label(
                    new Rect(
                        hintPanel.x + 14f,
                        hintPanel.y + 5f,
                        hintPanel.width - 28f,
                        24f),
                    interaction,
                    interactionStyle);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.94f, 0.90f, 0.70f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.86f, 0.91f, 0.80f) }
            };

            coordinateStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            interactionStyle = new GUIStyle(coordinateStyle)
            {
                fontSize = 13,
                wordWrap = false,
                normal = { textColor = new Color(0.98f, 0.89f, 0.48f) }
            };
        }
    }
}
