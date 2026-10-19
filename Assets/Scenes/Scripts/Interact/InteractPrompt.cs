using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Подсказка вверху экрана («Press [E]»). Показывается, пока её «держит» хоть один объект.
public static class InteractPrompt {
    static GameObject root;
    static TextMeshProUGUI label;
    static readonly List<Object> owners = new();
    static readonly List<string> messages = new();

    public static void Show(Object owner, string message, TMP_FontAsset font = null) {
        Ensure(font);
        int i = owners.IndexOf(owner);
        if (i >= 0) messages[i] = message;
        else { owners.Add(owner); messages.Add(message); }
        Refresh();
    }

    public static void Hide(Object owner) {
        int i = owners.IndexOf(owner);
        if (i < 0) return;
        owners.RemoveAt(i);
        messages.RemoveAt(i);
        Refresh();
    }

    static void Refresh() {
        if (root == null) return;
        // Последний показавший — главный
        for (int i = owners.Count - 1; i >= 0; i--)
            if (owners[i] == null) { owners.RemoveAt(i); messages.RemoveAt(i); }
        bool any = owners.Count > 0;
        root.SetActive(any);
        if (any) label.text = messages[^1];
    }

    static void Ensure(TMP_FontAsset font) {
        if (root != null) {
            if (font != null && label.font != font) label.font = font;
            return;
        }
        owners.Clear();
        messages.Clear();

        var canvasGo = new GameObject("[InteractPrompt]");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Плашка с текстом, подстраивается под длину надписи
        var panel = new GameObject("Panel", typeof(RectTransform));
        var prt = (RectTransform)panel.transform;
        prt.SetParent(canvasGo.transform, false);
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0, -60);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        bg.raycastTarget = false;
        var layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 8, 8);
        layout.childControlWidth = layout.childControlHeight = true;
        var fit = panel.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(prt, false);
        label = textGo.AddComponent<TextMeshProUGUI>();
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.fontSize = 40;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        root = canvasGo;
        root.SetActive(false);
    }
}
