using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Однокнопочная настройка ОС: Tools → OS → Настроить рабочий стол.
// Безопасно запускать повторно: существующие ассеты не перезаписываются, компонент не дублируется.
public static class OSSetup {
    const string DataFolder = "Assets/Scenes/OS_Data";
    const string IconsFolder = "Assets/Scenes/Visual_Elements/";
    const string FontPath = "Assets/Scenes/Fonts/VT323/VT323-Regular SDF.asset";

    [MenuItem("Tools/OS/Настроить рабочий стол")]
    static void Setup() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorUtility.DisplayDialog("OS Setup", "Выйди из Play Mode и запусти ещё раз.", "OK");
            return;
        }

        var log = new List<string>();

        // 1. Иконки → Sprite
        var sprites = new Dictionary<string, Sprite>();
        foreach (var n in new[] { "folder", "empty_folder", "txt_file", "my_computer", "trash" }) {
            string path = IconsFolder + n + ".png";
            if (AssetImporter.GetAtPath(path) is TextureImporter imp) {
                if (imp.textureType != TextureImporterType.Sprite) {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                    log.Add($"Иконка {n}.png переведена в Sprite");
                }
                sprites[n] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            } else {
                log.Add($"ВНИМАНИЕ: не найдена иконка {path}");
            }
        }

        // 2. Папки (ScriptableObject)
        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets/Scenes", "OS_Data");

        var desktop  = GetOrCreate("Desktop",    "Рабочий стол",        FolderKind.Folder,     out bool newDesktop, log);
        var myComp   = GetOrCreate("MyComputer", "Мой компьютер",       FolderKind.MyComputer, out bool newMyComp,  log);
        var driveC   = GetOrCreate("Drive_C",    "Локальный диск (C:)", FolderKind.Drive,      out bool newDrive,   log);
        var docs     = GetOrCreate("Documents",  "Мои документы",       FolderKind.Folder,     out bool newDocs,    log);
        var trash    = GetOrCreate("Trash",      "Корзина",             FolderKind.Trash,      out _,               log);

        // Содержимое заполняем только у только что созданных папок, чтобы не затереть твои правки
        if (newDesktop) Fill(desktop, new[] { myComp, docs, trash });
        if (newMyComp)  Fill(myComp,  new[] { driveC });
        if (newDrive)   Fill(driveC,  new[] { docs });
        if (newDocs) {
            docs.files = AssetDatabase.FindAssets("t:FileData")
                .Select(g => AssetDatabase.LoadAssetAtPath<FileData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(f => f != null).ToList();
            EditorUtility.SetDirty(docs);
            log.Add($"В «Мои документы» добавлено файлов: {docs.files.Count}");
        }
        AssetDatabase.SaveAssets();

        // 3. DesktopOS на объект Desktop (берём его из MonitorOS.desktopScreen)
        var monitors = Object.FindObjectsByType<MonitorOS>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (monitors.Length == 0) {
            log.Add("ВНИМАНИЕ: в открытой сцене нет MonitorOS — компонент не добавлен");
            Report(log);
            return;
        }

        foreach (var mos in monitors) {
            var deskGo = new SerializedObject(mos).FindProperty("desktopScreen").objectReferenceValue as GameObject;
            if (deskGo == null) {
                log.Add($"ВНИМАНИЕ: у {mos.name} не назначен Desktop Screen");
                continue;
            }

            var os = deskGo.GetComponent<DesktopOS>();
            if (os == null) {
                os = Undo.AddComponent<DesktopOS>(deskGo);
                log.Add($"DesktopOS добавлен на {deskGo.name}");
            } else {
                log.Add($"DesktopOS уже был на {deskGo.name} — обновил ссылки");
            }

            var so = new SerializedObject(os);
            SetIfEmpty(so, "desktopFolder", desktop);
            SetIfEmpty(so, "iconsArea", deskGo.transform.Find("Icons"));
            SetIfEmpty(so, "windowsArea", deskGo.transform.Find("Windows"));
            SetIfEmpty(so, "folderIcon", Icon(sprites, "folder"));
            SetIfEmpty(so, "emptyFolderIcon", Icon(sprites, "empty_folder"));
            SetIfEmpty(so, "textFileIcon", Icon(sprites, "txt_file"));
            SetIfEmpty(so, "myComputerIcon", Icon(sprites, "my_computer"));
            SetIfEmpty(so, "trashIcon", Icon(sprites, "trash"));
            SetIfEmpty(so, "font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));
            so.ApplyModifiedProperties();

            if (deskGo.transform.Find("Icons") == null || deskGo.transform.Find("Windows") == null)
                log.Add($"ВНИМАНИЕ: у {deskGo.name} нет дочерних Icons/Windows");

            ReplaceRaycaster(mos, deskGo, log);

            EditorSceneManager.MarkSceneDirty(deskGo.scene);
            EditorSceneManager.SaveScene(deskGo.scene);
            log.Add($"Сцена {deskGo.scene.name} сохранена");
            Selection.activeGameObject = deskGo;
        }

        Report(log);
    }

    // Обычный GraphicRaycaster не знает про ЭЛТ-искажение (Lens Distortion / Panini) и промахивается.
    // Меняем его на DistortedGraphicRaycaster с теми же настройками и переназначаем в MonitorOS.
    static void ReplaceRaycaster(MonitorOS mos, GameObject deskGo, List<string> log) {
        var mosSo = new SerializedObject(mos);
        var canvasProp = mosSo.FindProperty("canvas");
        var raycasterProp = mosSo.FindProperty("raycaster");

        var canvas = canvasProp?.objectReferenceValue as Canvas;
        if (canvas == null) canvas = deskGo.GetComponentInParent<Canvas>(true);
        if (canvas == null) {
            log.Add($"ВНИМАНИЕ: не найден Canvas для {deskGo.name} — Raycaster не заменён");
            return;
        }

        var go = canvas.gameObject;
        var distorted = go.GetComponent<DistortedGraphicRaycaster>();
        if (distorted == null) {
            var old = go.GetComponents<UnityEngine.UI.GraphicRaycaster>().FirstOrDefault(r => !(r is DistortedGraphicRaycaster));
            distorted = Undo.AddComponent<DistortedGraphicRaycaster>(go);
            if (old != null) {
                distorted.ignoreReversedGraphics = old.ignoreReversedGraphics;
                distorted.blockingObjects = old.blockingObjects;
                var oldSo = new SerializedObject(old);
                var newSo = new SerializedObject(distorted);
                newSo.FindProperty("m_BlockingMask").intValue = oldSo.FindProperty("m_BlockingMask").intValue;
                newSo.ApplyModifiedProperties();
                distorted.enabled = old.enabled;
                Undo.DestroyObjectImmediate(old);
            }
            log.Add($"На {go.name} GraphicRaycaster заменён на DistortedGraphicRaycaster");
        } else {
            log.Add($"На {go.name} уже стоит DistortedGraphicRaycaster");
        }

        if (raycasterProp != null) {
            raycasterProp.objectReferenceValue = distorted;
            mosSo.ApplyModifiedProperties();
        }
    }

    static FolderData GetOrCreate(string asset, string displayName, FolderKind kind, out bool created, List<string> log) {
        string path = $"{DataFolder}/{asset}.asset";
        var f = AssetDatabase.LoadAssetAtPath<FolderData>(path);
        created = f == null;
        if (created) {
            f = ScriptableObject.CreateInstance<FolderData>();
            f.folderName = displayName;
            f.kind = kind;
            AssetDatabase.CreateAsset(f, path);
            log.Add($"Создана папка {path}");
        }
        return f;
    }

    static void Fill(FolderData folder, FolderData[] children) {
        folder.folders = children.ToList();
        EditorUtility.SetDirty(folder);
    }

    static Sprite Icon(Dictionary<string, Sprite> sprites, string name) =>
        sprites.TryGetValue(name, out var s) ? s : null;

    static void SetIfEmpty(SerializedObject so, string prop, Object value) {
        var p = so.FindProperty(prop);
        if (p != null && p.objectReferenceValue == null && value != null) p.objectReferenceValue = value;
    }

    static void Report(List<string> log) {
        string text = string.Join("\n", log);
        Debug.Log("[OS Setup]\n" + text);
        EditorUtility.DisplayDialog("OS Setup", text, "OK");
    }
}
