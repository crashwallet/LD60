using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// GraphicRaycaster, который учитывает искажение картинки пост-эффектами (ЭЛТ-эффект):
// луч пускается не из пикселя под курсором, а из точки, которая реально там нарисована.
public class DistortedGraphicRaycaster : GraphicRaycaster {
    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
        Camera cam = eventCamera;
        if (cam == null) {
            base.Raycast(eventData, resultAppendList);
            return;
        }

        Vector2 original = eventData.position;
        eventData.position = ScreenDistortion.ToScene(original, cam);
        try {
            base.Raycast(eventData, resultAppendList);
        } finally {
            eventData.position = original;
        }
    }
}
