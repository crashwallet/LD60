using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Tools → Game → Наполнить компьютеры и ящики
// Создаёт файлы, задания (Цезарь → Виженер → DES), английские названия, записки в ящиках.
// Повторный запуск безопасен: существующее не перезаписывается, недостающее добавляется.
public static class ContentSetup {
    const string Data = "Assets/Scenes/OS_Data";
    const string StickerPath = "Assets/Scenes/office second/sprites/paper_hint.png";
    const string PhotoPath = "Assets/Scenes/office second/sprites/photo_2026-10-17_23-40-30.png";

    // ---- Головоломки (шифры всегда на английском) ----
    public const string VigenereKey = "GHOST";   // по стикеру: (3,1) → (1,2) → (2,3) → (3,3) → (4,4)
    const string LetterGrid = "R H A K\nN E O B\nG L S P\nD M X T";
    const string VigenerePlain = "MEET ME AT THE OLD SERVER ROOM AT MIDNIGHT. THE PASSWORD IS LANTERN";
    const string VigenereAnswer = "LANTERN";
    public const string DesKey = "NIGHTJAR";
    const string DesPlain = "SOMETHING COPIES OUR FILES EVERY NIGHT. ALL TRAFFIC GOES OUT THROUGH THIS TERMINAL. LAST WORD: MIRROR";

    // Тексты, которые были слишком «в лоб» про помощника — заменяются на новые, если их не правили вручную
    const string OldDesPlain = "THEY ARE WATCHING THROUGH THE SCREENS. DO NOT TRUST THE HELPER. LAST WORD: MIRROR";
    const string ResignRu = "Заявление\n\nПрошу уволить меня по собственному желанию.\nПричина: больше не могу работать по ночам. Здесь слишком... тихо.\n\n(переписать)";
    const string ResignEn = "Resignation letter\n\nI hereby resign.\nReason: I can't work night shifts anymore. It's too... quiet here.\n\n(rewrite)";
    const string OldResignRu = "Заявление\n\nПрошу уволить меня по собственному желанию, потому что компьютер со мной разговаривает.\n\n(нет, так нельзя писать. переписать)";
    const string TodoRu = "1. Отчёт до пятницы\n2. Позвонить маме\n3. Спросить у ИТ, откуда на рабочем столе новая программа";
    const string TodoEn = "1. Report by Friday\n2. Call mom\n3. Ask IT where the new program on my desktop came from";
    const string OldTodoRu = "1. Отчёт до пятницы\n2. Позвонить маме\n3. НЕ отвечать «помощнику», если он снова начнёт";
    const string EmailRu = "Кому: ИТ-отдел\n\nВ ПОСЛЕДНИЙ раз прошу: проверьте, почему компьютер сам открывает файлы по ночам. Утром всё лежит не на своих местах!!!";
    const string EmailEn = "To: IT department\n\nFor the LAST time: check why the computer opens files by itself at night. Every morning things are not where I left them!!!";
    const string OldEmailRu = "Кому: ИТ-отдел\n\nВ ПОСЛЕДНИЙ раз прошу: уберите эту прыгающую закорючку с экрана! Она смотрит на меня!!!";
    const string DesAnswer = "MIRROR";

    [MenuItem("Tools/Game/Наполнить компьютеры и ящики")]
    static void Run() {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            EditorUtility.DisplayDialog("Content", "Выйди из Play Mode и запусти ещё раз.", "OK");
            return;
        }
        var log = new List<string>();

        var sticker = EnsureSprite(StickerPath, true, log);
        var photo = EnsureSprite(PhotoPath, false, log);

        // ---------- PC1 (Цезарь) ----------
        var pc1Docs = Load<FolderData>($"{Data}/Documents.asset");
        var caesarHint = TextFile($"{Data}/Files/admin_note.txt.asset", "admin_note.txt",
            "Напоминание себе: всё сдвигается на три шага вперёд. Даже пароли.",
            "Note to self: everything moves three steps forward. Even passwords.", log);
        AddFile(pc1Docs, caesarHint, log);

