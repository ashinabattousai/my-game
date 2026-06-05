using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class RuntimeUI
{
    public static readonly Color Ink = Hex("223044");
    public static readonly Color Paper = Hex("FFF7E3");
    public static readonly Color Gold = Hex("F2BE5C");
    public static readonly Color Coral = Hex("FF7D6E");
    public static readonly Color Teal = Hex("2EC9B7");
    public static readonly Color Night = Hex("111D31");

    private static TMP_FontAsset uiFontAsset;

    public static Canvas CreateCanvas(string name)
    {
        foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>())
        {
            if (!canvas.name.StartsWith("Runtime"))
                Object.Destroy(canvas.gameObject);
        }

        EnsureEventSystem();

        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvasRoot = go.GetComponent<Canvas>();
        canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRoot.sortingOrder = 50;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Stretch(go.GetComponent<RectTransform>());
        return canvasRoot;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
            return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Object.DontDestroyOnLoad(go);
    }

    public static RawImage Background(Transform parent, Texture2D texture)
    {
        GameObject go = new GameObject("Background", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        image.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>());
        return image;
    }

    public static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return image;
    }

    public static TMP_Text Text(Transform parent, string name, string value, int size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        TMP_FontAsset font = GetUiFontAsset();
        if (font != null)
            text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontSizeMin = Mathf.Max(12, size / 2);
        text.fontSizeMax = size;
        text.enableAutoSizing = true;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.margin = new Vector4(8, 4, 8, 4);
        if (size >= 34)
        {
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }
        Stretch(go.GetComponent<RectTransform>());
        return text;
    }

    public static void RegisterTextCharacters(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        TMP_FontAsset font = GetUiFontAsset();
        if (font != null)
            font.TryAddCharacters(value);
    }

    private static TMP_FontAsset GetUiFontAsset()
    {
        if (uiFontAsset != null)
            return uiFontAsset;

        Font font = Resources.Load<Font>("Fonts/CJK_UI");
        if (font == null)
            font = Resources.Load<Font>("Fonts/NotoSansSC-VF");
        if (font == null)
        {
            string[] candidates =
            {
                "Noto Sans SC",
                "Microsoft YaHei UI",
                "Microsoft YaHei",
                "SimHei",
                "SimSun",
                "DengXian",
                "Meiryo",
                "Yu Gothic UI",
                "Yu Gothic",
                "Arial"
            };

            font = Font.CreateDynamicFontFromOSFont(candidates, 96);
        }

        if (font == null)
        {
            Debug.LogWarning("RuntimeUI could not load CJK font. Use Assets > Refresh, then enter Play again.");
            return null;
        }

        uiFontAsset = TMP_FontAsset.CreateFontAsset(font, 96, 9, GlyphRenderMode.SDFAA, 4096, 4096, AtlasPopulationMode.Dynamic, true);
        if (uiFontAsset == null)
            return null;

        uiFontAsset.name = "Runtime CJK TMP Font";
        uiFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        uiFontAsset.isMultiAtlasTexturesEnabled = true;
        uiFontAsset.TryAddCharacters("单词战斗训练开始冒险继续进度游玩说明错题本答对攻击敌人连击越高奖励越多选择模式日语英语返回主页返回首页清空还没有这里自动记录详情掌握已未找到可能已经被删除错误次数提交下一题重开新路线输入答案后按准备中答案胜利失败生命分数击败普通精英休息首领路线完成生成新路线恢复节点关卡地图难度通关图书馆老师朋友勇气樱花苹果月亮星星学校谢谢早上好敌影看提示先回忆再输入根据中文回忆英文拼写日语读法新词需复习较熟学习中正确写法首字母长度看过答案后命中标为权重金币经验等级遗物商店事件宝箱复习沙漏护身符连击剑复习之书钱袋凤羽购买治疗古书试炼错写法师遮蔽部分释义被封印猎物战利品便利店利润奖金福利卫生障碍不利条件东西方向事理剩下实物现货特产生物真货附加赠品买购物依据借助");
        return uiFontAsset;
    }

    public static Button Button(Transform parent, string name, string label, Color color, Color textColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.30f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
        shadow.effectDistance = new Vector2(0f, -4f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor = new Color(0.76f, 0.76f, 0.76f, 1f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.5f);
        button.colors = colors;

        TMP_Text buttonText = Text(go.transform, "Label", label, 28, textColor, TextAlignmentOptions.Center);
        buttonText.enableAutoSizing = true;
        return button;
    }

    public static TMP_InputField InputField(Transform parent, string placeholder)
    {
        GameObject go = new GameObject("AnswerInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(1f, 0.97f, 0.9f, 0.96f);

        GameObject viewport = new GameObject("TextViewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(go.transform, false);
        RectTransform viewportRt = viewport.GetComponent<RectTransform>();
        Stretch(viewportRt);
        viewportRt.offsetMin = new Vector2(24, 8);
        viewportRt.offsetMax = new Vector2(-24, -8);

        TMP_Text text = Text(viewport.transform, "Text", "", 32, Ink, TextAlignmentOptions.MidlineLeft);
        TMP_Text hint = Text(viewport.transform, "Placeholder", placeholder, 28, new Color(0.13f, 0.19f, 0.27f, 0.46f), TextAlignmentOptions.MidlineLeft);

        TMP_InputField input = go.GetComponent<TMP_InputField>();
        input.textViewport = viewportRt;
        input.textComponent = text;
        input.placeholder = hint;
        input.caretColor = Ink;
        input.selectionColor = new Color(0.18f, 0.75f, 0.69f, 0.28f);
        input.onFocusSelectAll = false;
        return input;
    }

    public static RawImage RawImage(Transform parent, string name, Texture2D texture)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        Stretch(go.GetComponent<RectTransform>());
        return image;
    }

    public static ScrollRect Scroll(Transform parent, string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = new Color(1f, 0.98f, 0.92f, 0.82f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(root.transform, false);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<RectTransform>().offsetMin = new Vector2(14, 14);
        viewport.GetComponent<RectTransform>().offsetMax = new Vector2(-14, -14);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 12;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = root.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        return scroll;
    }

    public static void SetRect(Component component, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rt = component.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    public static void AddVerticalLayout(GameObject go, int padding, int spacing, bool expandWidth = true)
    {
        VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = expandWidth;
    }

    public static void AddHorizontalLayout(GameObject go, int padding, int spacing)
    {
        HorizontalLayoutGroup layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(padding, padding, padding, padding);
        layout.spacing = spacing;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = true;
    }

    public static LayoutElement Layout(GameObject go, float minHeight = -1, float preferredHeight = -1, float flexibleWidth = -1)
    {
        LayoutElement element = go.GetComponent<LayoutElement>();
        if (element == null)
            element = go.AddComponent<LayoutElement>();
        if (minHeight >= 0)
            element.minHeight = minHeight;
        if (preferredHeight >= 0)
            element.preferredHeight = preferredHeight;
        if (flexibleWidth >= 0)
            element.flexibleWidth = flexibleWidth;
        return element;
    }

    public static Texture2D LoadTexture(string path)
    {
        return Resources.Load<Texture2D>(path);
    }

    public static Color Hex(string value)
    {
        if (ColorUtility.TryParseHtmlString("#" + value.TrimStart('#'), out Color color))
            return color;
        return Color.white;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }
}
