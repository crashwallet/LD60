using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools → Game → Настроить вступление
// Добавляет в открытую игровую сцену объект IntroSequence и связывает его с игроком, дверью («door»),
// трубкой («phone») и аппаратом («phone_base»).
public static class IntroSetup {
    [MenuItem("Tools/Game/Настроить вступление")]
    static void Setup() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorUtility.DisplayDialog("Intro", "Выйди из Play Mode и запусти ещё раз.", "OK");
            return;
        }

        var player = Object.FindAnyObjectByType<FirstPersonController>(FindObjectsInactive.Include);
        if (player == null) {
            EditorUtility.DisplayDialog("Intro", "В открытой сцене нет игрока (FirstPersonController). Открой игровую сцену.", "OK");
            return;
        }

        var intro = Object.FindAnyObjectByType<IntroSequence>(FindObjectsInactive.Include);
        if (intro == null) {
            var go = new GameObject("IntroSequence");
            Undo.RegisterCreatedObjectUndo(go, "Create Intro");
            intro = go.AddComponent<IntroSequence>();
        }

        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform Find(string name) => all.FirstOrDefault(t => t.name == name);

        var so = new SerializedObject(intro);
        string log = "";
        void Set(string prop, Object value, string what) {
            var p = so.FindProperty(prop);
            if (p.objectReferenceValue == null) p.objectReferenceValue = value;
            log += $"{what}: {(p.objectReferenceValue != null ? p.objectReferenceValue.name : "НЕ НАЙДЕН — перетащи вручную")}\n";
        }
        Set("player", player, "Игрок");
        Set("door", Find("door"), "Дверь (door)");
        Set("phoneHandset", Find("phone"), "Трубка (phone)");
        Set("phoneBase", Find("phone_base"), "Аппарат (phone_base)");
        Set("voidMaterial", DarkMaterial(), "Материал темноты за дверью");

        // Реплика про «программу, которой не верить» была слишком в лоб — меняем, если её не правили
        var lines = so.FindProperty("lines");
        for (int i = 0; i < lines.arraySize; i++) {
            var line = lines.GetArrayElementAtIndex(i);
            var ru = line.FindPropertyRelative("ru");
            if (ru.stringValue != "Если программа на экране начнёт с тобой... разговаривать — не верь ей.") continue;
            ru.stringValue = "Последние дни системы ведут себя... странно. Смотри внимательно, что именно ты делаешь.";
            line.FindPropertyRelative("en").stringValue = "The systems have been acting... strange lately. Pay attention to what exactly you are doing.";
            log += "Реплика звонка про «программу» заменена на более нейтральную\n";
        }
        so.ApplyModifiedProperties();

        // Направление двери по её вершинам: главная ось в горизонтальной плоскости — ширина двери.
        // (Оси меша из FBX могут не совпадать с поворотом двери в комнате — отсюда игрок шёл «боком».)
        var doorT = so.FindProperty("door").objectReferenceValue as Transform;
        if (doorT != null && BakeDoor(intro, doorT, out float yaw, out float width))
            log += $"Дверь: ширина {width:F2} м, повёрнута на {yaw:F0}° — направление входа посчитано по ней\n";

        var scene = player.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeObject = intro.gameObject;

        EditorUtility.DisplayDialog("Intro", log +
            "\nСцена сохранена. Если дверь открывается не в ту сторону или петли не с той стороны — " +
            "поставь галочки Flip Open Direction / Hinge On Other Side у IntroSequence.", "OK");
    }

    static bool BakeDoor(IntroSequence intro, Transform door, out float yawDeg, out float width) {
        yawDeg = width = 0f;
        var mf = door.GetComponent<MeshFilter>();
        if (mf == null) mf = door.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount < 3) return false;

        var l2w = mf.transform.localToWorldMatrix;
        var verts = mf.sharedMesh.vertices;
        var pts = new Vector3[verts.Length];
        Vector3 mean = Vector3.zero;
        for (int i = 0; i < verts.Length; i++) { pts[i] = l2w.MultiplyPoint3x4(verts[i]); mean += pts[i]; }
        mean /= pts.Length;

        // Ковариация в плоскости XZ → угол главной оси
        double cxx = 0, czz = 0, cxz = 0;
        foreach (var p in pts) {
            double dx = p.x - mean.x, dz = p.z - mean.z;
            cxx += dx * dx; czz += dz * dz; cxz += dx * dz;
        }
        double theta = 0.5 * System.Math.Atan2(2 * cxz, cxx - czz);
        var wAxis = new Vector3((float)System.Math.Cos(theta), 0f, (float)System.Math.Sin(theta));
        var tAxis = Vector3.Cross(Vector3.up, wAxis).normalized;

        float wMin = float.MaxValue, wMax = float.MinValue, tMin = float.MaxValue, tMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
        foreach (var p in pts) {
            float w = Vector3.Dot(p - mean, wAxis), t = Vector3.Dot(p - mean, tAxis);
            wMin = Mathf.Min(wMin, w); wMax = Mathf.Max(wMax, w);
            tMin = Mathf.Min(tMin, t); tMax = Mathf.Max(tMax, t);
            yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y);
        }
        Vector3 center = mean + wAxis * ((wMin + wMax) * 0.5f) + tAxis * ((tMin + tMax) * 0.5f);
        center.y = (yMin + yMax) * 0.5f;

        Undo.RecordObject(intro, "Bake door");
        intro.BakeDoor(center, wAxis, (wMax - wMin) * 0.5f, (tMax - tMin) * 0.5f);
        EditorUtility.SetDirty(intro);

        width = wMax - wMin;
        yawDeg = Mathf.Atan2(wAxis.x, wAxis.z) * Mathf.Rad2Deg;
        return true;
    }

    // Чёрный материал без освещения — для «темноты» за дверью
    static Material DarkMaterial() {
        const string path = "Assets/Scenes/OS_Data/Notes/DarkVoid.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes/OS_Data/Notes")) AssetDatabase.CreateFolder("Assets/Scenes/OS_Data", "Notes");
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        m = new Material(shader);
        m.SetColor("_BaseColor", Color.black);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }
}
