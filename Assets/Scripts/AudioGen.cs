using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sintetiza todos los sonidos por código (no hay archivos de audio): rugidos, gritos, latidos,
/// susurros, risa de niño, chillidos, pasos, crujidos, estática, etc.
/// </summary>
public static class AudioGen
{
    const int SR = 44100;
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static readonly System.Random rng = new System.Random(1993);

    static float Rnd() { return (float)(rng.NextDouble() * 2.0 - 1.0); }
    static float Rnd01() { return (float)rng.NextDouble(); }
    static float[] Buf(float sec) { return new float[(int)(sec * SR)]; }
    static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
    static float Tanh(float x) { return (float)Math.Tanh(x); }

    public static AudioClip Get(string name)
    {
        AudioClip c;
        if (cache.TryGetValue(name, out c) && c != null) return c;
        c = Make(name);
        cache[name] = c;
        return c;
    }

    public static void PreloadAll()
    {
        foreach (var n in new[] { "drone", "heart", "roar", "scream", "stinger", "boom", "skitter", "static", "siren", "giggle",
                                  "whisper", "creak", "glass", "drip", "step", "pickup", "checkpoint", "piano", "win", "click", "screech" })
            Get(n);
    }

    static AudioClip Clip(string name, float[] d, float peak = 0.9f, bool normalize = true)
    {
        if (normalize)
        {
            float m = 0f;
            for (int i = 0; i < d.Length; i++) m = Mathf.Max(m, Mathf.Abs(d[i]));
            if (m > 0.0001f) { float k = peak / m; for (int i = 0; i < d.Length; i++) d[i] *= k; }
        }
        var c = AudioClip.Create(name, d.Length, 1, SR, false);
        c.SetData(d, 0);
        return c;
    }

    // ------------------------------------------------------------ filtros
    /// <summary>Pasa-banda con frecuencia que cambia en el tiempo (formantes / barridos).</summary>
    static float[] BandPass(float[] x, Func<float, float> freqOfT, float q)
    {
        var y = new float[x.Length];
        float x1 = 0, x2 = 0, y1 = 0, y2 = 0, b0 = 0, b2 = 0, a0 = 1, a1 = 0, a2 = 0;
        for (int i = 0; i < x.Length; i++)
        {
            if ((i & 31) == 0)
            {
                float f = Mathf.Clamp(freqOfT((float)i / SR), 40f, 18000f);
                float w = 2f * Mathf.PI * f / SR, al = Mathf.Sin(w) / (2f * q), cs = Mathf.Cos(w);
                b0 = al; b2 = -al; a0 = 1f + al; a1 = -2f * cs; a2 = 1f - al;
            }
            float xi = x[i];
            float o = (b0 * xi + b2 * x2 - a1 * y1 - a2 * y2) / a0;
            x2 = x1; x1 = xi; y2 = y1; y1 = o;
            y[i] = o;
        }
        return y;
    }
    static float[] BandPass(float[] x, float f, float q) { return BandPass(x, t => f, q); }

    static float[] LowPass(float[] x, float fc)
    {
        var y = new float[x.Length];
        float a = 1f - Mathf.Exp(-2f * Mathf.PI * fc / SR), s = 0f;
        for (int i = 0; i < x.Length; i++) { s += a * (x[i] - s); y[i] = s; }
        return y;
    }

    static float[] Noise(int n) { var d = new float[n]; for (int i = 0; i < n; i++) d[i] = Rnd(); return d; }

    // ------------------------------------------------------------ catálogo
    static AudioClip Make(string name)
    {
        switch (name)
        {
            case "drone": return Drone();
            case "heart": return Heart();
            case "roar": return Roar();
            case "scream": return Scream(2.6f, 520f, 1150f, 1.0f);
            case "screech": return Scream(1.2f, 900f, 2400f, 0.8f);
            case "stinger": return Stinger();
            case "boom": return Boom();
            case "skitter": return Skitter();
            case "static": return Static();
            case "siren": return Siren();
            case "giggle": return Giggle();
            case "whisper": return Whisper();
            case "creak": return Creak();
            case "glass": return Glass();
            case "drip": return Drip();
            case "step": return Step();
            case "pickup": return Pickup();
            case "checkpoint": return CheckpointTone();
            case "piano": return Piano();
            case "win": return Win();
            case "click": return Click();
            case "fridgehum": return FridgeHum();
        }
        return Clip(name, new float[SR / 10]);
    }

