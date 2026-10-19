using UnityEngine;

public enum HelperEmotion { Default = 0, Angry = 1, Funny = 2, Peaceful = 3, Questionary = 4, Thinking = 5 }
public enum HelperShape { Question = 0, Exclamation = 1 }

// Внешний вид помощника: тело (знак ?/!), кадры превращения и глаза-эмоции
[CreateAssetMenu(menuName = "Game/Helper Look", fileName = "HelperLook")]
public class HelperLook : ScriptableObject {
    [Header("Тело")]
    public Sprite questionBody;
    public Sprite exclamationBody;
    [Tooltip("Промежуточные кадры от «?» к «!» (обратно проигрываются в обратном порядке).")]
    public Sprite[] morphFrames;
    [Tooltip("Сколько секунд показывается каждый кадр превращения.")]
    public float morphFrameTime = 0.07f;

    [Header("Глаза (эмоции)")]
    public Sprite defaultEyes;
    public Sprite angryEyes;
    public Sprite funnyEyes;
    public Sprite peacefulEyes;
    public Sprite questionaryEyes;
    public Sprite thinkingEyes;

    [Header("Положение глаз относительно тела")]
    [Tooltip("Смещение центра глаз от верхнего края тела, в долях размера тела (x вправо, y вниз).")]
    public Vector2 eyesOffset = new(0f, 0.25f);
    [Tooltip("Размер глаз в долях ширины тела.")]
    public float eyesScale = 1f;

    public Sprite Body(HelperShape s) => s == HelperShape.Exclamation ? exclamationBody : questionBody;

    public Sprite Eyes(HelperEmotion e) {
        Sprite s = e switch {
            HelperEmotion.Angry => angryEyes,
            HelperEmotion.Funny => funnyEyes,
            HelperEmotion.Peaceful => peacefulEyes,
            HelperEmotion.Questionary => questionaryEyes,
            HelperEmotion.Thinking => thinkingEyes,
            _ => defaultEyes
        };
        return s != null ? s : defaultEyes;
    }
}
