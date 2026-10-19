using UnityEngine;

// Все звуки игры в одном месте. Лежит в Resources (Assets/Scenes/Resources/SoundBank.asset),
// заполняется пунктом Tools → Game → Подключить звуки. Пустое поле — звука просто не будет.
[CreateAssetMenu(menuName = "Game/Sound Bank", fileName = "SoundBank")]
public class SoundBank : ScriptableObject {
    [Header("Компьютер")]
    public AudioClip mouseClick;
    public AudioClip keyPress;
    public AudioClip diskStart;
    [Tooltip("Петля: жёсткий диск работает во время загрузки.")]
    public AudioClip diskLoop;
    public AudioClip diskEnd;
    [Tooltip("Щелчок и размагничивание ЭЛТ при включении.")]
    public AudioClip crtStart;
    [Tooltip("Петля: гул включённого монитора.")]
    public AudioClip crtLoop;
    public AudioClip crtEnd;

    [Header("Окружение")]
    public AudioClip officeAmbience;
    public AudioClip lightBuzz;
    public AudioClip[] footsteps;

    [Header("Громкость")]
    [Range(0, 1)] public float pcVolume = 0.6f;
    [Range(0, 1)] public float ambienceVolume = 0.35f;
    [Range(0, 1)] public float buzzVolume = 0.12f;
    [Range(0, 1)] public float footstepVolume = 0.45f;

    static SoundBank cached;

    public static SoundBank Get() {
        if (cached == null) cached = Resources.Load<SoundBank>("SoundBank");
        return cached;
    }

    public AudioClip RandomFootstep() =>
        footsteps == null || footsteps.Length == 0 ? null : footsteps[Random.Range(0, footsteps.Length)];
}
