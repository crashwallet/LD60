using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Иконка на рабочем столе или в проводнике: один клик — выделить, двойной — открыть
public class OSIcon : MonoBehaviour, IPointerClickHandler {
    const float DoubleClickTime = 0.4f;

    DesktopOS os;
    Action onOpen;
    Image highlight;
    float lastClickTime = -1f;

    public static OSIcon Create(DesktopOS os, Transform parent, string label, Sprite icon, Color labelColor, Action onOpen) {
        var root = OSUI.Rect("Icon_" + label, parent);

        var hl = OSUI.Image("Highlight", root, os.SelectionColor);
        OSUI.Stretch(hl.rectTransform);
        hl.enabled = false;

        var pic = OSUI.Image("Picture", root, Color.white, icon, false);
        pic.rectTransform.anchorMin = pic.rectTransform.anchorMax = new Vector2(0.5f, 1);
        pic.rectTransform.pivot = new Vector2(0.5f, 1);
        pic.rectTransform.sizeDelta = new Vector2(os.IconSize, os.IconSize);
        pic.rectTransform.anchoredPosition = new Vector2(0, -3);

        var text = OSUI.Text("Label", root, label, os.Font, os.IconFontSize, labelColor, TextAlignmentOptions.Top);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = new Vector2(1, 0);
        text.rectTransform.pivot = new Vector2(0.5f, 0);
        text.rectTransform.offsetMin = new Vector2(2, 1);
        text.rectTransform.offsetMax = new Vector2(-2, 1 + os.IconFontSize * 2.1f);

        var icn = root.gameObject.AddComponent<OSIcon>();
        icn.os = os;
        icn.onOpen = onOpen;
        icn.highlight = hl;
        // Прозрачная подложка, чтобы клик ловился по всей ячейке
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        return icn;
    }

    public void SetSelected(bool on) => highlight.enabled = on;

    public void OnPointerClick(PointerEventData e) {
        if (e.button != PointerEventData.InputButton.Left) return;
        os.Select(this);

        if (Time.unscaledTime - lastClickTime <= DoubleClickTime) {
            lastClickTime = -1f;
            onOpen?.Invoke();
        } else {
            lastClickTime = Time.unscaledTime;
        }
    }
}
