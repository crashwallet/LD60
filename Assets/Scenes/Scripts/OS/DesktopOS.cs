using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Рабочий стол ОС: раскладывает иконки из desktopFolder и открывает окна проводника и блокнота.
// Вешается на объект Desktop (рядом с Icons, Windows и TaskBar).
public class DesktopOS : MonoBehaviour, IPointerClickHandler, IDropHandler {
    [Header("Содержимое")]
    [Tooltip("Что лежит на рабочем столе: «Мой компьютер», «Корзина», папки и файлы.")]
    [SerializeField] FolderData desktopFolder;

    [Header("Контейнеры на Desktop")]
    [SerializeField] RectTransform iconsArea;   // Icons
    [SerializeField] RectTransform windowsArea; // Windows
    [SerializeField] RectTransform taskBar;     // TaskBar

    [Header("Панель задач")]
    [SerializeField] string userName = "Пользователь";
    [SerializeField] string userNameEn = "User";
    [SerializeField] string startButtonText = "Пуск";
    [SerializeField] string startButtonTextEn = "Start";
    [SerializeField] float taskbarFontSize = 13f;
    [SerializeField] Color startButtonColor = new(0.22f, 0.58f, 0.22f);
    [SerializeField] Color taskbarButtonColor = new(0.75f, 0.88f, 0.95f);
    [SerializeField] Color taskbarActiveButtonColor = new(0.55f, 0.72f, 0.85f);
    [SerializeField] Color taskbarTextColor = Color.black;

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
    [SerializeField] Vector2 decryptorSize = new(440, 330);

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

    public string UserName => Loc.T(userName, userNameEn);
    public string StartButtonText => Loc.T(startButtonText, startButtonTextEn);
    public float TaskbarFontSize => taskbarFontSize;
    public Color StartButtonColor => startButtonColor;
    public Color TaskbarButtonColor => taskbarButtonColor;
    public Color TaskbarActiveButtonColor => taskbarActiveButtonColor;
    public Color TaskbarTextColor => taskbarTextColor;

    public HelperController Helper { get; private set; }
    public IReadOnlyList<OSWindow> Windows => openWindows;

    TaskbarOS taskbar;
    readonly List<OSWindow> openWindows = new();
    OSIcon selected;
    int cascade;

    void Awake() {
        if (font == null) font = TMP_Settings.defaultFontAsset;
        Helper = GetComponentInChildren<HelperController>(true);

        // Работаем с копией папок: перетаскивание файлов в Play Mode не должно менять ассеты в проекте
        if (desktopFolder != null) desktopFolder = CloneTree(desktopFolder, new Dictionary<FolderData, FolderData>());
        if (iconsArea == null) iconsArea = transform.Find("Icons") as RectTransform;
        if (windowsArea == null) windowsArea = transform.Find("Windows") as RectTransform;
        if (iconsArea == null || windowsArea == null) {
            Debug.LogError("[DesktopOS] Не найдены дочерние объекты Icons и/или Windows", this);
            enabled = false;
            return;
        }
        SetupIconsArea();
        BuildDesktopIcons();

        if (taskBar == null) taskBar = transform.Find("TaskBar") as RectTransform;
        if (taskBar != null) taskbar = TaskbarOS.Create(this, taskBar);
        else Debug.LogWarning("[DesktopOS] Не найден объект TaskBar — панель задач не создана", this);
    }

    // --- Пуск ---

    // Встать из-за компьютера, ОС остаётся включённой
    public void ExitSeat() {
        var seat = GetComponentInParent<MonitorInteract>(true);
        if (seat != null) seat.ExitSeat();
    }

    // Выключить: закрыть окна, при следующем входе будет загрузка заново
    public void ShutDown() {
        CloseAllWindows();
        var mos = GetComponentInParent<MonitorOS>(true);
        if (mos != null) mos.ShutDown();
        ExitSeat();
    }

