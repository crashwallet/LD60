using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Однокнопочная настройка ОС на всех компьютерах: Tools → OS → Настроить все компьютеры.
// Безопасно запускать повторно: существующие ассеты не перезаписываются, компоненты не дублируются,
// заполненные поля не трогаются.
public static class OSSetup {
    const string DataFolder = "Assets/Scenes/OS_Data";
    const string IconsFolder = "Assets/Scenes/Visual_Elements/";
    const string HelperSpritesFolder = "Assets/Scenes/office second/sprites/";
    const string FontPath = "Assets/Scenes/Fonts/VT323/VT323-Regular SDF.asset";

    static readonly string[] IconNames = { "folder", "empty_folder", "txt_file", "my_computer", "trash" };
    static readonly string[] HelperSpriteNames = {
        "question_mark_default", "question_mark_to_exclamation_mark_1", "question_mark_to_exclamation_mark_2",
        "question_mark_to_exclamation_mark_3", "exclamation_mark_default",
        "default_emotion", "angry_emotion", "funny_emotion", "peaceful_emotion", "questionary_emotion", "thinking_emotion"
    };

    class Dialogues { public HelperDialogue greeting, correct, wrong, allDone; }

    [MenuItem("Tools/OS/Настроить все компьютеры")]
    static void SetupAll() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorUtility.DisplayDialog("OS Setup", "Выйди из Play Mode и запусти ещё раз.", "OK");
            return;
        }

        var log = new List<string>();

        // 1. Спрайты
        var icons = PrepareSprites(IconsFolder, IconNames, false, log);
        var helperSprites = PrepareSprites(HelperSpritesFolder, HelperSpriteNames, true, log);

        // 2. Общие ассеты: программа, вид помощника, реплики, задания
        EnsureFolder(DataFolder);
        var decryptor = GetOrCreateAsset<AppData>($"{DataFolder}/Apps/Decryptor.asset", a => {
            a.appName = "Дешифратор";
            a.kind = AppKind.Decryptor;
        }, log);
        var look = EnsureHelperLook(helperSprites, log);
        var dialogues = EnsureDialogues(log);
        EnsureProgression(log);

        // 3. Компьютеры
        var monitors = Object.FindObjectsByType<MonitorInteract>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(m => m.name).ToArray();
        if (monitors.Length == 0) {
            log.Add("ВНИМАНИЕ: в открытой сцене нет MonitorInteract");
            Report(log);
            return;
        }

        // Образец — компьютер, на котором ОС уже собрана (BootScreen + Desktop)
        var refMi = monitors.FirstOrDefault(m => GetMonitorOS(m) != null && GetGO(GetMonitorOS(m), "desktopScreen") != null);
        var refMos = refMi != null ? GetMonitorOS(refMi) : null;
        if (refMos == null) log.Add("ВНИМАНИЕ: ни у одного монитора нет MonitorOS с Desktop — копировать не с чего");

        // Сначала образец (на нём создаётся помощник, который потом скопируется на остальные)
        foreach (var mi in monitors.OrderBy(m => m == refMi ? 0 : 1)) {
            int pc = Array.IndexOf(monitors, mi) + 1;
            SetupMonitor(mi, pc, refMi, refMos, icons, decryptor, look, dialogues, log);
        }

        var scene = monitors[0].gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Add($"Сцена {scene.name} сохранена");
        Report(log);
    }

    // ---------- Компьютер ----------

    static void SetupMonitor(MonitorInteract mi, int pc, MonitorInteract refMi, MonitorOS refMos, Dictionary<string, Sprite> icons,
                             AppData decryptor, HelperLook look, Dialogues dialogues, List<string> log) {
        string tag = $"PC{pc} ({mi.name})";
        var miSo = new SerializedObject(mi);

        var mos = GetMonitorOS(mi);
        bool cloned = false;
        if (mos == null) {
            if (refMos == null) { log.Add($"{tag}: пропущен — нет образца"); return; }
            mos = CloneOS(mi, refMos, tag, log);
            if (mos == null) return;
            miSo.FindProperty("monitorOS").objectReferenceValue = mos;
            cloned = true;
        }

        // Что выключать, пока сидим за компьютером (прицел и т.п.) — как у образца
        var dws = miSo.FindProperty("disableWhileSeated");
        if (dws != null && dws.arraySize == 0 && refMi != null && refMi != mi) {
            var refDws = new SerializedObject(refMi).FindProperty("disableWhileSeated");
            for (int i = 0; i < refDws.arraySize; i++) {
                dws.InsertArrayElementAtIndex(i);
                dws.GetArrayElementAtIndex(i).objectReferenceValue = refDws.GetArrayElementAtIndex(i).objectReferenceValue;
            }
            if (refDws.arraySize > 0) log.Add($"{tag}: скопирован список Disable While Seated");
        }
        miSo.ApplyModifiedProperties();

        var desk = GetGO(mos, "desktopScreen");
        if (desk == null) { log.Add($"{tag}: у MonitorOS не назначен Desktop Screen"); return; }

        // Содержимое этого компьютера
        var desktopFolder = EnsurePcFolders(pc, decryptor, log);

        var os = desk.GetComponent<DesktopOS>();
        if (os == null) { os = Undo.AddComponent<DesktopOS>(desk); log.Add($"{tag}: добавлен DesktopOS"); }
        var so = new SerializedObject(os);
        var folderProp = so.FindProperty("desktopFolder");
        // У копии осталась ссылка на папки PC1 — переназначаем на свои
        if (folderProp.objectReferenceValue == null || (cloned && folderProp.objectReferenceValue != desktopFolder))
            folderProp.objectReferenceValue = desktopFolder;
        SetIfEmpty(so, "iconsArea", desk.transform.Find("Icons"));
        SetIfEmpty(so, "windowsArea", desk.transform.Find("Windows"));
        SetIfEmpty(so, "folderIcon", Get(icons, "folder"));
        SetIfEmpty(so, "emptyFolderIcon", Get(icons, "empty_folder"));
        SetIfEmpty(so, "textFileIcon", Get(icons, "txt_file"));
        SetIfEmpty(so, "myComputerIcon", Get(icons, "my_computer"));
        SetIfEmpty(so, "trashIcon", Get(icons, "trash"));
        SetIfEmpty(so, "font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath));
        so.ApplyModifiedProperties();

        EnsureHelper(desk, look, dialogues, tag, log);
        ReplaceRaycaster(mos, desk, tag, log);

        // Canvas, который стоит в нулевой точке монитора без поворота, ещё не поставлен к экрану
        var canvasT = GetCanvas(mos)?.transform;
        bool unplaced = canvasT != null && canvasT.localPosition == Vector3.zero && canvasT.localRotation == Quaternion.identity;
        if (refMi != null && mi != refMi && (cloned || unplaced))
            AlignCanvasToReference(mi, refMi, tag, log);

        FixCanvasFacing(mi, mos, desk, tag, log);
    }

    // Копирует BootScreen и Desktop образца в Canvas этого монитора и вешает MonitorOS
    static MonitorOS CloneOS(MonitorInteract mi, MonitorOS refMos, string tag, List<string> log) {
        var refBoot = GetGO(refMos, "bootScreen");
        var refDesk = GetGO(refMos, "desktopScreen");
        var refCanvas = refDesk.GetComponentInParent<Canvas>(true);

        var canvas = mi.GetComponentsInChildren<Canvas>(true).FirstOrDefault(c => c != refCanvas && c.isRootCanvas);
        if (canvas == null) {
            log.Add($"{tag}: ВНИМАНИЕ — у монитора нет своего Canvas (как M1_canvas), ОС не скопирована");
            return null;
        }

        GameObject Copy(GameObject src) {
            if (src == null) return null;
            var existing = canvas.transform.Find(src.name);
            if (existing != null) return existing.gameObject;
            var copy = Object.Instantiate(src, canvas.transform, false);
            copy.name = src.name;
            copy.transform.SetSiblingIndex(Mathf.Min(src.transform.GetSiblingIndex(), canvas.transform.childCount - 1));
            Undo.RegisterCreatedObjectUndo(copy, "Clone OS");
            return copy;
        }

        var boot = Copy(refBoot);
        var desk = Copy(refDesk);

        var mos = Undo.AddComponent<MonitorOS>(mi.gameObject);
        EditorUtility.CopySerialized(refMos, mos);
        var so = new SerializedObject(mos);
        so.FindProperty("bootScreen").objectReferenceValue = boot;
        so.FindProperty("desktopScreen").objectReferenceValue = desk;
        var slider = so.FindProperty("progressSlider");
        if (slider != null) slider.objectReferenceValue = boot != null ? boot.GetComponentInChildren<Slider>(true) : null;
        var canvasProp = so.FindProperty("canvas");
        if (canvasProp != null) canvasProp.objectReferenceValue = canvas;
        var rcProp = so.FindProperty("raycaster");
        if (rcProp != null) rcProp.objectReferenceValue = canvas.GetComponent<GraphicRaycaster>();
        so.ApplyModifiedProperties();

        log.Add($"{tag}: ОС скопирована с образца в {canvas.name} (BootScreen, Desktop, MonitorOS)");
        return mos;
    }

    [MenuItem("Tools/OS/Выровнять экраны по монитору 1")]
    static void AlignScreensMenu() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorUtility.DisplayDialog("OS Setup", "Выйди из Play Mode и запусти ещё раз.", "OK");
            return;
        }
        var log = new List<string>();
        var monitors = Object.FindObjectsByType<MonitorInteract>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(m => m.name).ToArray();
        var refMi = monitors.FirstOrDefault(m => GetMonitorOS(m) != null && GetGO(GetMonitorOS(m), "desktopScreen") != null);
        if (refMi == null) { log.Add("Нет образца (монитора с MonitorOS)"); Report(log); return; }

        foreach (var mi in monitors.Where(m => m != refMi)) AlignCanvasToReference(mi, refMi, mi.name, log);

        var scene = refMi.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Add($"Сцена {scene.name} сохранена");
        Report(log);
    }

    // Ставит Canvas монитора относительно его камеры так же, как Canvas образца стоит относительно своей камеры.
    // Pivot у всех мониторов FBX в одной точке, поэтому локальные координаты образца переносить нельзя.
    static void AlignCanvasToReference(MonitorInteract mi, MonitorInteract refMi, string tag, List<string> log) {
        var mos = GetMonitorOS(mi);
        var refMos = GetMonitorOS(refMi);
        if (mos == null || refMos == null) { log.Add($"{tag}: нет MonitorOS — сначала «Настроить все компьютеры»"); return; }

        var cam = (new SerializedObject(mi).FindProperty("monitorCamera").objectReferenceValue as Component)?.transform;
        var refCam = (new SerializedObject(refMi).FindProperty("monitorCamera").objectReferenceValue as Component)?.transform;
        var canvas = GetCanvas(mos);
        var refCanvas = GetCanvas(refMos);
        if (cam == null || refCam == null || canvas == null || refCanvas == null) {
            log.Add($"{tag}: не хватает камеры или Canvas — экран не выровнен");
            return;
        }

        var desk = GetGO(mos, "desktopScreen")?.transform as RectTransform;
        var refDesk = GetGO(refMos, "desktopScreen")?.transform as RectTransform;
        if (desk == null || refDesk == null) { log.Add($"{tag}: нет Desktop — экран не выровнен"); return; }

        var t = (RectTransform)canvas.transform;
        var rt = (RectTransform)refCanvas.transform;
        Undo.RecordObject(t, "Align OS canvas");
        t.sizeDelta = rt.sizeDelta;
        t.pivot = rt.pivot;

        // Основной способ: мониторы — копии одной модели. Находим, как монитор повёрнут/сдвинут
        // относительно образца, и переносим Canvas образца тем же преобразованием.
        if (TryMatchMeshes(refMi.gameObject, mi.gameObject, out Matrix4x4 refToThis, out string why)) {
            Matrix4x4 w = refToThis * rt.localToWorldMatrix;
            ApplyLocalMatrix(t, t.parent != null ? t.parent.worldToLocalMatrix * w : w);
            log.Add($"{tag}: {canvas.name} поставлен на экран по геометрии монитора (как {refCanvas.name} у образца)");
            return;
        }
        log.Add($"{tag}: по геометрии не вышло ({why}) — выравниваю по камере");

        // 1. Та же поза относительно камеры, что у образца — через матрицы, чтобы сохранить и поворот,
        //    и знак масштаба (иначе текст может оказаться зеркальным из-за разных родителей FBX)
        Matrix4x4 camM = Matrix4x4.TRS(cam.position, cam.rotation, Vector3.one);
        Matrix4x4 refCamM = Matrix4x4.TRS(refCam.position, refCam.rotation, Vector3.one);
        Matrix4x4 world = camM * refCamM.inverse * rt.localToWorldMatrix;
        Matrix4x4 local = t.parent != null ? t.parent.worldToLocalMatrix * world : world;
        ApplyLocalMatrix(t, local);

        // 2. Расстояние до экрана: луч из камеры вперёд до монитора. Не попали — как у образца.
        var refCamComp = refCam.GetComponent<Unity.Cinemachine.CinemachineCamera>();
        var camComp = cam.GetComponent<Unity.Cinemachine.CinemachineCamera>();
        float refFov = refCamComp != null ? refCamComp.Lens.FieldOfView : 60f;
        float fov = camComp != null ? camComp.Lens.FieldOfView : refFov;

        Vector3 refCenterLocal = refCam.InverseTransformPoint(Center(refDesk));
        float refDist = Mathf.Max(0.01f, refCenterLocal.z);
        float dist = refDist;
        if (Physics.Raycast(cam.position, cam.forward, out var hit, 5f) && hit.distance > 0.05f) {
            dist = hit.distance - 0.01f; // чуть ближе стекла, чтобы экран не уходил в корпус
            log.Add($"{tag}: до экрана {hit.distance:F2} м (луч попал в {hit.collider.name})");
        } else {
            log.Add($"{tag}: луч из камеры ни во что не попал — беру расстояние как у образца ({refDist:F2} м)");
        }

        // 3. Масштаб: рабочий стол занимает ту же долю кадра, что у образца
        float refFrac = Height(refDesk) / (2f * refDist * Mathf.Tan(refFov * 0.5f * Mathf.Deg2Rad));
        float wantHeight = refFrac * 2f * dist * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        float have = Height(desk);
        if (have > 1e-6f) t.localScale *= wantHeight / have;

        // 4. Центр рабочего стола — в той же точке кадра, что у образца
        Vector3 want = cam.TransformPoint(new Vector3(refCenterLocal.x / refDist * dist, refCenterLocal.y / refDist * dist, dist));
        t.position += want - Center(desk);

        log.Add($"{tag}: {canvas.name} выровнен по {cam.name} (как {refCanvas.name} у {refCam.name})");
    }

    // Ищет жёсткое преобразование (поворот + сдвиг), которое переводит меш образца в меш этого монитора.
    // Работает, если мониторы — копии (одинаковое число и порядок вершин), что обычно для дубликатов из Blender.
    static bool TryMatchMeshes(GameObject refGo, GameObject go, out Matrix4x4 m, out string why) {
        m = Matrix4x4.identity;
        var a = WorldVertices(refGo);
        var b = WorldVertices(go);
        if (a == null || b == null) { why = "нет MeshFilter"; return false; }
        if (a.Length != b.Length || a.Length < 3) { why = $"разное число вершин ({a.Length} и {b.Length})"; return false; }

        // Три опорные точки, далёкие друг от друга: p0, самая дальняя от неё p1, самая дальняя от прямой p0p1 — p2
        int i0 = 0, i1 = 0, i2 = 0;
        float best = -1f;
        for (int i = 0; i < a.Length; i++) { float d = (a[i] - a[i0]).sqrMagnitude; if (d > best) { best = d; i1 = i; } }
        Vector3 axis = (a[i1] - a[i0]).normalized;
        best = -1f;
        for (int i = 0; i < a.Length; i++) {
            Vector3 v = a[i] - a[i0];
            float d = (v - Vector3.Dot(v, axis) * axis).sqrMagnitude;
            if (d > best) { best = d; i2 = i; }
        }
        if (best < 1e-8f) { why = "вырожденный меш"; return false; }

        Quaternion Frame(Vector3[] p) {
            Vector3 x = (p[i1] - p[i0]).normalized;
            Vector3 z = Vector3.Cross(x, p[i2] - p[i0]).normalized;
            return Quaternion.LookRotation(z, Vector3.Cross(z, x));
        }
        Quaternion rot = Frame(b) * Quaternion.Inverse(Frame(a));
        Vector3 shift = b[i0] - rot * a[i0];

        // Проверка: все вершины должны совпасть
        float size = (a[i1] - a[i0]).magnitude, maxErr = 0f;
        for (int i = 0; i < a.Length; i += Mathf.Max(1, a.Length / 2000))
            maxErr = Mathf.Max(maxErr, (rot * a[i] + shift - b[i]).magnitude);
        if (maxErr > size * 0.01f) { why = $"мониторы не копии (ошибка {maxErr:F3} м)"; return false; }

        m = Matrix4x4.TRS(shift, rot, Vector3.one);
        why = null;
        return true;
    }

    static Vector3[] WorldVertices(GameObject go) {
        var mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.GetComponentInChildren<MeshFilter>(true);
        if (mf == null || mf.sharedMesh == null) return null;
        var v = mf.sharedMesh.vertices;
        var l2w = mf.transform.localToWorldMatrix;
        for (int i = 0; i < v.Length; i++) v[i] = l2w.MultiplyPoint3x4(v[i]);
        return v;
    }

    // Разложить локальную матрицу в position/rotation/scale с учётом зеркальности
    static void ApplyLocalMatrix(Transform t, Matrix4x4 m) {
        Vector3 c0 = m.GetColumn(0), c1 = m.GetColumn(1), c2 = m.GetColumn(2);
        float sx = c0.magnitude, sy = c1.magnitude, sz = c2.magnitude;
        if (Vector3.Dot(Vector3.Cross(c0, c1), c2) < 0f) sx = -sx; // отражение уносим в X
        t.localPosition = m.GetColumn(3);
        t.localRotation = Quaternion.LookRotation(c2 / sz, c1 / sy);
        t.localScale = new Vector3(sx, sy, sz);
    }

    static Vector3 Center(RectTransform r) {
        var c = new Vector3[4];
        r.GetWorldCorners(c);
        return (c[0] + c[2]) * 0.5f;
    }

    static float Height(RectTransform r) {
        var c = new Vector3[4];
        r.GetWorldCorners(c);
        return Vector3.Distance(c[0], c[1]);
    }

    static Canvas GetCanvas(MonitorOS mos) {
        var c = new SerializedObject(mos).FindProperty("canvas")?.objectReferenceValue as Canvas;
        if (c != null) return c;
        var desk = GetGO(mos, "desktopScreen");
        return desk != null ? desk.GetComponentInParent<Canvas>(true) : null;
    }

    // UI виден только с «лицевой» стороны: если камера монитора смотрит на Canvas сзади — разворачиваем на 180°
    static void FixCanvasFacing(MonitorInteract mi, MonitorOS mos, GameObject desk, string tag, List<string> log) {
        var cam = new SerializedObject(mi).FindProperty("monitorCamera").objectReferenceValue as Component;
        var canvas = desk.GetComponentInParent<Canvas>(true);
        if (cam == null || canvas == null) return;

        var t = desk.transform;
        if (Vector3.Dot(t.forward, cam.transform.position - t.position) > 0f) {
            Undo.RecordObject(canvas.transform, "Flip OS canvas");
            canvas.transform.Rotate(0f, 180f, 0f, Space.Self);
            log.Add($"{tag}: {canvas.name} был повёрнут к камере обратной стороной — развернул на 180° по Y");
        }
    }

    // Обычный GraphicRaycaster не знает про ЭЛТ-искажение (Lens Distortion / Panini) и промахивается.
    static void ReplaceRaycaster(MonitorOS mos, GameObject desk, string tag, List<string> log) {
        var mosSo = new SerializedObject(mos);
        var canvas = mosSo.FindProperty("canvas")?.objectReferenceValue as Canvas;
        if (canvas == null) canvas = desk.GetComponentInParent<Canvas>(true);
        if (canvas == null) { log.Add($"{tag}: не найден Canvas — Raycaster не заменён"); return; }

        var go = canvas.gameObject;
        var distorted = go.GetComponent<DistortedGraphicRaycaster>();
        if (distorted == null) {
            var old = go.GetComponents<GraphicRaycaster>().FirstOrDefault(r => !(r is DistortedGraphicRaycaster));
            distorted = Undo.AddComponent<DistortedGraphicRaycaster>(go);
            if (old != null) {
                distorted.ignoreReversedGraphics = old.ignoreReversedGraphics;
                distorted.blockingObjects = old.blockingObjects;
                var newSo = new SerializedObject(distorted);
                newSo.FindProperty("m_BlockingMask").intValue = new SerializedObject(old).FindProperty("m_BlockingMask").intValue;
                newSo.ApplyModifiedProperties();
                distorted.enabled = old.enabled;
                Undo.DestroyObjectImmediate(old);
            }
            log.Add($"{tag}: на {go.name} поставлен DistortedGraphicRaycaster");
        }

        var rc = mosSo.FindProperty("raycaster");
        if (rc != null && rc.objectReferenceValue != distorted) {
            rc.objectReferenceValue = distorted;
            mosSo.ApplyModifiedProperties();
        }
    }

    // ---------- Папки компьютера ----------

    static FolderData EnsurePcFolders(int pc, AppData decryptor, List<string> log) {
        // PC1 — исходные папки в корне OS_Data, остальные — в OS_Data/PC2, OS_Data/PC3...
        string dir = pc == 1 ? DataFolder : $"{DataFolder}/PC{pc}";
        EnsureFolder(dir);

        var desktop = GetOrCreateFolder(dir, "Desktop", "Рабочий стол", FolderKind.Folder, out bool newDesktop, log);
        var myComp = GetOrCreateFolder(dir, "MyComputer", "Мой компьютер", FolderKind.MyComputer, out bool newMyComp, log);
        var driveC = GetOrCreateFolder(dir, "Drive_C", "Локальный диск (C:)", FolderKind.Drive, out bool newDrive, log);
        var docs = GetOrCreateFolder(dir, "Documents", "Мои документы", FolderKind.Folder, out bool newDocs, log);
        var trash = GetOrCreateFolder(dir, "Trash", "Корзина", FolderKind.Trash, out _, log);

        // Содержимое заполняем только у только что созданных папок, чтобы не затереть твои правки
        if (newDesktop) { desktop.folders = new List<FolderData> { myComp, docs, trash }; EditorUtility.SetDirty(desktop); }
        if (newMyComp) { myComp.folders = new List<FolderData> { driveC }; EditorUtility.SetDirty(myComp); }
        if (newDrive) { driveC.folders = new List<FolderData> { docs }; EditorUtility.SetDirty(driveC); }
        if (newDocs && pc == 1) {
            docs.files = AssetDatabase.FindAssets("t:FileData")
                .Select(g => AssetDatabase.LoadAssetAtPath<FileData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(f => f != null).ToList();
            EditorUtility.SetDirty(docs);
        }

        // Ярлык «Дешифратор» на рабочем столе (если программ там ещё нет совсем)
        if (decryptor != null && desktop.apps.Count == 0) {
            desktop.apps.Add(decryptor);
            EditorUtility.SetDirty(desktop);
            log.Add($"PC{pc}: на рабочий стол добавлен ярлык «Дешифратор»");
        }
        AssetDatabase.SaveAssets();
        return desktop;
    }

    static FolderData GetOrCreateFolder(string dir, string asset, string displayName, FolderKind kind, out bool created, List<string> log) {
        bool c = false;
        var f = GetOrCreateAsset<FolderData>($"{dir}/{asset}.asset", x => {
            x.folderName = displayName;
            x.kind = kind;
            c = true;
        }, log);
        created = c;
        return f;
    }

    // ---------- Помощник ----------

    static HelperLook EnsureHelperLook(Dictionary<string, Sprite> s, List<string> log) {
        var look = GetOrCreateAsset<HelperLook>($"{DataFolder}/Helper/HelperLook.asset", _ => { }, log);
        bool dirty = false;
        void Fill(ref Sprite field, string name) {
            if (field == null && s.TryGetValue(name, out var sp) && sp != null) { field = sp; dirty = true; }
        }
        Fill(ref look.questionBody, "question_mark_default");
        Fill(ref look.exclamationBody, "exclamation_mark_default");
        Fill(ref look.defaultEyes, "default_emotion");
        Fill(ref look.angryEyes, "angry_emotion");
        Fill(ref look.funnyEyes, "funny_emotion");
        Fill(ref look.peacefulEyes, "peaceful_emotion");
        Fill(ref look.questionaryEyes, "questionary_emotion");
        Fill(ref look.thinkingEyes, "thinking_emotion");
        if (look.morphFrames == null || look.morphFrames.Length == 0) {
            look.morphFrames = new[] { "question_mark_to_exclamation_mark_1", "question_mark_to_exclamation_mark_2", "question_mark_to_exclamation_mark_3" }
                .Select(n => Get(s, n)).Where(x => x != null).ToArray();
            dirty |= look.morphFrames.Length > 0;
        }
        if (dirty) { EditorUtility.SetDirty(look); AssetDatabase.SaveAssets(); }
        return look;
    }

    static Dialogues EnsureDialogues(List<string> log) {
        HelperDialogue D(string name, params HelperLine[] lines) =>
            GetOrCreateAsset<HelperDialogue>($"{DataFolder}/Helper/{name}.asset", d => d.lines = lines.ToList(), log);
        HelperLine L(string text, HelperEmotion e, HelperShape s) => new() { text = text, emotion = e, shape = s };

        return new Dialogues {
            greeting = D("Greeting",
                L("Привет! Я твой помощник.", HelperEmotion.Funny, HelperShape.Question),
                L("Найдёшь зашифрованный файл — открой его в «Дешифраторе». Я подскажу, что делать.", HelperEmotion.Peaceful, HelperShape.Exclamation)),
            correct = D("CorrectAnswer",
                L("Отлично! Так держать!", HelperEmotion.Funny, HelperShape.Exclamation)),
            wrong = D("WrongAnswer",
                L("Хм... Не похоже. Проверь шифр и ключ ещё раз.", HelperEmotion.Thinking, HelperShape.Question)),
            allDone = D("AllTasksDone",
                L("Кажется, на сегодня всё. Спасибо за помощь!", HelperEmotion.Peaceful, HelperShape.Exclamation)),
        };
    }

    static void EnsureHelper(GameObject desk, HelperLook look, Dialogues dlg, string tag, List<string> log) {
        var ctrl = desk.GetComponentInChildren<HelperController>(true);
        if (ctrl == null) {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var root = NewRect("Helper", desk.transform);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1, 0);
            root.sizeDelta = new Vector2(48, 72);
            root.anchoredPosition = new Vector2(-10, 32); // над панелью задач, в правом нижнем углу
            root.SetAsLastSibling();                       // поверх иконок и окон

            var visual = Stretch(NewRect("Visual", root));
            var body = Stretch(NewRect("Body", visual)).gameObject.AddComponent<Image>();
            body.preserveAspect = true;
            var eyes = NewRect("Eyes", body.transform).gameObject.AddComponent<Image>();
            eyes.preserveAspect = true;
            eyes.raycastTarget = false;

            var bubbleRt = NewRect("Bubble", root);
            bubbleRt.anchorMin = bubbleRt.anchorMax = new Vector2(0, 1);
            bubbleRt.pivot = new Vector2(1, 0);
            bubbleRt.sizeDelta = new Vector2(200, 74);
            bubbleRt.anchoredPosition = new Vector2(6, -12);
            var bubbleImg = bubbleRt.gameObject.AddComponent<Image>();
            bubbleImg.color = new Color(1f, 1f, 0.88f);
            var outline = bubbleRt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.1f, 0.1f, 0.1f);
            outline.effectDistance = new Vector2(1, -1);

            var textRt = Stretch(NewRect("Text", bubbleRt), 6, 4, 6, 4);
            var text = textRt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = "Привет! Я твой помощник.";
            text.color = Color.black;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8;
            text.fontSizeMax = 14;
            text.raycastTarget = false;

            var avatar = root.gameObject.AddComponent<HelperAvatar>();
            var aso = new SerializedObject(avatar);
            aso.FindProperty("look").objectReferenceValue = look;
            aso.FindProperty("visual").objectReferenceValue = visual;
            aso.FindProperty("body").objectReferenceValue = body;
            aso.FindProperty("eyes").objectReferenceValue = eyes;
            aso.ApplyModifiedPropertiesWithoutUndo();

            ctrl = root.gameObject.AddComponent<HelperController>();
            var cso = new SerializedObject(ctrl);
            cso.FindProperty("avatar").objectReferenceValue = avatar;
            cso.FindProperty("bubble").objectReferenceValue = bubbleRt.gameObject;
            cso.FindProperty("bubbleText").objectReferenceValue = text;
            cso.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root.gameObject, "Create Helper");
            avatar.Apply();
            log.Add($"{tag}: создан помощник (Desktop/Helper)");
        }

        var so = new SerializedObject(ctrl);
        SetIfEmpty(so, "greeting", dlg.greeting);
        SetIfEmpty(so, "correctAnswer", dlg.correct);
        SetIfEmpty(so, "wrongAnswer", dlg.wrong);
        SetIfEmpty(so, "allTasksDone", dlg.allDone);
        so.ApplyModifiedProperties();

        var av = ctrl.GetComponent<HelperAvatar>();
        if (av != null) {
            var aso = new SerializedObject(av);
            SetIfEmpty(aso, "look", look);
            aso.ApplyModifiedProperties();
        }
    }

    // ---------- Задания ----------

    static void EnsureProgression(List<string> log) {
        if (Object.FindAnyObjectByType<ProgressionManager>(FindObjectsInactive.Include) != null) return;

        var tasks = AssetDatabase.FindAssets("t:TaskData")
            .Select(g => AssetDatabase.GUIDToAssetPath(g)).OrderBy(p => p)
            .Select(p => AssetDatabase.LoadAssetAtPath<TaskData>(p)).Where(t => t != null).ToList();

        if (tasks.Count == 0) {
            var file = AssetDatabase.FindAssets("t:FileData")
                .Select(g => AssetDatabase.LoadAssetAtPath<FileData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(f => f != null)
                .OrderByDescending(f => f.encrypted).FirstOrDefault();
            if (file != null) {
                var task = GetOrCreateAsset<TaskData>($"{DataFolder}/Tasks/Task_01_Example.asset", t => {
                    t.clippyRequest = $"Смотри, в «Моих документах» лежит зашифрованный файл {file.fileName}. Открой его и расшифруй в «Дешифраторе»!";
                    t.answerQuestion = $"Что написано в {file.fileName}?";
                    t.file = file;
                    t.cipher = file.cipher;
                    t.expectedAnswer = (file.plainText ?? "").Trim().Trim('"', '«', '»').Trim();
                    t.accessGranted = 1;
                    t.hiddenEffectLog = "ПРИМЕР: вирус получил доступ к первому компьютеру.";
                }, log);
                tasks.Add(task);
            }
        }

        var go = new GameObject("ProgressionManager");
        Undo.RegisterCreatedObjectUndo(go, "Create ProgressionManager");
        var pm = go.AddComponent<ProgressionManager>();
        var so = new SerializedObject(pm);
        var arr = so.FindProperty("tasks");
        arr.arraySize = tasks.Count;
        for (int i = 0; i < tasks.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = tasks[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        log.Add($"Создан ProgressionManager, заданий: {tasks.Count}");
    }

    // ---------- Утилиты ----------

    static Dictionary<string, Sprite> PrepareSprites(string folder, string[] names, bool pixelArt, List<string> log) {
        var result = new Dictionary<string, Sprite>();
        foreach (var n in names) {
            string path = folder + n + ".png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter imp) {
                log.Add($"ВНИМАНИЕ: не найден спрайт {path}");
                continue;
            }
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite) {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                changed = true;
            }
            if (pixelArt && imp.filterMode != FilterMode.Point) {
                imp.filterMode = FilterMode.Point;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.mipmapEnabled = false;
                changed = true;
            }
            if (changed) { imp.SaveAndReimport(); log.Add($"{n}.png → Sprite"); }
            result[n] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return result;
    }

    static T GetOrCreateAsset<T>(string path, Action<T> init, List<string> log) where T : ScriptableObject {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        a = ScriptableObject.CreateInstance<T>();
        init(a);
        AssetDatabase.CreateAsset(a, path);
        log.Add($"Создан {path}");
        return a;
    }

    static void EnsureFolder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static MonitorOS GetMonitorOS(MonitorInteract mi) =>
        new SerializedObject(mi).FindProperty("monitorOS").objectReferenceValue as MonitorOS;

    static GameObject GetGO(Object o, string prop) =>
        new SerializedObject(o).FindProperty(prop)?.objectReferenceValue as GameObject;

    static Sprite Get(Dictionary<string, Sprite> d, string name) => d.TryGetValue(name, out var s) ? s : null;

    static RectTransform NewRect(string name, Transform parent) {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0) {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
        return rt;
    }

    static void SetIfEmpty(SerializedObject so, string prop, Object value) {
        var p = so.FindProperty(prop);
        if (p != null && p.objectReferenceValue == null && value != null) p.objectReferenceValue = value;
    }

    static void Report(List<string> log) {
        string text = string.Join("\n", log);
        Debug.Log("[OS Setup]\n" + text);
        EditorUtility.DisplayDialog("OS Setup", text.Length > 1500 ? text.Substring(0, 1500) + "\n… (полный список в Console)" : text, "OK");
    }
}
