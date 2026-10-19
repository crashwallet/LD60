using UnityEngine;

// Записка/стикер, которую можно взять и прочитать (ЛКМ). Показывается крупно поверх экрана (NoteViewer).
public class NoteItem : MonoBehaviour, IInteractable {
    [Tooltip("Текст записки (русский).")]
    [TextArea(3, 10)] public string text;
    [Tooltip("Текст записки (английский).")]
    [TextArea(3, 10)] public string textEn;
    [Tooltip("Если задано — вместо текста показывается картинка (например, стикер с точками).")]
    public Sprite image;
    public Color paperColor = new(0.96f, 0.94f, 0.86f);
    [Tooltip("Моноширинный крупный текст — для таблиц букв.")]
    public bool monospace;

    public string Text => Loc.T(text, textEn);

    public string PromptText => Loc.T("ЛКМ — прочитать", "LMB — read");

    public void Interact(Vector3 viewerPosition) => NoteViewer.Show(this);
}
