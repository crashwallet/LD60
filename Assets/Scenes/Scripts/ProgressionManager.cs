using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class CipherUnlock {
    public CipherType cipher;
    [Tooltip("С какого уровня доступа вируса шифр появляется в «Дешифраторе».")]
    public int requiredAccess;
}

public class ProgressionManager : MonoBehaviour {
    public static ProgressionManager Instance { get; private set; }

    public static event Action<int> OnAccessChanged;      // новый уровень доступа вируса
    public static event Action<TaskData> OnTaskChanged;   // null, когда задания кончились
    public static event Action<CipherType> OnToolUnlocked;

    [SerializeField] TaskData[] tasks;
    [SerializeField] int maxAccess = 10;

    public int MaxAccess => maxAccess;
    public int Current { get; private set; }
    public int VirusAccess { get; private set; }
    public TaskData CurrentTask => Current < tasks.Length ? tasks[Current] : null;

    // Скрытый лог: что вирус сделал на самом деле. Живёт только в памяти,
    // чтобы не портить ассеты в редакторе.
    readonly StringBuilder hiddenLog = new();
    public string HiddenLog => hiddenLog.ToString();

    [Header("Шифры открываются по уровню доступа вируса")]
    [SerializeField] CipherUnlock[] cipherUnlocks = {
        new() { cipher = CipherType.Caesar,   requiredAccess = 0 },
        new() { cipher = CipherType.Vigenere, requiredAccess = 1 },
        new() { cipher = CipherType.Des,      requiredAccess = 2 },
    };

    readonly HashSet<CipherType> unlocked = new();
    public bool IsUnlocked(CipherType c) => unlocked.Contains(c);

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UnlockCiphers();
        StartTask();
    }

    // Открыть все шифры, до которых дорос доступ вируса (шифр без строки в списке не откроется никогда)
    void UnlockCiphers() {
        foreach (var u in cipherUnlocks)
            if (VirusAccess >= u.requiredAccess && unlocked.Add(u.cipher))
                OnToolUnlocked?.Invoke(u.cipher);
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void StartTask() {
        var t = CurrentTask;
        if (t != null && !unlocked.Contains(t.cipher))
            Debug.LogWarning($"[Progression] Задание «{t.name}» требует шифр {t.cipher}, но при доступе {VirusAccess} он ещё закрыт", t);
        OnTaskChanged?.Invoke(t);
    }

    public bool Submit(string answer) {
        var t = CurrentTask;
        if (t == null || Normalize(answer) != Normalize(t.expectedAnswer)) return false;

        VirusAccess = Mathf.Min(VirusAccess + t.accessGranted, maxAccess);
        if (!string.IsNullOrEmpty(t.hiddenEffectLog))
            hiddenLog.AppendLine(t.hiddenEffectLog);
        OnAccessChanged?.Invoke(VirusAccess);
        UnlockCiphers();

        Current++;
        StartTask();
        return true;
    }

    static string Normalize(string s) =>
        (s ?? "").Trim().ToUpperInvariant().Replace(" ", "");
}