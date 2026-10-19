using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Звуки одного компьютера: включение ЭЛТ и гул монитора, жёсткий диск при загрузке,
// клики мыши и клавиши, пока игрок сидит за компьютером. Создаётся MonitorOS сам.
public class PCAudio : MonoBehaviour {
    SoundBank bank;
    AudioSource hum, disk, fx, ui;
    bool seated;

    // owner — объект монитора (всегда активен), at — где звучит (экран)
    public static PCAudio Create(Transform owner, Vector3 at) {
        var go = new GameObject("PCAudio");
        go.transform.SetParent(owner, true);
        go.transform.position = at;
        return go.AddComponent<PCAudio>();
    }

    void Awake() {
        bank = SoundBank.Get();
        hum = Source(true);
        disk = Source(true);
        fx = Source(false);
        ui = Source(false);
        ui.spatialBlend = 0f; // клики и клавиши — «у игрока в руках»
    }

    AudioSource Source(bool loop) {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop = loop;
        s.playOnAwake = false;
        s.spatialBlend = 1f;
        s.minDistance = 0.7f;
        s.maxDistance = 10f;
        return s;
    }

    float Vol => bank != null ? bank.pcVolume : 0f;

    public void SetSeated(bool on) => seated = on;

    // Нажали «включить»: щелчок ЭЛТ → потом гул
    public void PowerOn() {
        if (bank == null) return;
        fx.pitch = 1f;
        if (bank.crtStart != null) fx.PlayOneShot(bank.crtStart, Vol);
        StopAllCoroutines();
        StartCoroutine(HumAfter(bank.crtStart != null ? bank.crtStart.length * 0.8f : 0f));
    }

    IEnumerator HumAfter(float delay) {
        yield return new WaitForSeconds(delay);
        if (bank.crtLoop == null) yield break;
        hum.clip = bank.crtLoop;
        hum.volume = Vol * 0.5f;
        hum.Play();
    }

    // Началась загрузка (полоска)
    public void DiskStart() {
        if (bank == null) return;
        fx.pitch = 1f;
        if (bank.diskStart != null) fx.PlayOneShot(bank.diskStart, Vol);
        if (bank.diskLoop != null) {
            disk.clip = bank.diskLoop;
            disk.volume = Vol * 0.7f;
            disk.PlayDelayed(bank.diskStart != null ? bank.diskStart.length * 0.9f : 0f);
        }
    }

    // Загрузка закончилась (рабочий стол или «нет доступа»)
    public void DiskStop() {
        if (bank == null) return;
        fx.pitch = 1f;
        if (disk.isPlaying || disk.time > 0) disk.Stop();
        if (bank.diskEnd != null) fx.PlayOneShot(bank.diskEnd, Vol);
    }

    // Экран погас
    public void PowerOff() {
        if (bank == null) return;
        fx.pitch = 1f;
        StopAllCoroutines();
        bool wasOn = hum.isPlaying;
        hum.Stop();
        disk.Stop();
        if (wasOn && bank.crtEnd != null) fx.PlayOneShot(bank.crtEnd, Vol);
    }

    void Update() {
        if (!seated || bank == null) return;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && bank.mouseClick != null) {
            ui.pitch = Random.Range(0.95f, 1.05f);
            ui.PlayOneShot(bank.mouseClick, Vol);
        }
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame && bank.keyPress != null
            && !Keyboard.current.eKey.wasPressedThisFrame && !Keyboard.current.escapeKey.wasPressedThisFrame) {
            ui.pitch = Random.Range(0.9f, 1.1f);
            ui.PlayOneShot(bank.keyPress, Vol * 0.8f);
        }
    }
}
