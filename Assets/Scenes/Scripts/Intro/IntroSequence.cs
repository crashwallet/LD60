using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[Serializable]
public class CallLine {
    [TextArea(2, 4)] public string ru;
    [TextArea(2, 4)] public string en;
    [Tooltip("Запись голоса (русская). Пусто — синтезированное «бормотание».")]
    public AudioClip voiceRu;
    [Tooltip("Запись голоса (английская). Пусто — синтезированное «бормотание».")]
    public AudioClip voiceEn;
    [Tooltip("Сколько помех к концу реплики (0 — чисто, 1 — почти ничего не разобрать).")]
    [Range(0f, 1f)] public float glitch;
    [Tooltip("Длительность реплики в секундах. 0 — по длине текста (или длине записи).")]
    public float duration;
}

[Serializable]
public class StoryCall {
    [Tooltip("Звонит после того, как выполнено столько заданий (1 — после первого).")]
    public int afterTasks = 1;
    public string speakerRu = "Шеф";
    public string speakerEn = "Boss";
    public List<CallLine> lines = new();
    [NonSerialized] public bool queued;
}

// Сюжетные сцены с телефоном.
// Вступление: игрок входит с порога, закрывает дверь, звонит телефон у monitor.002, голос с помехами даёт задание,
// связь обрывается, трубка кладётся — управление игроку.
// Концовка (после последнего задания): помощник прощается и исчезает, звонит шеф — вирус, данные украдены. «Конец».
// Пробел или Esc — пропустить вступление.
public class IntroSequence : MonoBehaviour {
    [Tooltip("Играть вступление, даже если сцена запущена напрямую (не из главного меню). Удобно для проверки.")]
    [SerializeField] bool playWhenStartedDirectly = true;

    [Header("Игрок")]
    [SerializeField] FirstPersonController player;
    [SerializeField] float walkSpeed = 1.3f;
    [Tooltip("Сколько метров пройти от порога внутрь.")]
    [SerializeField] float walkInDistance = 1.5f;
    [Tooltip("На каком расстоянии от телефона остановиться.")]
    [SerializeField] float phoneStandDistance = 0.8f;
    [Tooltip("Необязательно: точная точка, куда встать у телефона.")]
    [SerializeField] Transform phoneStandPoint;

    [Header("Дверь")]
    [SerializeField] Transform door;
    [Tooltip("Петли с другой стороны двери.")]
    [SerializeField] bool hingeOnOtherSide;
    [Tooltip("Дверь открывается в другую сторону.")]
    [SerializeField] bool flipOpenDirection;
    [SerializeField] float doorOpenAngle = 80f;
    [Tooltip("Поправка направления, куда игрок входит и смотрит во вступлении (градусы, + — вправо). Если идёт «боком» — подкрути.")]
    [SerializeField] float yawOffset = 0f;

    // Геометрия двери, посчитанная в редакторе по вершинам (Tools → Game → Настроить вступление)
    [SerializeField, HideInInspector] bool doorBaked;
    [SerializeField, HideInInspector] Vector3 bakedDoorCenter;
    [SerializeField, HideInInspector] Vector3 bakedWidthAxis;
    [SerializeField, HideInInspector] float bakedHalfWidth;
    [SerializeField, HideInInspector] float bakedHalfThick;

    public void BakeDoor(Vector3 center, Vector3 widthDir, float halfWidth, float halfThick) {
        doorBaked = true;
        bakedDoorCenter = center;
        bakedWidthAxis = widthDir.normalized;
        bakedHalfWidth = halfWidth;
        bakedHalfThick = halfThick;
    }

    [Header("Темнота за дверью")]
    [Tooltip("Чёрный куб снаружи дверного проёма — чтобы за открытой дверью была темнота.")]
    [SerializeField] bool createDarkVoid = true;
    [SerializeField] Material voidMaterial;
    [SerializeField] float voidDepth = 3f;

    [Header("Телефон")]
    [Tooltip("Трубка (объект phone) — её поднимаем.")]
    [SerializeField] Transform phoneHandset;
    [Tooltip("Аппарат (phone_base) — отсюда звонит звонок.")]
    [SerializeField] Transform phoneBase;

