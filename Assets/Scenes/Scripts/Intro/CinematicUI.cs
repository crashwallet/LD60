using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Субтитры внизу экрана и затемнение для катсцен
public class CinematicUI : MonoBehaviour {
    static CinematicUI instance;

    Image fade;
    GameObject subtitlePanel;
    TextMeshProUGUI subtitle;
    GameObject titleRoot;
    TextMeshProUGUI title, titleSub;

    public static CinematicUI Get() {
        if (instance != null) return instance;
        var go = new GameObject("[CinematicUI]");
        instance = go.AddComponent<CinematicUI>();
        instance.Build();
        return instance;
    }

    void OnDestroy() {
        if (instance == this) instance = null;
    }

    void Build() {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 110;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Субтитры
        subtitlePanel = new GameObject("Subtitles", typeof(RectTransform));
        var prt = (RectTransform)subtitlePanel.transform;
        prt.SetParent(transform, false);
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0, 90);
        var bg = subtitlePanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.6f);
        bg.raycastTarget = false;
        var layout = subtitlePanel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 12, 12);
        layout.childControlWidth = layout.childControlHeight = true;
        var fit = subtitlePanel.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(LayoutElement));
        textGo.transform.SetParent(prt, false);
        subtitle = textGo.AddComponent<TextMeshProUGUI>();
        subtitle.fontSize = 38;
        subtitle.color = Color.white;
        subtitle.alignment = TextAlignmentOptions.Center;
        subtitle.textWrappingMode = TextWrappingModes.Normal;
        subtitle.raycastTarget = false;
        textGo.GetComponent<LayoutElement>().preferredWidth = 1300;
        subtitlePanel.SetActive(false);

        // Затемнение — поверх всего
        var fadeGo = new GameObject("Fade", typeof(RectTransform));
        var frt = (RectTransform)fadeGo.transform;
        frt.SetParent(transform, false);
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        fade = fadeGo.AddComponent<Image>();
        fade.color = new Color(0, 0, 0, 0);
        fade.raycastTarget = false;
        fadeGo.SetActive(false);

        // Титры — поверх затемнения
        titleRoot = new GameObject("Title", typeof(RectTransform));
        var trt = (RectTransform)titleRoot.transform;
        trt.SetParent(transform, false);
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        title = NewText("Main", trt, 120, new Vector2(0, 40));
        titleSub = NewText("Sub", trt, 40, new Vector2(0, -70));
        titleSub.color = new Color(1, 1, 1, 0.7f);
        titleRoot.SetActive(false);
    }

    static TextMeshProUGUI NewText(string name, RectTransform parent, float size, Vector2 pos) {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(1600, size * 1.5f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    public void ShowTitle(string main, string sub) {
        title.text = main;
        titleSub.text = sub;
        titleRoot.transform.SetAsLastSibling();
        titleRoot.SetActive(true);
    }

    public void HideTitle() => titleRoot.SetActive(false);

    public void ShowSubtitle(string text) {
        subtitle.text = text;
        subtitlePanel.SetActive(!string.IsNullOrEmpty(text));
    }

    public void HideSubtitle() => subtitlePanel.SetActive(false);

    public void SetFade(float alpha) {
        fade.gameObject.SetActive(alpha > 0.001f);
        fade.color = new Color(0, 0, 0, alpha);
    }

    public IEnumerator Fade(float from, float to, float time) {
        for (float t = 0; t < time; t += Time.deltaTime) {
            SetFade(Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        SetFade(to);
    }
}