    /// <summary>Zumbido grave y ruido de fondo, en bucle.</summary>
    static AudioClip Drone()
    {
        float len = 8f; int n = (int)((len + 0.5f) * SR);
        var d = new float[n];
        var nz = LowPass(Noise(n), 180f);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float s = Mathf.Sin(2 * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2 * Mathf.PI * 55.75f * t) * 0.5f
                    + Mathf.Sin(2 * Mathf.PI * 82.5f * t) * 0.25f + Mathf.Sin(2 * Mathf.PI * 110.25f * t) * 0.12f;
            float eerie = Mathf.Sin(2 * Mathf.PI * 1240f * t) * 0.012f * (0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * 0.25f * t));
            d[i] = s * 0.35f + nz[i] * 3f * (0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * 0.125f * t)) + eerie;
        }
        // bucle sin salto: mezcla el final extra con el inicio
        int L = (int)(len * SR), x = n - L;
        var o = new float[L];
        for (int i = 0; i < L; i++)
        {
            float w = i < x ? (float)i / x : 1f;
            o[i] = i < x ? Mathf.Lerp(d[L + i], d[i], w) : d[i];
        }
        return Clip("drone", o, 0.7f);
    }

    static AudioClip Heart()
    {
        var d = Buf(0.95f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float b1 = Mathf.Sin(2 * Mathf.PI * 52f * t) * Mathf.Exp(-t * 16f);
            float t2 = t - 0.27f;
            float b2 = t2 > 0 ? Mathf.Sin(2 * Mathf.PI * 46f * t2) * Mathf.Exp(-t2 * 18f) * 0.75f : 0f;
            d[i] = b1 + b2;
        }
        return Clip("heart", d, 0.9f);
    }

    /// <summary>Rugido de bestia / león: gruñido grave con pulsos finales.</summary>
    static AudioClip Roar()
    {
        float len = 3.4f; int n = (int)(len * SR);
        var tone = new float[n]; var nz = Noise(n);
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float f0 = Mathf.Lerp(62f, 108f, Smooth(0f, 0.7f, t)) * (t > 2.0f ? Mathf.Lerp(1f, 0.68f, Smooth(2.0f, 3.3f, t)) : 1f);
            ph += 2f * Mathf.PI * f0 * (1f + 0.10f * Mathf.Sin(2f * Mathf.PI * 27f * t) + 0.04f * Rnd()) / SR;
            float saw = 2f * (ph / (2f * Mathf.PI) - Mathf.Floor(ph / (2f * Mathf.PI) + 0.5f));
            float amp = Smooth(0f, 0.09f, t) * (1f - Smooth(2.6f, 3.4f, t));
            if (t > 1.3f) amp *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Mathf.PI * 4.2f * (t - 1.3f)));   // "uh-uh-uh" de león
            tone[i] = Tanh(3.2f * saw * amp);
            nz[i] *= amp;
        }
        var growl = LowPass(tone, 2200f);
        var rasp = BandPass(nz, t => 700f + 500f * Mathf.Sin(t * 3f), 0.8f);
        var o = new float[n];
        for (int i = 0; i < n; i++) o[i] = Tanh(1.6f * (growl[i] * 0.9f + rasp[i] * 1.4f));
        return Clip("roar", o, 0.95f);
    }

    /// <summary>Grito agudo con formantes (suena a voz). f0 sube de f0a a f0b.</summary>
    static AudioClip Scream(float len, float f0a, float f0b, float rough)
    {
        int n = (int)(len * SR);
        var src = new float[n];
        float ph = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float f0 = Mathf.Lerp(f0a, f0b, Smooth(0f, 0.45f, t)) - 120f * Smooth(len * 0.6f, len, t);
            f0 *= 1f + 0.035f * Mathf.Sin(2f * Mathf.PI * 6.5f * t) + 0.012f * Rnd() * rough;
            ph += 2f * Mathf.PI * f0 / SR;
            float saw = 2f * (ph / (2f * Mathf.PI) - Mathf.Floor(ph / (2f * Mathf.PI) + 0.5f));
            float amp = Smooth(0f, 0.04f, t) * (1f - Smooth(len - 0.6f, len, t));
            src[i] = saw * amp + Rnd() * 0.25f * rough * amp;
        }
        var f1 = BandPass(src, t => Mathf.Lerp(700f, 1000f, Smooth(0f, 0.6f, t)), 4f);
        var f2 = BandPass(src, t => Mathf.Lerp(1500f, 2300f, Smooth(0f, 0.6f, t)), 5f);
        var br = BandPass(src, 3200f, 1.2f);
        var o = new float[n];
        for (int i = 0; i < n; i++) o[i] = Tanh(2.6f * (f1[i] * 1.0f + f2[i] * 0.9f + br[i] * 0.3f + src[i] * 0.12f));
        return Clip(len > 2f ? "scream" : "screech", o, 0.95f);
    }

    /// <summary>Chillido de violín disonante + golpe grave (susto).</summary>
    static AudioClip Stinger()
    {
        var d = Buf(1.8f); float ph = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float f = Mathf.Lerp(2000f, 1550f, Smooth(0f, 1.2f, t));
            ph += 2f * Mathf.PI * f / SR;
            float saw = 2f * (ph / (2f * Mathf.PI) - Mathf.Floor(ph / (2f * Mathf.PI) + 0.5f));
            float trem = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 13f * t);
            float env = Smooth(0f, 0.01f, t) * Mathf.Exp(-t * 1.6f);
            float cluster = Mathf.Sin(2f * Mathf.PI * 1960f * t) + Mathf.Sin(2f * Mathf.PI * 2013f * t) + Mathf.Sin(2f * Mathf.PI * 1480f * t);
            float boom = Mathf.Sin(2f * Mathf.PI * (45f - 12f * t) * t) * Mathf.Exp(-t * 2.5f) * 1.8f;
            d[i] = (saw * 0.6f + cluster * 0.25f) * trem * env + boom;
        }
        return Clip("stinger", d, 0.95f);
    }

    static AudioClip Boom()
    {
        var d = Buf(2.2f); var nz = LowPass(Noise(d.Length), 220f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float f = Mathf.Lerp(70f, 28f, Smooth(0f, 1.2f, t));
            d[i] = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 2.2f) + nz[i] * 9f * Mathf.Exp(-t * 5f);
        }
        return Clip("boom", d, 0.95f);
    }

    /// <summary>Patitas de araña: clics rápidos que se aceleran, y un chillido final.</summary>
    static AudioClip Skitter()
    {
        var d = Buf(2.4f); float next = 0f; float gap = 0.06f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            if (t >= next)
            {
                int len = (int)(0.004f * SR);
                float a = 0.5f + Rnd01() * 0.5f;
                for (int k = 0; k < len && i + k < d.Length; k++) d[i + k] += Rnd() * a * Mathf.Exp(-k / (len * 0.35f));
                gap = Mathf.Max(0.012f, gap * 0.97f);
                next = t + gap * (0.5f + Rnd01());
            }
        }
        var hp = BandPass(d, 2500f, 0.7f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float sq = t > 1.5f ? Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(3300f, 2300f, (t - 1.5f) / 0.9f) * t) * Smooth(1.5f, 1.55f, t) * (1f - Smooth(2.1f, 2.4f, t)) * 0.9f : 0f;
            d[i] = hp[i] * 1.5f + sq + Rnd() * sq * 0.3f;
        }
        return Clip("skitter", d, 0.9f);
    }

    static AudioClip Static()
    {
        var d = Buf(1.6f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = Rnd() * (0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * 47f * t));
            if (Rnd01() > 0.9985f) d[i] += Rnd() * 3f;
        }
        return Clip("static", d, 0.8f);
    }

    static AudioClip Siren()
    {
        var d = Buf(3.2f); float ph = 0f;
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float tri = Mathf.Abs(((t * 1.4f) % 1f) * 2f - 1f);
            float f = Mathf.Lerp(620f, 940f, tri);
            ph += 2f * Mathf.PI * f / SR;
            d[i] = Tanh(3f * Mathf.Sin(ph)) * Smooth(0f, 0.05f, t) * (1f - Smooth(2.9f, 3.2f, t));
        }
        return Clip("siren", d, 0.8f);
    }

    /// <summary>Risa de niño, entrecortada y desafinada.</summary>
    static AudioClip Giggle()
    {
        var d = Buf(2.4f);
        for (int b = 0; b < 8; b++)
        {
            float start = 0.12f + b * 0.26f, dur = 0.14f, f0 = 640f + b * 28f + Rnd() * 25f;
            int i0 = (int)(start * SR), cnt = (int)(dur * SR);
            for (int k = 0; k < cnt && i0 + k < d.Length; k++)
            {
                float u = (float)k / cnt, t = (float)k / SR;
                float f = f0 * (1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 9f * t) - 0.1f * u);
                float s = Mathf.Sin(2f * Mathf.PI * f * t) + 0.55f * Mathf.Sin(4f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(6f * Mathf.PI * f * t);
                d[i0 + k] += s * Mathf.Sin(Mathf.PI * u) * (1f - b * 0.06f);
            }
        }
        var f1 = BandPass(d, 1100f, 2f);
        for (int i = 0; i < d.Length; i++) d[i] = Tanh(1.4f * (d[i] * 0.5f + f1[i] * 1.2f));
        return Clip("giggle", d, 0.8f);
    }

    static AudioClip Whisper()
    {
        int n = (int)(4f * SR);
        var nz = Noise(n);
        var a = BandPass(nz, t => 2600f + 700f * Mathf.Sin(t * 5f), 1.6f);
        var b = BandPass(nz, 5200f, 1.2f);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR;
            float syl = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 3.3f * t + 1.4f * Mathf.Sin(2f * Mathf.PI * 0.6f * t))), 2f);
            float slow = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 0.37f * t));
            d[i] = (a[i] + b[i] * 0.5f) * syl * slow * Smooth(0f, 0.4f, t) * (1f - Smooth(3.4f, 4f, t));
        }
        return Clip("whisper", d, 0.55f);
    }

    static AudioClip Creak()
    {
        var d = Buf(1.9f); float ph = 0f;
        var raw = new float[d.Length];
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float f = 140f + 70f * Mathf.Sin(2f * Mathf.PI * 1.3f * t) + 40f * Mathf.Sin(2f * Mathf.PI * 4.7f * t) + t * 30f;
            ph += 2f * Mathf.PI * f / SR;
            raw[i] = (2f * (ph / (2f * Mathf.PI) - Mathf.Floor(ph / (2f * Mathf.PI) + 0.5f))) + Rnd() * 0.3f;
        }
        var bp = BandPass(raw, t => 500f + 700f * Mathf.Sin(t * 2.3f) * Mathf.Sin(t * 2.3f), 7f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = bp[i] * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.9f)) * (0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 11f * t));
        }
        return Clip("creak", d, 0.8f);
    }

    static AudioClip Glass()
    {
        var d = Buf(1.5f);
        var nz = BandPass(Noise(d.Length), 6000f, 0.6f);
        for (int i = 0; i < d.Length; i++) { float t = (float)i / SR; d[i] = nz[i] * Mathf.Exp(-t * 10f) * 2f; }
        for (int k = 0; k < 40; k++)
        {
            float st = Rnd01() * 1.0f, f = 2500f + Rnd01() * 6000f;
            int i0 = (int)(st * SR), cnt = (int)(0.05f * SR);
            for (int j = 0; j < cnt && i0 + j < d.Length; j++) { float t = (float)j / SR; d[i0 + j] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 70f) * 0.5f; }
        }
        return Clip("glass", d, 0.9f);
    }

    static AudioClip Drip()
    {
        var d = Buf(0.7f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float f = 1000f * Mathf.Exp(-t * 7f) + 350f;
            float s = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 16f);
            float t2 = t - 0.33f;
            if (t2 > 0) s += Mathf.Sin(2f * Mathf.PI * (900f * Mathf.Exp(-t2 * 7f) + 300f) * t2) * Mathf.Exp(-t2 * 20f) * 0.35f;
            d[i] = s;
        }
        return Clip("drip", d, 0.7f);
    }

    static AudioClip Step()
    {
        var d = Buf(0.3f); var nz = LowPass(Noise(d.Length), 520f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = nz[i] * 6f * Mathf.Exp(-t * 28f) + Mathf.Sin(2f * Mathf.PI * 78f * t) * Mathf.Exp(-t * 22f);
        }
        return Clip("step", d, 0.7f);
    }

    static AudioClip Pickup()
    {
        var d = Buf(0.9f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 880f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1320f * t) * Smooth(0.08f, 0.1f, t)
                  + 0.3f * Mathf.Sin(2f * Mathf.PI * 1760f * t) * Smooth(0.16f, 0.18f, t)) * Mathf.Exp(-t * 5f);
        }
        return Clip("pickup", d, 0.6f);
    }

    static AudioClip CheckpointTone()
    {
        var d = Buf(1.4f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR, u = t / 1.4f;
            float s = Mathf.Sin(2f * Mathf.PI * 392f * t) + Mathf.Sin(2f * Mathf.PI * 587f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 784f * t) * 0.5f;
            d[i] = s * Mathf.Pow(Mathf.Sin(Mathf.PI * u), 2f);
        }
        return Clip("checkpoint", d, 0.45f);
    }

    static AudioClip Piano()
    {
        float[] notes = { 220f, 261.6f, 329.6f, 440f, 329.6f, 261.6f, 220f };
        var d = Buf(3.4f);
        for (int k = 0; k < notes.Length; k++)
        {
            int i0 = (int)(k * 0.4f * SR), cnt = (int)(1.2f * SR);
            for (int j = 0; j < cnt && i0 + j < d.Length; j++)
            {
                float t = (float)j / SR, f = notes[k];
                d[i0 + j] += (Mathf.Sin(2f * Mathf.PI * f * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * f * t) + 0.25f * Mathf.Sin(6f * Mathf.PI * f * t)) * Mathf.Exp(-t * 3.2f) * Smooth(0f, 0.005f, t);
            }
        }
        return Clip("piano", d, 0.6f);
    }

    static AudioClip Win()
    {
        float[] notes = { 523.3f, 659.3f, 784f, 1046.5f };
        var d = Buf(2.6f);
        for (int k = 0; k < notes.Length; k++)
        {
            int i0 = (int)(k * 0.28f * SR), cnt = (int)(1.4f * SR);
            for (int j = 0; j < cnt && i0 + j < d.Length; j++)
            {
                float t = (float)j / SR;
                d[i0 + j] += (Mathf.Sin(2f * Mathf.PI * notes[k] * t) + 0.4f * Mathf.Sin(4f * Mathf.PI * notes[k] * t)) * Mathf.Exp(-t * 2.5f);
            }
        }
        return Clip("win", d, 0.6f);
    }

    /// <summary>Zumbido eléctrico fuerte de un refrigerador viejo, en bucle sin cortes (todas las frecuencias son múltiplos de 0.25 Hz).</summary>
    static AudioClip FridgeHum()
    {
        var d = Buf(4f);
        for (int i = 0; i < d.Length; i++)
        {
            float t = (float)i / SR;
            float wob = 1f + 0.18f * Mathf.Sin(2f * Mathf.PI * 0.5f * t);
            d[i] = (Mathf.Sin(2f * Mathf.PI * 50f * t) * 1.0f + Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.7f +
                    Mathf.Sin(2f * Mathf.PI * 150f * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * 250f * t) * 0.2f +
                    Mathf.Sin(2f * Mathf.PI * 350f * t) * 0.08f) * wob;
        }
        return Clip("fridgehum", d, 0.55f);
    }

    static AudioClip Click()
    {
        var d = Buf(0.06f);
        for (int i = 0; i < d.Length; i++) { float t = (float)i / SR; d[i] = Mathf.Sin(2f * Mathf.PI * 1500f * t) * Mathf.Exp(-t * 90f); }
        return Clip("click", d, 0.3f);
    }
}