    // Копия дерева папок. Одна и та же папка (например, «Мои документы» на столе и на диске C:) остаётся одной копией.
    static FolderData CloneTree(FolderData src, Dictionary<FolderData, FolderData> map) {
        if (src == null) return null;
        if (map.TryGetValue(src, out var done)) return done;
        var copy = Instantiate(src);
        copy.name = src.name;
        map[src] = copy;
        copy.folders = src.folders.Select(f => CloneTree(f, map)).ToList();
        copy.files = new List<FileData>(src.files);
        copy.apps = new List<AppData>(src.apps);
        return copy;
    }

    // --- Перетаскивание ---

    // Где лежит предмет: ищем папку, в списках которой он есть (поиск по всему дереву компьютера)
    public FolderData FindParent(Object item) => FindParent(desktopFolder, item, new HashSet<FolderData>());

    static FolderData FindParent(FolderData folder, Object item, HashSet<FolderData> seen) {
        if (folder == null || !seen.Add(folder)) return null;
        if (item is FolderData fd && folder.folders.Contains(fd)) return folder;
        if (item is FileData fl && folder.files.Contains(fl)) return folder;
        if (item is AppData ap && folder.apps.Contains(ap)) return folder;
        foreach (var sub in folder.folders) {
            var p = FindParent(sub, item, seen);
            if (p != null) return p;
        }
        return null;
    }

    public FolderData DesktopFolder => desktopFolder;

    // Переложить предмет из папки from в папку to. false — если так нельзя (в саму себя, системные папки и т.п.)
    public bool Move(Object item, FolderData from, FolderData to) {
        if (item == null || from == null || to == null || from == to) return false;
        if (to.kind == FolderKind.MyComputer) return false; // в «Мой компьютер» кладутся только диски
        if (item is FolderData f) {
            if (f.kind != FolderKind.Folder) return false;          // «Мой компьютер», корзину и диски не двигаем
            if (f == to || Contains(f, to, new HashSet<FolderData>())) return false; // папку в саму себя
            if (!from.folders.Remove(f)) return false;
            to.folders.Add(f);
        } else if (item is FileData file) {
            if (!from.files.Remove(file)) return false;
            to.files.Add(file);
        } else if (item is AppData app) {
            if (!from.apps.Remove(app)) return false;
            to.apps.Add(app);
        } else return false;

        RefreshAllViews();
        return true;
    }

    static bool Contains(FolderData root, FolderData target, HashSet<FolderData> seen) {
        if (root == null || !seen.Add(root)) return false;
        foreach (var sub in root.folders)
            if (sub == target || Contains(sub, target, seen)) return true;
        return false;
    }

    public void RefreshAllViews() {
        BuildDesktopIcons();
        foreach (var w in openWindows)
            if (w != null && w.TryGetComponent(out ExplorerWindow ex)) ex.Refresh();
    }

    // Иконку бросили на пустое место рабочего стола
    public void OnDrop(PointerEventData e) {
        var dragged = OSIcon.Dragged(e);
        if (dragged != null) Move(dragged.Item, dragged.Source, desktopFolder);
    }

    // Призрак иконки при перетаскивании рисуется поверх всего рабочего стола
    public RectTransform DragLayer => (RectTransform)transform;

    void NotifyWindowsChanged() {
        if (taskbar != null) taskbar.Refresh();
    }

