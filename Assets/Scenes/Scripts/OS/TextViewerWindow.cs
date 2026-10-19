using TMPro;
using UnityEngine;
using UnityEngine.UI;

// «Блокнот»: показывает текст файла (для зашифрованных — шифротекст из FileData.content)
public class TextViewerWindow : MonoBehaviour {
    const float ToolbarHeight = 22f;

    public static TextViewerWindow Create(DesktopOS os, OSWindow window, FileData file) {
        var viewer = window.gameObject.AddComponent<TextViewerWindow>();
        string body = FileText.Visible(file);
        float top = 0;

        // У зашифрованных файлов — кнопка, которая открывает текст в «Дешифраторе»
        if (file.encrypted) {
            var toolbar = OSUI.TopBar(OSUI.Image("Toolbar", window.Content, os.ToolbarColor).rectTransform, ToolbarHeight);
            var btn = OSUI.Button("ToDecryptor", toolbar, "Открыть в дешифраторе", os.Font, os.TextFontSize - 1,
                                  os.ButtonColor, os.TextColor, () => os.OpenDecryptor(body));
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0, 0.5f);
            brt.sizeDelta = new Vector2(170, ToolbarHeight - 4);
            brt.anchoredPosition = new Vector2(2, 0);
            top = ToolbarHeight;
        }

        var content = OSUI.ScrollArea("Text", window.Content, Color.white, out _);
        OSUI.Stretch((RectTransform)content.parent.parent, 0, 0, 0, top);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var text = OSUI.Text("Body", content, body, os.Font, os.TextFontSize, os.TextColor, TextAlignmentOptions.TopLeft);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return viewer;
    }
}
