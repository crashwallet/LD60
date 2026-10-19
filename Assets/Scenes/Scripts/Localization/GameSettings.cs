using UnityEngine;

// Общая громкость игры (AudioListener.volume), сохраняется в PlayerPrefs и применяется при старте любой сцены
public static class GameSettings {
    const string VolumeKey = "game.volume";

    public static float Volume {
        get => PlayerPrefs.GetFloat(VolumeKey, 0.8f);
        set {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, value);
            PlayerPrefs.Save();
            AudioListener.volume = value;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply() => AudioListener.volume = Volume;
}