        // ---------- PC2 (Виженер) ----------
        string pc2 = $"{Data}/PC2";
        var pc2Desktop = Load<FolderData>($"{pc2}/Desktop.asset");
        var pc2Docs = Load<FolderData>($"{pc2}/Documents.asset");
        var pc2Trash = Load<FolderData>($"{pc2}/Trash.asset");
        var pc2Photos = Folder($"{pc2}/Photos.asset", "Фото", "Photos", log);
        AddFolder(pc2Docs, pc2Photos, log);

        var projectX = CipherFile($"{pc2}/Files/project_x.txt.asset", "project_x.txt", CipherType.Vigenere, VigenereKey, VigenerePlain, log);
        AddFile(pc2Docs, projectX, log);
        AddFile(pc2Docs, TextFile($"{pc2}/Files/readme_security.txt.asset", "readme_security.txt",
            "Ключ от архива больше НЕ храню в компьютере.\nСтикер с точками — в ящике стола. Таблица с буквами — там же, рядом.\nСоедини точки — получишь ключ (5 букв).",
            "I no longer keep the archive key on this computer.\nThe sticker with dots is in the desk drawer. The letter table is there too.\nConnect the dots and you get the key (5 letters).", log), log);
        AddFile(pc2Docs, TextFile($"{pc2}/Files/letter_from_boss.txt.asset", "letter_from_boss.txt",
            "От: шеф\nТема: секретный проект\n\nКоллеги, с понедельника переходим на проект «NIGHTJAR». Никому не рассказывайте название.\nВсе логи третьего компьютера теперь шифруются — ключ: кодовое имя проекта.\n\nP.S. Кто опять ел мой йогурт?",
            "From: boss\nSubject: secret project\n\nColleagues, starting Monday we move to project \"NIGHTJAR\". Do not tell anyone the name.\nAll logs on the third computer are now encrypted — the key is the project codename.\n\nP.S. Who ate my yogurt again?", log), log);
        AddFile(pc2Docs, TextFile($"{pc2}/Files/meeting_notes.txt.asset", "meeting_notes.txt",
            "Планёрка, вторник:\n- принтер снова жуёт бумагу\n- кофемашина — вызвать мастера\n- кто-то ночью включает третий компьютер (??)\n- премии — «позже»",
            "Meeting, Tuesday:\n- the printer chews paper again\n- coffee machine: call a technician\n- someone turns on the third computer at night (??)\n- bonuses: \"later\"", log), log);
        AddFile(pc2Docs, TextFile($"{pc2}/Files/shopping_list.txt.asset", "shopping_list.txt",
            "Купить:\n- кофе (много)\n- батарейки для мышки\n- скотч\n- новый фикус (старый засох... странно, поливали же)",
            "Buy:\n- coffee (a lot)\n- batteries for the mouse\n- tape\n- a new ficus (the old one dried out... weird, we watered it)", log), log);
        var vacation = ImageFile($"{pc2}/Files/vacation.jpg.asset", "vacation.jpg", photo, log);
        AddFile(pc2Photos, vacation, log);
        AddFile(pc2Trash, TextFile($"{pc2}/Files/resignation_draft.txt.asset", "resignation_draft.txt",
            ResignRu, ResignEn, log), log);
        AddFile(pc2Desktop, TextFile($"{pc2}/Files/todo.txt.asset", "todo.txt",
            TodoRu, TodoEn, log), log);

        // ---------- PC3 (DES) ----------
        string pc3 = $"{Data}/PC3";
        var pc3Desktop = Load<FolderData>($"{pc3}/Desktop.asset");
        var pc3Docs = Load<FolderData>($"{pc3}/Documents.asset");
        var pc3Trash = Load<FolderData>($"{pc3}/Trash.asset");
        var pc3Work = Folder($"{pc3}/Work.asset", "Работа", "Work", log);
        AddFolder(pc3Docs, pc3Work, log);

