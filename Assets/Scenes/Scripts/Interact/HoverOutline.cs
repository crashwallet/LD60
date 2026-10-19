using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

// Обводка объекта (например, монитора, к которому можно подойти). Создаёт дочернюю «оболочку» из того же меша.
public class HoverOutline : MonoBehaviour {
    [SerializeField] Color color = new(1f, 0.85f, 0.25f, 1f);
    [Tooltip("Толщина обводки в метрах.")]
    [SerializeField] float width = 0.008f;
    [Tooltip("Лёгкая пульсация толщины.")]
    [SerializeField] bool pulse = true;

    GameObject hull;
    Material material;
    bool visible;

    public void SetVisible(bool v) {
        visible = v;
        if (v && hull == null) Build();
        if (hull != null) hull.SetActive(v);
    }

    void Update() {
        if (!visible || material == null || !pulse) return;
        material.SetFloat("_Width", width * (0.8f + 0.2f * Mathf.Sin(Time.time * 4f)));
    }

    void Build() {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) {
            Debug.LogWarning($"[HoverOutline] У {name} нет MeshFilter — обводки не будет", this);
            return;
        }
        var shader = Shader.Find("Hidden/Game/OutlineHull");
        if (shader == null) {
            Debug.LogWarning("[HoverOutline] Не найден шейдер Hidden/Game/OutlineHull (Assets/Scenes/Resources/OutlineHull.shader)", this);
            return;
        }

        material = new Material(shader);
        material.SetColor("_Color", color);
        material.SetFloat("_Width", width);

        hull = new GameObject("Outline");
        hull.layer = gameObject.layer;
        hull.transform.SetParent(transform, false);
        hull.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
        var r = hull.AddComponent<MeshRenderer>();
        r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, mf.sharedMesh.subMeshCount)).ToArray();
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = LightProbeUsage.Off;
        r.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    void OnDestroy() {
        if (material != null) Destroy(material);
    }
}
