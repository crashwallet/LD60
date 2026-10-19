using UnityEngine;

// Просмотр картинок (FileData с fileType = Image)
public class ImageViewerWindow : MonoBehaviour {
    public static ImageViewerWindow Create(DesktopOS os, OSWindow window, FileData file) {
        var viewer = window.gameObject.AddComponent<ImageViewerWindow>();

        var bg = OSUI.Image("Background", window.Content, new Color(0.12f, 0.12f, 0.12f));
        OSUI.Stretch(bg.rectTransform);

        if (file.image != null) {
            var pic = OSUI.Image("Picture", bg.transform, Color.white, file.image, false);
            pic.preserveAspect = true;
            OSUI.Stretch(pic.rectTransform, 6, 6, 6, 6);
        } else {
            var t = OSUI.Text("NoImage", bg.transform, Loc.T("Не удалось открыть изображение", "Cannot open the image"),
                              os.Font, os.TextFontSize, Color.white, TMPro.TextAlignmentOptions.Center);
            OSUI.Stretch(t.rectTransform);
        }
        return viewer;
    }
}
