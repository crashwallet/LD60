using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CRTBootEffect : MonoBehaviour
{
    [Header("Элементы экрана")]
    public RectTransform screenContent; // Объект со всем интерфейсом Windows XP
    public Image whiteFlash;            // Белая картинка поверх экрана для эффекта луча
    
    [Header("Звуки")]
    public AudioSource audioSource;
    public AudioClip crtTurnOnSound;    // Щелчок и гул размагничивания
    public AudioClip winXPStartupSound; // Мелодия запуска Windows XP

    private bool isTurnedOn = false;

    void Start()
    {
        // Изначально компьютер выключен (схлопнут в ноль)
        screenContent.localScale = Vector3.zero;
        whiteFlash.gameObject.SetActive(false);
    }

    // Вызывайте этот метод по нажатию на кнопку питания на системном блоке/мониторе
    public void TurnOnComputer()
    {
        if (!isTurnedOn)
        {
            StartCoroutine(BootSequence());
        }
    }

    private IEnumerator BootSequence()
    {
        isTurnedOn = true;

        // 1. Звук щелчка кнопки и размагничивания ЭЛТ
        if (audioSource && crtTurnOnSound)
            audioSource.PlayOneShot(crtTurnOnSound);

        yield return new WaitForSeconds(0.4f); // Небольшая пауза перед появлением луча

        // 2. Появляется точка и быстро растягивается в горизонтальную белую линию
        whiteFlash.gameObject.SetActive(true);
        whiteFlash.color = Color.white;
        screenContent.localScale = new Vector3(0.01f, 0.005f, 1f);

        float timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.deltaTime;
            float progress = timer / 0.15f;
            // Растягиваем по ширине (X от 0.01 до 1)
            screenContent.localScale = new Vector3(Mathf.Lerp(0.01f, 1f, progress), 0.005f, 1f);
            yield return null;
        }

        // 3. Линия раскрывается по вертикали на весь экран, а белая вспышка гаснет
        timer = 0f;
        while (timer < 0.25f)
        {
            timer += Time.deltaTime;
            float progress = timer / 0.25f;
            
            // Растягиваем по высоте (Y от 0.005 до 1)
            screenContent.localScale = new Vector3(1f, Mathf.Lerp(0.005f, 1f, progress), 1f);
            
            // Плавно убираем белую вспышку
            whiteFlash.color = new Color(1f, 1f, 1f, 1f - progress);
            yield return null;
        }

        screenContent.localScale = Vector3.one;
        whiteFlash.gameObject.SetActive(false);

        // 4. Запускаем звук приветствия Windows XP
        if (audioSource && winXPStartupSound)
            audioSource.PlayOneShot(winXPStartupSound);
    }
}