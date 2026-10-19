using System.Collections.Generic;
using UnityEngine;

public enum FolderKind { Folder = 0, MyComputer = 1, Trash = 2, Drive = 3 }

[CreateAssetMenu(menuName = "Game/Folder", fileName = "NewFolder")]
public class FolderData : ScriptableObject {
    [Header("Папка")]
    public string folderName = "Новая папка";
    [Tooltip("Folder — обычная папка, MyComputer — «Мой компьютер», Trash — корзина, Drive — диск (C:).")]
    public FolderKind kind = FolderKind.Folder;

    [Header("Содержимое")]
    public List<FolderData> folders = new();
    public List<FileData> files = new();

    public bool IsEmpty => folders.Count == 0 && files.Count == 0;
}
