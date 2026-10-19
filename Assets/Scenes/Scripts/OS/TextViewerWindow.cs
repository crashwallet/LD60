using TMPro;
using UnityEngine;
using UnityEngine.UI;

// «Блокнот»: показывает текст файла (для зашифрованных — шифротекст из FileData.content)
public class TextViewerWindow : MonoBehaviour {
    public static TextViewerWindow Create(DesktopOS os, OSWindow window, FileData file) {
        var viewer = window.gameObject.AddComponent<TextViewerWindow>();

        var content = OSUI.ScrollArea("Text", window.Content, Color.white, out _);
        OSUI.Stretch((RectTransform)content.parent.parent);

        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        // Незашифрованный файл можно не прогонять через «Зашифровать» — тогда показываем plainText
        string body = string.IsNullOrEmpty(file.content) && !file.encrypted ? file.plainText : file.content;
        var text = OSUI.Text("Body", content, body, os.Font, os.TextFontSize, os.TextColor, TextAlignmentOptions.TopLeft);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return viewer;
    }
}
