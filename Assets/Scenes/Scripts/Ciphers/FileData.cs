using UnityEngine;

public enum FileType { Text = 0, Image = 1 }

[CreateAssetMenu(menuName = "Game/File", fileName = "NewFile")]
public class FileData : ScriptableObject {
    [Header("Файл")]
    public string fileName = "readme.txt";
    [Tooltip("Text — открывается в «Блокноте», Image — в просмотрщике картинок.")]
    public FileType fileType = FileType.Text;
    [Tooltip("Для Image: сама картинка (Texture Type = Sprite). Она же будет иконкой файла.")]
    public Sprite image;
    public bool encrypted = true;

    [Header("Шифрование")]
    public CipherType cipher;
    [Tooltip("Цезарь: число сдвига (\"3\"). Виженер и DES: слово (\"LEMON\").")]
    public string key;

    [Header("Текст")]
    [Tooltip("Исходный текст. Игрок его не видит, нужен только для шифрования.")]
    [TextArea(4, 15)] public string plainText;

    [Tooltip("Английский текст для незашифрованных файлов (шифры всегда на английском — для них не нужен).")]
    [TextArea(4, 15)] public string plainTextEn;

    [Tooltip("То, что видит игрок. Заполняется кнопкой «Зашифровать».")]
    [TextArea(4, 15)] public string content;

    [ContextMenu("Зашифровать plainText → content")]
    void EncryptInEditor() {
        content = encrypted
            ? CipherFactory.Get(cipher).Encrypt(plainText, key)
            : plainText;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this); // иначе изменение может не сохраниться
#endif
    }
}