        var sysLog = CipherFile($"{pc3}/Files/system_log.txt.asset", "system_log.txt", CipherType.Des, DesKey, DesPlain, log);
        AddFile(pc3Docs, sysLog, log);
        AddFile(pc3Work, TextFile($"{pc3}/Files/report_q3.txt.asset", "report_q3.txt",
            "Квартальный отчёт\n\nПродажи: стабильно.\nРасходы: на кофе выросли на 300%.\nИнциденты: 3 (компьютеры включались сами).",
            "Quarterly report\n\nSales: stable.\nExpenses: coffee up 300%.\nIncidents: 3 (computers turned on by themselves).", log), log);
        AddFile(pc3Work, TextFile($"{pc3}/Files/budget.txt.asset", "budget.txt",
            "Бюджет отдела:\nКанцелярия — 120\nКофе — 900\nНовый сервер — отклонено\nЭкзорцист — ??? (шутка)",
            "Department budget:\nStationery — 120\nCoffee — 900\nNew server — rejected\nExorcist — ??? (joke)", log), log);
        AddFile(pc3Docs, TextFile($"{pc3}/Files/birthday_party.txt.asset", "birthday_party.txt",
            "День рождения Лены — в пятницу в 17:00.\nСобираем по 200 на торт. Не говорите ей!",
            "Lena's birthday — Friday at 5 pm.\nCollecting 200 each for a cake. Don't tell her!", log), log);
        AddFile(pc3Trash, TextFile($"{pc3}/Files/angry_email_draft.txt.asset", "angry_email_draft.txt",
            EmailRu, EmailEn, log), log);
        AddFile(pc3Desktop, TextFile($"{pc3}/Files/notes.txt.asset", "notes.txt",
            "Пароль от почты сменил. Новый ключ шифрования логов знает только шеф (и его письма...).",
            "Changed the mail password. Only the boss knows the new log encryption key (and his letters...).", log), log);

        // ---------- Задания ----------
        var t1 = Load<TaskData>($"{Data}/Tasks/Task_01_Example.asset");
        if (t1 != null) {
            SetIfEmpty(ref t1.clippyRequestEn, "Look, there is an encrypted file secret.txt in My Documents. Open it and decrypt it in the Decryptor!");
            SetIfEmpty(ref t1.answerQuestionEn, "What does secret.txt say?");
            EditorUtility.SetDirty(t1);
        }
        var t2 = Task($"{Data}/Tasks/Task_02_Vigenere.asset", projectX, CipherType.Vigenere, VigenereAnswer,
            "Отлично! Второй компьютер теперь открыт. Там файл project_x.txt зашифрован шифром Виженера. Ключ из 5 букв — поищи подсказки в ящиках столов!",
            "Great! The second computer is open now. Its file project_x.txt is encrypted with Vigenere. The key has 5 letters — look for hints in the desk drawers!",
            "Какой пароль в project_x.txt?", "What is the password in project_x.txt?",
            "Вирус получил доступ ко второму компьютеру.", log);
        var t3 = Task($"{Data}/Tasks/Task_03_DES.asset", sysLog, CipherType.Des, DesAnswer,
            "Остался последний компьютер. system_log.txt зашифрован DES. Ключ — какое-то слово... Кажется, в ящике третьего стола что-то было.",
            "Only the last computer is left. system_log.txt is encrypted with DES. The key is some word... I think there was something in the third desk drawer.",
            "Какое последнее слово в system_log.txt?", "What is the last word in system_log.txt?",
            "Вирус получил полный доступ к сети.", log);

        // ---------- Вторые задания на каждом компьютере (ключ ищется в файлах / звонках) ----------
        bool chainExisted = Load<TaskData>($"{Data}/Tasks/Task_01b_Caesar.asset") != null;

