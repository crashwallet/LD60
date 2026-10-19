using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools → Game → Подключить звуки
// Ищет звуки в Assets/Scenes/Sounds по именам файлов и заполняет Assets/Scenes/Resources/SoundBank.asset.
// Уже заполненные поля не трогает — можно переназначить вручную.
public static class SoundSetup {
    const string SoundsFolder = "Assets/Scenes/Sounds";
    const string BankPath = "Assets/Scenes/Resources/SoundBank.asset";

    [MenuItem("Tools/Game/Подключить звуки")]
    static void Run() {
        var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
        if (bank == null) {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/Resources")) AssetDatabase.CreateFolder("Assets/Scenes", "Resources");
            bank = ScriptableObject.CreateInstance<SoundBank>();
            AssetDatabase.CreateAsset(bank, BankPath);
        }

        var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { SoundsFolder })
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => (path: p.ToLowerInvariant(), clip: AssetDatabase.LoadAssetAtPath<AudioClip>(p)))
            .Where(x => x.clip != null).ToList();

        AudioClip Find(params string[] parts) =>
            clips.FirstOrDefault(c => parts.All(p => c.path.Contains(p))).clip;

        string log = "";
        void Set(ref AudioClip field, AudioClip value, string name) {
            if (field == null) field = value;
            log += $"{name}: {(field != null ? field.name : "— не найден")}\n";
        }

        Set(ref bank.mouseClick, Find("click", "mouse"), "Клик мыши");
        Set(ref bank.keyPress, Find("keyboard"), "Клавиша");
        Set(ref bank.diskStart, Find("disk_start"), "Диск — старт");
        Set(ref bank.diskLoop, Find("disk_mid"), "Диск — работа");
        Set(ref bank.diskEnd, Find("disk_end"), "Диск — стоп");
        Set(ref bank.crtStart, Find("degausser_start"), "ЭЛТ — включение");
        Set(ref bank.crtLoop, Find("degausser_mid"), "ЭЛТ — гул");
        Set(ref bank.crtEnd, Find("degausser_end"), "ЭЛТ — выключение");
        Set(ref bank.officeAmbience, Find("office-ambience"), "Фон офиса");
        Set(ref bank.lightBuzz, Find("light-bulb"), "Жужжание ламп");

        if (bank.footsteps == null || bank.footsteps.Length == 0)
            bank.footsteps = clips.Where(c => c.path.Contains("/walk/")).OrderBy(c => c.path).Select(c => c.clip).ToArray();
        log += $"Шаги: {bank.footsteps.Length} шт.\n";

        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        Selection.activeObject = bank;
        EditorUtility.DisplayDialog("Звуки", log + $"\nГотово: {BankPath}\nГромкость каждой группы — в инспекторе SoundBank.", "OK");
    }
}
