using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem; 
using TMPro;
using UnityEngine.EventSystems;

public class MonitorInteract : MonoBehaviour
{
    private static readonly List<MonitorInteract> allMonitors = new List<MonitorInteract>();
    [SerializeField] private MonitorOS monitorOS;
    private static MonitorInteract activeMonitor = null;

    [Header("Настройки Cinemachine 3.x")]
    [SerializeField] private CinemachineCamera monitorCamera;
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;

    [Header("Дистанция до игрока")]
    [SerializeField] private Transform player;
    [SerializeField] private float maxDistance = 2.5f;
    [SerializeField] private Behaviour[] disableWhileSeated;

    [Header("Подсказка при приближении")]
    [SerializeField] private bool highlightOnApproach = true;
    [SerializeField] private string promptText = "Press [E]";
    [Tooltip("Шрифт подсказки. Пусто — шрифт TMP по умолчанию.")]
    [SerializeField] private TMP_FontAsset promptFont;

    private FirstPersonController playerController; // ходьба и обзор мышью, выключаем за компьютером
    private Renderer monitorRenderer;
    private HoverOutline outline;
    private bool focused;
    private static int lastToggleFrame = -1; // чтобы одно нажатие E не выключило один монитор и не включило другой

    // У мониторов из FBX pivot у всех в одной точке (начало координат модели),
    // поэтому расстояние меряем от центра меша, иначе все мониторы «одинаково близко»
    private Vector3 InteractionPosition => monitorRenderer != null ? monitorRenderer.bounds.center : transform.position;

    private float DistanceToPlayer() => Vector3.Distance(InteractionPosition, player.position);

    private void Awake()
    {
        monitorRenderer = GetComponent<Renderer>();
        if (monitorRenderer == null) monitorRenderer = GetComponentInChildren<Renderer>();
    }

    private void OnEnable()
    {
        if (!allMonitors.Contains(this))
            allMonitors.Add(this);
    }

    private void OnDisable()
    {
        allMonitors.Remove(this);
        SetFocus(false);
        if (activeMonitor == this)
        {
            activeMonitor = null;
            SetPlayerControl(true); // не оставляем игрока замороженным, если монитор выключили
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        if (player != null) playerController = player.GetComponent<FirstPersonController>();

        if (highlightOnApproach)
        {
            outline = GetComponent<HoverOutline>();
            if (outline == null) outline = gameObject.AddComponent<HoverOutline>();
        }

        if (monitorCamera != null)
        {
            monitorCamera.Priority.Enabled = true; // Включаем переопределение приоритета в CM 3.x
            monitorCamera.Priority.Value = inactivePriority;
        }
    }

    private void Update()
{
    if (player == null || monitorCamera == null || Keyboard.current == null) return;

    // Обводка и «Press [E]», когда к этому монитору можно сесть
    bool canSit = activeMonitor == null
                  && (playerController == null || playerController.enabled)
                  && GetClosestMonitor() == this;
    SetFocus(canSit);

    if (activeMonitor == this && DistanceToPlayer() > maxDistance)
    {
        DeactivateMonitor();
        return;
    }

    if (IsTyping()) return; // пока печатаем, E и Esc не трогаем

    var kb = Keyboard.current;
    if (activeMonitor == this && kb.escapeKey.wasPressedThisFrame)
    {
        DeactivateMonitor();
        return;
    }

    if (kb.eKey.wasPressedThisFrame && lastToggleFrame != Time.frameCount)
    {
        if (activeMonitor == this) { DeactivateMonitor(); return; }
        if (activeMonitor == null && GetClosestMonitor() == this) ActivateMonitor();
    }
}
private void SetFocus(bool on)
{
    if (!highlightOnApproach) on = false;
    if (focused == on) return;
    focused = on;
    if (outline != null) outline.SetVisible(on);
    if (on) InteractPrompt.Show(this, promptText, promptFont);
    else InteractPrompt.Hide(this);
}

// Встать из-за компьютера (например, из меню «Пуск» в ОС)
public void ExitSeat()
{
    if (activeMonitor == this) DeactivateMonitor();
}

private static bool IsTyping()
{
    var es = EventSystem.current;
    if (es == null || es.currentSelectedGameObject == null) return false;
    return es.currentSelectedGameObject.TryGetComponent(out TMP_InputField f) && f.isFocused;
}

private void SetPlayerControl(bool on)
{
    // Тело и камера игрока не двигаются, пока он сидит за компьютером
    if (playerController != null) playerController.enabled = on;

    if (disableWhileSeated == null) return;
    foreach (var b in disableWhileSeated) if (b != null) b.enabled = on;
}


    private MonitorInteract GetClosestMonitor()
    {
        MonitorInteract closest = null;
        float minDistance = maxDistance;

        foreach (var monitor in allMonitors)
        {
            if (monitor.player == null) continue;

            float dist = monitor.DistanceToPlayer();
            if (dist <= minDistance)
            {
                minDistance = dist;
                closest = monitor;
            }
        }
        return closest;
    }

   private void ActivateMonitor()
{
    activeMonitor = this;
    lastToggleFrame = Time.frameCount;
    monitorCamera.Priority.Value = activePriority;
    SetPlayerControl(false);
    if (monitorOS != null) monitorOS.TurnOnMonitor();
}

private void DeactivateMonitor()
{
    activeMonitor = null;
    lastToggleFrame = Time.frameCount;
    monitorCamera.Priority.Value = inactivePriority;
    SetPlayerControl(true);
    if (monitorOS != null) monitorOS.TurnOffMonitor();
}
}