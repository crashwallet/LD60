using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Рабочий стол ОС: раскладывает иконки из desktopFolder и открывает окна проводника и блокнота.
// Вешается на объект Desktop (рядом с Icons, Windows и TaskBar).
public class DesktopOS : MonoBehaviour, IPointerClickHandler {
    [Header("Содержимое")]
    [Tooltip("Что лежит на рабочем столе: «Мой компьютер», «Корзина», папки и файлы.")]
    [SerializeField] FolderData desktopFolder;

    [Header("Контейнеры на Desktop")]
    [SerializeField] RectTransform iconsArea;   // Icons
    [SerializeField] RectTransform windowsArea; // Windows

    [Header("Иконки (Texture Type = Sprite)")]
    [SerializeField] Sprite folderIcon;
    [SerializeField] Sprite emptyFolderIcon;
    [SerializeField] Sprite textFileIcon;
    [SerializeField] Sprite myComputerIcon;
    [SerializeField] Sprite trashIcon;
    [SerializeField] Sprite driveIcon;

    [Header("Шрифт и размеры")]
    [SerializeField] TMP_FontAsset font;
    [SerializeField] float iconSize = 36f;
    [SerializeField] Vector2 iconCellSize = new(72, 66);
    [SerializeField] float iconFontSize = 13f;
    [SerializeField] float textFontSize = 14f;
    [SerializeField] float titleFontSize = 14f;
    [SerializeField] Vector2 explorerSize = new(360, 250);
    [SerializeField] Vector2 textViewerSize = new(380, 270);

    [Header("Цвета")]
    [SerializeField] Color desktopLabelColor = Color.white;
    [SerializeField] Color selectionColor = new(0.2f, 0.4f, 0.9f, 0.45f);
    [SerializeField] Color windowBorderColor = new(0.0f, 0.33f, 0.9f);
    [SerializeField] Color titleBarColor = new(0.0f, 0.33f, 0.9f);
    [SerializeField] Color titleTextColor = Color.white;
    [SerializeField] Color closeButtonColor = new(0.85f, 0.25f, 0.15f);
    [SerializeField] Color windowColor = new(0.93f, 0.91f, 0.85f);
    [SerializeField] Color toolbarColor = new(0.93f, 0.91f, 0.85f);
    [SerializeField] Color buttonColor = new(0.85f, 0.85f, 0.85f);
    [SerializeField] Color textColor = Color.black;
    [SerializeField] Color mutedTextColor = new(0.45f, 0.45f, 0.45f);

    public TMP_FontAsset Font => font;
    public float IconSize => iconSize;
    public Vector2 IconCellSize => iconCellSize;
    public float IconFontSize => iconFontSize;
    public float TextFontSize => textFontSize;
    public float TitleFontSize => titleFontSize;
    public Color SelectionColor => selectionColor;
    public Color WindowBorderColor => windowBorderColor;
    public Color TitleBarColor => titleBarColor;
    public Color TitleTextColor => titleTextColor;
    public Color CloseButtonColor => closeButtonColor;
    public Color WindowColor => windowColor;
    public Color ToolbarColor => toolbarColor;
    public Color ButtonColor => buttonColor;
    public Color TextColor => textColor;
    public Color MutedTextColor => mutedTextColor;

    readonly List<OSWindow> openWindows = new();
    OSIcon selected;
    int cascade;

    void Awake() {
        if (font == null) font = TMP_Settings.defaultFontAsset;
        if (iconsArea == null) iconsArea = transform.Find("Icons") as RectTransform;
        if (windowsArea == null) windowsArea = transform.Find("Windows") as RectTransform;
        if (iconsArea == null || windowsArea == null) {
            Debug.LogError("[DesktopOS] Не найдены дочерние объекты Icons и/или Windows", this);
            enabled = false;
            return;
        }
        SetupIconsArea();
        BuildDesktopIcons();
    }

