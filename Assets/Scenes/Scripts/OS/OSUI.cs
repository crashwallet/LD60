using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Помощники для сборки UI из кода
public static class OSUI {
    public static RectTransform Rect(string name, Transform parent) {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    // Растянуть по родителю с отступами (слева, снизу, справа, сверху)
    public static RectTransform Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        return rt;
    }

    // Полоса у верхнего края родителя заданной высоты
    public static RectTransform TopBar(RectTransform rt, float height, float top = 0, float side = 0) {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(side, -top - height);
        rt.offsetMax = new Vector2(-side, -top);
        return rt;
    }

    public static Image Image(string name, Transform parent, Color color, Sprite sprite = null, bool raycast = true) {
        var img = Rect(name, parent).gameObject.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.preserveAspect = sprite != null;
        img.raycastTarget = raycast;
        return img;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color,
                                       TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft) {
        var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        return t;
    }

    public static Button Button(string name, Transform parent, string label, TMP_FontAsset font, float size,
                                Color bg, Color fg, UnityEngine.Events.UnityAction onClick) {
        var img = Image(name, parent, bg);
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        var t = Text("Label", img.transform, label, font, size, fg, TextAlignmentOptions.Center);
        Stretch(t.rectTransform);
        return btn;
    }

    // Прокручиваемая область: возвращает content, который растёт вниз
    public static RectTransform ScrollArea(string name, RectTransform parent, Color bg, out ScrollRect scroll) {
        var bgImg = Image(name, parent, bg);
        scroll = bgImg.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 25f;

        var viewport = Stretch(Rect("Viewport", bgImg.transform));
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        var content = Rect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.offsetMin = content.offsetMax = Vector2.zero;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        return content;
    }

    public static void Clear(Transform t) {
        for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
    }
}
