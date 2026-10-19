using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class MonitorOS : MonoBehaviour
{
    [Header("Экраны монитора")]
    [SerializeField] private GameObject bootScreen;    // Экран загрузки
    [SerializeField] private GameObject desktopScreen; // Рабочий стол

    [Header("Параметры загрузки (Slider)")]
    [SerializeField] private Slider progressSlider;    // Ваш Slider
    [SerializeField] private float startDelay = 1.67f;  // Задержка перед включением (в секундах)
    [SerializeField] private float bootTime = 2.0f;    // Время самой загрузки
    [SerializeField] private bool bootOnlyOnce = true; // Загружать ОС только при первом входе?

    private bool isBooted = false;
    private Coroutine bootCoroutine;

    [Header("Ввод мышью")]
    [SerializeField] private Canvas canvas;              // M1_canvas
    [SerializeField] private GraphicRaycaster raycaster; // на M1_canvas

    [Header("Доступ (ProgressionManager.VirusAccess)")]
    [Tooltip("Сколько доступа вируса нужно, чтобы этот компьютер загрузился. 0 — доступен сразу.")]
    [SerializeField] private int requiredAccess = 0;
    [Tooltip("Свой экран «нет доступа». Если пусто — синий экран создастся автоматически.")]
    [SerializeField] private GameObject deniedScreen;
    [SerializeField] private TMP_FontAsset deniedFont;
    [SerializeField] private Color deniedBackground = new Color(0.0f, 0.0f, 0.66f);
    [SerializeField] private string deniedTitle = ":(";
    [TextArea(2, 6)]
    [SerializeField] private string deniedText = "НЕТ ДОСТУПА\n\nСистема заблокирована администратором.\nКод ошибки: 0x000000ACCESS_DENIED";
    [Tooltip("Пауза на полностью заполненной полоске перед ошибкой.")]
    [SerializeField] private float deniedDelay = 0.5f;

    private TextMeshProUGUI deniedInfo;

    public int RequiredAccess => requiredAccess;

    public bool HasAccess
    {
        get
        {
            if (requiredAccess <= 0) return true;
            var pm = ProgressionManager.Instance;
            return pm != null && pm.VirusAccess >= requiredAccess;
        }
    }

    private void Start()
    {
        bootScreen.SetActive(false);
        desktopScreen.SetActive(false);
        if (deniedScreen != null) deniedScreen.SetActive(false);
        if (canvas != null) canvas.worldCamera = Camera.main; // камера с CinemachineBrain
        SetInput(false);
    }

    private void SetInput(bool on)
    {
        if (raycaster != null) raycaster.enabled = on;
        if (!on && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }


    public void TurnOnMonitor()
    {
        SetInput(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Если ОС уже загружена и не нужно грузить заново — сразу включаем рабочий стол
        if (bootOnlyOnce && isBooted)
        {
            desktopScreen.SetActive(true);
            return;
        }

        if (bootCoroutine != null) StopCoroutine(bootCoroutine);
        bootCoroutine = StartCoroutine(BootSequence());
    }

    public void TurnOffMonitor()
    {
        SetInput(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (bootCoroutine != null) StopCoroutine(bootCoroutine);

        // Если нужно выключать монитор при уходе (или если загрузка прервалась на середине)
        if (!bootOnlyOnce || !isBooted)
        {
            bootScreen.SetActive(false);
            desktopScreen.SetActive(false);
            if (deniedScreen != null) deniedScreen.SetActive(false);
        }
    }

    // «Пуск → Выключить компьютер»: гасим экраны, при следующем входе ОС загрузится заново
    public void ShutDown()
    {
        if (bootCoroutine != null) StopCoroutine(bootCoroutine);
        bootCoroutine = null;
        isBooted = false;
        bootScreen.SetActive(false);
        desktopScreen.SetActive(false);
        if (deniedScreen != null) deniedScreen.SetActive(false);
    }

    // Синий экран «нет доступа» поверх экрана монитора (того же размера и положения, что рабочий стол)
    private void ShowDenied()
    {
        if (deniedScreen == null) deniedScreen = BuildDeniedScreen();

        if (deniedInfo != null)
        {
            int have = ProgressionManager.Instance != null ? ProgressionManager.Instance.VirusAccess : 0;
            deniedInfo.text = $"{deniedText}\n\nТребуемый уровень доступа: {requiredAccess}\nТекущий уровень доступа: {have}";
        }
        deniedScreen.transform.SetAsLastSibling();
        deniedScreen.SetActive(true);
    }

    private GameObject BuildDeniedScreen()
    {
        var src = (RectTransform)desktopScreen.transform;
        var go = new GameObject("DeniedScreen", typeof(RectTransform));
        go.layer = desktopScreen.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(src.parent, false);
        rt.anchorMin = src.anchorMin;
        rt.anchorMax = src.anchorMax;
        rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition;
        rt.sizeDelta = src.sizeDelta;
        rt.localPosition = src.localPosition;
        rt.localRotation = src.localRotation;
        rt.localScale = src.localScale;

        var bg = go.AddComponent<Image>();
        bg.color = deniedBackground;

        TMP_FontAsset font = deniedFont != null ? deniedFont : TMP_Settings.defaultFontAsset;

        var title = NewText("Title", rt, deniedTitle, font, 64);
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0, 1);
        trt.anchorMax = new Vector2(1, 1);
        trt.pivot = new Vector2(0.5f, 1);
        trt.offsetMin = new Vector2(30, -110);
        trt.offsetMax = new Vector2(-30, -20);

        deniedInfo = NewText("Info", rt, deniedText, font, 18);
        var irt = deniedInfo.rectTransform;
        irt.anchorMin = Vector2.zero;
        irt.anchorMax = Vector2.one;
        irt.offsetMin = new Vector2(30, 30);
        irt.offsetMax = new Vector2(-30, -120);
        return go;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string text, TMP_FontAsset font, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.TopLeft;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    private IEnumerator BootSequence()
    {
        // 1. Пока идет задержка, оба UI-экрана выключены (видна поверхность 3D-модели)
        bootScreen.SetActive(false);
        desktopScreen.SetActive(false);
        if (deniedScreen != null) deniedScreen.SetActive(false);

        if (progressSlider != null) progressSlider.value = 0f;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 2. Включаем экран загрузки и запускаем полоску
        bootScreen.SetActive(true);

        float timer = 0f;
        while (timer < bootTime)
        {
            timer += Time.deltaTime;
            if (progressSlider != null)
            {
                progressSlider.value = Mathf.Clamp01(timer / bootTime);
            }
            yield return null;
        }

        // 3. Не хватает доступа вируса — вместо рабочего стола синий экран.
        //    isBooted не ставим: при следующем входе загрузка пойдёт заново и доступ проверится ещё раз.
        if (!HasAccess)
        {
            if (deniedDelay > 0f) yield return new WaitForSeconds(deniedDelay);
            bootScreen.SetActive(false);
            ShowDenied();
            bootCoroutine = null;
            yield break;
        }

        // 4. Показываем рабочий стол
        isBooted = true;
        bootScreen.SetActive(false);
        desktopScreen.SetActive(true);
    }
}