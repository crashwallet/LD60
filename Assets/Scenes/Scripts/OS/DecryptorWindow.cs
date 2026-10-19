using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Программа «Дешифратор»: выбрать открытый шифр, ввести ключ, расшифровать текст и отправить ответ на задание
public class DecryptorWindow : MonoBehaviour {
    static readonly Dictionary<CipherType, string> CipherNames = new() {
        { CipherType.Caesar, "Caesar" },
        { CipherType.Vigenere, "Vigenere" },
        { CipherType.Des, "DES" },
    };

    DesktopOS os;
    List<CipherType> available = new();
    int cipherIndex;

    TextMeshProUGUI cipherLabel, outputText, statusText, questionText, answerStatus;
    TMP_InputField keyInput, sourceInput, answerInput;
    RectTransform taskPanel;

    public static DecryptorWindow Create(DesktopOS os, OSWindow window, string sourceText) {
        var d = window.gameObject.AddComponent<DecryptorWindow>();
        d.os = os;
        d.Build(window.Content);
        d.SetSource(sourceText);
        d.RefreshCiphers();
        d.RefreshTask(ProgressionManager.Instance ? ProgressionManager.Instance.CurrentTask : null);
        ProgressionManager.OnTaskChanged += d.RefreshTask;
        ProgressionManager.OnToolUnlocked += d.OnToolUnlocked;
        return d;
    }

    void OnDestroy() {
        ProgressionManager.OnTaskChanged -= RefreshTask;
        ProgressionManager.OnToolUnlocked -= OnToolUnlocked;
    }

    public void SetSource(string text) {
        if (sourceInput != null && text != null) sourceInput.text = text;
    }

    void Build(RectTransform c) {
        float fs = os.TextFontSize;
        Color txt = os.TextColor, muted = os.MutedTextColor;

        // Строка 1: шифр и ключ
        Box(OSUI.Text("CipherCaption", c, Loc.T("Шифр:", "Cipher:"), os.Font, fs, txt).rectTransform, 6, 4, 44, 20);
        var prev = OSUI.Button("Prev", c, "<", os.Font, fs, os.ButtonColor, txt, () => StepCipher(-1));
        Box((RectTransform)prev.transform, 50, 4, 20, 20);
        cipherLabel = OSUI.Text("Cipher", c, "", os.Font, fs, txt, TextAlignmentOptions.Center);
        Box(cipherLabel.rectTransform, 72, 4, 118, 20);
        var next = OSUI.Button("Next", c, ">", os.Font, fs, os.ButtonColor, txt, () => StepCipher(1));
        Box((RectTransform)next.transform, 192, 4, 20, 20);

        Box(OSUI.Text("KeyCaption", c, Loc.T("Ключ:", "Key:"), os.Font, fs, txt).rectTransform, 222, 4, 44, 20);
        keyInput = OSUI.InputField("Key", c, Loc.T("число или слово", "number or word"), os.Font, fs, txt, muted, false);
        Band((RectTransform)keyInput.transform, 268, 6, 4, 20);

        // Исходный текст
        Band(OSUI.Text("SourceCaption", c, Loc.T("Зашифрованный текст:", "Encrypted text:"), os.Font, fs - 1, txt).rectTransform, 6, 6, 28, 16);
        sourceInput = OSUI.InputField("Source", c, Loc.T("вставь или открой зашифрованный файл", "paste text or open an encrypted file"), os.Font, fs, txt, muted, true);
        Band((RectTransform)sourceInput.transform, 6, 6, 44, 62);

        var run = OSUI.Button("Decrypt", c, Loc.T("Расшифровать", "Decrypt"), os.Font, fs, os.ButtonColor, txt, Decrypt);
        var rrt = (RectTransform)run.transform;
        rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(1, 1);
        rrt.sizeDelta = new Vector2(120, 20);
        rrt.anchoredPosition = new Vector2(-6, -110);
        statusText = OSUI.Text("Status", c, "", os.Font, fs - 1, muted);
        Band(statusText.rectTransform, 6, 132, 110, 20);

        // Результат
        Band(OSUI.Text("ResultCaption", c, Loc.T("Результат:", "Result:"), os.Font, fs - 1, txt).rectTransform, 6, 6, 134, 16);
        var outContent = OSUI.ScrollArea("Result", c, new Color(0.97f, 0.97f, 0.94f), out _);
        var outRt = (RectTransform)outContent.parent.parent;
        outRt.anchorMin = new Vector2(0, 0);
        outRt.anchorMax = new Vector2(1, 1);
        outRt.offsetMin = new Vector2(6, 76);
        outRt.offsetMax = new Vector2(-6, -152);
        var layout = outContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 2, 2);
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        outputText = OSUI.Text("Text", outContent, "", os.Font, fs, txt, TextAlignmentOptions.TopLeft);
        outputText.textWrappingMode = TextWrappingModes.Normal;
        outputText.overflowMode = TextOverflowModes.Overflow;

