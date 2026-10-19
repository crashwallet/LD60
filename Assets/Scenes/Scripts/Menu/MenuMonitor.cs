using System.Collections;
using TMPro;
using UnityEngine;

public enum MenuAction { Play = 0, Options = 1, Exit = 2 }

// Монитор в главном меню: при наведении «включается» (вспышка, свечение экрана, надпись), при уходе гаснет
public class MenuMonitor : MonoBehaviour {
    public MenuAction action;
    [SerializeField] Renderer screen;
    [SerializeField] TMP_Text label;
    [SerializeField] Color offColor = new(0.02f, 0.02f, 0.02f);
    [SerializeField] Color onColor = new(0.10f, 0.35f, 0.18f);
    [SerializeField] Color flashColor = new(0.85f, 0.95f, 0.9f);
    [SerializeField] float turnOnTime = 0.25f;

    Material mat;
    Coroutine anim;
    bool isOn;

    public bool IsOn => isOn;

    void Awake() {
        if (screen != null) {
            mat = screen.material; // своя копия материала
            SetScreen(offColor);
        }
        SetLabelAlpha(0f);
    }

    public void SetOn(bool on) {
        if (on == isOn) return;
        isOn = on;
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(on ? TurnOn() : TurnOff());
    }

    IEnumerator TurnOn() {
        // Короткая вспышка ЭЛТ, потом экран разгорается
        SetScreen(flashColor);
        yield return new WaitForSecondsRealtime(0.05f);
        SetScreen(offColor);
        yield return new WaitForSecondsRealtime(0.04f);
        for (float t = 0; t < turnOnTime; t += Time.unscaledDeltaTime) {
            float k = t / turnOnTime;
            SetScreen(Color.Lerp(offColor, onColor, k));
            SetLabelAlpha(k);
            yield return null;
        }
        SetScreen(onColor);
        SetLabelAlpha(1f);
        anim = null;
    }

    IEnumerator TurnOff() {
        var from = mat != null ? mat.GetColor("_BaseColor") : onColor;
        for (float t = 0; t < 0.12f; t += Time.unscaledDeltaTime) {
            float k = t / 0.12f;
            SetScreen(Color.Lerp(from, offColor, k));
            SetLabelAlpha(1f - k);
            yield return null;
        }
        SetScreen(offColor);
        SetLabelAlpha(0f);
        anim = null;
    }

    void Update() {
        // Лёгкое мерцание включённого экрана
        if (isOn && anim == null && mat != null)
            SetScreen(onColor * (0.93f + 0.07f * Mathf.PerlinNoise(Time.unscaledTime * 8f, transform.position.y)));
    }

    void SetScreen(Color c) {
        if (mat == null) return;
        mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
    }

    void SetLabelAlpha(float a) {
        if (label == null) return;
        var c = label.color;
        c.a = a;
        label.color = c;
    }

    void OnDestroy() {
        if (mat != null) Destroy(mat);
    }
}
