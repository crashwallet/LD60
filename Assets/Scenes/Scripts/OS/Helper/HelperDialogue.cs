using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HelperLine {
    [TextArea(2, 5)] public string text;
    [TextArea(2, 5)] public string textEn;
    public string Text => Loc.T(text, textEn);
    public HelperEmotion emotion = HelperEmotion.Default;
    public HelperShape shape = HelperShape.Question;
    [Tooltip("0 — пауза по умолчанию из HelperController (3 с). Больше 0 — своя пауза в секундах для этой реплики.")]
    public float autoNext = 0f;
}

// Набор реплик помощника: создаётся в Project → Create → Game → Helper Dialogue
[CreateAssetMenu(menuName = "Game/Helper Dialogue", fileName = "NewHelperDialogue")]
public class HelperDialogue : ScriptableObject {
    public List<HelperLine> lines = new();
}
