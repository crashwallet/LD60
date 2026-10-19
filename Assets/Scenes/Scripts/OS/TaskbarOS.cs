using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Панель задач: кнопка «Пуск» с меню (выключить компьютер), кнопки открытых окон и часы.
// Создаётся из DesktopOS на объекте TaskBar.
public class TaskbarOS : MonoBehaviour {
    const float StartWidth = 58f;
    const float ClockWidth = 46f;
    const float WindowButtonWidth = 112f;

    DesktopOS os;
    RectTransform bar, buttonsArea;
    GameObject menu;
    TextMeshProUGUI clock;
    readonly Dictionary<OSWindow, RectTransform> windowButtons = new();
    float nextClockUpdate;

    public static TaskbarOS Create(DesktopOS os, RectTransform bar) {
        var tb = bar.gameObject.AddComponent<TaskbarOS>();
        tb.os = os;
        tb.bar = bar;
        tb.Build();
        return tb;
    }

    void Build() {
        float h = bar.rect.height;
        float fs = os.TaskbarFontSize;

        // Пуск
        var start = OSUI.Button("Start", bar, os.StartButtonText, os.Font, fs, os.StartButtonColor, Color.white, ToggleMenu);
        var srt = (RectTransform)start.transform;
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0, 0.5f);
        srt.sizeDelta = new Vector2(StartWidth, h - 4);
        srt.anchoredPosition = new Vector2(2, 0);

        // Часы
        clock = OSUI.Text("Clock", bar, "", os.Font, fs, os.TaskbarTextColor, TextAlignmentOptions.Center);
        var crt = clock.rectTransform;
        crt.anchorMin = new Vector2(1, 0);
        crt.anchorMax = Vector2.one;
        crt.pivot = new Vector2(1, 0.5f);
        crt.offsetMin = new Vector2(-ClockWidth - 2, 2);
        crt.offsetMax = new Vector2(-2, -2);

        // Кнопки окон
        buttonsArea = OSUI.Stretch(OSUI.Rect("Windows", bar), StartWidth + 8, 2, ClockWidth + 8, 2);
        var layout = buttonsArea.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 3;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        BuildMenu(h);
        Refresh();
    }

    void BuildMenu(float barHeight) {
        var menuImg = OSUI.Image("StartMenu", os.transform, os.WindowBorderColor);
        var mrt = menuImg.rectTransform;
        mrt.anchorMin = mrt.anchorMax = mrt.pivot = Vector2.zero;
        mrt.sizeDelta = new Vector2(190, 82);
        mrt.anchoredPosition = new Vector2(2, barHeight);
        menu = menuImg.gameObject;

        // Шапка с именем пользователя
        var header = OSUI.TopBar(OSUI.Image("Header", mrt, os.TitleBarColor).rectTransform, 26, 2, 2);
        var user = OSUI.Text("User", header, os.UserName, os.Font, os.TitleFontSize, os.TitleTextColor);
        OSUI.Stretch(user.rectTransform, 8, 0, 4, 0);

        var body = OSUI.Image("Body", mrt, os.WindowColor).rectTransform;
        OSUI.Stretch(body, 2, 2, 2, 30);

        Item(body, 0, Loc.T("Выключить компьютер", "Shut down"), () => { HideMenu(); os.ShutDown(); });

        menu.SetActive(false);
    }

    void Item(RectTransform parent, int index, string label, UnityEngine.Events.UnityAction onClick) {
        var b = OSUI.Button("Item_" + label, parent, label, os.Font, os.TextFontSize, os.WindowColor, os.TextColor, onClick);
        var brt = (RectTransform)b.transform;
        OSUI.TopBar(brt, 26, 6 + index * 30, 4);
        var text = b.GetComponentInChildren<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.MidlineLeft;
        OSUI.Stretch(text.rectTransform, 8, 0, 4, 0);
        // Подсветка при наведении
        var colors = b.colors;
        colors.highlightedColor = os.SelectionColor;
        b.colors = colors;
    }

    public void ToggleMenu() {
        if (menu.activeSelf) HideMenu();
        else {
            menu.transform.SetAsLastSibling();
            menu.SetActive(true);
        }
    }

    public void HideMenu() {
        if (menu != null) menu.SetActive(false);
    }

    // Перерисовать кнопки окон (вызывается из DesktopOS при любом изменении окон)
    public void Refresh() {
        if (buttonsArea == null) return;
        OSUI.Clear(buttonsArea);
        windowButtons.Clear();

        foreach (var w in os.Windows) {
            if (w == null) continue;
            var window = w;
            bool active = window.IsTopmost;
            var b = OSUI.Button("Task_" + window.Title, buttonsArea, window.Title, os.Font, os.TaskbarFontSize,
                                active ? os.TaskbarActiveButtonColor : os.TaskbarButtonColor, os.TaskbarTextColor,
                                () => { if (window.IsTopmost) window.Minimize(); else window.Focus(); });
            ((RectTransform)b.transform).sizeDelta = new Vector2(WindowButtonWidth, 0);
            windowButtons[window] = (RectTransform)b.transform;

            var text = b.GetComponentInChildren<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.MidlineLeft;
            OSUI.Stretch(text.rectTransform, window.Icon != null ? 20 : 4, 0, 3, 0);

            if (window.Icon != null) {
                var icon = OSUI.Image("Icon", b.transform, Color.white, window.Icon, false);
                var irt = icon.rectTransform;
                irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0, 0.5f);
                irt.sizeDelta = new Vector2(16, 16);
                irt.anchoredPosition = new Vector2(2, 0);
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsArea); // чтобы позиции кнопок были известны сразу (для анимации)
    }

    // Мировая точка кнопки окна — туда «улетает» окно при сворачивании
    public Vector3 ButtonWorldPos(OSWindow w) =>
        windowButtons.TryGetValue(w, out var b) && b != null ? b.position : bar.position;

    void OnDisable() => HideMenu();

    void Update() {
        if (Time.unscaledTime < nextClockUpdate) return;
        nextClockUpdate = Time.unscaledTime + 1f;
        clock.text = DateTime.Now.ToString("HH:mm");
    }
}