    void SetupIconsArea() {
        // Иконки идут столбиком сверху вниз слева, как в Windows; область = Windows (над панелью задач)
        iconsArea.anchorMin = windowsArea.anchorMin;
        iconsArea.anchorMax = windowsArea.anchorMax;
        iconsArea.pivot = windowsArea.pivot;
        iconsArea.anchoredPosition = windowsArea.anchoredPosition;
        iconsArea.sizeDelta = windowsArea.sizeDelta;
        windowsArea.SetAsLastSibling(); // окна поверх иконок

        var grid = iconsArea.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (grid == null) grid = iconsArea.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
        grid.cellSize = iconCellSize;
        grid.spacing = new Vector2(4, 6);
        grid.padding = new RectOffset(6, 6, 6, 6);
        grid.startCorner = UnityEngine.UI.GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = UnityEngine.UI.GridLayoutGroup.Axis.Vertical;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.Flexible;
    }

    public void BuildDesktopIcons() {
        OSUI.Clear(iconsArea);
        if (desktopFolder == null) {
            Debug.LogWarning("[DesktopOS] Не назначен desktopFolder", this);
            return;
        }
        foreach (var f in desktopFolder.folders.Where(f => f != null)) {
            var folder = f;
            OSIcon.Create(this, iconsArea, folder.folderName, IconFor(folder), desktopLabelColor, () => OpenFolder(folder));
        }
        foreach (var f in desktopFolder.files.Where(f => f != null)) {
            var file = f;
            OSIcon.Create(this, iconsArea, file.fileName, IconFor(file), desktopLabelColor, () => OpenFile(file));
        }
    }

    public Sprite IconFor(FolderData f) {
        Sprite s = f.kind switch {
            FolderKind.MyComputer => myComputerIcon,
            FolderKind.Trash => trashIcon,
            FolderKind.Drive => driveIcon,
            _ => f.IsEmpty ? emptyFolderIcon : folderIcon
        };
        return s != null ? s : folderIcon;
    }

    public Sprite IconFor(FileData f) => textFileIcon;

    public void Select(OSIcon icon) {
        if (selected == icon) return;
        if (selected != null) selected.SetSelected(false);
        selected = icon;
        if (selected != null) selected.SetSelected(true);
    }

    // Клик по пустому месту рабочего стола снимает выделение
    public void OnPointerClick(PointerEventData e) {
        if (e.pointerPressRaycast.gameObject == null || e.pointerPressRaycast.gameObject.GetComponentInParent<OSIcon>() == null)
            Select(null);
    }

    public void OpenFolder(FolderData folder) {
        // Если эта папка уже открыта в каком-то окне — просто выводим его вперёд
        var existing = openWindows.FirstOrDefault(w => w != null && w.TryGetComponent(out ExplorerWindow ex) && ex.Current == folder);
        if (existing != null) { existing.Focus(); return; }

        var window = CreateWindow(folder.folderName, explorerSize, folder);
        ExplorerWindow.Create(this, window, folder);
    }

    public void OpenFile(FileData file) {
        var existing = openWindows.FirstOrDefault(w => w != null && w.Key == file);
        if (existing != null) { existing.Focus(); return; }

        var window = CreateWindow(file.fileName + " - Блокнот", textViewerSize, file);
        TextViewerWindow.Create(this, window, file);
    }

    OSWindow CreateWindow(string title, Vector2 size, Object key) {
        // Каждое новое окно чуть смещается, чтобы не перекрывать предыдущее полностью
        Vector2 pos = new Vector2(-40 + cascade * 18, 30 - cascade * 18);
        cascade = (cascade + 1) % 6;

        var window = OSWindow.Create(this, windowsArea, title, size, pos, key);
        openWindows.Add(window);
        window.Closed += () => openWindows.Remove(window);
        window.Focus();
        return window;
    }

    public void CloseAllWindows() {
        foreach (var w in openWindows.ToList()) if (w != null) w.Close();
    }
}
