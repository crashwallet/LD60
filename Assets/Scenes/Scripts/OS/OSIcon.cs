using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Иконка на рабочем столе или в проводнике: один клик — выделить, двойной — открыть,
// перетащить — переложить в другую папку / корзину (если иконка — папка, на неё можно бросать)
public class OSIcon : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler {
    const float DoubleClickTime = 0.4f;

    DesktopOS os;
    Action onOpen;
    Image highlight;
    Sprite sprite;
    float lastClickTime = -1f;

    // Что это за иконка и где она лежит (для перетаскивания)
    public UnityEngine.Object Item { get; private set; }
    public FolderData Source { get; private set; }
    public FolderData DropTarget => Item as FolderData; // на папку можно бросать

    RectTransform ghost;
    CanvasGroup group;

    public static OSIcon Create(DesktopOS os, Transform parent, string label, Sprite icon, Color labelColor, Action onOpen,
                                UnityEngine.Object item = null, FolderData source = null) {
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
        icn.sprite = icon;
        icn.Item = item;
        icn.Source = source;
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

    // --- Перетаскивание ---

    bool CanDrag => Item != null && Source != null && !(Item is FolderData f && f.kind != FolderKind.Folder);

    public void OnBeginDrag(PointerEventData e) {
        if (e.button != PointerEventData.InputButton.Left || !CanDrag) return;
        os.Select(this);

        // Сама иконка становится полупрозрачной и не ловит лучи — чтобы «под ней» нашлась папка для сброса
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0.4f;
        group.blocksRaycasts = false;

        var img = OSUI.Image("DragGhost", os.DragLayer, new Color(1, 1, 1, 0.75f), sprite, false);
        ghost = img.rectTransform;
        ghost.anchorMin = ghost.anchorMax = ghost.pivot = new Vector2(0.5f, 0.5f);
        ghost.sizeDelta = new Vector2(os.IconSize, os.IconSize);
        ghost.SetAsLastSibling();
        MoveGhost(e);
    }

    public void OnDrag(PointerEventData e) {
        if (ghost != null) MoveGhost(e);
    }

    public void OnEndDrag(PointerEventData e) => EndDrag();

    void OnDisable() => EndDrag(); // иконку пересоздали посреди перетаскивания — убрать призрак

    void EndDrag() {
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        if (group != null) {
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }
    }

    void MoveGhost(PointerEventData e) {
        var cam = e.pressEventCamera;
        var layer = os.DragLayer;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, ScreenDistortion.ToScene(e.position, cam), cam, out var p))
            ghost.localPosition = new Vector3(p.x, p.y, 0f);
    }

    // На иконку что-то бросили: папка принимает, остальные передают сброс дальше (в окно проводника / на стол)
    public void OnDrop(PointerEventData e) {
        var dragged = e.pointerDrag != null ? e.pointerDrag.GetComponent<OSIcon>() : null;
        if (dragged == null || dragged == this || !dragged.CanDrag) return;

        if (DropTarget != null) {
            os.Move(dragged.Item, dragged.Source, DropTarget);
            return;
        }
        if (transform.parent != null)
            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, e, ExecuteEvents.dropHandler);
    }

    // Для зон сброса: из события достать перетаскиваемую иконку
    public static OSIcon Dragged(PointerEventData e) {
        var icon = e.pointerDrag != null ? e.pointerDrag.GetComponent<OSIcon>() : null;
        return icon != null && icon.CanDrag ? icon : null;
    }
}
