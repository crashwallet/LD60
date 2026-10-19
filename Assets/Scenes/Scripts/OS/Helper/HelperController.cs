using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Помощник в ОС, как Скрепка: говорит реплики в облачке, меняет эмоцию и форму (?/!).
// Клик по облачку — следующая реплика (или допечатать текущую), клик по помощнику без реплик — повторить задание.
public class HelperController : MonoBehaviour, IPointerClickHandler {
    [Header("Части")]
    [SerializeField] HelperAvatar avatar;
    [SerializeField] GameObject bubble;
    [SerializeField] TextMeshProUGUI bubbleText;

    [Header("Реплики")]
    [Tooltip("Звучит при первом включении этого компьютера.")]
    [SerializeField] HelperDialogue greeting;
    [SerializeField] HelperDialogue correctAnswer;
    [SerializeField] HelperDialogue wrongAnswer;
    [SerializeField] HelperDialogue allTasksDone;

    [Header("Как помощник озвучивает задание (TaskData.clippyRequest)")]
    [SerializeField] bool announceTasks = true;
    [SerializeField] HelperEmotion taskEmotion = HelperEmotion.Questionary;
    [SerializeField] HelperShape taskShape = HelperShape.Exclamation;

    [Header("Печать")]
    [SerializeField] float charsPerSecond = 45f;

    [Header("Автопрокрутка")]
    [Tooltip("Реплики сами сменяются после паузы. Клик по помощнику всё равно листает сразу.")]
    [SerializeField] bool autoAdvance = true;
    [Tooltip("Пауза после того, как реплика допечатана: случайно от X до Y секунд.")]
    [SerializeField] Vector2 autoDelay = new(1f, 1.5f);
    [Tooltip("Последняя реплика остаётся в облачке, пока не кликнут или не появится новая.")]
    [SerializeField] bool keepLastLine = true;

    readonly List<HelperLine> queue = new();
    HelperLine current;
    Coroutine typing;
    bool isTyping;
    bool greeted;

    void Awake() {
        if (bubble != null) bubble.SetActive(false);
    }

    void OnEnable() {
        ProgressionManager.OnTaskChanged += OnTaskChanged;

        if (!greeted) {
            greeted = true;
            Play(greeting);
            var pm = ProgressionManager.Instance;
            if (announceTasks && pm != null && pm.CurrentTask != null) Enqueue(TaskLine(pm.CurrentTask));
        }

        // Компьютер включили снова — продолжаем с того места, где остановились
        if (current != null) Show(current);
        else Next();
    }

    void OnDisable() {
        ProgressionManager.OnTaskChanged -= OnTaskChanged;
        typing = null;
        isTyping = false;
    }

    // --- Публичный API ---

    public void Play(HelperDialogue dialogue) {
        if (dialogue == null) return;
        foreach (var l in dialogue.lines) Enqueue(l);
    }

    public void Say(string text, HelperEmotion emotion = HelperEmotion.Default, HelperShape shape = HelperShape.Question) =>
        Enqueue(new HelperLine { text = text, emotion = emotion, shape = shape });

    // Сразу перебить текущую реплику (остальная очередь сохранится после)
    public void PlayNow(HelperDialogue dialogue) {
        if (dialogue == null || dialogue.lines.Count == 0) return;
        queue.InsertRange(0, dialogue.lines);
        current = null;
        if (isActiveAndEnabled) Next();
    }

    public void ReactToAnswer(bool correct) => PlayNow(correct ? correctAnswer : wrongAnswer);

    // --- Внутреннее ---

    HelperLine TaskLine(TaskData t) => new() { text = t.clippyRequest, emotion = taskEmotion, shape = taskShape };

    void OnTaskChanged(TaskData t) {
        if (t == null) Play(allTasksDone);
        else if (announceTasks && !string.IsNullOrEmpty(t.clippyRequest)) Enqueue(TaskLine(t));
    }

    void Enqueue(HelperLine line) {
        if (line == null || string.IsNullOrEmpty(line.text)) return;
        queue.Add(line);
        if (current == null && isActiveAndEnabled) Next();
    }

    void Next() {
        if (typing != null) { StopCoroutine(typing); typing = null; }
        isTyping = false;

        if (queue.Count == 0) {
            current = null;
            if (bubble != null) bubble.SetActive(false);
            if (avatar != null) {
                avatar.SetEmotion(HelperEmotion.Default);
                avatar.SetShape(HelperShape.Question);
            }
            return;
        }

        current = queue[0];
        queue.RemoveAt(0);
        Show(current);
    }

    void Show(HelperLine line) {
        if (!isActiveAndEnabled) return;
        if (avatar != null) {
            avatar.SetEmotion(line.emotion);
            avatar.SetShape(line.shape);
        }
        if (bubble != null) bubble.SetActive(true);
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(Type(line));
    }

    IEnumerator Type(HelperLine line) {
        isTyping = true;
        bubbleText.text = line.text;
        bubbleText.maxVisibleCharacters = 0;
        bubbleText.ForceMeshUpdate();
        int total = bubbleText.textInfo.characterCount;

        float shown = 0f;
        while (shown < total) {
            shown += charsPerSecond * Time.unscaledDeltaTime;
            bubbleText.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        bubbleText.maxVisibleCharacters = total;
        isTyping = false;

        yield return WaitThenAdvance(line);
    }

    // Пауза после реплики и переход к следующей
    IEnumerator WaitThenAdvance(HelperLine line) {
        float delay = line.autoNext > 0f ? line.autoNext
                    : autoAdvance ? Random.Range(autoDelay.x, Mathf.Max(autoDelay.x, autoDelay.y)) : -1f;
        if (delay < 0f) yield break; // автопрокрутка выключена — ждём клика

        yield return new WaitForSecondsRealtime(delay);

        // Последнюю реплику не убираем, пока не появится новая (или игрок не кликнет)
        if (keepLastLine)
            while (queue.Count == 0 && current == line) yield return null;

        typing = null; // Next() не должен останавливать сам этот корутин
        if (current == line) Next();
    }

    public void OnPointerClick(PointerEventData e) {
        if (e.button != PointerEventData.InputButton.Left) return;

        if (isTyping) {
            // Допечатать мгновенно
            if (typing != null) StopCoroutine(typing);
            typing = null;
            isTyping = false;
            bubbleText.maxVisibleCharacters = int.MaxValue;
            if (current != null) typing = StartCoroutine(WaitThenAdvance(current));
            return;
        }

        if (current != null) { Next(); return; }

        // Реплик нет — напомнить текущее задание
        var pm = ProgressionManager.Instance;
        if (pm != null && pm.CurrentTask != null) Enqueue(TaskLine(pm.CurrentTask));
    }
}
