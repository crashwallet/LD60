using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Tools → Game → Создать сцену главного меню
// Собирает Assets/Scenes/MainMenu.unity: камера, свет, три монитора стопкой (Play / Options / Exit),
// и ставит её первой в Build Settings (игровая сцена — второй). Визуал простой — дальше дорисуешь сам.
public static class MenuSceneBuilder {
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    const string MatFolder = "Assets/Scenes/Menu";
    const string FontPath = "Assets/Scenes/Fonts/VT323/VT323-Regular SDF.asset";

    [MenuItem("Tools/Game/Создать сцену главного меню")]
    static void Build() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorUtility.DisplayDialog("Main Menu", "Выйди из Play Mode и запусти ещё раз.", "OK");
            return;
        }
        if (System.IO.File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Main Menu", $"{ScenePath} уже есть. Пересоздать (старая будет перезаписана)?", "Пересоздать", "Отмена"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string previous = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Камера и свет
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
        cam.fieldOfView = 45f;
        camGo.transform.position = new Vector3(0f, 0.1f, -6f);

        var lightGo = new GameObject("Key Light", typeof(Light));
        var light = lightGo.GetComponent<Light>();
        light.type = LightType.Spot;
        light.range = 15f;
        light.spotAngle = 60f;
        light.intensity = 25f;
        light.color = new Color(1f, 0.92f, 0.8f);
        lightGo.transform.position = new Vector3(2.5f, 3.5f, -4f);
        lightGo.transform.LookAt(Vector3.zero);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.1f);

        // Материалы
        EnsureFolder(MatFolder);
        var bodyMat = Mat($"{MatFolder}/MenuMonitorBody.mat", "Universal Render Pipeline/Lit", new Color(0.78f, 0.74f, 0.64f));
        var screenMat = Mat($"{MatFolder}/MenuMonitorScreen.mat", "Universal Render Pipeline/Unlit", new Color(0.02f, 0.02f, 0.02f));
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // Три монитора стопкой: сверху Play, посередине Options, снизу Exit
        var defs = new (MenuAction action, string ru, string en, float y, float tilt)[] {
            (MenuAction.Play,    "ИГРАТЬ",    "PLAY",    1.25f,  4f),
            (MenuAction.Options, "НАСТРОЙКИ", "OPTIONS", 0.0f,  -3f),
            (MenuAction.Exit,    "ВЫХОД",     "EXIT",   -1.25f,  2f),
        };
        var monitors = new List<MenuMonitor>();
        foreach (var d in defs) monitors.Add(MakeMonitor(d.action, d.ru, d.en, d.y, d.tilt, bodyMat, screenMat, font));

        // Контроллер меню
        var menuGo = new GameObject("MainMenu");
        var menu = menuGo.AddComponent<MainMenu>();
        var so = new SerializedObject(menu);
        var arr = so.FindProperty("monitors");
        arr.arraySize = monitors.Count;
        for (int i = 0; i < monitors.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = monitors[i];
        so.FindProperty("font").objectReferenceValue = font;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings();

        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
        EditorUtility.DisplayDialog("Main Menu",
            $"Сцена {ScenePath} создана и стоит первой в Build Settings.\n\nОткрой её и нажми Play, чтобы проверить меню.", "OK");
    }

    static MenuMonitor MakeMonitor(MenuAction action, string ru, string en, float y, float tilt, Material body, Material screen, TMP_FontAsset font) {
        var root = new GameObject($"Monitor_{en}");
        root.transform.position = new Vector3(0f, y, 0f);
        root.transform.rotation = Quaternion.Euler(0f, tilt, 0f);

        // Корпус
        var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shell.name = "Body";
        shell.transform.SetParent(root.transform, false);
        shell.transform.localScale = new Vector3(1.7f, 1.15f, 1.1f);
        shell.GetComponent<Renderer>().sharedMaterial = body;

        // Экран (чуть впереди корпуса)
        var scr = GameObject.CreatePrimitive(PrimitiveType.Quad);
        scr.name = "Screen";
        Object.DestroyImmediate(scr.GetComponent<Collider>());
        scr.transform.SetParent(root.transform, false);
        scr.transform.localPosition = new Vector3(0f, 0.03f, -0.556f);
        scr.transform.localScale = new Vector3(1.4f, 0.9f, 1f);
        scr.GetComponent<Renderer>().sharedMaterial = screen;

        // Надпись на экране
        var textGo = new GameObject("Label", typeof(RectTransform));
        textGo.transform.SetParent(root.transform, false);
        textGo.transform.localPosition = new Vector3(0f, 0.03f, -0.565f);
        var rt = (RectTransform)textGo.transform;
        rt.sizeDelta = new Vector2(1.3f, 0.8f);
        var tmp = textGo.AddComponent<TextMeshPro>();
        if (font != null) tmp.font = font;
        tmp.fontSize = 3.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.6f, 1f, 0.7f, 0f);
        textGo.AddComponent<LocalizedText>().Set(ru, en);

        var mon = root.AddComponent<MenuMonitor>();
        var so = new SerializedObject(mon);
        so.FindProperty("action").enumValueIndex = (int)action;
        so.FindProperty("screen").objectReferenceValue = scr.GetComponent<Renderer>();
        so.FindProperty("label").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();
        return mon;
    }

    static void AddToBuildSettings() {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != GameScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(GameScenePath, true));
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Material Mat(string path, string shaderName, Color color) {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        var shader = Shader.Find(shaderName);
        if (shader == null) shader = Shader.Find("Standard");
        m = new Material(shader);
        m.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void EnsureFolder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
