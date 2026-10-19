using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Главное меню: три монитора стопкой. Наведи — монитор включается, ЛКМ — действие.
// Верхний — Play, средний — Options (громкость и язык), нижний — Exit.
public class MainMenu : MonoBehaviour {
    [SerializeField] MenuMonitor[] monitors;
    [SerializeField] TMP_FontAsset font;
    [SerializeField] string gameScene = GameFlow.GameScene;
    [SerializeField] float fadeTime = 0.6f;

    Camera cam;
    MenuMonitor hovered;
    GameObject options;
    Image fade;
    Slider volume;
    TextMeshProUGUI volumeValue;
    Image ruButton, enButton;
    bool busy;

    static readonly Color ButtonColor = new(0.18f, 0.2f, 0.22f);
    static readonly Color ButtonActive = new(0.15f, 0.45f, 0.25f);

    void Start() {
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (font == null) font = TMP_Settings.defaultFontAsset;
        BuildUI();
        StartCoroutine(Fade(1f, 0f));
    }

    void Update() {
        if (busy || cam == null || Mouse.current == null) return;

        // Пока открыты настройки — мониторы не реагируют
        if (options.activeSelf) {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseOptions();
            return;
        }

        MenuMonitor target = null;
        var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out var hit, 50f)) target = hit.collider.GetComponentInParent<MenuMonitor>();
        SetHovered(target);

        if (hovered != null && Mouse.current.leftButton.wasPressedThisFrame) Act(hovered.action);
    }

    void SetHovered(MenuMonitor m) {
        if (m == hovered) return;
        hovered = m;
        foreach (var mon in monitors) if (mon != null) mon.SetOn(mon == m);
    }

    void Act(MenuAction action) {
        switch (action) {
            case MenuAction.Play: StartCoroutine(PlayRoutine()); break;
            case MenuAction.Options: OpenOptions(); break;
            case MenuAction.Exit: StartCoroutine(ExitRoutine()); break;
        }
    }

    IEnumerator PlayRoutine() {
        busy = true;
        GameFlow.PlayIntro = true;
        yield return Fade(0f, 1f);
        SceneManager.LoadScene(gameScene);
    }

    IEnumerator ExitRoutine() {
        busy = true;
        yield return Fade(0f, 1f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Fade(float from, float to) {
        fade.gameObject.SetActive(true);
        for (float t = 0; t < fadeTime; t += Time.unscaledDeltaTime) {
            fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, t / fadeTime));
            yield return null;
        }
        fade.color = new Color(0, 0, 0, to);
        if (to <= 0f) fade.gameObject.SetActive(false);
    }

    // ---------- Настройки ----------

    void OpenOptions() {
        SetHovered(null);
        options.SetActive(true);
        volume.SetValueWithoutNotify(GameSettings.Volume);
        RefreshOptions();
    }

    void CloseOptions() => options.SetActive(false);

    void RefreshOptions() {
        volumeValue.text = Mathf.RoundToInt(volume.value * 100f) + "%";
        ruButton.color = Loc.Language == GameLanguage.Russian ? ButtonActive : ButtonColor;
        enButton.color = Loc.Language == GameLanguage.English ? ButtonActive : ButtonColor;
    }

    void BuildUI() {
        if (EventSystem.current == null) {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            es.transform.SetParent(transform, false);
        }

        var canvasGo = new GameObject("MenuUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)canvasGo.transform;

        // Окно настроек
        var panel = OSUI.Image("Options", root, new Color(0.06f, 0.07f, 0.08f, 0.96f));
        options = panel.gameObject;
        var prt = panel.rectTransform;
        prt.sizeDelta = new Vector2(680, 470);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.15f, 0.6f, 0.3f);
        outline.effectDistance = new Vector2(3, -3);

        Label(prt, "Настройки", "Options", 52, new Vector2(0, -50), TextAlignmentOptions.Center, new Vector2(600, 70));

        Label(prt, "Громкость", "Volume", 34, new Vector2(-150, -140), TextAlignmentOptions.MidlineLeft, new Vector2(300, 50));
        var sliderGo = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderGo.transform.SetParent(prt, false);
        var srt = (RectTransform)sliderGo.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
        srt.sizeDelta = new Vector2(440, 26);
        srt.anchoredPosition = new Vector2(-40, -200);
        volume = sliderGo.GetComponent<Slider>();
        volume.minValue = 0f;
        volume.maxValue = 1f;
        foreach (var img in sliderGo.GetComponentsInChildren<Image>()) {
            if (img.name == "Background") img.color = new Color(0.2f, 0.2f, 0.22f);
            else if (img.name == "Fill") img.color = new Color(0.2f, 0.7f, 0.35f);
            else if (img.name == "Handle") img.color = new Color(0.85f, 0.95f, 0.88f);
        }
        volume.onValueChanged.AddListener(v => { GameSettings.Volume = v; RefreshOptions(); });
        volumeValue = OSUI.Text("VolumeValue", prt, "", font, 32, Color.white, TextAlignmentOptions.MidlineRight);
        Place(volumeValue.rectTransform, new Vector2(250, -200), new Vector2(110, 40));

        Label(prt, "Язык", "Language", 34, new Vector2(-150, -265), TextAlignmentOptions.MidlineLeft, new Vector2(300, 50));
        ruButton = MenuButton(prt, "Русский", "Русский", new Vector2(-120, -325), () => { Loc.Language = GameLanguage.Russian; RefreshOptions(); });
        enButton = MenuButton(prt, "English", "English", new Vector2(120, -325), () => { Loc.Language = GameLanguage.English; RefreshOptions(); });

        MenuButton(prt, "Назад", "Back", new Vector2(0, -410), CloseOptions);
        options.SetActive(false);

        // Затемнение для переходов
        fade = OSUI.Image("Fade", root, Color.black);
        OSUI.Stretch(fade.rectTransform);
        fade.raycastTarget = false;
        fade.transform.SetAsLastSibling();
    }

    TextMeshProUGUI Label(RectTransform parent, string ru, string en, float size, Vector2 pos, TextAlignmentOptions align, Vector2 sizeDelta) {
        var t = OSUI.Text("Label_" + en, parent, "", font, size, Color.white, align);
        t.gameObject.AddComponent<LocalizedText>().Set(ru, en);
        Place(t.rectTransform, pos, sizeDelta);
        return t;
    }

    Image MenuButton(RectTransform parent, string ru, string en, Vector2 pos, UnityEngine.Events.UnityAction onClick) {
        var b = OSUI.Button("Button_" + en, parent, "", font, 32, ButtonColor, Color.white, onClick);
        b.GetComponentInChildren<TextMeshProUGUI>().gameObject.AddComponent<LocalizedText>().Set(ru, en);
        Place((RectTransform)b.transform, pos, new Vector2(220, 56));
        return (Image)b.targetGraphic;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size) {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }
}
