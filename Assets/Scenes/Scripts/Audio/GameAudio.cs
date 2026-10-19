using UnityEngine;

// Фон офиса (гул + жужжание ламп) и шаги игрока. Создаётся сам в сцене, где есть игрок.
// Шаги считаются по реальному перемещению — поэтому звучат и при обычной ходьбе, и в катсценах.
public class GameAudio : MonoBehaviour {
    [SerializeField] float stepInterval = 0.5f;
    [SerializeField] float minSpeed = 0.6f;

    SoundBank bank;
    Transform player;
    AudioSource steps;
    Vector3 lastPos;
    float stepTimer;

    // AfterSceneLoad срабатывает только для ПЕРВОЙ сцены (главного меню), поэтому подписываемся на загрузку каждой сцены
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) {
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc == null || FindAnyObjectByType<GameAudio>() != null) return;
        var go = new GameObject("[GameAudio]");
        var ga = go.AddComponent<GameAudio>();
        ga.player = fpc.transform;
    }

    void Start() {
        bank = SoundBank.Get();
        if (bank == null) { enabled = false; return; }

        Loop(bank.officeAmbience, bank.ambienceVolume);
        Loop(bank.lightBuzz, bank.buzzVolume);

        if (player != null) {
            steps = player.gameObject.AddComponent<AudioSource>();
            steps.spatialBlend = 0f;
            steps.playOnAwake = false;
            lastPos = player.position;
        }
    }

    void Loop(AudioClip clip, float volume) {
        if (clip == null) return;
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.volume = volume;
        s.spatialBlend = 0f;
        s.Play();
    }

    void Update() {
        if (player == null || steps == null) return;
        Vector3 d = player.position - lastPos;
        d.y = 0f;
        lastPos = player.position;
        float speed = Time.deltaTime > 0 ? d.magnitude / Time.deltaTime : 0f;

        if (speed < minSpeed || speed > 20f) { stepTimer = stepInterval * 0.5f; return; } // стоим (или телепорт)
        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f) return;
        stepTimer = stepInterval;

        var clip = bank.RandomFootstep();
        if (clip == null) return;
        steps.pitch = Random.Range(0.92f, 1.08f);
        steps.PlayOneShot(clip, bank.footstepVolume);
    }
}
