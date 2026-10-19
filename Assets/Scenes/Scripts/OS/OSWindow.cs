using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Окно: заголовок, кнопка закрытия, перетаскивание за заголовок, вывод на передний план по клику
public class OSWindow : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler {
    const float TitleHeight = 22f;

    public RectTransform Content { get; private set; }
    public UnityEngine.Object Key { get; private set; } // что открыто в окне (папка или файл)
    public event Action Closed;

    RectTransform rt, parent, titleBar;
    TextMeshProUGUI titleText;
    bool dragging;
    Vector2 dragStartPointer, dragStartPos;

    public static OSWindow Create(DesktopOS os, RectTransform parent, string title, Vector2 size, Vector2 position, UnityEngine.Object key) {
        // Рамка
        var frame = OSUI.Image("Window_" + title, parent, os.WindowBorderColor);
        var rt = frame.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Mathf.Min(size.x, parent.rect.width - 10), Mathf.Min(size.y, parent.rect.height - 10));

        var w = frame.gameObject.AddComponent<OSWindow>();
        w.rt = rt;
        w.parent = parent;
        w.Key = key;

        // Заголовок
        var bar = OSUI.Image("TitleBar", rt, os.TitleBarColor);
        w.titleBar = OSUI.TopBar(bar.rectTransform, TitleHeight, 2, 2);
        w.titleText = OSUI.Text("Title", bar.transform, title, os.Font, os.TitleFontSize, os.TitleTextColor);
        OSUI.Stretch(w.titleText.rectTransform, 6, 0, TitleHeight + 4, 0);

        var close = OSUI.Button("Close", bar.transform, "X", os.Font, os.TitleFontSize, os.CloseButtonColor, Color.white, w.Close);
        var crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1, 0.5f);
        crt.sizeDelta = new Vector2(TitleHeight - 4, TitleHeight - 4);
        crt.anchoredPosition = new Vector2(-2, 0);

        // Клиентская область
        var body = OSUI.Image("Body", rt, os.WindowColor);
        w.Content = OSUI.Stretch(body.rectTransform, 2, 2, 2, TitleHeight + 4);

        w.SetPosition(position);
        return w;
    }

    public void SetTitle(string title) => titleText.text = title;

    public void Focus() => rt.SetAsLastSibling();

    public void Close() {
        Closed?.Invoke();
        Destroy(gameObject);
    }

    public void SetPosition(Vector2 pos) {
        Vector2 limit = (parent.rect.size - rt.rect.size) * 0.5f;
        limit = Vector2.Max(limit, Vector2.zero);
        rt.anchoredPosition = new Vector2(Mathf.Clamp(pos.x, -limit.x, limit.x), Mathf.Clamp(pos.y, -limit.y, limit.y));
    }

    public void OnPointerDown(PointerEventData e) => Focus();

    // Позиции курсора пересчитываем с учётом ЭЛТ-искажения, как в DistortedGraphicRaycaster
    static Vector2 ScenePos(Vector2 screenPos, Camera cam) => ScreenDistortion.ToScene(screenPos, cam);

    public void OnBeginDrag(PointerEventData e) {
        var cam = e.pressEventCamera;
        dragging = e.button == PointerEventData.InputButton.Left &&
                   RectTransformUtility.RectangleContainsScreenPoint(titleBar, ScenePos(e.pressPosition, cam), cam);
        if (!dragging) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, ScenePos(e.position, cam), cam, out dragStartPointer);
        dragStartPos = rt.anchoredPosition;
    }

    public void OnDrag(PointerEventData e) {
        if (!dragging) return;
        var cam = e.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, ScenePos(e.position, cam), cam, out var p))
            SetPosition(dragStartPos + p - dragStartPointer);
    }
}
