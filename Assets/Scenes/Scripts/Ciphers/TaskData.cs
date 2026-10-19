using UnityEngine;

[CreateAssetMenu(menuName = "Game/Task", fileName = "NewTask")]
public class TaskData : ScriptableObject {
    [Header("Что говорит Скрепка")]
    [TextArea] public string clippyRequest;   // "Расшифруй этот файл, я помогу!"
    public string answerQuestion;             // "Какой пароль в файле?"
    [TextArea] public string clippyRequestEn;
    public string answerQuestionEn;

    public string Request => Loc.T(clippyRequest, clippyRequestEn);
    public string Question => Loc.T(answerQuestion, answerQuestionEn);

    [Header("Головоломка")]
    public FileData file;                     // файл, который надо расшифровать
    [Tooltip("Инструмент, который откроется вместе с этим заданием.")]
    public CipherType cipher;
    [Tooltip("Регистр и пробелы не важны.")]
    public string expectedAnswer;

    [Header("Скрытый слой")]
    [Tooltip("Насколько вырастет доступ вируса после выполнения.")]
    public int accessGranted = 1;
    [TextArea] public string hiddenEffectLog; // что вирус сделал на самом деле
}