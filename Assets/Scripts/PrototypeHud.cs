using UnityEngine;

namespace DoNotOpen.Prototype
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        private ProceduralWorld world;
        private TopDownPlayer player;
        private Font pixelFont;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle coinStyle;
        private GUIStyle coordinateStyle;
        private GUIStyle interactionStyle;
        private GUIStyle menuTitleStyle;
        private GUIStyle menuSubtitleStyle;
        private GUIStyle menuDescriptionStyle;
        private GUIStyle menuButtonStyle;
        private GUIStyle menuControlsStyle;
        private Texture2D menuCardTexture;
        private Texture2D menuButtonTexture;
        private bool showMainMenu;

        public void Initialize(
            ProceduralWorld generatedWorld,
            TopDownPlayer controlledPlayer,
            Font interfaceFont)
        {
            world = generatedWorld;
            player = controlledPlayer;
            pixelFont = interfaceFont;
            if (pixelFont != null)
            {
                const string hudCharacters = "种植建造金币WASD移动R返回坐标0123456789:，";
                pixelFont.RequestCharactersInTexture(hudCharacters, 24, FontStyle.Normal);
            }

            // 网页入口已经有主界面，避免网页菜单和 Unity 菜单重叠；独立运行时显示 Unity 菜单。
            showMainMenu = Application.platform != RuntimePlatform.WebGLPlayer;
            if (showMainMenu && player != null)
            {
                player.enabled = false;
            }
        }

        private void OnGUI()
        {
            if (world == null || player == null)
            {
                return;
            }

            // Reset IMGUI state so labels cannot inherit a transparent color
            // from a previous panel or the web loading overlay.
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            EnsureStyles();

            if (showMainMenu)
            {
                DrawMainMenu();
                return;
            }

            GUI.Box(new Rect(18f, 18f, 240f, 62f), GUIContent.none);
            GUI.Label(new Rect(32f, 27f, 212f, 25f), "种植建造", titleStyle);
            GUI.Label(new Rect(32f, 55f, 212f, 20f), "WASD 移动 · R 返回", bodyStyle);

            const float coinPanelWidth = 160f;
            Rect coinPanel = new Rect(Screen.width - coinPanelWidth - 18f, 18f, coinPanelWidth, 38f);
            GUI.color = new Color(0.08f, 0.14f, 0.09f, 0.96f);
            GUI.DrawTexture(coinPanel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(
                new Rect(coinPanel.x + 10f, coinPanel.y + 7f, coinPanel.width - 20f, 24f),
                "金币: " + player.Coins.ToString("N0"),
                coinStyle);

            Vector2Int tile = world.WorldToTile(player.transform.position);
            string coordinates = world.IsInCave
                ? "矿洞"
                : "坐标 " + tile.x.ToString("N0") + "，" + tile.y.ToString("N0");
            Vector2 size = coordinateStyle.CalcSize(new GUIContent(coordinates));
            Rect panel = new Rect((Screen.width - size.x) * 0.5f - 14f, Screen.height - 56f, size.x + 28f, 38f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 7f, size.x, 24f), coordinates, coordinateStyle);

            string interaction = world.GetInteractionHint(player.transform.position);
            if (!string.IsNullOrEmpty(interaction))
            {
                Vector2 hintSize = interactionStyle.CalcSize(new GUIContent(interaction));
                float hintPanelWidth = Mathf.Max(hintSize.x + 40f, 260f);
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
                font = pixelFont,
                fontSize = 24,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.94f, 0.90f, 0.70f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = pixelFont,
                fontSize = 16,
                normal = { textColor = new Color(0.86f, 0.91f, 0.80f) }
            };

            coinStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.88f, 0.40f) }
            };

            coordinateStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };

            interactionStyle = new GUIStyle(coordinateStyle)
            {
                fontSize = 16,
                wordWrap = false,
                normal = { textColor = new Color(0.98f, 0.89f, 0.48f) }
            };

            menuCardTexture = CreateColorTexture(new Color(0.10f, 0.17f, 0.11f, 0.97f));
            menuButtonTexture = CreateColorTexture(new Color(0.95f, 0.83f, 0.44f, 1f));
            menuTitleStyle = new GUIStyle(titleStyle)
            {
                fontSize = 42,
                alignment = TextAnchor.MiddleCenter
            };
            menuSubtitleStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.91f, 0.76f) }
            };
            menuDescriptionStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.80f, 0.87f, 0.73f) }
            };
            menuButtonStyle = new GUIStyle(GUI.skin.button)
            {
                font = pixelFont,
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = menuButtonTexture, textColor = new Color(0.12f, 0.20f, 0.12f) },
                hover = { background = menuButtonTexture, textColor = Color.white },
                active = { background = menuButtonTexture, textColor = Color.white }
            };
            menuControlsStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.73f, 0.82f, 0.66f) }
            };
        }

        private void DrawMainMenu()
        {
            float width = Mathf.Max(320f, Screen.width);
            float height = Mathf.Max(240f, Screen.height);
            GUI.color = new Color(0.08f, 0.15f, 0.09f, 0.92f);
            GUI.DrawTexture(new Rect(0f, 0f, width, height), Texture2D.whiteTexture);

            float cardWidth = Mathf.Min(520f, width - 36f);
            float cardHeight = Mathf.Min(350f, height - 36f);
            Rect card = new Rect(
                (width - cardWidth) * 0.5f,
                (height - cardHeight) * 0.5f,
                cardWidth,
                cardHeight);
            GUI.color = new Color(0.64f, 0.82f, 0.42f, 1f);
            GUI.DrawTexture(new Rect(card.x - 4f, card.y - 4f, card.width + 8f, card.height + 8f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(card, menuCardTexture);

            GUI.Label(new Rect(card.x + 20f, card.y + 34f, card.width - 40f, 58f), "种植建造", menuTitleStyle);
            GUI.Label(new Rect(card.x + 20f, card.y + 94f, card.width - 40f, 34f), "像素世界", menuSubtitleStyle);
            GUI.Label(
                new Rect(card.x + 35f, card.y + 136f, card.width - 70f, 50f),
                "一片由你的像素素材生成的大世界。\n先四处走走，寻找湖泊、石地与花草。",
                menuDescriptionStyle);

            Rect button = new Rect(card.x + (card.width - 190f) * 0.5f, card.y + 204f, 190f, 48f);
            GUI.color = new Color(0.32f, 0.25f, 0.10f, 1f);
            GUI.DrawTexture(new Rect(button.x, button.y + 5f, button.width, button.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (GUI.Button(button, "开始游戏", menuButtonStyle))
            {
                StartGame();
            }

            GUI.Label(
                new Rect(card.x + 20f, card.y + 270f, card.width - 40f, 28f),
                "WASD 移动 · R 返回",
                menuControlsStyle);
            GUI.color = Color.white;
        }

        private void StartGame()
        {
            showMainMenu = false;
            if (player != null)
            {
                player.enabled = true;
            }

        }

        private static Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            if (menuCardTexture != null)
            {
                Destroy(menuCardTexture);
            }
            if (menuButtonTexture != null)
            {
                Destroy(menuButtonTexture);
            }
        }
    }
}
