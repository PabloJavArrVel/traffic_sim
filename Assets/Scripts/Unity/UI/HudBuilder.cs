using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small helpers to build screen UI from code. Used by FleetManagerHUD and FollowCamHUD.</summary>
public static class HudBuilder
{
    public static Canvas CreateScreenCanvas(Transform parent, string name, int sortingOrder)
    {
        var canvasObject = new GameObject(name);
        canvasObject.transform.SetParent(parent, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>A panel that stacks its children vertically and grows to fit them.</summary>
    public static GameObject CreateVerticalPanel(Transform parent, string name, Color background,
                                                 Vector2 corner, Vector2 offsetFromCorner, float width)
    {
        GameObject panel = CreatePanel(parent, name, background, corner, offsetFromCorner, new Vector2(width, 0f));

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return panel;
    }

    /// <summary>A panel that lines up its children horizontally and grows to fit them.</summary>
    public static GameObject CreateHorizontalPanel(Transform parent, string name, Color background,
                                                   Vector2 corner, Vector2 offsetFromCorner, float height)
    {
        GameObject panel = CreatePanel(parent, name, background, corner, offsetFromCorner, new Vector2(0f, height));

        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(12, 12, 0, 0);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        var fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        return panel;
    }

    /// <summary>A text label. Give a width to use it inside a horizontal panel.</summary>
    public static TMP_Text CreateLabel(Transform parent, string name, string text, Color color, float fontSize,
                                       FontStyles style = FontStyles.Normal,
                                       TextAlignmentOptions alignment = TextAlignmentOptions.Left,
                                       float width = -1f)
    {
        var labelObject = new GameObject(name, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);

        var layout = labelObject.AddComponent<LayoutElement>();
        if (width > 0f) layout.preferredWidth = width;
        else layout.flexibleWidth = 1f;

        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.color = color;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    public static Button CreateButton(Transform parent, string name, string text, float width, float height,
                                      Color background, Color textColor, out TMP_Text label)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);

        var layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;

        var image = buttonObject.AddComponent<Image>();
        image.color = background;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        bool transparent = background.a == 0f;
        colors.normalColor = background;
        colors.selectedColor = background;
        colors.highlightedColor = transparent ? new Color(1f, 1f, 1f, 0.12f) : background * 1.35f;
        colors.pressedColor = transparent ? new Color(1f, 1f, 1f, 0.06f) : background * 0.7f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        label = CreateLabel(buttonObject.transform, "Text", text, textColor, 14f, FontStyles.Bold, TextAlignmentOptions.Center);
        label.richText = false;   // so labels like "<" are shown as they are, not read as formatting tags
        StretchToParent(label.rectTransform);
        return button;
    }

    /// <summary>A black image covering the whole screen, invisible at first. Used to fade between camera targets.</summary>
    public static CanvasGroup CreateScreenFade(Transform canvas)
    {
        var fadeObject = new GameObject("Screen fade", typeof(RectTransform));
        fadeObject.transform.SetParent(canvas, false);
        StretchToParent((RectTransform)fadeObject.transform);
        fadeObject.AddComponent<Image>().color = Color.black;

        var group = fadeObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        return group;
    }

    public static string ColorTag(Color color) => $"#{ColorUtility.ToHtmlStringRGB(color)}";

    static GameObject CreatePanel(Transform parent, string name, Color background,
                                  Vector2 corner, Vector2 offsetFromCorner, Vector2 size)
    {
        var panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        var rect = (RectTransform)panel.transform;
        rect.anchorMin = corner;
        rect.anchorMax = corner;
        rect.pivot = corner;
        rect.anchoredPosition = offsetFromCorner;
        rect.sizeDelta = size;

        panel.AddComponent<Image>().color = background;
        return panel;
    }

    static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
