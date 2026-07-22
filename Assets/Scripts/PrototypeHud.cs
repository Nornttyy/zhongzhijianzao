using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;

        private void OnGUI()
        {
            EnsureStyles();

            GUI.Box(new Rect(18f, 18f, 330f, 92f), GUIContent.none);
            GUI.Label(new Rect(34f, 29f, 290f, 26f), "DO NOT OPEN — PIXEL PROTOTYPE", titleStyle);
            GUI.Label(new Rect(34f, 57f, 290f, 22f), "WASD / Arrow Keys   Move", bodyStyle);
            GUI.Label(new Rect(34f, 79f, 290f, 22f), "R   Reset position", bodyStyle);

            string hint = "The apartment is quiet. Walk around and look closely.";
            Vector2 size = bodyStyle.CalcSize(new GUIContent(hint));
            GUI.Box(new Rect((Screen.width - size.x) * 0.5f - 12f, Screen.height - 55f, size.x + 24f, 36f),
                GUIContent.none);
            GUI.Label(new Rect((Screen.width - size.x) * 0.5f, Screen.height - 48f, size.x, 24f), hint, bodyStyle);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.93f, 0.91f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.72f, 0.8f, 0.78f) }
            };
        }
    }
}
