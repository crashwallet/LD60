using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Крупный просмотр записки поверх экрана. Закрывается ЛКМ, Esc или E.
public class NoteViewer : MonoBehaviour {
    static NoteViewer instance;

    public static bool IsOpen => instance != null && instance.root.activeSelf;
    // Кадр, в котором записку убрали — тот же клик не должен сразу открыть её снова
    public static int LastClosedFrame { get; private set; } = -1;

    GameObject root;
    Image paper;
    Image picture;
    TextMeshProUGUI label;
    FirstPersonController controller;
    int openedFrame;

    public static void Show(NoteItem note) {
        if (instance == null) {
            var go = new GameObject("[NoteViewer]");
            instance = go.AddComponent<NoteViewer>();
            instance.Build();
        }
        instance.Open(note);
    }

    void Build() {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        root = new GameObject("Root", typeof(RectTransform));
        var rrt = (RectTransform)root.transform;
        rrt.SetParent(transform, false);
        rrt.anchorMin = Vector2.zero;
        rrt.anchorMax = Vector2.one;
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        var dim = root.AddComponent<Image>();
        dim.color = new Color(0, 0, 0, 0.7f);

        var paperGo = new GameObject("Paper", typeof(RectTransform));
        var prt = (RectTransform)paperGo.transform;
        prt.SetParent(rrt, false);
        prt.sizeDelta = new Vector2(720, 820);
        paper = paperGo.AddComponent<Image>();

        var picGo = new GameObject("Picture", typeof(RectTransform));
        var pirt = (RectTransform)picGo.transform;
        pirt.SetParent(prt, false);
        pirt.anchorMin = Vector2.zero;
        pirt.anchorMax = Vector2.one;
        pirt.offsetMin = new Vector2(40, 40);
        pirt.offsetMax = new Vector2(-40, -40);
        picture = picGo.AddComponent<Image>();
        picture.preserveAspect = true;

        var textGo = new GameObject("Text", typeof(RectTransform));
        var trt = (RectTransform)textGo.transform;
        trt.SetParent(prt, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(50, 50);
        trt.offsetMax = new Vector2(-50, -50);
        label = textGo.AddComponent<TextMeshProUGUI>();
        label.color = new Color(0.12f, 0.12f, 0.18f);
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;

        var hintGo = new GameObject("Hint", typeof(RectTransform));
        var hrt = (RectTransform)hintGo.transform;
        hrt.SetParent(rrt, false);
        hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0, 40);
        hrt.sizeDelta = new Vector2(800, 50);
        var hint = hintGo.AddComponent<TextMeshProUGUI>();
        hint.alignment = TextAlignmentOptions.Center;
        hint.fontSize = 28;
        hint.color = new Color(1, 1, 1, 0.7f);
        hint.text = Loc.T("ЛКМ / Esc — убрать", "LMB / Esc — put away");
        hint.raycastTarget = false;

        root.SetActive(false);
    }

    void Open(NoteItem note) {
        if (controller == null) controller = FindAnyObjectByType<FirstPersonController>();
        if (controller != null) controller.enabled = false; // пока читаем — не ходим и не крутим головой

        paper.color = note.paperColor;
        bool hasImage = note.image != null;
        picture.gameObject.SetActive(hasImage);
        label.gameObject.SetActive(!hasImage);
        if (hasImage) picture.sprite = note.image;
        else {
            label.text = note.Text;
            label.fontSize = note.monospace ? 64 : 34;
            label.alignment = note.monospace ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            label.characterSpacing = note.monospace ? 20 : 0;
        }

        root.SetActive(true);
        openedFrame = Time.frameCount;
    }

    void Close() {
        root.SetActive(false);
        LastClosedFrame = Time.frameCount;
        if (controller != null) controller.enabled = true;
    }

    void Update() {
        if (!root.activeSelf || Time.frameCount == openedFrame) return; // тот же клик, что открыл
        bool close = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                     || (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame));
        if (close) Close();
    }

    void OnDestroy() {
        if (instance == this) instance = null;
    }
}
