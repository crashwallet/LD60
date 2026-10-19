using UnityEngine;
using UnityEngine.InputSystem; // Новая система ввода Unity 6
using Unity.Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 4.5f;
    public float gravity = -9.81f;

    [Header("Настройки камеры (Мышь)")]
    public Transform cameraRoot;        // Перетащите сюда объект CameraRoot
    public float mouseSensitivity = 0.15f;
    public float maxLookAngle = 85f;    // Ограничение взгляда вверх/вниз

    [Header("Тело игрока")]
    [Tooltip("Не рисовать капсулу игрока: от первого лица она не нужна и попадает в кадр при переходе к монитору.")]
    public bool hideBody = true;

    [Header("Зум колёсиком мыши")]
    [Tooltip("Камера игрока (P_camera). Если пусто — найдётся среди дочерних объектов игрока.")]
    public CinemachineCamera playerCamera;
    [Tooltip("Минимальный угол обзора при максимальном приближении.")]
    public float minFov = 25f;
    [Tooltip("На сколько градусов меняется угол обзора за один щелчок колёсика.")]
    public float zoomStep = 7f;
    [Tooltip("Плавность зума: чем больше, тем быстрее догоняет.")]
    public float zoomSmooth = 12f;

    private float defaultFov = 60f;
    private float targetFov = 60f;

    private CharacterController controller;
    private float verticalRotation = 0f;
    private Vector3 velocity;
    private int skipLookFrames;

    void Awake()
    {
        if (hideBody)
            foreach (var r in GetComponents<Renderer>()) r.enabled = false;
    }

    void OnEnable()
    {
        // После выхода из-за компьютера курсор снова блокируется, и первый delta мыши
        // бывает огромным — пропускаем пару кадров, чтобы камера не дёргалась
        skipLookFrames = 2;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null) playerCamera = GetComponentInChildren<CinemachineCamera>(true);
        if (playerCamera != null) defaultFov = targetFov = playerCamera.Lens.FieldOfView;
        else Debug.LogWarning("[FirstPersonController] Не найдена камера игрока — зум отключён", this);

        // Прячем и блокируем курсор по центру экрана при старте игры
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleZoom();
        HandleMouseLook();
        HandleMovement();
    }

    public float Pitch => verticalRotation;

    // Повернуть игрока и голову (для катсцен). Работает и при выключенном контроллере,
    // а после включения обзор продолжится с этого положения без рывка.
    public void SetLook(float yaw, float pitch)
    {
        verticalRotation = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraRoot != null) cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    // Колёсико вперёд — приблизить, назад — отдалить до обычного угла обзора
    private void HandleZoom()
    {
        if (playerCamera == null || Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0f)
            targetFov = Mathf.Clamp(targetFov - Mathf.Sign(scroll) * zoomStep, minFov, defaultFov);

        var lens = playerCamera.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, targetFov, 1f - Mathf.Exp(-zoomSmooth * Time.deltaTime));
        playerCamera.Lens = lens;
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null || cameraRoot == null) return;
        if (skipLookFrames > 0) { skipLookFrames--; return; }

        // Получаем движение мыши за кадр из новой Input System
        // При зуме мышь медленнее, чтобы прицеливаться было удобно
        float zoomFactor = playerCamera != null ? playerCamera.Lens.FieldOfView / defaultFov : 1f;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity * zoomFactor;

        // 1. Вращение вверх-вниз (наклоняем только шею CameraRoot)
        verticalRotation -= mouseDelta.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        cameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // 2. Вращение влево-вправо (поворачиваем всё тело Player)
        transform.Rotate(Vector3.up * mouseDelta.x);
    }

    private void HandleMovement()
    {
        if (Keyboard.current == null) return;

        // Читаем нажатия WASD напрямую
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;

        // Нормализуем вектор, чтобы по диагонали игрок не бегал быстрее
        input = input.normalized;

        // Двигаем персонажа вперед и вбок относительно того, куда он смотрит
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Простая гравитация, чтобы персонаж прижимался к полу
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}