using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Область, куда можно бросить иконку (пустое место в окне проводника) — она попадает в текущую папку окна
public class FolderDropZone : MonoBehaviour, IDropHandler {
    DesktopOS os;
    Func<FolderData> folder;

    public static FolderDropZone Add(GameObject go, DesktopOS os, Func<FolderData> folder) {
        var z = go.AddComponent<FolderDropZone>();
        z.os = os;
        z.folder = folder;
        return z;
    }

    public void OnDrop(PointerEventData e) {
        var dragged = OSIcon.Dragged(e);
        if (dragged != null) os.Move(dragged.Item, dragged.Source, folder());
    }
}
