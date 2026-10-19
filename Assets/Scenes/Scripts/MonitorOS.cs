using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class MonitorOS : MonoBehaviour
{
    [Header("Экраны монитора")]
    [SerializeField] private GameObject bootScreen;    // Экран загрузки
    [SerializeField] private GameObject desktopScreen; // Рабочий стол

    [Header("Параметры загрузки (Slider)")]
    [SerializeField] private Slider progressSlider;    // Ваш Slider
    [SerializeField] private float startDelay = 1.67f;  // Задержка перед включением (в секундах)
    [SerializeField] private float bootTime = 2.0f;    // Время самой загрузки
    [SerializeField] private bool bootOnlyOnce = true; // Загружать ОС только при первом входе?

    private bool isBooted = false;
    private Coroutine bootCoroutine;

    [Header("Ввод мышью")]
    [SerializeField] private Canvas canvas;              // M1_canvas
    [SerializeField] private GraphicRaycaster raycaster; // на M1_canvas

    private void Start()
    {
        bootScreen.SetActive(false);
        desktopScreen.SetActive(false);
        if (canvas != null) canvas.worldCamera = Camera.main; // камера с CinemachineBrain
        SetInput(false);
    }

    private void SetInput(bool on)
    {
        if (raycaster != null) raycaster.enabled = on;
        if (!on && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }


    public void TurnOnMonitor()
    {
        SetInput(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Если ОС уже загружена и не нужно грузить заново — сразу включаем рабочий стол
        if (bootOnlyOnce && isBooted)
        {
            desktopScreen.SetActive(true);
            return;
        }

        if (bootCoroutine != null) StopCoroutine(bootCoroutine);
        bootCoroutine = StartCoroutine(BootSequence());
    }

    public void TurnOffMonitor()
    {
        SetInput(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (bootCoroutine != null) StopCoroutine(bootCoroutine);

        // Если нужно выключать монитор при уходе (или если загрузка прервалась на середине)
        if (!bootOnlyOnce || !isBooted)
        {
            bootScreen.SetActive(false);
            desktopScreen.SetActive(false);
        }
    }

    private IEnumerator BootSequence()
    {
        // 1. Пока идет задержка, оба UI-экрана выключены (видна поверхность 3D-модели)
        bootScreen.SetActive(false);
        desktopScreen.SetActive(false);

        if (progressSlider != null) progressSlider.value = 0f;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 2. Включаем экран загрузки и запускаем полоску
        bootScreen.SetActive(true);

        float timer = 0f;
        while (timer < bootTime)
        {
            timer += Time.deltaTime;
            if (progressSlider != null)
            {
                progressSlider.value = Mathf.Clamp01(timer / bootTime);
            }
            yield return null;
        }

        // 3. Показываем рабочий стол
        isBooted = true;
        bootScreen.SetActive(false);
        desktopScreen.SetActive(true);
    }
}