using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Окно: заголовок, кнопки «свернуть» и «закрыть», перетаскивание за заголовок, вывод на передний план по клику
public class OSWindow : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IDropHandler {
    const float TitleHeight = 22f;
    const float AnimTime = 0.16f;     // длительность сворачивания/разворачивания
    const float MinimizedScale = 0.1f;

    public RectTransform Content { get; private set; }
    public UnityEngine.Object Key { get; private set; } // что открыто в окне (папка или файл)
    public string Title { get; private set; }
    public Sprite Icon { get; private set; }
    public bool IsMinimized => !gameObject.activeSelf;
    public bool IsTopmost => !IsMinimized && rt.GetSiblingIndex() == LastVisibleIndex();

    public event Action Closed;
    public event Action Changed; // заголовок, свёрнутость или фокус — для панели задач

    // Куда «улетает» окно при сворачивании (мировая точка кнопки на панели задач)
    public Func<Vector3> MinimizeTarget;

    Coroutine anim;
    Vector3 restoredLocalPos;

    RectTransform rt, parent, titleBar;
    TextMeshProUGUI titleText;
    bool dragging;
    Vector2 dragStartPointer, dragStartPos;

    public static OSWindow Create(DesktopOS os, RectTransform parent, string title, Vector2 size, Vector2 position,
                                  UnityEngine.Object key, Sprite icon = null) {
        // Рамка
        var frame = OSUI.Image("Window_" + title, parent, os.WindowBorderColor);
        var rt = frame.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(Mathf.Min(size.x, parent.rect.width - 10), Mathf.Min(size.y, parent.rect.height - 10));

        var w = frame.gameObject.AddComponent<OSWindow>();
        w.rt = rt;
        w.parent = parent;
        w.Key = key;
        w.Title = title;
        w.Icon = icon;

        // Заголовок
        var bar = OSUI.Image("TitleBar", rt, os.TitleBarColor);
        w.titleBar = OSUI.TopBar(bar.rectTransform, TitleHeight, 2, 2);
        w.titleText = OSUI.Text("Title", bar.transform, title, os.Font, os.TitleFontSize, os.TitleTextColor);
        OSUI.Stretch(w.titleText.rectTransform, 6, 0, TitleHeight * 2 + 4, 0);

        var close = OSUI.Button("Close", bar.transform, "X", os.Font, os.TitleFontSize, os.CloseButtonColor, Color.white, w.Close);
        var crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(1, 0.5f);
        crt.sizeDelta = new Vector2(TitleHeight - 4, TitleHeight - 4);
        crt.anchoredPosition = new Vector2(-2, 0);

        var min = OSUI.Button("Minimize", bar.transform, "_", os.Font, os.TitleFontSize, os.ButtonColor, os.TextColor, w.Minimize);
        var mrt = (RectTransform)min.transform;
        mrt.anchorMin = mrt.anchorMax = mrt.pivot = new Vector2(1, 0.5f);
        mrt.sizeDelta = new Vector2(TitleHeight - 4, TitleHeight - 4);
        mrt.anchoredPosition = new Vector2(-TitleHeight, 0);

        // Клиентская область
        var body = OSUI.Image("Body", rt, os.WindowColor);
        w.Content = OSUI.Stretch(body.rectTransform, 2, 2, 2, TitleHeight + 4);

        w.SetPosition(position);
        return w;
    }

    public void SetTitle(string title) {
        Title = title;
        titleText.text = title;
        Changed?.Invoke();
    }

    public void Focus() {
        if (IsMinimized) {
            gameObject.SetActive(true);
            rt.SetAsLastSibling();
            Changed?.Invoke();
            anim = StartCoroutine(Animate(TargetLocalPos(), restoredLocalPos, MinimizedScale, 1f, null));
            return;
        }
        rt.SetAsLastSibling();
        Changed?.Invoke();
    }

    public void Minimize() {
        if (IsMinimized || anim != null) return;
        restoredLocalPos = rt.localPosition;
        anim = StartCoroutine(Animate(restoredLocalPos, TargetLocalPos(), 1f, MinimizedScale, () => {
            gameObject.SetActive(false);
            rt.localPosition = restoredLocalPos;
            rt.localScale = Vector3.one;
            Changed?.Invoke();
        }));
    }

    Vector3 TargetLocalPos() {
        if (MinimizeTarget == null) return restoredLocalPos + new Vector3(0, -parent.rect.height * 0.5f, 0);
        Vector3 p = parent.InverseTransformPoint(MinimizeTarget());
        return new Vector3(p.x, p.y, restoredLocalPos.z);
    }

    IEnumerator Animate(Vector3 fromPos, Vector3 toPos, float fromScale, float toScale, Action done) {
        for (float t = 0f; t < AnimTime; t += Time.unscaledDeltaTime) {
            float k = Mathf.SmoothStep(0f, 1f, t / AnimTime);
            rt.localPosition = Vector3.Lerp(fromPos, toPos, k);
            rt.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, k);
            yield return null;
        }
        rt.localPosition = toPos;
        rt.localScale = Vector3.one * toScale;
        anim = null;
        done?.Invoke();
    }

    void OnDisable() {
        // Анимацию прервали (например, выключили компьютер) — вернуть окно в нормальный вид
        if (anim != null) {
            anim = null;
            rt.localPosition = restoredLocalPos;
            rt.localScale = Vector3.one;
        }
    }

    public void Close() {
        gameObject.SetActive(false); // чтобы панель задач сразу считала верхним другое окно
        Closed?.Invoke();
        Destroy(gameObject);
    }

    int LastVisibleIndex() {
        for (int i = parent.childCount - 1; i >= 0; i--)
            if (parent.GetChild(i).gameObject.activeSelf) return i;
        return -1;
    }

    public void SetPosition(Vector2 pos) {
        Vector2 limit = (parent.rect.size - rt.rect.size) * 0.5f;
        limit = Vector2.Max(limit, Vector2.zero);
        rt.anchoredPosition = new Vector2(Mathf.Clamp(pos.x, -limit.x, limit.x), Mathf.Clamp(pos.y, -limit.y, limit.y));
    }

    public void OnPointerDown(PointerEventData e) => Focus();

    // Иконку бросили на заголовок или рамку окна — ничего не делаем (а не кладём на рабочий стол под окном)
    public void OnDrop(PointerEventData e) { }

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
