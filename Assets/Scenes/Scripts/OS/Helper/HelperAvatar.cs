using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Конструктор вида помощника: собирает тело и глаза из HelperLook.
// Меняй Emotion/Shape в Inspector — превью обновляется прямо в редакторе.
[ExecuteAlways]
public class HelperAvatar : MonoBehaviour {
    [SerializeField] HelperLook look;
    [SerializeField] RectTransform visual; // покачивается в Play Mode
    [SerializeField] Image body;
    [SerializeField] Image eyes;

    [Header("Превью / текущее состояние")]
    [SerializeField] HelperEmotion emotion = HelperEmotion.Default;
    [SerializeField] HelperShape shape = HelperShape.Question;

    [Header("Покачивание")]
    [SerializeField] float bobAmplitude = 2f;
    [SerializeField] float bobSpeed = 2f;

    Coroutine morph;
    Vector2 visualBasePos;

    public HelperEmotion Emotion => emotion;
    public HelperShape Shape => shape;

    void OnEnable() {
        if (visual != null) visualBasePos = visual.anchoredPosition;
        Apply();
    }

    void OnDisable() {
        if (visual != null) visual.anchoredPosition = visualBasePos;
        morph = null;
    }

#if UNITY_EDITOR
    void OnValidate() {
        // Менять другие компоненты прямо в OnValidate нельзя — откладываем на следующий тик редактора
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(); };
    }
#endif

    void Update() {
        if (!Application.isPlaying || visual == null || bobAmplitude <= 0f) return;
        visual.anchoredPosition = visualBasePos + new Vector2(0, Mathf.Sin(Time.time * bobSpeed) * bobAmplitude);
    }

    public void SetEmotion(HelperEmotion e) {
        emotion = e;
        if (eyes != null && look != null) eyes.sprite = look.Eyes(e);
    }

    public void SetShape(HelperShape s, bool animate = true) {
        if (s == shape && morph == null) return;
        var from = shape;
        shape = s;
        if (morph != null) { StopCoroutine(morph); morph = null; }

        if (animate && Application.isPlaying && isActiveAndEnabled && look != null && look.morphFrames != null && look.morphFrames.Length > 0 && from != s)
            morph = StartCoroutine(Morph(s));
        else
            ApplyBody();
    }

    IEnumerator Morph(HelperShape to) {
        var frames = look.morphFrames;
        bool forward = to == HelperShape.Exclamation;
        var wait = new WaitForSeconds(look.morphFrameTime);
        for (int i = 0; i < frames.Length; i++) {
            var f = frames[forward ? i : frames.Length - 1 - i];
            if (f != null) body.sprite = f;
            yield return wait;
        }
        ApplyBody();
        morph = null;
    }

    public void Apply() {
        if (look == null) return;
        ApplyBody();
        SetEmotion(emotion);
        LayoutEyes();
    }

    void ApplyBody() {
        if (body != null && look != null) body.sprite = look.Body(shape);
    }

    // Глаза привязаны к верху тела, положение и размер — в долях размера тела
    void LayoutEyes() {
        if (eyes == null || body == null) return;
        var brt = body.rectTransform;
        var ert = eyes.rectTransform;
        Vector2 size = brt.rect.size;
        ert.anchorMin = ert.anchorMax = new Vector2(0.5f, 1f);
        ert.pivot = new Vector2(0.5f, 0.5f);
        float w = size.x * look.eyesScale;
        ert.sizeDelta = new Vector2(w, w * 0.75f); // спрайты глаз 64x48
        ert.anchoredPosition = new Vector2(look.eyesOffset.x * size.x, -look.eyesOffset.y * size.y);
    }
}