    [Header("Свои звуки (пусто — сгенерированные)")]
    [SerializeField] AudioClip ringClip;
    [SerializeField] AudioClip doorCloseClip;
    [SerializeField] AudioClip pickUpClip;
    [SerializeField] AudioClip hangUpClip;
    [SerializeField] AudioClip lineStaticClip;
    [SerializeField] AudioClip cutClip;
    [SerializeField] AudioClip busyToneClip;

    [Header("Звонок во вступлении")]
    [SerializeField] string speakerRu = "Голос в трубке";
    [SerializeField] string speakerEn = "Voice on the phone";
    [SerializeField] List<CallLine> lines = new() {
        new CallLine { ru = "Алло? Ты на месте? Хорошо... Слушай внимательно, связь плохая.",
                       en = "Hello? Are you there? Good... Listen carefully, the connection is bad.", glitch = 0.1f },
        new CallLine { ru = "Нужно проверить три компьютера в этом кабинете. Все три. Убедись, что они работают.",
                       en = "You need to check the three computers in this office. All three. Make sure they work.", glitch = 0.15f },
        new CallLine { ru = "Последние дни системы ведут себя... странно. Смотри внимательно, что именно ты делаешь.",
                       en = "The systems have been acting... strange lately. Pay attention to what exactly you are doing.", glitch = 0.35f },
        new CallLine { ru = "И будь осторожен. Если увидишь что-то странное, сразу выключай и...",
                       en = "And be careful. If you see anything strange, shut it down at once and...", glitch = 0.7f },
    };

    [Header("Звонки по ходу игры (шеф, сисадмин)")]
    [Tooltip("Через сколько секунд после выполненного задания зазвонит телефон.")]
    [SerializeField] float callDelay = 5f;
    [SerializeField] List<StoryCall> calls = new() {
        new StoryCall { afterTasks = 1, speakerRu = "Сисадмин", speakerEn = "Sysadmin", lines = new() {
            new CallLine { ru = "Алло, это сисадмин. Ну как там, компьютеры запускаются?",
                           en = "Hello, it's the sysadmin. So, are the computers starting up?", glitch = 0.05f },
            new CallLine { ru = "Хорошо. Слушай, на первом есть архив, inventory.txt. Я ему недавно сменил ключ — теперь сдвиг на семь.",
                           en = "Good. Listen, there's an archive on the first one, inventory.txt. I changed its key recently — the shift is seven now.", glitch = 0.05f },
            new CallLine { ru = "Ладно, работай. Я ещё позвоню.",
                           en = "Okay, keep working. I'll call again.", glitch = 0.05f },
        }},
        new StoryCall { afterTasks = 3, speakerRu = "Шеф", speakerEn = "Boss", lines = new() {
            new CallLine { ru = "Это шеф. Ну что, как продвигается? Второй компьютер ожил?",
                           en = "It's the boss. So, how's it going? Is the second computer alive?", glitch = 0.05f },
            new CallLine { ru = "Отлично. Мне к утру нужен отчёт, что всё работает. И не задерживайся там слишком долго.",
                           en = "Great. I need a report by morning that everything works. And don't stay there too long.", glitch = 0.05f },
        }},
        new StoryCall { afterTasks = 4, speakerRu = "Сисадмин", speakerEn = "Sysadmin", lines = new() {
            new CallLine { ru = "Снова я. Странное дело — у меня в мониторинге скачок трафика из вашего кабинета.",
                           en = "Me again. Strange thing — my monitoring shows a traffic spike from your office.", glitch = 0.1f },
            new CallLine { ru = "Наверное, обновления качаются... Ладно. Проверь третий компьютер и иди домой.",
                           en = "Probably updates downloading... Okay. Check the third computer and go home.", glitch = 0.15f },
        }},
    };

    [Header("Концовка")]
    [SerializeField] bool playEnding = true;
    [SerializeField] string bossRu = "Шеф";
    [SerializeField] string bossEn = "Boss";
    [SerializeField] List<CallLine> endingLines = new() {
        new CallLine { ru = "Алло! Это шеф. Хорошо, что ты ещё там... Слушай внимательно.",
                       en = "Hello! It's the boss. Good, you're still there... Listen carefully.", glitch = 0.05f },
        new CallLine { ru = "Нас атаковали. В сети был вирус — он сидел там всё это время.",
                       en = "We've been attacked. There was a virus in the network — it was there the whole time.", glitch = 0.05f },
        new CallLine { ru = "Все данные украдены. Базы, переписка, проект «NIGHTJAR» — хакеры забрали всё.",
                       en = "All our data has been stolen. Databases, mail, project NIGHTJAR — the hackers took everything.", glitch = 0.05f },
        new CallLine { ru = "Ты... ты ведь ничего там не запускал?",
                       en = "You... you didn't run anything there, did you?", glitch = 0.05f },
    };
    [SerializeField] string theEndRu = "КОНЕЦ";
    [SerializeField] string theEndEn = "THE END";
    [SerializeField] string thanksRu = "Спасибо за игру!";
    [SerializeField] string thanksEn = "Thanks for playing!";
    [SerializeField] float endTitleTime = 5f;
    [Tooltip("После титров вернуться в главное меню (если оно есть в Build Settings).")]
    [SerializeField] bool returnToMenu = true;