    void SetupIconsArea() {
        // Иконки идут столбиком сверху вниз слева, как в Windows; область = Windows (над панелью задач)
        iconsArea.anchorMin = windowsArea.anchorMin;
        iconsArea.anchorMax = windowsArea.anchorMax;
        iconsArea.pivot = windowsArea.pivot;
        iconsArea.anchoredPosition = windowsArea.anchoredPosition;
        iconsArea.sizeDelta = windowsArea.sizeDelta;
        // Окна поверх иконок, но помощник (если он стоит ниже в иерархии) остаётся поверх окон
        if (iconsArea.GetSiblingIndex() > windowsArea.GetSiblingIndex())
            iconsArea.SetSiblingIndex(windowsArea.GetSiblingIndex());

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
            OSIcon.Create(this, iconsArea, folder.DisplayName, IconFor(folder), desktopLabelColor, () => OpenFolder(folder), folder, desktopFolder);
        }
        foreach (var f in desktopFolder.files.Where(f => f != null)) {
            var file = f;
            OSIcon.Create(this, iconsArea, file.fileName, IconFor(file), desktopLabelColor, () => OpenFile(file), file, desktopFolder);
        }
        foreach (var a in desktopFolder.apps.Where(a => a != null)) {
            var app = a;
            OSIcon.Create(this, iconsArea, app.DisplayName, IconFor(app), desktopLabelColor, () => OpenApp(app), app, desktopFolder);
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

    public Sprite IconFor(FileData f) => f.fileType == FileType.Image && f.image != null ? f.image : textFileIcon;

    public Sprite IconFor(AppData a) => a.icon != null ? a.icon : textFileIcon;

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
        if (taskbar != null) taskbar.HideMenu();
    }

    public void OpenFolder(FolderData folder) {
        // Если эта папка уже открыта в каком-то окне — просто выводим его вперёд
        var existing = openWindows.FirstOrDefault(w => w != null && w.TryGetComponent(out ExplorerWindow ex) && ex.Current == folder);
        if (existing != null) { existing.Focus(); return; }

        var window = CreateWindow(folder.DisplayName, explorerSize, folder, IconFor(folder));
        ExplorerWindow.Create(this, window, folder);
    }

    public void OpenFile(FileData file) {
        var existing = openWindows.FirstOrDefault(w => w != null && w.Key == file);
        if (existing != null) { existing.Focus(); return; }

        if (file.fileType == FileType.Image) {
            var imgWindow = CreateWindow(file.fileName + Loc.T(" - Просмотр", " - Viewer"), textViewerSize, file, IconFor(file));
            ImageViewerWindow.Create(this, imgWindow, file);
            return;
        }

        var window = CreateWindow(file.fileName + Loc.T(" - Блокнот", " - Notepad"), textViewerSize, file, IconFor(file));
        TextViewerWindow.Create(this, window, file);
    }

    public void OpenApp(AppData app) {
        switch (app.kind) {
            case AppKind.Decryptor: decryptorIcon = IconFor(app); OpenDecryptor(null); break;
        }
    }

    // Дешифратор один на компьютер: если уже открыт — подставляем текст и выводим вперёд
    public void OpenDecryptor(string sourceText) {
        var existing = openWindows.FirstOrDefault(w => w != null && w.TryGetComponent(out DecryptorWindow _));
        if (existing != null) {
            existing.GetComponent<DecryptorWindow>().SetSource(sourceText);
            existing.Focus();
            return;
        }
        var window = CreateWindow(Loc.T("Дешифратор", "Decryptor"), decryptorSize, null, decryptorIcon != null ? decryptorIcon : textFileIcon);
        DecryptorWindow.Create(this, window, sourceText);
    }

    Sprite decryptorIcon;

    OSWindow CreateWindow(string title, Vector2 size, Object key, Sprite icon) {
        // Каждое новое окно чуть смещается, чтобы не перекрывать предыдущее полностью
        Vector2 pos = new Vector2(-40 + cascade * 18, 30 - cascade * 18);
        cascade = (cascade + 1) % 6;

        var window = OSWindow.Create(this, windowsArea, title, size, pos, key, icon);
        openWindows.Add(window);
        window.Closed += () => { openWindows.Remove(window); NotifyWindowsChanged(); };
        window.Changed += NotifyWindowsChanged;
        window.MinimizeTarget = () => taskbar != null ? taskbar.ButtonWorldPos(window) : windowsArea.position;
        window.Focus(); // вызовет Changed → панель задач обновится
        return window;
    }

    public void CloseAllWindows() {
        foreach (var w in openWindows.ToList()) if (w != null) w.Close();
    }
}
