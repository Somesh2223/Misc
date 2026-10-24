using UnityEngine;

// One look for every on-screen menu (plan M5 UI polish): dark see-through panels and warm
// "sodium streetlight" text. Call UITheme.Apply() at the top of a menu's OnGUI.
public static class UITheme
{
    public static readonly Color Text = new Color(1f, 0.86f, 0.62f);
    public static readonly Color Accent = new Color(1f, 0.62f, 0.2f);
    public static readonly Color Danger = new Color(1f, 0.35f, 0.3f);

    private static GUISkin skin;
    private static GUIStyle title;
    private static GUIStyle prompt;

    // Big heading text (menu titles).
    public static GUIStyle Title { get { Build(); return title; } }
    // Centred text with a soft shadow box (interaction prompts).
    public static GUIStyle Prompt { get { Build(); return prompt; } }

    // Only works inside OnGUI.
    public static void Apply()
    {
        Build();
        GUI.skin = skin;
    }

    private static void Build()
    {
        if (skin != null)
        {
            return;
        }

        // Start from Unity's default skin and recolour it.
        skin = Object.Instantiate(GUI.skin);
        skin.name = "Night Shift";

        Texture2D panel = MakeTexture(new Color(0.05f, 0.06f, 0.08f, 0.88f));
        Texture2D button = MakeTexture(new Color(0.16f, 0.15f, 0.14f, 0.95f));
        Texture2D buttonHover = MakeTexture(new Color(0.3f, 0.22f, 0.12f, 0.95f));
        Texture2D buttonDown = MakeTexture(new Color(0.45f, 0.3f, 0.12f, 1f));
        Texture2D field = MakeTexture(new Color(0.1f, 0.1f, 0.1f, 1f));
        Texture2D sliderBar = MakeTexture(new Color(0.25f, 0.23f, 0.2f, 1f));
        Texture2D sliderThumb = MakeTexture(Accent);

        skin.box.normal.background = panel;
        skin.box.normal.textColor = Text;
        skin.box.padding = new RectOffset(12, 12, 10, 10);
        skin.window.normal.background = panel;
        skin.window.onNormal.background = panel;

        SetButton(skin.button, button, buttonHover, buttonDown);
        SetButton(skin.toggle, button, buttonHover, buttonDown);
        skin.toggle.padding = new RectOffset(8, 8, 4, 4);
        skin.toggle.onNormal.background = buttonDown;
        skin.toggle.onHover.background = buttonDown;
        skin.toggle.onNormal.textColor = Color.white;
        skin.toggle.onHover.textColor = Color.white;

        skin.label.normal.textColor = Text;
        skin.label.fontSize = 14;
        skin.textField.normal.background = field;
        skin.textField.focused.background = field;
        skin.textField.normal.textColor = Color.white;
        skin.textField.focused.textColor = Color.white;

        skin.horizontalSlider.normal.background = sliderBar;
        skin.horizontalSlider.fixedHeight = 8f;
        skin.horizontalSlider.margin = new RectOffset(4, 4, 8, 8);
        skin.horizontalSliderThumb.normal.background = sliderThumb;
        skin.horizontalSliderThumb.hover.background = sliderThumb;
        skin.horizontalSliderThumb.active.background = sliderThumb;
        skin.horizontalSliderThumb.fixedWidth = 12f;
        skin.horizontalSliderThumb.fixedHeight = 16f;

        title = new GUIStyle(skin.label);
        title.fontSize = 26;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.normal.textColor = Accent;

        prompt = new GUIStyle(skin.box);
        prompt.fontSize = 16;
        prompt.alignment = TextAnchor.MiddleCenter;
        prompt.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.55f));
        prompt.normal.textColor = Text;
        prompt.padding = new RectOffset(10, 10, 6, 6);
    }

    private static void SetButton(GUIStyle style, Texture2D normal, Texture2D hover, Texture2D down)
    {
        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = down;
        style.focused.background = normal;
        style.normal.textColor = Text;
        style.hover.textColor = Color.white;
        style.active.textColor = Color.white;
        style.fontSize = 14;
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }
}
