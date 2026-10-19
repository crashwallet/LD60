using UnityEngine;

// Что передаётся между сценами
public static class GameFlow {
    public const string MenuScene = "MainMenu";
    public const string GameScene = "SampleScene";

    // Нажали Play в главном меню — в игровой сцене проиграть вступление
    public static bool PlayIntro;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => PlayIntro = false;
}