        // Задание и ответ (только если в сцене есть ProgressionManager)
        taskPanel = OSUI.Image("Task", c, os.ToolbarColor).rectTransform;
        taskPanel.anchorMin = Vector2.zero;
        taskPanel.anchorMax = new Vector2(1, 0);
        taskPanel.pivot = new Vector2(0.5f, 0);
        taskPanel.offsetMin = new Vector2(0, 0);
        taskPanel.offsetMax = new Vector2(0, 72);

        questionText = OSUI.Text("Question", taskPanel, "", os.Font, fs, txt);
        questionText.textWrappingMode = TextWrappingModes.Normal;
        Band(questionText.rectTransform, 6, 6, 2, 22);
        answerInput = OSUI.InputField("Answer", taskPanel, Loc.T("ответ", "answer"), os.Font, fs, txt, muted, false);
        Band((RectTransform)answerInput.transform, 6, 96, 26, 20);
        answerInput.onSubmit.AddListener(_ => SubmitAnswer());
        var send = OSUI.Button("Send", taskPanel, Loc.T("Отправить", "Submit"), os.Font, fs, os.ButtonColor, txt, SubmitAnswer);
        var srt = (RectTransform)send.transform;
        srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(1, 1);
        srt.sizeDelta = new Vector2(84, 20);
        srt.anchoredPosition = new Vector2(-6, -26);
        answerStatus = OSUI.Text("AnswerStatus", taskPanel, "", os.Font, fs - 1, muted);
        Band(answerStatus.rectTransform, 6, 6, 50, 18);
    }

    // Элемент фиксированного размера от левого верхнего угла
    static void Box(RectTransform rt, float left, float top, float width, float height) {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = new Vector2(left, -top);
    }

    // Полоса на всю ширину с отступами слева/справа
    static void Band(RectTransform rt, float left, float right, float top, float height) {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(left, -top - height);
        rt.offsetMax = new Vector2(-right, -top);
    }

    void OnToolUnlocked(CipherType _) => RefreshCiphers();

    void RefreshCiphers() {
        var pm = ProgressionManager.Instance;
        CipherType? selected = available.Count > 0 ? available[cipherIndex] : null;
        available = Enum.GetValues(typeof(CipherType)).Cast<CipherType>()
            .Where(t => pm == null || pm.IsUnlocked(t)).ToList();
        cipherIndex = selected.HasValue ? Mathf.Max(0, available.IndexOf(selected.Value)) : 0;
        UpdateCipherLabel();
    }

    void StepCipher(int dir) {
        if (available.Count == 0) return;
        cipherIndex = (cipherIndex + dir + available.Count) % available.Count;
        UpdateCipherLabel();
    }

    void UpdateCipherLabel() {
        cipherLabel.text = available.Count == 0 ? Loc.T("нет доступных", "none available")
            : CipherNames.TryGetValue(available[cipherIndex], out var n) ? n : available[cipherIndex].ToString();
    }

    void Decrypt() {
        if (available.Count == 0) { statusText.text = Loc.T("Пока нет ни одного открытого шифра", "No ciphers unlocked yet"); return; }
        if (string.IsNullOrWhiteSpace(sourceInput.text)) { statusText.text = Loc.T("Нет текста для расшифровки", "Nothing to decrypt"); return; }
        if (string.IsNullOrWhiteSpace(keyInput.text)) { statusText.text = Loc.T("Введи ключ", "Enter a key"); return; }

        try {
            outputText.text = CipherFactory.Get(available[cipherIndex]).Decrypt(sourceInput.text, keyInput.text.Trim());
            statusText.text = Loc.T("Готово", "Done");
        } catch (Exception) {
            outputText.text = "";
            statusText.text = Loc.T("Не получилось — проверь шифр и ключ", "Failed — check the cipher and the key");
        }
    }

    void RefreshTask(TaskData task) {
        bool hasPm = ProgressionManager.Instance != null;
        taskPanel.gameObject.SetActive(hasPm);
        if (!hasPm) return;

        if (task == null) {
            questionText.text = Loc.T("Заданий больше нет", "No more tasks");
            answerInput.interactable = false;
        } else {
            questionText.text = string.IsNullOrEmpty(task.Question) ? Loc.T("Ответ:", "Answer:") : task.Question;
            answerInput.interactable = true;
        }
        RefreshCiphers();
    }

    void SubmitAnswer() {
        var pm = ProgressionManager.Instance;
        if (pm == null || pm.CurrentTask == null || string.IsNullOrWhiteSpace(answerInput.text)) return;

        bool ok = pm.Submit(answerInput.text);
        answerStatus.text = ok ? Loc.T("Ответ принят", "Answer accepted") : Loc.T("Неверно, попробуй ещё", "Wrong, try again");
        if (ok) answerInput.text = "";
        if (os.Helper != null) os.Helper.ReactToAnswer(ok);
    }
}
