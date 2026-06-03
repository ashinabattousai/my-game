using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies a lightweight fantasy-learning skin to existing Unity UI scenes.
/// The project currently uses hand-built UGUI scenes, so this bootstrapper keeps
/// the original scene layout and adds reusable backgrounds, panels, button states,
/// and small generated illustrations at runtime.
/// </summary>
public class UITheme : MonoBehaviour
{
    private const string BootstrapName = "UIThemeBootstrap";
    private static Sprite backgroundSprite;
    private static Sprite panelSprite;
    private static Sprite buttonSprite;
    private static Sprite inputSprite;
    private static Sprite heroSprite;
    private static Sprite enemySprite;
    private static Sprite bookSprite;

    private readonly Color textPrimary = new Color(0.95f, 0.91f, 0.80f, 1f);
    private readonly Color textAccent = new Color(1f, 0.75f, 0.35f, 1f);
    private readonly Color textMuted = new Color(0.70f, 0.78f, 0.92f, 1f);
    private readonly Color panelColor = new Color(0.10f, 0.13f, 0.25f, 0.88f);
    private readonly Color buttonColor = new Color(0.28f, 0.42f, 0.78f, 0.96f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<UITheme>() != null)
            return;

        GameObject host = new GameObject(BootstrapName);
        DontDestroyOnLoad(host);
        host.AddComponent<UITheme>();
    }

    private void Awake()
    {
        CreateSpritesIfNeeded();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyToActiveScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToActiveScene();
    }

    private void ApplyToActiveScene()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (!canvas.isRootCanvas)
                continue;

            AddBackground(canvas);
            AddSceneArt(canvas, SceneManager.GetActiveScene().name);
            StylePanels(canvas);
            StyleButtons(canvas);
            StyleInputFields(canvas);
            StyleText(canvas);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            mainCamera.backgroundColor = new Color(0.04f, 0.06f, 0.12f, 1f);
    }

    private void AddBackground(Canvas canvas)
    {
        Transform existing = canvas.transform.Find("Theme_Background");
        if (existing != null)
            return;

        GameObject background = new GameObject("Theme_Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(canvas.transform, false);
        background.transform.SetAsFirstSibling();

        RectTransform rect = background.GetComponent<RectTransform>();
        Stretch(rect);

        Image image = background.GetComponent<Image>();
        image.sprite = backgroundSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;

        AddStars(background.transform);
    }

    private void AddStars(Transform parent)
    {
        for (int i = 0; i < 18; i++)
        {
            GameObject star = new GameObject("Theme_Star", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            star.transform.SetParent(parent, false);

            RectTransform rect = star.GetComponent<RectTransform>();
            float x = Mathf.Lerp(-520f, 520f, Mathf.Repeat(i * 0.37f, 1f));
            float y = Mathf.Lerp(-300f, 300f, Mathf.Repeat(i * 0.61f, 1f));
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = Vector2.one * (6f + (i % 4) * 3f);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);

            Image image = star.GetComponent<Image>();
            image.sprite = CreateSolidSprite(new Color(1f, 0.86f, 0.45f, 0.70f));
            image.raycastTarget = false;
        }
    }

    private void AddSceneArt(Canvas canvas, string sceneName)
    {
        if (canvas.transform.Find("Theme_SceneArt") != null)
            return;

        Sprite sprite = bookSprite;
        Vector2 anchoredPosition = new Vector2(360f, -130f);
        Vector2 size = new Vector2(170f, 170f);

        if (sceneName == "Home" || sceneName == "ModeSelect")
        {
            sprite = heroSprite;
            anchoredPosition = new Vector2(360f, -110f);
            size = new Vector2(190f, 190f);
        }
        else if (sceneName == "main")
        {
            sprite = enemySprite;
            anchoredPosition = new Vector2(360f, 20f);
            size = new Vector2(190f, 190f);
        }

        GameObject art = new GameObject("Theme_SceneArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        art.transform.SetParent(canvas.transform, false);
        RectTransform rect = art.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.SetSiblingIndex(Mathf.Min(2, canvas.transform.childCount - 1));

        Image image = art.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void StylePanels(Canvas canvas)
    {
        Image[] images = canvas.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null || image.name.StartsWith("Theme_"))
                continue;

            if (image.GetComponent<Button>() != null || image.GetComponent<TMP_InputField>() != null)
                continue;

            string lowerName = image.name.ToLowerInvariant();
            if (lowerName.Contains("panel") || lowerName.Contains("content") || lowerName.Contains("viewport"))
            {
                image.sprite = panelSprite;
                image.type = Image.Type.Sliced;
                image.color = panelColor;
                AddOutline(image.gameObject, new Color(1f, 0.76f, 0.32f, 0.40f), new Vector2(2f, -2f));
            }
        }
    }

    private void StyleButtons(Canvas canvas)
    {
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
                image.color = buttonColor;
                AddOutline(button.gameObject, new Color(0.04f, 0.05f, 0.10f, 0.80f), new Vector2(2f, -2f));
            }

            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = new Color(0.38f, 0.58f, 1f, 1f);
            colors.pressedColor = new Color(0.18f, 0.27f, 0.56f, 1f);
            colors.disabledColor = new Color(0.24f, 0.27f, 0.36f, 0.55f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }
    }

    private void StyleInputFields(Canvas canvas)
    {
        TMP_InputField[] inputs = canvas.GetComponentsInChildren<TMP_InputField>(true);
        foreach (TMP_InputField input in inputs)
        {
            Image image = input.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = inputSprite;
                image.type = Image.Type.Sliced;
                image.color = new Color(0.06f, 0.08f, 0.15f, 0.95f);
                AddOutline(input.gameObject, new Color(0.95f, 0.72f, 0.28f, 0.55f), new Vector2(2f, -2f));
            }

            if (input.textComponent != null)
                input.textComponent.color = textPrimary;
            if (input.placeholder is TMP_Text placeholder)
                placeholder.color = new Color(0.70f, 0.74f, 0.86f, 0.70f);
        }
    }

    private void StyleText(Canvas canvas)
    {
        TMP_Text[] texts = canvas.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            string lowerName = text.name.ToLowerInvariant();
            bool isTitle = lowerName.Contains("title") || lowerName.Contains("word") || text.fontSize >= 42f;

            text.color = isTitle ? textAccent : textPrimary;
            text.enableWordWrapping = true;
            text.margin = new Vector4(6f, 3f, 6f, 3f);
            text.fontStyle = isTitle ? FontStyles.Bold : text.fontStyle;

            if (lowerName.Contains("score") || lowerName.Contains("combo") || lowerName.Contains("hp") || lowerName.Contains("kill") || lowerName.Contains("mode"))
                text.color = textMuted;

            AddShadow(text.gameObject, new Color(0f, 0f, 0f, 0.45f), new Vector2(2f, -2f));
        }
    }

    private static void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.AddComponent<Outline>();

        outline.effectColor = color;
        outline.effectDistance = distance;
    }

    private static void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
            shadow = target.AddComponent<Shadow>();

        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CreateSpritesIfNeeded()
    {
        if (backgroundSprite != null)
            return;

        backgroundSprite = CreateGradientSprite(128, 128, new Color(0.04f, 0.06f, 0.14f, 1f), new Color(0.18f, 0.12f, 0.32f, 1f));
        panelSprite = CreateRoundedSprite(64, 64, new Color(0.10f, 0.13f, 0.25f, 1f), new Color(0.98f, 0.72f, 0.28f, 1f), 14);
        buttonSprite = CreateRoundedSprite(64, 64, new Color(0.25f, 0.38f, 0.78f, 1f), new Color(1f, 0.78f, 0.34f, 1f), 16);
        inputSprite = CreateRoundedSprite(64, 64, new Color(0.05f, 0.07f, 0.14f, 1f), new Color(0.72f, 0.88f, 1f, 1f), 12);
        heroSprite = CreatePixelCharacterSprite(false);
        enemySprite = CreatePixelCharacterSprite(true);
        bookSprite = CreateBookSprite();
    }

    private static Sprite CreateGradientSprite(int width, int height, Color top, Color bottom)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            Color row = Color.Lerp(bottom, top, y / (float)(height - 1));
            for (int x = 0; x < width; x++)
            {
                float vignette = Vector2.Distance(new Vector2(x / (float)width, y / (float)height), new Vector2(0.5f, 0.5f));
                texture.SetPixel(x, y, Color.Lerp(row, Color.black, vignette * 0.35f));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
    }

    private static Sprite CreateRoundedSprite(int width, int height, Color fill, Color border, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = RoundedDistance(x, y, width, height, radius);
                if (distance > 1f)
                    texture.SetPixel(x, y, Color.clear);
                else if (distance > -3f)
                    texture.SetPixel(x, y, border);
                else
                    texture.SetPixel(x, y, fill);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
    }

    private static float RoundedDistance(int x, int y, int width, int height, int radius)
    {
        float px = Mathf.Abs(x - width * 0.5f) - (width * 0.5f - radius);
        float py = Mathf.Abs(y - height * 0.5f) - (height * 0.5f - radius);
        return Mathf.Min(Mathf.Max(px, py), 0f) + new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude - radius;
    }

    private static Sprite CreateSolidSprite(Color color)
    {
        Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
                texture.SetPixel(x, y, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 8, 8), Vector2.one * 0.5f);
    }

    private static Sprite CreatePixelCharacterSprite(bool monster)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color clear = Color.clear;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);

        Color body = monster ? new Color(0.20f, 0.84f, 0.62f, 1f) : new Color(0.42f, 0.62f, 1f, 1f);
        Color dark = monster ? new Color(0.06f, 0.34f, 0.28f, 1f) : new Color(0.12f, 0.20f, 0.42f, 1f);
        Color accent = monster ? new Color(1f, 0.42f, 0.38f, 1f) : new Color(1f, 0.78f, 0.30f, 1f);

        FillEllipse(texture, 32, monster ? 29 : 28, monster ? 22 : 18, monster ? 18 : 22, body);
        FillRect(texture, 18, 10, 28, 18, dark);
        FillRect(texture, 22, 34, 7, 7, Color.white);
        FillRect(texture, 40, 34, 7, 7, Color.white);
        FillRect(texture, 24, 35, 3, 3, Color.black);
        FillRect(texture, 42, 35, 3, 3, Color.black);

        if (monster)
        {
            FillRect(texture, 26, 20, 18, 4, accent);
            FillRect(texture, 14, 42, 8, 14, dark);
            FillRect(texture, 46, 42, 8, 14, dark);
        }
        else
        {
            FillRect(texture, 27, 43, 10, 12, accent);
            FillRect(texture, 18, 4, 28, 8, accent);
            FillRect(texture, 48, 18, 6, 28, new Color(0.86f, 0.92f, 1f, 1f));
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 32f);
    }

    private static Sprite CreateBookSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, Color.clear);

        FillRect(texture, 12, 12, 20, 40, new Color(0.24f, 0.42f, 0.86f, 1f));
        FillRect(texture, 32, 12, 20, 40, new Color(0.18f, 0.32f, 0.70f, 1f));
        FillRect(texture, 30, 10, 4, 44, new Color(1f, 0.78f, 0.32f, 1f));
        FillRect(texture, 17, 38, 10, 4, Color.white);
        FillRect(texture, 38, 38, 10, 4, Color.white);
        FillRect(texture, 17, 29, 10, 3, new Color(0.8f, 0.9f, 1f, 1f));
        FillRect(texture, 38, 29, 10, 3, new Color(0.8f, 0.9f, 1f, 1f));

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, 32f);
    }

    private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
            for (int xx = x; xx < x + width; xx++)
                if (xx >= 0 && xx < texture.width && yy >= 0 && yy < texture.height)
                    texture.SetPixel(xx, yy, color);
    }

    private static void FillEllipse(Texture2D texture, int centerX, int centerY, int radiusX, int radiusY, Color color)
    {
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
        {
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                float dx = (x - centerX) / (float)radiusX;
                float dy = (y - centerY) / (float)radiusY;
                if (dx * dx + dy * dy <= 1f && x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    texture.SetPixel(x, y, color);
            }
        }
    }
}
