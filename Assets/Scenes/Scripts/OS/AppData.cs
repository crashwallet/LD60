using UnityEngine;

public enum AppKind { Decryptor = 0 }

// Программа на рабочем столе или в папке (ярлык). Кладётся в FolderData.apps.
[CreateAssetMenu(menuName = "Game/App", fileName = "NewApp")]
public class AppData : ScriptableObject {
    public string appName = "Дешифратор";
    public AppKind kind = AppKind.Decryptor;
    [Tooltip("Иконка ярлыка. Если пусто — иконка текстового файла.")]
    public Sprite icon;
}
