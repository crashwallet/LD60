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

    private FirstPersonController playerController; // ходьба и обзор мышью, выключаем за компьютером

    private void OnEnable()
    {
        if (!allMonitors.Contains(this))
            allMonitors.Add(this);
    }

    private void OnDisable()
    {
        allMonitors.Remove(this);
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

        if (monitorCamera != null)
        {
            monitorCamera.Priority.Enabled = true; // Включаем переопределение приоритета в CM 3.x
            monitorCamera.Priority.Value = inactivePriority;
        }
    }

    private void Update()
{
    if (player == null || monitorCamera == null || Keyboard.current == null) return;

    if (activeMonitor == this && Vector3.Distance(transform.position, player.position) > maxDistance)
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

    if (kb.eKey.wasPressedThisFrame)
    {
        if (activeMonitor == this) { DeactivateMonitor(); return; }
        if (activeMonitor == null && GetClosestMonitor() == this) ActivateMonitor();
    }
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

            float dist = Vector3.Distance(monitor.transform.position, monitor.player.position);
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
    monitorCamera.Priority.Value = activePriority;
    SetPlayerControl(false);
    if (monitorOS != null) monitorOS.TurnOnMonitor();
}

private void DeactivateMonitor()
{
    activeMonitor = null;
    monitorCamera.Priority.Value = inactivePriority;
    SetPlayerControl(true);
    if (monitorOS != null) monitorOS.TurnOffMonitor();
}
}