using UnityEngine;

// Выдвижной ящик стола/шкафа: смотришь на него и жмёшь ЛКМ (см. PlayerInteractor).
// Вешается на отдельный объект ящика (нужен коллайдер — Tools → Interact добавит BoxCollider сам).
public class DeskDrawer : MonoBehaviour {
    [Tooltip("Куда выезжает ящик, в мировых осях (например (0,0,1)). (0,0,0) — определить при первом открытии: в сторону игрока.")]
    [SerializeField] Vector3 openDirection = Vector3.zero;
    [Tooltip("На сколько метров выезжает. 0 — 70% глубины ящика.")]
    [SerializeField] float openDistance = 0f;
    [Tooltip("Скорость открытия (полных открытий в секунду).")]
    [SerializeField] float speed = 3f;

    [Header("Звук (необязательно)")]
    [SerializeField] AudioClip openSound;
    [SerializeField] AudioClip closeSound;
    [SerializeField, Range(0f, 1f)] float volume = 0.7f;

    bool isOpen;
    float progress;          // 0 — закрыт, 1 — открыт
    Vector3 closedLocalPos;
    Vector3 openLocalOffset; // смещение в локальных координатах родителя
    bool directionReady;

    public bool IsOpen => isOpen;

    void Awake() {
        closedLocalPos = transform.localPosition;
        if (openDirection != Vector3.zero) SetupOffset(openDirection.normalized);
    }

    public void Toggle(Vector3 viewerPosition) {
        if (!directionReady) SetupOffset(DirectionToward(viewerPosition));
        isOpen = !isOpen;

        var clip = isOpen ? openSound : closeSound;
        if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }

    void Update() {
        float target = isOpen ? 1f : 0f;
        if (Mathf.Approximately(progress, target)) return;
        progress = Mathf.MoveTowards(progress, target, speed * Time.deltaTime);
        transform.localPosition = closedLocalPos + openLocalOffset * Mathf.SmoothStep(0f, 1f, progress);
    }

    // Горизонтальная ось (родителя), ближайшая к направлению на игрока
    Vector3 DirectionToward(Vector3 viewer) {
        Vector3 to = viewer - Bounds().center;
        to.y = 0f;
        if (to.sqrMagnitude < 1e-6f) to = -transform.forward;

        Transform basis = transform.parent != null ? transform.parent : transform;
        Vector3[] axes = { basis.right, -basis.right, basis.forward, -basis.forward, basis.up, -basis.up };
        Vector3 best = Vector3.forward;
        float bestDot = float.MinValue;
        foreach (var a in axes) {
            Vector3 h = new Vector3(a.x, 0f, a.z);
            if (h.sqrMagnitude < 0.5f) continue; // ось почти вертикальная — пропускаем
            float d = Vector3.Dot(h.normalized, to.normalized);
            if (d > bestDot) { bestDot = d; best = h.normalized; }
        }
        return best;
    }

    void SetupOffset(Vector3 worldDir) {
        float dist = openDistance;
        if (dist <= 0f) {
            var b = Bounds();
            float depth = Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(worldDir.x), Mathf.Abs(worldDir.y), Mathf.Abs(worldDir.z))));
            dist = Mathf.Max(0.05f, depth * 0.7f);
        }
        Vector3 worldOffset = worldDir * dist;
        openLocalOffset = transform.parent != null ? transform.parent.InverseTransformVector(worldOffset) : worldOffset;
        directionReady = true;
    }

    Bounds Bounds() {
        var r = GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds;
        var c = GetComponent<Collider>();
        return c != null ? c.bounds : new Bounds(transform.position, Vector3.one * 0.3f);
    }
}
