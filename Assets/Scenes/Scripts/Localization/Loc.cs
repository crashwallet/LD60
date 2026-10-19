using System;
using UnityEngine;

public enum GameLanguage { Russian = 0, English = 1 }

// Простая локализация: строки пишутся парой Loc.T("русский", "english").
// Язык выбирается в главном меню (Options) и хранится в PlayerPrefs.
public static class Loc {
    const string PrefKey = "game.language";

    static bool loaded;
    static GameLanguage language;

    public static event Action Changed;

    public static GameLanguage Language {
        get { Load(); return language; }
        set {
            Load();
            if (language == value) return;
            language = value;
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public static bool IsEnglish => Language == GameLanguage.English;

    // Русский текст или английский (если английского нет — показываем русский)
    public static string T(string ru, string en) => IsEnglish && !string.IsNullOrEmpty(en) ? en : ru;

    static void Load() {
        if (loaded) return;
        loaded = true;
        language = PlayerPrefs.HasKey(PrefKey)
            ? (GameLanguage)PlayerPrefs.GetInt(PrefKey)
            : Application.systemLanguage == SystemLanguage.Russian ? GameLanguage.Russian : GameLanguage.English;
    }

    // В проекте отключена перезагрузка домена — сбрасываем статику при каждом входе в Play Mode
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() {
        loaded = false;
        Changed = null;
    }
}
