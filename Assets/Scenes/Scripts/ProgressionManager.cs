using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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

    readonly HashSet<CipherType> unlocked = new();
    public bool IsUnlocked(CipherType c) => unlocked.Contains(c);

    void Awake() {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        StartTask();
    }

    void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    void StartTask() {
        var t = CurrentTask;
        if (t != null && unlocked.Add(t.cipher)) OnToolUnlocked?.Invoke(t.cipher);
        OnTaskChanged?.Invoke(t);
    }

    public bool Submit(string answer) {
        var t = CurrentTask;
        if (t == null || Normalize(answer) != Normalize(t.expectedAnswer)) return false;

        VirusAccess = Mathf.Min(VirusAccess + t.accessGranted, maxAccess);
        if (!string.IsNullOrEmpty(t.hiddenEffectLog))
            hiddenLog.AppendLine(t.hiddenEffectLog);
        OnAccessChanged?.Invoke(VirusAccess);

        Current++;
        StartTask();
        return true;
    }

    static string Normalize(string s) =>
        (s ?? "").Trim().ToUpperInvariant().Replace(" ", "");
}