using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools → Interact → Сделать выделенные объекты ящиками
public static class InteractSetup {
    [MenuItem("Tools/Interact/Сделать выделенные объекты ящиками")]
    static void MakeDrawers() {
        var targets = Selection.gameObjects;
        if (targets.Length == 0) {
            EditorUtility.DisplayDialog("Ящики", "Выдели в Hierarchy объекты-ящики (каждый ящик — отдельный объект) и запусти ещё раз.", "OK");
            return;
        }

        int added = 0;
        foreach (var go in targets) {
            if (go.GetComponent<DeskDrawer>() == null) {
                Undo.AddComponent<DeskDrawer>(go);
                added++;
            }
            // Луч взгляда должен во что-то попасть: если коллайдера нет — коробка по размеру меша
            if (go.GetComponent<Collider>() == null && go.GetComponent<Renderer>() != null)
                Undo.AddComponent<BoxCollider>(go);
        }

        string names = string.Join(", ", targets.Select(t => t.name));
        Debug.Log($"[Interact] Ящики: {names}");
        EditorUtility.DisplayDialog("Ящики",
            $"Готово: {added} новых ящиков ({names}).\n\nНе забудь сохранить сцену (Ctrl+S).", "OK");
    }

    [MenuItem("Tools/Interact/Сделать выделенные объекты ящиками", true)]
    static bool MakeDrawersValidate() => !EditorApplication.isPlaying;
}
