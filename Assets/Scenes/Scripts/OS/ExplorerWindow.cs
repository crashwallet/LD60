using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Проводник: показывает содержимое папки, двойной клик по папке — зайти внутрь, «Назад» — вернуться
public class ExplorerWindow : MonoBehaviour {
    const float ToolbarHeight = 22f;
    const float StatusHeight = 16f;

    DesktopOS os;
    OSWindow window;
    FolderData current;
    readonly Stack<FolderData> history = new();

    Button backButton;
    TextMeshProUGUI addressText, statusText, emptyText;
    RectTransform grid;
    ScrollRect scroll;

    public FolderData Current => current;

    public static ExplorerWindow Create(DesktopOS os, OSWindow window, FolderData folder) {
        var ex = window.gameObject.AddComponent<ExplorerWindow>();
        ex.os = os;
        ex.window = window;
        ex.Build();
        ex.Navigate(folder, false);
        return ex;
    }

    void Build() {
        var c = window.Content;

        // Панель: кнопка «Назад» и адресная строка
        var toolbar = OSUI.TopBar(OSUI.Image("Toolbar", c, os.ToolbarColor).rectTransform, ToolbarHeight);
        backButton = OSUI.Button("Back", toolbar, "<", os.Font, os.TextFontSize, os.ButtonColor, os.TextColor, Back);
        var brt = (RectTransform)backButton.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0, 0.5f);
        brt.sizeDelta = new Vector2(ToolbarHeight + 4, ToolbarHeight - 4);
        brt.anchoredPosition = new Vector2(2, 0);

        var address = OSUI.Image("Address", toolbar, Color.white);
        OSUI.Stretch(address.rectTransform, ToolbarHeight + 10, 2, 2, 2);
        addressText = OSUI.Text("Path", address.transform, "", os.Font, os.TextFontSize, os.TextColor);
        OSUI.Stretch(addressText.rectTransform, 4, 0, 4, 0);

        // Сетка с иконками
        var area = OSUI.ScrollArea("Files", c, Color.white, out scroll);
        OSUI.Stretch((RectTransform)area.parent.parent, 0, StatusHeight, 0, ToolbarHeight);
        // Бросить иконку на пустое место окна — положить в открытую папку
        FolderDropZone.Add(area.parent.parent.gameObject, os, () => current);
        grid = area;
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = os.IconCellSize;
        layout.spacing = new Vector2(4, 4);
        layout.padding = new RectOffset(6, 6, 6, 6);

        emptyText = OSUI.Text("Empty", scroll.transform, Loc.T("Эта папка пуста", "This folder is empty"), os.Font, os.TextFontSize, os.MutedTextColor, TextAlignmentOptions.Center);
        OSUI.Stretch(emptyText.rectTransform);

        // Строка состояния
        var status = OSUI.Image("Status", c, os.ToolbarColor);
        var srt = status.rectTransform;
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = new Vector2(1, 0);
        srt.pivot = new Vector2(0.5f, 0);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = new Vector2(0, StatusHeight);
        statusText = OSUI.Text("Text", status.transform, "", os.Font, os.TextFontSize - 2, os.TextColor);
        OSUI.Stretch(statusText.rectTransform, 4, 0, 4, 0);
    }

    public void Navigate(FolderData folder, bool remember = true) {
        if (folder == null) return;
        if (remember && current != null) history.Push(current);
        current = folder;
        Refresh();
    }

    void Back() {
        if (history.Count > 0) {
            current = history.Pop();
            Refresh();
        }
    }

    public void Refresh() {
        OSUI.Clear(grid);

        foreach (var f in current.folders.Where(f => f != null)) {
            var folder = f;
            OSIcon.Create(os, grid, folder.DisplayName, os.IconFor(folder), os.TextColor, () => Navigate(folder), folder, current);
        }
        foreach (var f in current.files.Where(f => f != null)) {
            var file = f;
            OSIcon.Create(os, grid, file.fileName, os.IconFor(file), os.TextColor, () => os.OpenFile(file), file, current);
        }
        foreach (var a in current.apps.Where(a => a != null)) {
            var app = a;
            OSIcon.Create(os, grid, app.DisplayName, os.IconFor(app), os.TextColor, () => os.OpenApp(app), app, current);
        }

        int count = current.folders.Count(f => f != null) + current.files.Count(f => f != null) + current.apps.Count(a => a != null);
        emptyText.gameObject.SetActive(count == 0);
        statusText.text = Loc.T($"Объектов: {count}", $"Items: {count}");
        backButton.interactable = history.Count > 0;
        addressText.text = BuildPath();
        window.SetTitle(current.DisplayName);
        scroll.verticalNormalizedPosition = 1f;
    }

    string BuildPath() {
        var names = history.Reverse().Select(f => f.DisplayName).ToList();
        names.Add(current.DisplayName);
        return string.Join("\\", names);
    }
}
