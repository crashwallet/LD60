using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem; // Обязательно для новой Input System

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

    private void OnEnable()
    {
        if (!allMonitors.Contains(this))
            allMonitors.Add(this);
    }

    private void OnDisable()
    {
        allMonitors.Remove(this);
        if (activeMonitor == this)
            activeMonitor = null;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
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

        // Автовыход, если игрок отошел от активного монитора
        if (activeMonitor == this && Vector3.Distance(transform.position, player.position) > maxDistance)
        {
            DeactivateMonitor();
            return;
        }

        // Проверяем нажатие клавиши E через новую Input System
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            // Если мы уже смотрим в ЭТОТ монитор — выходим из него
            if (activeMonitor == this)
            {
                DeactivateMonitor();
                return;
            }

            // Если ни один монитор не включен, активируем ТОЛЬКО самый ближайший к игроку
            if (activeMonitor == null && GetClosestMonitor() == this)
            {
                ActivateMonitor();
            }
        }
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
    if (monitorOS != null) monitorOS.TurnOnMonitor();
}

private void DeactivateMonitor()
{
    activeMonitor = null;
    monitorCamera.Priority.Value = inactivePriority;
    if (monitorOS != null) monitorOS.TurnOffMonitor();
}
}