using UnityEngine;
using UnityEngine.InputSystem;

// Взаимодействие с предметами взглядом: луч из центра экрана, ЛКМ — открыть/закрыть ящик.
// Создаётся сам на игроке (объект с FirstPersonController), вешать вручную не нужно.
public class PlayerInteractor : MonoBehaviour {
    // Ящик, на который сейчас смотрит игрок (null — ни на какой)
    public static DeskDrawer Hovered { get; private set; }

    [Tooltip("Дальность, с которой можно открыть ящик (м).")]
    [SerializeField] float reach = 2.2f;

    FirstPersonController controller;
    readonly RaycastHit[] hits = new RaycastHit[16];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn() {
        var fpc = FindAnyObjectByType<FirstPersonController>();
        if (fpc != null && fpc.GetComponent<PlayerInteractor>() == null) fpc.gameObject.AddComponent<PlayerInteractor>();
    }

    void Awake() => controller = GetComponent<FirstPersonController>();

    void OnDisable() => Hovered = null;

    void Update() {
        Hovered = null;
        if (controller != null && !controller.enabled) return; // сидим за компьютером
        var cam = Camera.main;
        if (cam == null) return;

        Hovered = FindDrawer(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)));

        if (Hovered != null && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Hovered.Toggle(transform.position);
    }

    // Ящик часто стоит вплотную к коллайдеру стола — берём ящик, если он почти так же близко, как ближайшее попадание
    DeskDrawer FindDrawer(Ray ray) {
        int n = Physics.RaycastNonAlloc(ray, hits, reach, ~0, QueryTriggerInteraction.Ignore);
        float nearest = float.MaxValue;
        for (int i = 0; i < n; i++)
            if (!hits[i].collider.transform.IsChildOf(transform)) nearest = Mathf.Min(nearest, hits[i].distance);

        DeskDrawer best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < n; i++) {
            if (hits[i].distance > nearest + 0.05f) continue;
            var d = hits[i].collider.GetComponentInParent<DeskDrawer>();
            if (d != null && hits[i].distance < bestDist) { best = d; bestDist = hits[i].distance; }
        }
        return best;
    }
}
