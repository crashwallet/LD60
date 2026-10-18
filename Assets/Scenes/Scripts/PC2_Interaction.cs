using UnityEngine;
using UnityEngine.InputSystem; // Новая система ввода Unity 6
using Unity.Cinemachine;       // Cinemachine 3.x для Unity 6

public class PC2Interaction : MonoBehaviour
{
    [Header("Камера (Cinemachine 3.x)")]
    public CinemachineCamera pcCamera;

    [Header("Игрок и UI")]
    public MonoBehaviour playerController; // Скрипт ходьбы/вращения камеры
    public GameObject crosshair;           // Прицел по центру экрана

    private bool isSitting = false;


    void Update()
    {
        // Проверяем наличие клавиатуры (безопасная проверка в Input System)
        if (Keyboard.current == null) return;

        // Нажатие клавиши E для входа/выхода
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (!isSitting) SitDown();
            else StandUp();
        }

        // Выход по клавише Escape
        if (isSitting && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StandUp();
        }
    }

    public void SitDown()
    {
        isSitting = true;
        
        // В Cinemachine 3.x приоритет меняется через Value
        
        pcCamera.Priority.Value = 20;

        if (playerController != null) playerController.enabled = false;
        if (crosshair != null) crosshair.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StandUp()
    {
        isSitting = false;
        
        pcCamera.Priority.Value = 5;

        if (playerController != null) playerController.enabled = true;
        if (crosshair != null) crosshair.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}