        // PC1: второй Цезарь, сдвиг 7 — подсказка в записке сисадмина и в его звонке
        var inventory = CipherFile($"{Data}/Files/inventory.txt.asset", "inventory.txt", CipherType.Caesar, "7",
            "ARCHIVE PASSWORD FOR THE NEW SEASON: COBALT", log);
        AddFile(pc1Docs, inventory, log);
        AddFile(pc1Docs, TextFile($"{Data}/Files/sysadmin_note.txt.asset", "sysadmin_note.txt",
            "Сисадмин: ключ к архиву inventory.txt сменил.\nНовый сдвиг = сколько дней в неделе.\nСтарый (3) больше не работает!",
            "Sysadmin: I changed the key for the inventory.txt archive.\nNew shift = number of days in a week.\nThe old one (3) no longer works!", log), log);
        AddFile(pc1Docs, TextFile($"{Data}/Files/phone_list.txt.asset", "phone_list.txt",
            "Внутренние номера:\nШеф — 101\nБухгалтерия — 104\nСисадмин — 112 (звонит сам, когда ему надо)\nОхрана — 100",
            "Internal numbers:\nBoss — 101\nAccounting — 104\nSysadmin — 112 (calls you himself when he needs to)\nSecurity — 100", log), log);
        var t1b = Task($"{Data}/Tasks/Task_01b_Caesar.asset", inventory, CipherType.Caesar, "COBALT",
            "Здорово! Тут есть ещё архив — inventory.txt. Тоже Цезарь, но ключ сменили. Поищи записку от сисадмина.",
            "Nice! There is another archive here — inventory.txt. Caesar again, but the key was changed. Look for a note from the sysadmin.",
            "Какой пароль от архива?", "What is the archive password?",
            "Вирус получил доступ к сети офиса.", log);

        // PC2: второй Виженер, ключ — ответ первого (LANTERN)
        var nightShift = CipherFile($"{pc2}/Files/night_shift.txt.asset", "night_shift.txt", CipherType.Vigenere, VigenereAnswer,
            "THE SYSADMIN SAW SOMEONE AT TERMINAL THREE LAST NIGHT. THE BACKUP WORD IS FALCON", log);
        AddFile(pc2Docs, nightShift, log);
        var t2b = Task($"{Data}/Tasks/Task_02b_Vigenere.asset", nightShift, CipherType.Vigenere, "FALCON",
            "Пароль из project_x подходит и к night_shift.txt — попробуй его как ключ Виженера.",
            "The password from project_x also fits night_shift.txt — try it as the Vigenere key.",
            "Какое запасное слово в night_shift.txt?", "What is the backup word in night_shift.txt?",
            "Вирус скопировал почту отдела.", log);

        // PC3: второй DES, ключ — слово из конца system_log (MIRROR)
        var transfer = CipherFile($"{pc3}/Files/transfer.log.asset", "transfer.log", CipherType.Des, DesAnswer,
            "TRANSFER COMPLETE. ALL FILES SENT. RECEIVER: GHOSTNET", log);
        AddFile(pc3Docs, transfer, log);
        var t3b = Task($"{Data}/Tasks/Task_03b_DES.asset", transfer, CipherType.Des, "GHOSTNET",
            "В конце лога было слово... Возможно, это ключ к transfer.log?",
            "There was a word at the end of the log... Maybe it's the key to transfer.log?",
            "Кому отправлены файлы?", "Who received the files?",
            "Вирус отправил все данные наружу.", log);

        // Цепочка теперь из 6 заданий: доступ даёт только второе задание на каждом компьютере
        if (!chainExisted) {
            foreach (var t in new[] { t1, t2, t3 })
                if (t != null && t.accessGranted == 1) { t.accessGranted = 0; EditorUtility.SetDirty(t); }
            log.Add("Доступ теперь дают задания 1b и 2b и 3b (первые задания на компьютерах — 0)");
        }
        var chain = new[] { t1, t1b, t2, t2b, t3, t3b };

        // ---------- Английские названия и реплики ----------
        FillEnglishNames(log);
        MigrateTexts(sysLog, log);

        AssetDatabase.SaveAssets();

        // ---------- Сцена: задания, доступ к мониторам, записки в ящиках ----------
        SetupScene(chain, sticker, log);