    CharacterController cc;
    Bounds doorBounds;
    Vector3 doorCenter, doorClosedPos, hingePoint, inward, widthAxis;
    float doorHalfWidth, doorHalfThick;
    Vector3 walkDir;
    Quaternion doorClosedRot;
    float doorSign = 1f;

    Vector3 handsetStartPos, handsetStartCenter, handsetLocalCenter, handsetLong, handsetThin;
    Quaternion handsetStartRot;
    bool handsetHasMesh;

    Vector3 standPoint;
    float standYaw, standPitch, floorY;
    AudioSource ringSource, lineSource, voiceSource;
    Coroutine main;
    bool running, holdingHandset, endingStarted, prepared;

    readonly Queue<StoryCall> pendingCalls = new();
    StoryCall ringingCall;
    Coroutine ringRoutine;

    public static bool IsPlaying { get; private set; }

    void Start() {
        if (player == null) return;
        floorY = player.transform.position.y;
        Prepare();
        if (createDarkVoid) CreateDarkVoid();
        PhoneInteractable.Attach(this, phoneHandset);
        PhoneInteractable.Attach(this, phoneBase);

        ProgressionManager.OnTaskChanged += OnTaskChanged;

        bool play = GameFlow.PlayIntro || playWhenStartedDirectly;
        GameFlow.PlayIntro = false;
        if (play) main = StartCoroutine(RunIntro());
    }

    void OnDestroy() {
        ProgressionManager.OnTaskChanged -= OnTaskChanged;
        if (running) IsPlaying = false;
    }

