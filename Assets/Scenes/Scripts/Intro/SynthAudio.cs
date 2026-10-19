using UnityEngine;

// Звуки, сгенерированные кодом (пока нет настоящих записей): звонок, щелчок трубки, помехи,
// «голос» в телефоне, короткие гудки, стук двери. Любой можно заменить своим клипом в IntroSequence.
public static class SynthAudio {
    const int Rate = 22050;

    static AudioClip Make(string name, float[] data) {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Звонок старого телефона: «дзынь-дзынь … пауза» (одна петля ~3 с)
    public static AudioClip Ring() {
        float len = 3f;
        var d = new float[(int)(Rate * len)];
        for (int i = 0; i < d.Length; i++) {
            float t = i / (float)Rate;
            bool on = t < 0.4f || (t > 0.6f && t < 1.0f);
            if (!on) continue;
            // «Звон»: две частоты + дребезг молоточка 20 Гц
            float bell = Mathf.Sin(2 * Mathf.PI * 440 * t) + Mathf.Sin(2 * Mathf.PI * 480 * t);
            float hammer = 0.6f + 0.4f * Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 20 * t));
            d[i] = bell * hammer * 0.25f;
        }
        return Make("Ring", d);
    }

    public static AudioClip Click(int seed = 1) {
        var rnd = new System.Random(seed);
        var d = new float[(int)(Rate * 0.06f)];
        for (int i = 0; i < d.Length; i++) {
            float env = Mathf.Exp(-i / (Rate * 0.008f));
            d[i] = ((float)rnd.NextDouble() * 2f - 1f) * env * 0.8f;
        }
        return Make("Click", d);
    }

    // Глухой удар закрывающейся двери
    public static AudioClip DoorThud() {
        var rnd = new System.Random(7);
        var d = new float[(int)(Rate * 0.6f)];
        float lp = 0f;
        for (int i = 0; i < d.Length; i++) {
            float t = i / (float)Rate;
            float env = Mathf.Exp(-t * 9f);
            float noise = (float)rnd.NextDouble() * 2f - 1f;
            lp += (noise - lp) * 0.08f; // низкие частоты
            d[i] = (lp * 2.5f + Mathf.Sin(2 * Mathf.PI * 70 * t) * 0.6f) * env;
        }
        return Make("DoorThud", d);
    }

    // Шипение телефонной линии (петля)
    public static AudioClip Static(float seconds = 2f, int seed = 3) {
        var rnd = new System.Random(seed);
        var d = new float[(int)(Rate * seconds)];
        float lp = 0f;
        for (int i = 0; i < d.Length; i++) {
            float noise = (float)rnd.NextDouble() * 2f - 1f;
            lp += (noise - lp) * 0.35f;
            float crackle = rnd.NextDouble() < 0.0015 ? (float)rnd.NextDouble() * 0.8f : 0f;
            d[i] = lp * 0.35f + crackle;
        }
        return Make("Static", d);
    }

    // Громкий всплеск помех при обрыве связи
    public static AudioClip StaticBurst() {
        var rnd = new System.Random(11);
        var d = new float[(int)(Rate * 0.45f)];
        for (int i = 0; i < d.Length; i++) {
            float t = i / (float)d.Length;
            float env = t < 0.85f ? 1f : (1f - t) / 0.15f;
            float buzz = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 120 * i / (float)Rate)) * 0.3f;
            d[i] = (((float)rnd.NextDouble() * 2f - 1f) * 0.8f + buzz) * env;
        }
        return Make("StaticBurst", d);
    }

    // Короткие гудки после обрыва: «пи … пи … пи»
    public static AudioClip BusyTone(int beeps = 4) {
        var d = new float[(int)(Rate * 0.7f * beeps)];
        for (int i = 0; i < d.Length; i++) {
            float t = i / (float)Rate;
            if (t % 0.7f > 0.35f) continue;
            d[i] = Mathf.Sin(2 * Mathf.PI * 425 * t) * 0.3f;
        }
        return Make("BusyTone", d);
    }

    // «Голос» в трубке: слоги с плавающей высотой, приглушённые как через телефон
    public static AudioClip Mumble(float seconds, int seed) {
        var rnd = new System.Random(seed);
        var d = new float[(int)(Rate * seconds)];
        int i = 0;
        float phase = 0f, lp1 = 0f, lp2 = 0f;
        while (i < d.Length) {
            float syl = 0.11f + (float)rnd.NextDouble() * 0.14f;     // длина слога
            float gap = rnd.NextDouble() < 0.18 ? 0.12f : 0.02f;     // иногда пауза между словами
            float pitch = 115f + (float)rnd.NextDouble() * 55f;
            int n = (int)(syl * Rate);
            for (int k = 0; k < n && i < d.Length; k++, i++) {
                float u = k / (float)n;
                float env = Mathf.Sin(Mathf.PI * u);
                float f = pitch * (1f + 0.08f * Mathf.Sin(u * 6f));
                phase += f / Rate;
                float saw = (phase % 1f) * 2f - 1f;
                lp1 += (saw - lp1) * 0.18f;          // «формант»
                lp2 += (lp1 - lp2) * 0.35f;
                d[i] = (lp1 - lp2 * 0.6f) * env * 0.9f;
            }
            i += (int)(gap * Rate);
        }
        return Make("Mumble", d);
    }
}
