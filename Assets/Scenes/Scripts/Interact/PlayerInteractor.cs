using UnityEngine;
using UnityEngine.InputSystem;

// Взаимодействие взглядом: луч из центра экрана. Предмет под прицелом обводится,
// сверху пишется подсказка, ЛКМ — действие (открыть ящик, прочитать записку).
// Создаётся сам на игроке (объект с FirstPersonController), вешать вручную не нужно.
public class PlayerInteractor : MonoBehaviour {
    // Предмет, на который сейчас смотрит игрок (null — ни на какой)
    public static IInteractable Hovered { get; private set; }

    [Tooltip("Дальность взаимодействия (м).")]
    [SerializeField] float reach = 2.2f;

    FirstPersonController controller;
    readonly RaycastHit[] hits = new RaycastHit[16];
    MonoBehaviour hoveredBehaviour;
    HoverOutline hoveredOutline;
    string shownPrompt;

    // AfterSceneLoad срабатывает только для ПЕРВОЙ сцены (главного меню), поэтому подписываемся на загрузку каждой сцены
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init() {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) {
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc != null && fpc.GetComponent<PlayerInteractor>() == null) fpc.gameObject.AddComponent<PlayerInteractor>();
    }

    void Awake() => controller = GetComponent<FirstPersonController>();

    void OnDisable() => SetHovered(null);

    void Update() {
        IInteractable target = null;
        var cam = Camera.main;
        bool canAct = (controller == null || controller.enabled) && !NoteViewer.IsOpen
                      && NoteViewer.LastClosedFrame != Time.frameCount && cam != null;
        if (canAct) target = FindTarget(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)));
        SetHovered(target);

        bool lmb = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool e = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && target is PhoneInteractable; // на звонок можно ответить и E
        if (target != null && (lmb || e)) {
            target.Interact(transform.position);
            RefreshPrompt(); // «открыть» → «закрыть»
        }
    }

    void SetHovered(IInteractable target) {
        var behaviour = target as MonoBehaviour;
        if (behaviour == hoveredBehaviour) { RefreshPrompt(); return; }

        if (hoveredOutline != null) hoveredOutline.SetVisible(false);
        hoveredOutline = null;
        hoveredBehaviour = behaviour;
        Hovered = target;

        if (behaviour != null) {
            hoveredOutline = behaviour.GetComponent<HoverOutline>();
            if (hoveredOutline == null) hoveredOutline = behaviour.gameObject.AddComponent<HoverOutline>();
            hoveredOutline.SetVisible(true);
        }
        // Сначала убираем старую подсказку: RefreshPrompt не прячет её, если новой подсказки нет (null == null)
        InteractPrompt.Hide(this);
        shownPrompt = null;
        RefreshPrompt();
    }

    void RefreshPrompt() {
        string p = Hovered?.PromptText;
        if (p == shownPrompt) return;
        shownPrompt = p;
        if (p != null) InteractPrompt.Show(this, p);
        else InteractPrompt.Hide(this);
    }

    // Ящик часто стоит вплотную к коллайдеру стола — берём предмет, если он почти так же близко, как ближайшее попадание
    IInteractable FindTarget(Ray ray) {
        int n = Physics.RaycastNonAlloc(ray, hits, reach, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        for (int i = 0; i < n; i++)
            if (!hits[i].collider.transform.IsChildOf(transform)) nearest = Mathf.Min(nearest, hits[i].distance);

        // Записка важнее ящика, на котором лежит (иначе в открытом ящике её не взять)
        IInteractable best = null;
        int bestPriority = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < n; i++) {
            // Допуск 25 см: перед мелкими предметами (телефон, записка) часто стоит общий коллайдер стола
            if (hits[i].distance > nearest + 0.25f) continue;
            var it = hits[i].collider.GetComponentInParent<IInteractable>();
            if (it == null || string.IsNullOrEmpty(it.PromptText)) continue; // пустая подсказка — сейчас недоступно (телефон молчит)

            int priority = 0;
            if (it is NoteItem note) {
                var drawer = note.GetComponentInParent<DeskDrawer>();
                if (drawer != null && !drawer.IsOpen) continue; // в закрытом ящике записку не видно
                priority = 1;
            }
            if (priority > bestPriority || (priority == bestPriority && hits[i].distance < bestDist)) {
                best = it;
                bestPriority = priority;
                bestDist = hits[i].distance;
            }
        }
        return best;
    }
}