        Report(log);
    }

    // ================= Сцена =================

    static void SetupScene(TaskData[] chain, Sprite sticker, List<string> log) {
        var pm = Object.FindAnyObjectByType<ProgressionManager>(FindObjectsInactive.Include);
        if (pm != null) {
            var so = new SerializedObject(pm);
            var arr = so.FindProperty("tasks");
            var list = Enumerable.Range(0, arr.arraySize).Select(i => arr.GetArrayElementAtIndex(i).objectReferenceValue as TaskData).ToList();
            var ordered = chain.Where(t => t != null).ToList();
            if (list.All(t => t == null || ordered.Contains(t))) {
                // В списке только наши задания — выставляем правильный порядок цепочки
                if (!list.SequenceEqual(ordered)) log.Add("Задания в ProgressionManager: " + string.Join(", ", ordered.Select(t => t.name)));
                list = ordered;
            } else {
                foreach (var t in ordered)
                    if (!list.Contains(t)) { list.Add(t); log.Add($"Задание {t.name} добавлено в ProgressionManager"); }
            }
            arr.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
            so.ApplyModifiedProperties();
        } else log.Add("ВНИМАНИЕ: в сцене нет ProgressionManager — запусти Tools → OS → Настроить все компьютеры");

        var monitors = Object.FindObjectsByType<MonitorInteract>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(m => m.name).ToArray();

        // Доступ: monitor — 0, monitor.001 — 1, monitor.002 — 2 (если ещё не настроен)
        for (int i = 0; i < monitors.Length; i++) {
            var mos = new SerializedObject(monitors[i]).FindProperty("monitorOS").objectReferenceValue as MonitorOS;
            if (mos == null) continue;
            var so = new SerializedObject(mos);
            var req = so.FindProperty("requiredAccess");
            if (req != null && req.intValue == 0 && i > 0) {
                req.intValue = i;
                so.ApplyModifiedProperties();
                log.Add($"{monitors[i].name}: Required Access = {i}");
            }
        }

        // Помощник: своё приветствие на каждом компьютере (а не одно и то же интро) и пауза автоперехода 3 с
        var pc1Greeting = Load<HelperDialogue>($"{Data}/Helper/Greeting.asset");
        HelperLine HL(string ru, string en, HelperEmotion e, HelperShape s) => new() { text = ru, textEn = en, emotion = e, shape = s };
        var greetings = new[] {
            pc1Greeting,
            GetOrCreate<HelperDialogue>($"{Data}/Helper/Greeting_PC2.asset", d => d.lines = new List<HelperLine> {
                HL("О, и тут я! Этот компьютер тоже мой... то есть, наш.", "Oh, I'm here too! This computer is mine as well... I mean, ours.",
                   HelperEmotion.Funny, HelperShape.Question),
                HL("Давай посмотрим, что тут зашифровано.", "Let's see what's encrypted here.", HelperEmotion.Peaceful, HelperShape.Exclamation),
            }, log),
            GetOrCreate<HelperDialogue>($"{Data}/Helper/Greeting_PC3.asset", d => d.lines = new List<HelperLine> {
                HL("Последний! Я так ждал... то есть, ты молодец, что дошёл.", "The last one! I've been waiting... I mean, well done getting here.",
                   HelperEmotion.Funny, HelperShape.Exclamation),
                HL("Осталось совсем чуть-чуть.", "Just a little bit left.", HelperEmotion.Peaceful, HelperShape.Question),
            }, log),
        };
        for (int i = 0; i < monitors.Length && i < greetings.Length; i++) {
            var mos = new SerializedObject(monitors[i]).FindProperty("monitorOS").objectReferenceValue as MonitorOS;
            var desk = mos != null ? new SerializedObject(mos).FindProperty("desktopScreen").objectReferenceValue as GameObject : null;
            var helper = desk != null ? desk.GetComponentInChildren<HelperController>(true) : null;
            if (helper == null) continue;
            var hso = new SerializedObject(helper);
            var g = hso.FindProperty("greeting");
            if (i > 0 && (g.objectReferenceValue == null || g.objectReferenceValue == pc1Greeting)) {
                g.objectReferenceValue = greetings[i];
                log.Add($"{monitors[i].name}: помощник здоровается по-своему ({greetings[i].name})");
            }
            var delay = hso.FindProperty("autoDelay");
            if (delay != null && delay.vector2Value == new Vector2(1f, 1.5f)) delay.vector2Value = new Vector2(3f, 3f);
            hso.ApplyModifiedProperties();
        }

        // Записки по ящикам: ящик относится к ближайшему монитору
        var drawers = Object.FindObjectsByType<DeskDrawer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (drawers.Length == 0) {
            log.Add("ВНИМАНИЕ: в сцене нет ящиков (DeskDrawer) — записки не разложены");
        } else if (monitors.Length > 0) {
            var byMonitor = monitors.ToDictionary(m => m, _ => new List<DeskDrawer>());
            foreach (var d in drawers) {
                var c = Bounds(d.gameObject).center;
                var nearest = monitors.OrderBy(m => Vector3.Distance(Bounds(m.gameObject).center, c)).First();
                byMonitor[nearest].Add(d);
            }
            foreach (var kv in byMonitor) kv.Value.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            var paper = EnsureMaterial($"{Data}/Notes/NotePaper.mat", new Color(0.95f, 0.93f, 0.85f), null);
            var stickerMat = EnsureMaterial($"{Data}/Notes/NoteSticker.mat", Color.white, sticker != null ? sticker.texture : null);

            DeskDrawer Pick(int monitorIndex, int drawerIndex) {
                if (monitorIndex >= monitors.Length) return null;
                var list = byMonitor[monitors[monitorIndex]];
                return list.Count == 0 ? null : list[Mathf.Min(drawerIndex, list.Count - 1)];
            }

            PlaceNote(Pick(0, 0), "Note_CaesarHint", paper, new Vector2(0.10f, 0.14f), 0,
                "«Шаг вперёд, шаг вперёд, шаг вперёд».\nИ так с каждой буквой.",
                "\"Step forward, step forward, step forward.\"\nAnd so with every letter.", null, false, log);
            PlaceNote(Pick(1, 0), "Note_Sticker", stickerMat, new Vector2(0.075f, 0.075f), 0, null, null, sticker, false, log);
            PlaceNote(Pick(1, 1), "Note_LetterGrid", paper, new Vector2(0.10f, 0.10f), Pick(1, 1) == Pick(1, 0) ? 1 : 0,
                LetterGrid, LetterGrid, null, true, log);
            PlaceNote(Pick(2, 0), "Note_DesHint", paper, new Vector2(0.10f, 0.14f), 0,
                "Новый ключ для логов (DES) — кодовое имя проекта.\nТолько ЗАГЛАВНЫМИ!\nИмя есть в письме шефа на соседнем компьютере.",
                "The new log key (DES) is the project codename.\nCAPITALS only!\nThe name is in the boss's letter on the next computer.", null, false, log);
        }

        var scene = monitors.Length > 0 ? monitors[0].gameObject.scene : UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.Add($"Сцена {scene.name} сохранена");
    }

    // Записка лежит на ящике (дочерний объект — выезжает вместе с ним). slot — сдвиг, если в ящике уже есть записка.
    static void PlaceNote(DeskDrawer drawer, string name, Material mat, Vector2 size, int slot,
                          string ru, string en, Sprite image, bool mono, List<string> log) {
        if (drawer == null) { log.Add($"ВНИМАНИЕ: нет ящика для {name}"); return; }
        if (drawer.transform.Find(name) != null) return; // уже лежит

        var b = Bounds(drawer.gameObject);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Undo.RegisterCreatedObjectUndo(go, "Place note");
        go.GetComponent<Renderer>().sharedMaterial = mat;

        // Кладём горизонтально на верх ящика, по центру (второй — со сдвигом по длинной стороне)
        bool alongX = b.size.x >= b.size.z;
        Vector3 offset = slot == 0 ? Vector3.zero : (alongX ? Vector3.right : Vector3.forward) * (size.x + 0.02f);
        go.transform.position = new Vector3(b.center.x, b.max.y + 0.002f, b.center.z) + offset;
        go.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(-12f, 12f), 0);
        go.transform.localScale = new Vector3(size.x, 0.002f, size.y);
        go.transform.SetParent(drawer.transform, true);

        var note = go.AddComponent<NoteItem>();
        note.text = ru;
        note.textEn = en;
        note.image = image;
        note.monospace = mono;
        if (image != null) note.paperColor = new Color(0.98f, 0.93f, 0.45f);
        log.Add($"{name} положена в ящик {drawer.name}");
    }

    // ================= Ассеты =================

    static FileData TextFile(string path, string fileName, string ru, string en, List<string> log) =>
        GetOrCreate<FileData>(path, f => {
            f.fileName = fileName;
            f.fileType = FileType.Text;
            f.encrypted = false;
            f.plainText = ru;
            f.plainTextEn = en;
        }, log);

    static FileData ImageFile(string path, string fileName, Sprite image, List<string> log) =>
        GetOrCreate<FileData>(path, f => {
            f.fileName = fileName;
            f.fileType = FileType.Image;
            f.image = image;
            f.encrypted = false;
        }, log);

    static FileData CipherFile(string path, string fileName, CipherType cipher, string key, string plain, List<string> log) =>
        GetOrCreate<FileData>(path, f => {
            f.fileName = fileName;
            f.fileType = FileType.Text;
            f.encrypted = true;
            f.cipher = cipher;
            f.key = key;
            f.plainText = plain;
            f.content = CipherFactory.Get(cipher).Encrypt(plain, key);
        }, log);

    static TaskData Task(string path, FileData file, CipherType cipher, string answer, string ru, string en,
                         string qRu, string qEn, string hidden, List<string> log) =>
        GetOrCreate<TaskData>(path, t => {
            t.file = file;
            t.cipher = cipher;
            t.expectedAnswer = answer;
            t.clippyRequest = ru;
            t.clippyRequestEn = en;
            t.answerQuestion = qRu;
            t.answerQuestionEn = qEn;
            t.accessGranted = 1;
            t.hiddenEffectLog = hidden;
        }, log);

    static FolderData Folder(string path, string ru, string en, List<string> log) =>
        GetOrCreate<FolderData>(path, f => {
            f.folderName = ru;
            f.folderNameEn = en;
            f.kind = FolderKind.Folder;
        }, log);

    static void AddFile(FolderData folder, FileData file, List<string> log) {
        if (folder == null || file == null || folder.files.Contains(file)) return;
        folder.files.Add(file);
        EditorUtility.SetDirty(folder);
        log.Add($"{file.fileName} → {AssetDatabase.GetAssetPath(folder)}");
    }

    static void AddFolder(FolderData parent, FolderData child, List<string> log) {
        if (parent == null || child == null || parent.folders.Contains(child)) return;
        parent.folders.Add(child);
        EditorUtility.SetDirty(parent);
        log.Add($"Папка {child.folderName} → {AssetDatabase.GetAssetPath(parent)}");
    }

    static void FillEnglishNames(List<string> log) {
        var names = new Dictionary<string, string> {
            { "Рабочий стол", "Desktop" }, { "Мой компьютер", "My Computer" }, { "Локальный диск (C:)", "Local Disk (C:)" },
            { "Мои документы", "My Documents" }, { "Корзина", "Recycle Bin" }, { "Фото", "Photos" }, { "Работа", "Work" },
        };
        foreach (var f in FindAll<FolderData>()) {
            if (string.IsNullOrEmpty(f.folderNameEn) && names.TryGetValue(f.folderName, out var en)) {
                f.folderNameEn = en;
                EditorUtility.SetDirty(f);
            }
        }

        var lines = new Dictionary<string, string> {
            { "Привет! Я твой помощник.", "Hi! I'm your helper." },
            { "Найдёшь зашифрованный файл — открой его в «Дешифраторе». Я подскажу, что делать.", "Found an encrypted file? Open it in the Decryptor. I'll tell you what to do." },
            { "Отлично! Так держать!", "Great! Keep it up!" },
            { "Хм... Не похоже. Проверь шифр и ключ ещё раз.", "Hmm... Doesn't look right. Check the cipher and the key again." },
            { "Кажется, на сегодня всё. Спасибо за помощь!", "Looks like that's all for today. Thanks for your help!" },
        };
        foreach (var d in FindAll<HelperDialogue>()) {
            bool dirty = false;
            foreach (var l in d.lines)
                if (string.IsNullOrEmpty(l.textEn) && l.text != null && lines.TryGetValue(l.text, out var en)) { l.textEn = en; dirty = true; }
            if (dirty) EditorUtility.SetDirty(d);
        }

        foreach (var a in FindAll<AppData>())
            if (string.IsNullOrEmpty(a.appNameEn) && a.kind == AppKind.Decryptor) { a.appNameEn = "Decryptor"; EditorUtility.SetDirty(a); }

        log.Add("Английские названия папок и реплик заполнены (где были пустыми)");
    }

    // Старые формулировки → новые (только если текст не меняли вручную)
    static void MigrateTexts(FileData sysLog, List<string> log) {
        foreach (var f in FindAll<FileData>()) {
            void Swap(string oldRu, string ru, string en) {
                if (f.plainText != oldRu) return;
                f.plainText = ru;
                f.plainTextEn = en;
                if (!f.encrypted) f.content = "";
                EditorUtility.SetDirty(f);
                log.Add($"Обновлён текст {f.fileName}");
            }
            Swap(OldResignRu, ResignRu, ResignEn);
            Swap(OldTodoRu, TodoRu, TodoEn);
            Swap(OldEmailRu, EmailRu, EmailEn);
        }

        if (sysLog != null && sysLog.plainText == OldDesPlain) {
            sysLog.plainText = DesPlain;
            sysLog.content = CipherFactory.Get(sysLog.cipher).Encrypt(DesPlain, sysLog.key);
            EditorUtility.SetDirty(sysLog);
            log.Add("system_log.txt перешифрован с новым текстом");
        }

        // Прощание помощника перед исчезновением
        var farewell = Load<HelperDialogue>($"{Data}/Helper/AllTasksDone.asset");
        if (farewell != null && farewell.lines.Count == 1 && farewell.lines[0].text == "Кажется, на сегодня всё. Спасибо за помощь!") {
            farewell.lines = new List<HelperLine> {
                new() { text = "Спасибо! Ты мне очень помог.", textEn = "Thank you! You helped me a lot.",
                        emotion = HelperEmotion.Funny, shape = HelperShape.Exclamation },
                new() { text = "Теперь у меня есть всё, что нужно.", textEn = "Now I have everything I need.",
                        emotion = HelperEmotion.Peaceful, shape = HelperShape.Question },
                new() { text = "Прощай!", textEn = "Goodbye!",
                        emotion = HelperEmotion.Funny, shape = HelperShape.Exclamation },
            };
            EditorUtility.SetDirty(farewell);
            log.Add("Прощальные реплики помощника обновлены (AllTasksDone)");
        }
    }

    static Sprite EnsureSprite(string path, bool pixel, List<string> log) {
        if (AssetImporter.GetAtPath(path) is not TextureImporter imp) { log.Add($"ВНИМАНИЕ: нет файла {path}"); return null; }
        if (imp.textureType != TextureImporterType.Sprite || (pixel && imp.filterMode != FilterMode.Point)) {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            if (pixel) { imp.filterMode = FilterMode.Point; imp.textureCompression = TextureImporterCompression.Uncompressed; }
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Material EnsureMaterial(string path, Color color, Texture tex) {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        m = new Material(shader);
        m.SetColor("_BaseColor", color);
        if (tex != null) { m.SetTexture("_BaseMap", tex); m.mainTexture = tex; }
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static T GetOrCreate<T>(string path, Action<T> init, List<string> log) where T : ScriptableObject {
        var a = AssetDatabase.LoadAssetAtPath<T>(path);
        if (a != null) return a;
        EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
        a = ScriptableObject.CreateInstance<T>();
        init(a);
        AssetDatabase.CreateAsset(a, path);
        log.Add($"Создан {path}");
        return a;
    }

    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    static IEnumerable<T> FindAll<T>() where T : Object =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name).Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g))).Where(x => x != null);

    static void EnsureFolder(string path) {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static void SetIfEmpty(ref string field, string value) {
        if (string.IsNullOrEmpty(field)) field = value;
    }

    static Bounds Bounds(GameObject go) {
        var r = go.GetComponent<Renderer>();
        if (r == null) r = go.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds : new Bounds(go.transform.position, Vector3.one * 0.2f);
    }

    static void Report(List<string> log) {
        string text = string.Join("\n", log);
        Debug.Log("[Content Setup]\n" + text);
        EditorUtility.DisplayDialog("Content Setup", text.Length > 1500 ? text.Substring(0, 1500) + "\n… (полный список в Console)" : text, "OK");
    }
}