    void Update() {
        if (!running || endingStarted || Keyboard.current == null) return;
        if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) SkipIntro();
    }

    void LateUpdate() {
        if (holdingHandset) HandsetPose(1f);
    }

    // ================= Вступление =================

    IEnumerator RunIntro() {
        var ui = CinematicUI.Get();
        ui.SetFade(1f);
        BeginCutscene();

        // 1. На пороге, дверь открыта
        SetDoor(1f);
        Vector3 start = Flat(doorCenter) + inward * 0.15f;
        start.y = floorY;
        player.transform.position = start;
        player.SetLook(Yaw(walkDir), 4f);
        yield return ui.Fade(1f, 0f, 1.2f);

        // 2. Проходим внутрь, оборачиваемся, закрываем дверь
        yield return Walk(start, start + walkDir * walkInDistance, Yaw(walkDir), 4f);
        yield return Turn(Yaw(-walkDir), 6f, 0.9f);
        for (float t = 0; t < 0.7f; t += Time.deltaTime) {
            float k = t / 0.7f;
            SetDoor(1f - k * k); // ускоряется к концу
            yield return null;
        }
        SetDoor(0f);
        AudioSource.PlayClipAtPoint(doorCloseClip != null ? doorCloseClip : SynthAudio.DoorThud(), door != null ? door.position : start, 1f);
        yield return new WaitForSeconds(0.9f);

        // 3. Телефон
        yield return RingAndPickUp();
        yield return Call(lines, Loc.T(speakerRu, speakerEn), true);
        yield return HangUp();

        EndCutscene();
    }

    void SkipIntro() {
        if (main != null) StopCoroutine(main);
        StopAudio();
        holdingHandset = false;
        SetDoor(0f);
        HandsetPose(0f);

        var ui = CinematicUI.Get();
        ui.HideSubtitle();
        ui.SetFade(0f);

        // Пропустили посреди сцены — ставим игрока у телефона
        player.transform.position = standPoint;
        player.SetLook(standYaw, 0f);
        EndCutscene();
    }

    // ================= Концовка =================

    void OnTaskChanged(TaskData t) {
        var pm = ProgressionManager.Instance;
        if (pm == null || pm.Current == 0) return;

        if (t != null) {
            // Звонки по ходу игры: после N выполненных заданий
            foreach (var c in calls)
                if (c.afterTasks == pm.Current && !c.queued) { c.queued = true; pendingCalls.Enqueue(c); }
            if (ringRoutine == null && ringingCall == null && pendingCalls.Count > 0) ringRoutine = StartCoroutine(RingLater());
            return;
        }

        if (!playEnding || endingStarted) return;
        endingStarted = true;
        // Концовка важнее: несостоявшиеся звонки отменяем
        pendingCalls.Clear();
        if (ringRoutine != null) { StopCoroutine(ringRoutine); ringRoutine = null; }
        ringingCall = null;
        if (ringSource != null) ringSource.Stop();
        StartCoroutine(RunEnding());
    }

    // ================= Звонки по ходу игры =================

    public bool IsRinging => ringingCall != null;

    IEnumerator RingLater() {
        yield return new WaitForSeconds(callDelay);
        while (running) yield return null; // не перебиваем катсцену
        if (pendingCalls.Count == 0 || endingStarted) { ringRoutine = null; yield break; }

        ringingCall = pendingCalls.Dequeue();
        ringSource = Source3D(PhonePos(), ringClip != null ? ringClip : SynthAudio.Ring(), true);
        ringSource.Play();
        ringRoutine = null;

        // Короткая подсказка, даже если игрок сидит за компьютером
        var ui = CinematicUI.Get();
        ui.ShowSubtitle("<i>" + Loc.T("[Звонит телефон]", "[The phone is ringing]") + "</i>");
        yield return new WaitForSeconds(3f);
        if (!running) ui.HideSubtitle();
    }

    // Игрок нажал ЛКМ по телефону (PhoneInteractable)
    public void AnswerCall() {
        if (ringingCall == null || running) return;
        StartCoroutine(AnswerRoutine(ringingCall));
        ringingCall = null;
    }

    IEnumerator AnswerRoutine(StoryCall call) {
        floorY = player.transform.position.y;
        BeginCutscene();

        // Поворачиваемся к телефону, где стоим
        Vector3 phone = PhonePos();
        Vector3 look = Flat(phone) - Flat(player.transform.position);
        float eyeY = player.cameraRoot != null ? player.cameraRoot.position.y : player.transform.position.y + 0.6f;
        float pitch = Mathf.Atan2(eyeY - phone.y, Mathf.Max(0.1f, look.magnitude)) * Mathf.Rad2Deg;
        yield return Turn(Yaw(look), pitch, 0.5f);

        yield return PickUp(Yaw(look));
        yield return Call(call.lines, Loc.T(call.speakerRu, call.speakerEn), false);
        yield return HangUp();
        EndCutscene();

        if (pendingCalls.Count > 0 && ringRoutine == null) ringRoutine = StartCoroutine(RingLater());
    }

    IEnumerator RunEnding() {
        // Ждём, пока помощник попрощается и исчезнет
        for (float t = 0; t < 20f && !HelperController.Gone; t += Time.deltaTime) yield return null;
        yield return new WaitForSeconds(1.5f);

        // Встаём из-за компьютера
        foreach (var m in FindObjectsByType<MonitorInteract>(FindObjectsSortMode.None)) m.ExitSeat();
        yield return new WaitForSeconds(1.2f); // камера возвращается к игроку

        floorY = player.transform.position.y;
        standPoint.y = floorY;
        BeginCutscene();

        yield return RingAndPickUp();
        yield return Call(endingLines, Loc.T(bossRu, bossEn), false);
        yield return HangUp();

        // Титры
        var ui = CinematicUI.Get();
        yield return ui.Fade(0f, 1f, 2f);
        ui.ShowTitle(Loc.T(theEndRu, theEndEn), Loc.T(thanksRu, thanksEn));
        yield return new WaitForSeconds(endTitleTime);

        if (returnToMenu && Application.CanStreamedLevelBeLoaded(GameFlow.MenuScene)) {
            SceneManager.LoadScene(GameFlow.MenuScene);
        } else {
            ui.HideTitle();
            yield return ui.Fade(1f, 0f, 1f);
            EndCutscene();
        }
    }

    // ================= Общие части =================

    void BeginCutscene() {
        running = IsPlaying = true;
        cc = player.GetComponent<CharacterController>();
        player.enabled = false;
        if (cc != null) cc.enabled = false;
    }

    void EndCutscene() {
        running = IsPlaying = false;
        holdingHandset = false;
        player.SetLook(player.transform.eulerAngles.y, player.Pitch);
        if (cc != null) cc.enabled = true;
        player.enabled = true;
    }

    // Звонок → подойти к телефону → поднять трубку
    IEnumerator RingAndPickUp() {
        var ui = CinematicUI.Get();
        ringSource = Source3D(PhonePos(), ringClip != null ? ringClip : SynthAudio.Ring(), true);
        ringSource.Play();
        ui.ShowSubtitle("<i>" + Loc.T("[Звонит телефон]", "[The phone is ringing]") + "</i>");
        yield return new WaitForSeconds(1.0f);

        // Идём к точке у телефона, глядя по ходу движения, и только там поворачиваемся к телефону
        // (раньше смотрели на телефон во время ходьбы — получалось «боком» и рывок на ~30°)
        Vector3 toStand = Flat(standPoint) - Flat(player.transform.position);
        float walkYaw = toStand.sqrMagnitude > 0.01f ? Yaw(toStand) : standYaw;
        yield return Turn(walkYaw, 8f, 0.8f);
        yield return Walk(player.transform.position, standPoint, walkYaw, 8f);
        yield return Turn(standYaw, standPitch, 0.6f);
        yield return new WaitForSeconds(0.5f);
        yield return PickUp(standYaw);
    }

    // Снять трубку и поднести к уху
    IEnumerator PickUp(float yaw) {
        if (ringSource != null) ringSource.Stop();
        CinematicUI.Get().HideSubtitle();
        Play2D(pickUpClip != null ? pickUpClip : SynthAudio.Click(1), 0.8f);
        for (float t = 0; t < 0.7f; t += Time.deltaTime) {
            HandsetPose(Mathf.SmoothStep(0f, 1f, t / 0.7f));
            yield return null;
        }
        holdingHandset = true;
        yield return Turn(yaw, 0f, 0.5f);
    }

    // Разговор. cutOff — связь обрывается на последней реплике (помехи), иначе собеседник просто кладёт трубку.
    IEnumerator Call(List<CallLine> callLines, string speakerName, bool cutOff) {
        var ui = CinematicUI.Get();
        lineSource = Source2D(lineStaticClip != null ? lineStaticClip : SynthAudio.Static(), true, 0.18f);
        lineSource.Play();
        voiceSource = Source2D(null, false, 1f);
        yield return new WaitForSeconds(0.6f);

        string speaker = $"<color=#9fd9b0>{speakerName}:</color> ";
        for (int i = 0; i < callLines.Count; i++) {
            var line = callLines[i];
            bool last = i == callLines.Count - 1;
            string text = Loc.T(line.ru, line.en);
            var recorded = Loc.IsEnglish ? line.voiceEn : line.voiceRu;
            float dur = line.duration > 0 ? line.duration
                      : recorded != null ? recorded.length
                      : Mathf.Clamp(text.Length / 14f, 2f, 7f);

            voiceSource.clip = recorded != null ? recorded : SynthAudio.Mumble(dur, i + 1 + (cutOff ? 0 : 50));
            voiceSource.Play();

            float until = last && cutOff ? dur * 0.75f : dur; // обрыв на полуслове
            float nextUpdate = 0f;
            for (float t = 0; t < until; t += Time.deltaTime) {
                float g = line.glitch * (t / dur);
                lineSource.volume = 0.18f + g * 0.5f * Mathf.PerlinNoise(t * 6f, i);
                if (t >= nextUpdate) {
                    // Субтитры показываем по мере «произнесения», с помехами
                    int shown = Mathf.Clamp(Mathf.CeilToInt(text.Length * Mathf.Min(1f, t / (dur * 0.85f))), 1, text.Length);
                    ui.ShowSubtitle(speaker + Glitch(text.Substring(0, shown), g));
                    nextUpdate = t + 0.07f;
                }
                yield return null;
            }

            if (!(last && cutOff)) {
                ui.ShowSubtitle(speaker + Glitch(text, line.glitch * 0.5f));
                yield return new WaitForSeconds(last ? 1.2f : 0.45f);
            }
        }

        voiceSource.Stop();
        lineSource.Stop();
        if (cutOff && callLines.Count > 0) {
            Play2D(cutClip != null ? cutClip : SynthAudio.StaticBurst(), 1f);
            var lastLine = callLines[^1];
            ui.ShowSubtitle(speaker + Glitch(Loc.T(lastLine.ru, lastLine.en), 1f));
            yield return new WaitForSeconds(0.45f);
            ui.HideSubtitle();
            yield return new WaitForSeconds(0.6f);
        } else {
            Play2D(hangUpClip != null ? hangUpClip : SynthAudio.Click(3), 0.6f); // собеседник положил трубку
            yield return new WaitForSeconds(0.4f);
        }

        var busy = busyToneClip != null ? busyToneClip : SynthAudio.BusyTone();
        Play2D(busy, 0.7f);
        ui.ShowSubtitle("<i>" + (cutOff ? Loc.T("[Связь оборвалась]", "[The line went dead]")
                                        : Loc.T("[Короткие гудки]", "[Busy tone]")) + "</i>");
        yield return new WaitForSeconds(busy.length * 0.8f);
    }

    IEnumerator HangUp() {
        holdingHandset = false;
        for (float t = 0; t < 0.7f; t += Time.deltaTime) {
            HandsetPose(1f - Mathf.SmoothStep(0f, 1f, t / 0.7f));
            yield return null;
        }
        HandsetPose(0f);
        Play2D(hangUpClip != null ? hangUpClip : SynthAudio.Click(2), 0.8f);
        CinematicUI.Get().HideSubtitle();
    }

    void StopAudio() {
        if (ringSource != null) ringSource.Stop();
        if (lineSource != null) lineSource.Stop();
        if (voiceSource != null) voiceSource.Stop();
    }

    // ================= Подготовка геометрии =================

    void Prepare() {
        if (prepared) return;
        prepared = true;

        // Центр комнаты — среднее мониторов (или телефон)
        var monitors = FindObjectsByType<MonitorInteract>(FindObjectsSortMode.None);
        Vector3 room = Vector3.zero;
        foreach (var m in monitors) room += Bounds(m.transform).center;
        room = monitors.Length > 0 ? room / monitors.Length : PhonePos();

        // Дверь: оси берём из меша самой двери (комната может стоять под углом к осям мира).
        // Широкая горизонтальная ось — ширина, узкая — толщина.
        doorBounds = door != null ? Bounds(door) : new Bounds(player.transform.position, Vector3.one);
        doorCenter = doorBounds.center;
        Vector3 thickAxis;
        var doorMesh = door != null ? door.GetComponent<MeshFilter>() : null;
        if (doorBaked) {
            doorCenter = bakedDoorCenter;
            widthAxis = bakedWidthAxis;
            doorHalfWidth = bakedHalfWidth;
            doorHalfThick = bakedHalfThick;
            thickAxis = Vector3.Cross(Vector3.up, widthAxis).normalized;
        } else if (doorMesh != null && doorMesh.sharedMesh != null) {
            var lb = doorMesh.sharedMesh.bounds;
            doorCenter = door.TransformPoint(lb.center);
            Vector3[] half = {
                door.TransformVector(new Vector3(lb.extents.x, 0, 0)),
                door.TransformVector(new Vector3(0, lb.extents.y, 0)),
                door.TransformVector(new Vector3(0, 0, lb.extents.z)),
            };
            // Вертикальная ось — та, что больше всего смотрит вверх; из двух оставшихся шире — ширина
            int v = 0;
            for (int i = 1; i < 3; i++) if (Mathf.Abs(half[i].normalized.y) > Mathf.Abs(half[v].normalized.y)) v = i;
            int a = (v + 1) % 3, b = (v + 2) % 3;
            Vector3 wa = Flat(half[a]), wb = Flat(half[b]);
            Vector3 w = wa.magnitude >= wb.magnitude ? wa : wb, th = wa.magnitude >= wb.magnitude ? wb : wa;
            widthAxis = w.normalized;
            doorHalfWidth = w.magnitude;
            thickAxis = th.sqrMagnitude > 1e-8f ? th.normalized : Vector3.Cross(Vector3.up, widthAxis);
            doorHalfThick = th.magnitude;
        } else {
            widthAxis = doorBounds.size.x >= doorBounds.size.z ? Vector3.right : Vector3.forward;
            thickAxis = widthAxis == Vector3.right ? Vector3.forward : Vector3.right;
            doorHalfWidth = Vector3.Dot(doorBounds.extents, widthAxis);
            doorHalfThick = Vector3.Dot(doorBounds.extents, thickAxis);
        }
        float s = Vector3.Dot(room - doorCenter, thickAxis);
        inward = thickAxis * (s >= 0 ? 1f : -1f);
        // Куда игрок идёт и смотрит во вступлении — «внутрь» плюс ручная поправка
        walkDir = Quaternion.AngleAxis(yawOffset, Vector3.up) * inward;

        if (door != null) {
            doorClosedPos = door.position;
            doorClosedRot = door.rotation;
            hingePoint = doorCenter + widthAxis * doorHalfWidth * (hingeOnOtherSide ? -1f : 1f);
            // Дверь открывается внутрь комнаты: выбираем знак угла, при котором свободный край уходит внутрь
            Vector3 freeEdge = 2f * doorCenter - hingePoint;
            Vector3 rotated = Quaternion.AngleAxis(doorOpenAngle, Vector3.up) * (freeEdge - hingePoint);
            doorSign = Vector3.Dot(rotated, inward) > 0 ? 1f : -1f;
            if (flipOpenDirection) doorSign = -doorSign;
        }

        // Трубка: запоминаем, как лежит, и её длинную/плоскую оси (чтобы поднести к уху вертикально)
        if (phoneHandset != null) {
            handsetStartPos = phoneHandset.position;
            handsetStartRot = phoneHandset.rotation;
            handsetStartCenter = Bounds(phoneHandset).center;
            var mf = phoneHandset.GetComponent<MeshFilter>();
            handsetHasMesh = mf != null && mf.sharedMesh != null;
            if (handsetHasMesh) {
                var lb = mf.sharedMesh.bounds;
                handsetLocalCenter = lb.center;
                handsetLong = MaxAxis(lb.extents);
                handsetThin = MinAxis(lb.extents);
            }
        }

        // Куда встать у телефона и как на него смотреть
        Vector3 phone = PhonePos();
        if (phoneStandPoint != null) standPoint = phoneStandPoint.position;
        else {
            // Встаём со стороны центра комнаты (там, где проход), а не за столом
            Vector3 dir = Flat(phone) - Flat(room);
            if (dir.sqrMagnitude < 0.01f) dir = -inward;
            standPoint = Flat(phone) - dir.normalized * phoneStandDistance;
        }
        standPoint.y = floorY;
        Vector3 look = Flat(phone) - Flat(standPoint);
        standYaw = Yaw(look);
        float eyeY = player.cameraRoot != null ? player.cameraRoot.position.y - player.transform.position.y + floorY : floorY + 0.6f;
        standPitch = Mathf.Atan2(eyeY - phone.y, Mathf.Max(0.1f, look.magnitude)) * Mathf.Rad2Deg;
    }

    // Чёрный куб снаружи проёма: за открытой дверью — темнота
    void CreateDarkVoid() {
        if (door == null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "DarkVoid";
        go.transform.SetParent(transform, false);

        float width = doorHalfWidth * 2f + 0.8f;
        float height = doorBounds.size.y + 0.8f;
        Vector3 center = doorCenter - inward * (doorHalfThick + voidDepth * 0.5f + 0.05f);

        go.transform.position = center;
        go.transform.rotation = Quaternion.LookRotation(inward, Vector3.up);
        go.transform.localScale = new Vector3(width, height, voidDepth);

        var r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        if (voidMaterial != null) r.sharedMaterial = voidMaterial;
        else {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            var m = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", Color.black);
            r.sharedMaterial = m;
        }
    }

    void SetDoor(float open01) {
        if (door == null) return;
        door.SetPositionAndRotation(doorClosedPos, doorClosedRot);
        door.RotateAround(hingePoint, Vector3.up, doorOpenAngle * doorSign * Mathf.Clamp01(open01));
    }

    // k = 0 — трубка лежит как лежала, k = 1 — у правого уха вертикально
    void HandsetPose(float k) {
        if (phoneHandset == null) return;
        var cam = Camera.main != null ? Camera.main.transform : player.cameraRoot;
        Vector3 ear = cam.position + cam.right * 0.13f - cam.up * 0.06f + cam.forward * 0.07f;

        if (!handsetHasMesh) {
            phoneHandset.position = handsetStartPos + Vector3.Lerp(Vector3.zero, ear - handsetStartCenter, k);
            return;
        }

        // Длинная ось трубки — от уха ко рту (вниз и чуть вперёд), плоская сторона — к голове
        Vector3 longW = (cam.up - cam.forward * 0.6f).normalized;
        Vector3 thinW = -cam.right;
        Quaternion atEar = Quaternion.LookRotation(thinW, longW) * Quaternion.Inverse(Quaternion.LookRotation(handsetThin, handsetLong));

        phoneHandset.rotation = Quaternion.Slerp(handsetStartRot, atEar, k);
        // Двигаем так, чтобы центр трубки шёл по прямой от места на столе к уху (небольшая дуга вверх)
        Vector3 wantCenter = Vector3.Lerp(handsetStartCenter, ear, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.08f;
        Vector3 nowCenter = phoneHandset.TransformPoint(handsetLocalCenter);
        phoneHandset.position += wantCenter - nowCenter;
        if (k <= 0f) phoneHandset.SetPositionAndRotation(handsetStartPos, handsetStartRot);
    }

    // ================= Движение =================

    IEnumerator Walk(Vector3 from, Vector3 to, float yaw, float pitch) {
        from.y = to.y = floorY;
        float dist = Vector3.Distance(from, to);
        float time = Mathf.Max(0.2f, dist / walkSpeed);
        float startYaw = player.transform.eulerAngles.y;
        for (float t = 0; t < time; t += Time.deltaTime) {
            float k = Mathf.SmoothStep(0f, 1f, t / time);
            player.transform.position = Vector3.Lerp(from, to, k);
            float bob = Mathf.Sin(t * 9f) * 0.6f; // покачивание головы при шаге
            player.SetLook(Mathf.LerpAngle(startYaw, yaw, Mathf.Min(1f, t / 0.4f)), pitch + bob);
            yield return null;
        }
        player.transform.position = to;
        player.SetLook(yaw, pitch);
    }

    IEnumerator Turn(float yaw, float pitch, float time) {
        float y0 = player.transform.eulerAngles.y, p0 = player.Pitch;
        for (float t = 0; t < time; t += Time.deltaTime) {
            float k = Mathf.SmoothStep(0f, 1f, t / time);
            player.SetLook(Mathf.LerpAngle(y0, yaw, k), Mathf.Lerp(p0, pitch, k));
            yield return null;
        }
        player.SetLook(yaw, pitch);
    }

    // ================= Звук =================

    AudioSource Source3D(Vector3 pos, AudioClip clip, bool loop) {
        var go = new GameObject("StoryAudio3D");
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        var s = go.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = loop;
        s.spatialBlend = 1f;
        s.minDistance = 1f;
        s.maxDistance = 15f;
        return s;
    }

    AudioSource Source2D(AudioClip clip, bool loop, float volume) {
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = loop;
        s.volume = volume;
        s.spatialBlend = 0f;
        return s;
    }

    void Play2D(AudioClip clip, float volume) {
        var s = Source2D(clip, false, volume);
        s.Play();
        Destroy(s, clip.length + 0.1f);
    }

    // ================= Утилиты =================

    Vector3 PhonePos() {
        if (phoneBase != null) return Bounds(phoneBase).center;
        if (phoneHandset != null) return Bounds(phoneHandset).center;
        return player.transform.position + player.transform.forward * 2f;
    }

    static string Glitch(string s, float amount) {
        if (amount <= 0.01f) return s;
        const string junk = "#%&@*/\\?~";
        var chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
            if (!char.IsWhiteSpace(chars[i]) && UnityEngine.Random.value < amount * 0.55f)
                chars[i] = junk[UnityEngine.Random.Range(0, junk.Length)];
        return new string(chars);
    }

    static Vector3 MaxAxis(Vector3 e) => e.x >= e.y && e.x >= e.z ? Vector3.right : e.y >= e.z ? Vector3.up : Vector3.forward;
    static Vector3 MinAxis(Vector3 e) => e.x <= e.y && e.x <= e.z ? Vector3.right : e.y <= e.z ? Vector3.up : Vector3.forward;

    static Bounds Bounds(Transform t) {
        var r = t.GetComponent<Renderer>();
        if (r == null) r = t.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds : new Bounds(t.position, Vector3.one * 0.2f);
    }

    static Vector3 Flat(Vector3 v) => new(v.x, 0f, v.z);

    static float Yaw(Vector3 dir) => Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
}
