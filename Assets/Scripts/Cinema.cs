using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Escenas de muerte animadas (en vez de texto): la cámara se mueve, aparece la criatura, hay luces que
/// se apagan, sacudidas y sonidos de terror (gritos, rugidos, latidos). Una escena por cada final inesperado.
/// </summary>
public class Cinema : MonoBehaviour
{
    public GameManager gm;
    PlayerController player; Transform camT; Camera cam;
    Vector3 camPos; float yaw, pitch, roll, shake, fov = 72f;
    bool active;
    readonly List<GameObject> junk = new List<GameObject>();

    public void Setup(GameManager g, PlayerController p, Camera c) { gm = g; player = p; camT = c.transform; cam = c; }

    public void Cleanup()
    {
        StopAllCoroutines();
        foreach (var j in junk) if (j != null) Destroy(j);
        junk.Clear();
        active = false; shake = 0f; fov = 72f;
        if (cam != null) cam.fieldOfView = 72f;
    }

    public void Play(string kind, GameObject focus)
    {
        Cleanup();
        StartCoroutine(Run(kind, focus));
    }

    void LateUpdate()
    {
        if (!active) return;
        float n = Time.time * 32f;
        Vector3 sh = new Vector3(Mathf.PerlinNoise(n, 0f) - 0.5f, Mathf.PerlinNoise(0f, n) - 0.5f, Mathf.PerlinNoise(n, n) - 0.5f) * shake;
        player.SetView(camPos + sh, yaw, pitch, roll + (Mathf.PerlinNoise(n, 7f) - 0.5f) * shake * 40f);
        cam.fieldOfView = fov;
    }

    // ------------------------------------------------------------ utilidades
    IEnumerator Tween(float dur, Action<float> f)
    {
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; f(Mathf.Clamp01(t / dur)); yield return null; }
        f(1f);
    }
    static float Ease(float u) { return u * u * (3f - 2f * u); }
    IEnumerator Wait(float s) { yield return new WaitForSeconds(s); }

    IEnumerator LookAt(Vector3 target, float dur)
    {
        Vector3 d = target - camPos;
        float ty = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        float tp = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        float y0 = yaw, p0 = pitch, dy = Mathf.DeltaAngle(y0, ty);
        yield return Tween(dur, u => { float e = Ease(u); yaw = y0 + dy * e; pitch = Mathf.Lerp(p0, tp, e); });
    }

    IEnumerator FadeBlack(float dur)
    {
        gm.fadeColor = Color.black;
        yield return Tween(dur, u => gm.fadeAlpha = u);
    }

    void Flash(Color c, float peak, float dur) { StartCoroutine(FlashR(c, peak, dur)); }
    IEnumerator FlashR(Color c, float peak, float dur)
    {
        gm.flashColor = c;
        yield return Tween(dur, u => gm.flashAlpha = peak * (1f - u));
    }

    T Track<T>(T c) where T : Component { junk.Add(c.gameObject); return c; }
    GameObject Track(GameObject g) { junk.Add(g); return g; }

    GameObject Cube(Vector3 pos, Vector3 size, Material m)
    {
        var g = Mon.Part(null, PrimitiveType.Cube, pos, size, m);
        return Track(g);
    }

    Light LightAt(Vector3 pos, Color c, float intensity, float range)
    {
        var g = Track(new GameObject("LuzEscena"));
        g.transform.position = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = c; l.intensity = intensity; l.range = range;
        return l;
    }

    static Vector3 Front(Bounds b, Vector3 fwd)
    {
        float ext = Mathf.Abs(fwd.x) * b.extents.x + Mathf.Abs(fwd.z) * b.extents.z;
        return b.center + fwd * ext;
    }

    IEnumerator Swing(Vector3 hinge, Vector3 dir, float width, float h, Vector3 fwd, Material m, float dur)
    {
        var piv = Track(new GameObject("Hoja"));
        piv.transform.position = hinge;
        Quaternion q0 = Quaternion.LookRotation(dir);
        piv.transform.rotation = q0;
        Mon.Part(piv.transform, PrimitiveType.Cube, new Vector3(0, 0, width / 2f), new Vector3(0.05f, h, width), m);
        float ang = Vector3.Dot(Quaternion.AngleAxis(100f, Vector3.up) * dir, fwd) > 0f ? 100f : -100f;
        yield return Tween(dur, u => piv.transform.rotation = Quaternion.AngleAxis(ang * Ease(u), Vector3.up) * q0);
    }

    // ------------------------------------------------------------ director
    IEnumerator Run(string kind, GameObject focus)
    {
        active = true;
        camPos = camT.position; yaw = player.Yaw; pitch = player.Pitch; roll = 0f; shake = 0f; fov = 72f;
        gm.fadeAlpha = 0f; gm.flashAlpha = 0f; gm.staticAlpha = 0f;
        gm.audio.SetAmbient(0f);
        float floorY = player.transform.position.y;

        Bounds b = new Bounds(camPos + player.transform.forward * 3f, Vector3.one);
        // los muebles son grupos de piezas: el volumen es el de todas las piezas visibles juntas
        var rends = focus != null ? focus.GetComponentsInChildren<Renderer>() : null;
        if (rends != null && rends.Length > 0)
        {
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        }
        Vector3 fwd = camPos - b.center; fwd.y = 0f;
        fwd = fwd.sqrMagnitude < 0.001f ? -player.transform.forward : fwd.normalized;
        // las puertas y el hueco son ortogonales: usa el eje dominante para que todo caiga sobre la cara del mueble
        fwd = Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z) ? new Vector3(Mathf.Sign(fwd.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(fwd.z));

        switch (kind)
        {
            case "closet": yield return Wardrobe(focus, b, fwd, floorY, false); break;
            case "wardrobe": yield return Wardrobe(focus, b, fwd, floorY, true); break;
            case "fridge": yield return Fridge(b, fwd); break;
            case "mirror": yield return Mirror(b, fwd); break;
            case "bed": yield return Bed(b, floorY); break;
            case "tub": yield return Tub(b, fwd, floorY); break;
            case "pit": yield return Pit(b, floorY); break;
            case "tv": yield return TV(b, fwd, floorY); break;
            case "doll": yield return DollScene(focus, b, fwd); break;
            default: yield return Alarm(floorY); break;
        }

        yield return FadeBlack(0.3f);
        gm.audio.SetHeart(0f);
        yield return Wait(0.7f);
        active = false;
        gm.EndDeath();
    }

    // ------------------------------------------------------------ escenas
    /// <summary>Closet / armario: se abren las puertas, ojos en la oscuridad, brazos largos y rugido.</summary>
    IEnumerator Wardrobe(GameObject go, Bounds b, Vector3 fwd, float floorY, bool tall)
    {
        Vector3 front = Front(b, fwd);
        Vector3 face = new Vector3(front.x, b.center.y + 0.2f, front.z);
        gm.audio.Sfx("stinger", 0.55f); gm.audio.Sfx("creak", 0.9f); gm.audio.SetHeart(0.75f);
        yield return LookAt(face, 0.6f);

        bool alongX = Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z);
        Vector3 side = alongX ? Vector3.forward : Vector3.right;
        float wdt = alongX ? b.size.z : b.size.x, h = b.size.y;
        var black = Mon.Lit(Color.black, 0f);
        Vector3 holeSize = alongX ? new Vector3(0.02f, h * 0.94f, wdt * 0.9f) : new Vector3(wdt * 0.9f, h * 0.94f, 0.02f);
        Cube(new Vector3(front.x, b.center.y, front.z) + fwd * 0.012f, holeSize, black);
        var wood = Mon.Lit(new Color(0.22f, 0.13f, 0.08f), 0.2f);
        for (int s = -1; s <= 1; s += 2)
            StartCoroutine(Swing(new Vector3(front.x, b.min.y + 0.02f, front.z) + side * s * wdt * 0.5f + fwd * 0.03f, -side * s, wdt * 0.5f, h * 0.96f, fwd, wood, 0.45f));
        yield return Wait(0.6f);

        float scale = tall ? 1.35f : 1.1f;
        Color eye = tall ? new Color(1f, 0.1f, 0.05f) : new Color(1f, 0.85f, 0.15f);
        var fig = Track(Mon.Figure(new Vector3(front.x, floorY, front.z) + fwd * 0.06f, fwd, new Color(0.02f, 0.02f, 0.025f), eye, scale));
        // ojos que parpadean en la oscuridad
        for (int i = 0; i < 3; i++) { fig.SetEyes(1f); yield return Wait(0.09f); fig.SetEyes(0f); yield return Wait(0.13f); }
        fig.SetEyes(1f);
        gm.audio.Sfx("whisper", 0.8f); gm.Pulse(0.5f);
        yield return Wait(0.6f);

        // si estás muy cerca del mueble, das un paso atrás asustado: la criatura necesita espacio para verse
        const float safeDist = 2.3f;
        float dist = Vector2.Distance(new Vector2(camPos.x, camPos.z), new Vector2(front.x, front.z));
        if (dist < safeDist)
        {
            Vector3 from = camPos, to = camPos + fwd * (safeDist - dist);
            gm.audio.Sfx("step", 0.7f, 0.8f); gm.Pulse(0.4f);
            yield return Tween(0.3f, u => camPos = Vector3.Lerp(from, to, Ease(u)));
            dist = safeDist;
            yield return LookAt(face, 0.15f);
        }

        // el rugido de la bestia
        gm.audio.Sfx("roar", 1f); gm.Pulse(1f); shake = 0.025f;
        Vector3 p0 = fig.transform.position, p1 = p0 + fwd * (tall ? 1.3f : 0.6f);
        yield return Tween(0.7f, u => { fig.transform.position = Vector3.Lerp(p0, p1, Ease(u)); fov = Mathf.Lerp(72f, 62f, u); });

        // los brazos se estiran hacia ti
        gm.audio.Sfx("scream", 0.85f, 0.9f);
        yield return Tween(0.55f, u => { fig.Reach(Ease(u), 3.2f); shake = 0.03f + 0.04f * u; });

        // se lanza sobre ti: la cara llena la pantalla, iluminada de rojo
        Vector3 dir = new Vector3(-fwd.x, 0f, -fwd.z).normalized;                 // de ti hacia el mueble
        var redLight = LightAt(camPos + dir * 0.2f + Vector3.up * 0.1f, new Color(1f, 0.2f, 0.12f), 5f, 5f);
        float headH = 1.85f * scale;
        Vector3 s0 = fig.transform.position;
        Vector3 s1 = new Vector3(camPos.x, camPos.y - headH + 0.05f, camPos.z) + dir * 1.05f;
        gm.audio.Sfx("roar", 1f, 1.15f); gm.audio.Sfx("stinger", 0.7f);
        Flash(new Color(0.8f, 0f, 0f), 0.25f, 0.25f);
        yield return Tween(0.3f, u =>
        {
            float e = u * u;
            fig.transform.position = Vector3.Lerp(s0, s1, e);
            fig.transform.rotation = Quaternion.LookRotation(-dir);
            fig.Reach(1f, 3.2f); fig.Mouth(e); fov = Mathf.Lerp(62f, 80f, e); shake = 0.08f + 0.1f * u; gm.Pulse(1f);
        });

        // te sujeta y te arrastra hacia el mueble, sin atravesarlo: solo recorre el espacio libre
        gm.audio.Sfx("scream", 1f, 1.05f);
        Vector3 c0 = camPos, off = fig.transform.position - camPos;
        float travel = Mathf.Max(0f, dist - 1.05f - 0.25f);
        Vector3 c1 = camPos + dir * travel;
        float r0 = roll;
        yield return Tween(1.1f, u =>
        {
            float e = Ease(u);
            camPos = Vector3.Lerp(c0, c1, e) + (tall ? Vector3.up * 0.5f * e : Vector3.zero);
            fig.transform.position = camPos + off;
            redLight.transform.position = camPos + dir * 0.2f + Vector3.up * 0.1f;
            fig.Mouth(0.7f + 0.3f * Mathf.Sin(u * 30f)); fig.Reach(1f, 3.2f);
            roll = Mathf.Lerp(r0, 22f, e); shake = 0.12f + 0.1f * u; gm.Pulse(1f); fov = Mathf.Lerp(80f, 100f, e);
        });
    }

    /// <summary>Refrigerador: sale luz fría y una araña gigante salta a tu cara.</summary>
    IEnumerator Fridge(Bounds b, Vector3 fwd)
    {
        Vector3 front = Front(b, fwd);
        Vector3 face = new Vector3(front.x, b.center.y + 0.3f, front.z);
        gm.audio.Sfx("stinger", 0.4f); gm.audio.Sfx("creak", 0.8f, 0.8f); gm.audio.SetHeart(0.8f);
        yield return LookAt(face, 0.6f);

        bool alongX = Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z);
        Vector3 side = alongX ? Vector3.forward : Vector3.right;
        float wdt = alongX ? b.size.z : b.size.x;
        // luz fría desde dentro
        Vector3 holeSize = alongX ? new Vector3(0.02f, b.size.y * 0.94f, wdt * 0.92f) : new Vector3(wdt * 0.92f, b.size.y * 0.94f, 0.02f);
        Cube(new Vector3(front.x, b.center.y, front.z) + fwd * 0.012f, holeSize, Mon.Glow(new Color(0.8f, 0.95f, 1f), 1.6f));
        var l = LightAt(front + fwd * 0.4f + Vector3.up * 0.3f, new Color(0.75f, 0.9f, 1f), 3.5f, 6f);
        l.gameObject.AddComponent<Flicker>().baseIntensity = 3.5f;
        StartCoroutine(Swing(new Vector3(front.x, b.min.y + 0.02f, front.z) + side * wdt * 0.5f + fwd * 0.03f, -side, wdt, b.size.y * 0.96f, fwd, Mon.Lit(new Color(0.62f, 0.64f, 0.67f), 0.78f), 0.5f));
        yield return Wait(0.9f);

        // la araña
        var sp = Track(Mon.Spider(new Vector3(front.x, b.center.y - 0.5f, front.z) - fwd * 0.45f, fwd, 0.7f));
        gm.audio.Sfx("skitter", 1f); gm.Pulse(0.6f);
        sp.SetEyes(1f);
        Vector3 s0 = sp.transform.position;
        yield return Tween(0.6f, u => { sp.Wiggle(Time.time); sp.transform.position = s0 + fwd * (0.9f * u); });
        yield return Wait(0.15f);

        // salto a tu cara
        gm.audio.Sfx("screech", 1f); shake = 0.04f;
        Vector3 a0 = sp.transform.position;
        yield return Tween(0.38f, u =>
        {
            Vector3 target = camPos + (camPos - a0).normalized * -0.35f;
            sp.transform.position = Vector3.Lerp(a0, target, u * u) + Vector3.up * Mathf.Sin(Mathf.PI * u) * 0.5f;
            sp.transform.rotation = Quaternion.LookRotation(camPos - sp.transform.position);
            sp.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 2.4f, u);
            sp.Wiggle(Time.time, 40f); fov = Mathf.Lerp(72f, 90f, u);
        });
        Flash(Color.white, 0.9f, 0.4f); gm.Pulse(1f); shake = 0.2f;
        gm.audio.Sfx("scream", 1f, 1.1f);
        yield return Wait(0.8f);
    }

    /// <summary>Espejo: se rompe, una mano pálida te jala hacia el reflejo.</summary>
    IEnumerator Mirror(Bounds b, Vector3 fwd)
    {
        Vector3 front = Front(b, fwd);
        gm.audio.Sfx("whisper", 0.9f); gm.audio.SetHeart(0.7f);
        yield return LookAt(front, 0.6f);
        yield return Wait(0.35f);
        // se rompe
        gm.audio.Sfx("glass", 1f); gm.audio.Sfx("stinger", 0.6f);
        Flash(Color.white, 0.8f, 0.3f); shake = 0.03f; gm.Pulse(0.8f);
        var dark = Mon.Lit(new Color(0.02f, 0.02f, 0.03f), 0.9f);
        bool alongX = Mathf.Abs(fwd.x) > Mathf.Abs(fwd.z);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f + UnityEngine.Random.Range(-10f, 10f);
            var g = Cube(front + fwd * 0.02f + Vector3.up * 0.1f, alongX ? new Vector3(0.005f, 0.015f, 0.9f) : new Vector3(0.9f, 0.015f, 0.005f), dark);
            g.transform.RotateAround(front + fwd * 0.02f + Vector3.up * 0.1f, fwd, a);
        }
        yield return Wait(0.5f);
        // brazo pálido
        var arm = Track(Mon.PaleArm(new Vector3(front.x, b.center.y - 0.1f, front.z) + fwd * 0.02f, fwd));
        gm.audio.Sfx("creak", 0.9f, 1.3f);
        yield return Tween(0.8f, u => arm.Extend(Mathf.Lerp(0.1f, 2.0f, Ease(u))));
        gm.audio.Sfx("scream", 1f, 1.05f); shake = 0.06f;
        Vector3 c0 = camPos, c1 = new Vector3(front.x, b.center.y, front.z) - fwd * 0.05f;
        yield return Tween(1.1f, u =>
        {
            float e = Ease(u);
            camPos = Vector3.Lerp(c0, c1, e); fov = Mathf.Lerp(72f, 105f, e); roll = 20f * e; shake = 0.06f + 0.1f * u;
            arm.Extend(Mathf.Lerp(2.0f, 1.2f, e)); gm.Pulse(1f);
        });
        Flash(Color.white, 1f, 0.3f);
    }

    /// <summary>
    /// Cama: te acuestas, la luz parpadea y, en cada parpadeo, una figura pálida está más cerca (al pie de la cama);
    /// en el último se lanza sobre ti, la cara llena la pantalla y te arrastra fuera de la cama. Siempre se ve qué te ataca.
    /// </summary>
    IEnumerator Bed(Bounds b, float floorY)
    {
        gm.audio.SetHeart(0.9f); gm.audio.Sfx("whisper", 0.9f);
        // el pie de la cama: eje largo del colchón, del lado desde el que llegaste
        Vector3 axis = b.size.x > b.size.z ? Vector3.right : Vector3.forward;
        Vector3 footDir = Vector3.Dot(camPos - b.center, axis) >= 0f ? axis : -axis;
        Vector3 lie = new Vector3(b.center.x, b.max.y + 0.3f, b.center.z) - footDir * 0.3f;
        Vector3 c0 = camPos; float p0 = pitch, f0 = fov, y0 = yaw;
        float yawFoot = Mathf.Atan2(footDir.x, footDir.z) * Mathf.Rad2Deg;
        float dyaw = Mathf.DeltaAngle(y0, yawFoot);
        gm.fadeColor = new Color(0.25f, 0f, 0f);
        gm.audio.HeartSpeed(1f);
        // te acuestas mirando al techo...
        yield return Tween(1.2f, u =>
        {
            float e = Ease(u);
            camPos = Vector3.Lerp(c0, lie, e); pitch = Mathf.Lerp(p0, -75f, e); fov = Mathf.Lerp(f0, 60f, e);
            gm.fadeAlpha = 0.45f * e; roll = 5f * Mathf.Sin(u * 5f); gm.audio.HeartSpeed(1f + 0.3f * u);
        });
        gm.audio.Sfx("giggle", 0.5f, 0.8f);
        yield return Wait(0.8f);
        // ...y te incorporas al oír algo: miras hacia el pie de la cama, en la oscuridad
        gm.audio.Sfx("creak", 0.8f, 0.7f); gm.Pulse(0.5f);
        Vector3 sit = new Vector3(lie.x, b.max.y + 0.85f, lie.z);
        float py = pitch, rr = roll;
        yield return Tween(0.6f, u =>
        {
            float e = Ease(u);
            camPos = Vector3.Lerp(lie, sit, e); pitch = Mathf.Lerp(py, -4f, e); yaw = y0 + dyaw * e; roll = Mathf.Lerp(rr, 0f, e);
            gm.fadeAlpha = Mathf.Lerp(0.45f, 0.15f, e); fov = Mathf.Lerp(60f, 66f, e);
        });

        // la figura pálida, de pie junto a la pared, ilumina su propia cara con una luz fría
        var fig = Track(Mon.Figure(new Vector3(b.center.x, floorY, b.center.z) + footDir * 3.6f, -footDir, new Color(0.7f, 0.72f, 0.76f), new Color(1f, 0.15f, 0.1f), 1.05f));
        var glow = LightAt(fig.transform.position + Vector3.up * 1.7f - footDir * 0.5f, new Color(0.7f, 0.8f, 1f), 3.2f, 5f);
        fig.SetEyes(0f); glow.enabled = false;
        float[] dists = { 3.6f, 2.5f, 1.7f };
        for (int i = 0; i < 3; i++)
        {
            gm.SetDark(true); glow.enabled = false; fig.SetEyes(0f);
            gm.audio.Sfx("creak", 0.5f, 0.6f + 0.15f * i);
            yield return Wait(0.35f);
            // parpadeo de luz: aparece a esa distancia
            gm.SetDark(false);
            glow.enabled = true; fig.SetEyes(1f);
            fig.transform.position = new Vector3(b.center.x, floorY, b.center.z) + footDir * dists[i];
            glow.transform.position = fig.transform.position + Vector3.up * 1.7f - footDir * 0.5f;
            fig.Reach(i * 0.35f, 2.4f); fig.Mouth(i * 0.3f);
            gm.Pulse(0.5f + 0.2f * i); shake = 0.02f + 0.02f * i;
            gm.audio.Sfx(i == 2 ? "stinger" : "whisper", i == 2 ? 0.5f : 0.7f);
            yield return Wait(0.32f);
        }

        // se lanza sobre ti: la cara llena la pantalla, con la boca abierta
        gm.SetDark(false);
        var redLight = LightAt(camPos + footDir * 0.2f + Vector3.up * 0.1f, new Color(1f, 0.2f, 0.12f), 5f, 5f);
        float headH = 1.85f * 1.05f;
        Vector3 s0 = fig.transform.position;
        Vector3 s1 = new Vector3(camPos.x, camPos.y - headH + 0.05f, camPos.z) + footDir * 1.0f;
        gm.audio.Sfx("roar", 1f, 1.1f); gm.audio.Sfx("stinger", 0.7f);
        Flash(new Color(0.8f, 0f, 0f), 0.25f, 0.25f);
        yield return Tween(0.3f, u =>
        {
            float e = u * u;
            fig.transform.position = Vector3.Lerp(s0, s1, e);
            fig.Reach(0.4f + 0.6f * e, 2.6f); fig.Mouth(e); fov = Mathf.Lerp(66f, 82f, e); shake = 0.08f + 0.1f * u; gm.Pulse(1f);
        });

        // te agarra y te arrastra fuera de la cama, hacia el piso
        gm.audio.Sfx("scream", 1f, 1.05f);
        Vector3 d0 = camPos, off = fig.transform.position - camPos;
        Vector3 d1 = new Vector3(camPos.x, floorY + 0.5f, camPos.z) + footDir * 1.1f;
        float r0 = roll, pp = pitch;
        yield return Tween(1.0f, u =>
        {
            float e = Ease(u);
            camPos = Vector3.Lerp(d0, d1, e); fig.transform.position = camPos + off;
            redLight.transform.position = camPos + footDir * 0.2f + Vector3.up * 0.1f;
            pitch = Mathf.Lerp(pp, -25f, e); roll = Mathf.Lerp(r0, 28f, e); fov = Mathf.Lerp(82f, 100f, e);
            fig.Mouth(0.7f + 0.3f * Mathf.Sin(u * 30f)); fig.Reach(1f, 2.6f); shake = 0.12f + 0.1f * u; gm.Pulse(1f);
        });
    }

    /// <summary>Bañera: una figura empapada se levanta tras la cortina y se lanza sobre ti.</summary>
    IEnumerator Tub(Bounds b, Vector3 fwd, float floorY)
    {
        Vector3 center = new Vector3(b.center.x, b.max.y, b.center.z);
        gm.audio.Sfx("drip", 0.9f); gm.audio.SetHeart(0.7f);
        yield return LookAt(center + Vector3.up * 0.6f, 0.6f);
        // la cortina corrediza se abre sola, deslizándose por el riel
        var curtain = UnityEngine.Object.FindFirstObjectByType<SlidingCurtain>();
        if (curtain != null) { gm.audio.Sfx("creak", 0.8f, 1.6f); curtain.Slide(1f); }
        gm.audio.Sfx("drip", 0.8f); yield return Wait(0.9f);
        gm.audio.Sfx("drip", 0.9f); gm.audio.Sfx("whisper", 0.6f); yield return Wait(0.5f);

        var fig = Track(Mon.Figure(new Vector3(b.center.x, floorY - 1.4f, b.center.z), fwd, new Color(0.62f, 0.7f, 0.72f), new Color(0.9f, 0.95f, 1f), 1.05f));
        gm.audio.Sfx("creak", 0.9f, 0.7f); gm.audio.Sfx("stinger", 0.5f);
        yield return Tween(1.0f, u => fig.transform.position = new Vector3(b.center.x, Mathf.Lerp(floorY - 1.4f, floorY + 0.05f, Ease(u)), b.center.z));
        fig.SetEyes(1f); gm.Pulse(0.7f);
        yield return Wait(0.4f);
        gm.audio.Sfx("scream", 1f, 0.95f); gm.audio.Sfx("roar", 0.7f, 1.2f); shake = 0.04f;
        Vector3 f0 = fig.transform.position, f1 = new Vector3(camPos.x, floorY - 0.9f, camPos.z) - fwd * 0.5f;
        yield return Tween(0.55f, u => { fig.transform.position = Vector3.Lerp(f0, f1, u * u); fig.Reach(u, 2.6f); fov = Mathf.Lerp(72f, 95f, u); });
        Flash(Color.white, 0.8f, 0.35f); gm.Pulse(1f); shake = 0.2f; roll = 18f;
        yield return Wait(0.7f);
    }

    /// <summary>Libro rojo: trampa; el piso se abre y caes gritando.</summary>
    IEnumerator Pit(Bounds b, float floorY)
    {
        gm.audio.Sfx("boom", 1f); gm.audio.Sfx("creak", 0.9f, 0.7f); shake = 0.03f;
        yield return LookAt(b.center, 0.4f);
        yield return Wait(0.25f);
        gm.audio.Sfx("glass", 0.5f, 0.5f); gm.audio.SetHeart(0.9f);
        var pit = Cube(new Vector3(camPos.x, floorY + 0.035f, camPos.z), new Vector3(0.1f, 0.03f, 0.1f), Mon.Lit(Color.black, 0f));
        yield return Tween(0.5f, u => { float s = Mathf.Lerp(0.2f, 3.6f, Ease(u)); pit.transform.localScale = new Vector3(s, 0.03f, s); shake = 0.05f * u; });
        // caída
        gm.audio.Sfx("scream", 1f, 0.75f);
        float y0 = camPos.y, p0 = pitch; float vy = 0f;
        float t = 0f;
        while (t < 1.6f)
        {
            t += Time.deltaTime; vy += 26f * Time.deltaTime;
            camPos.y = y0 - 0.5f * 26f * t * t * 0.6f;
            pitch = Mathf.Lerp(p0, -55f, Mathf.Clamp01(t / 0.5f)); roll = 12f * Mathf.Sin(t * 6f);
            fov = Mathf.Lerp(72f, 100f, Mathf.Clamp01(t / 1.2f)); shake = 0.04f;
            gm.fadeColor = Color.black; gm.fadeAlpha = Mathf.Clamp01((t - 0.5f) / 1.1f);
            gm.Pulse(0.6f);
            yield return null;
        }
        gm.audio.Sfx("boom", 1f, 0.6f);
        Flash(Color.white, 0.5f, 0.2f);
    }

    /// <summary>Televisión: estática, se apagan las luces y una silueta sale arrastrándose.</summary>
    IEnumerator TV(Bounds b, Vector3 fwd, float floorY)
    {
        Vector3 front = Front(b, fwd);
        gm.audio.Sfx("static", 1f); gm.audio.SetHeart(0.7f);
        yield return LookAt(new Vector3(front.x, b.center.y + 0.1f, front.z), 0.5f);
        for (int i = 0; i < 8; i++) { gm.staticAlpha = (i % 2 == 0) ? 0.8f : 0.25f; yield return Wait(0.09f); }
        gm.staticAlpha = 0.55f;
        gm.audio.Sfx("boom", 0.9f);
        gm.SetDark(true);
        var glow = LightAt(front + fwd * 0.5f + Vector3.up * 0.2f, new Color(0.75f, 0.85f, 1f), 2.4f, 5f);
        glow.gameObject.AddComponent<Flicker>().baseIntensity = 2.4f;
        yield return Wait(0.5f);

        var fig = Track(Mon.Figure(new Vector3(front.x, floorY, front.z) + fwd * 0.1f, fwd, new Color(0.01f, 0.01f, 0.015f), new Color(0.9f, 0.95f, 1f), 1.0f));
        fig.SetEyes(1f); gm.audio.Sfx("whisper", 0.9f); gm.audio.Sfx("stinger", 0.6f); gm.Pulse(0.8f);
        Vector3 p0 = fig.transform.position;
        yield return Tween(1.1f, u => { fig.transform.position = p0 + fwd * (1.2f * u); gm.staticAlpha = 0.35f + 0.3f * Mathf.PerlinNoise(Time.time * 40f, 1f); });
        gm.audio.Sfx("scream", 1f); gm.audio.Sfx("roar", 0.6f, 1.3f); shake = 0.05f;
        Vector3 q0 = fig.transform.position, q1 = new Vector3(camPos.x, floorY, camPos.z) - fwd * 0.3f;
        yield return Tween(0.5f, u => { fig.transform.position = Vector3.Lerp(q0, q1, u * u); fig.Reach(u, 2.5f); gm.staticAlpha = 0.6f; fov = Mathf.Lerp(72f, 95f, u); });
        Flash(Color.white, 1f, 0.3f); gm.Pulse(1f); shake = 0.2f;
        yield return Wait(0.6f);
    }

    /// <summary>Muñeca: gira la cabeza, ríe y salta a tu cara.</summary>
    IEnumerator DollScene(GameObject go, Bounds b, Vector3 fwd)
    {
        if (go != null) foreach (var r in go.GetComponentsInChildren<Renderer>()) r.enabled = false;
        gm.audio.Sfx("giggle", 0.9f); gm.audio.SetHeart(0.8f);
        var doll = Track(Mon.Doll(new Vector3(b.center.x, b.min.y, b.center.z), fwd, 1.4f));
        yield return LookAt(b.center + Vector3.up * 0.1f, 0.5f);
        yield return Wait(0.6f);
        doll.SetEyes(1f); gm.Pulse(0.6f);
        gm.audio.Sfx("creak", 0.8f, 1.2f);
        yield return Tween(0.9f, u => doll.head.localRotation = Quaternion.Euler(0f, 360f * Ease(u), 0f));
        yield return Wait(0.3f);
        gm.audio.Sfx("stinger", 1f); gm.audio.Sfx("giggle", 1f, 1.3f); shake = 0.03f;
        Vector3 d0 = doll.transform.position;
        yield return Tween(0.3f, u =>
        {
            doll.transform.position = Vector3.Lerp(d0, camPos + (camPos - d0).normalized * -0.4f, u * u);
            doll.transform.rotation = Quaternion.LookRotation(camPos - doll.transform.position);
            doll.transform.localScale = Vector3.one * Mathf.Lerp(1.4f, 5f, u);
        });
        gm.audio.Sfx("scream", 1f, 1.2f);
        Flash(Color.white, 1f, 0.3f); gm.Pulse(1f); shake = 0.2f;
        yield return Wait(0.7f);
    }

    /// <summary>Alarma: sirena, luces rojas, oscuridad total y unos ojos que corren hacia ti.</summary>
    IEnumerator Alarm(float floorY)
    {
        gm.audio.Sfx("siren", 0.9f); gm.audio.SetHeart(0.8f);
        gm.flashColor = new Color(0.9f, 0f, 0f);
        for (int i = 0; i < 10; i++)
        {
            gm.flashAlpha = (i % 2 == 0) ? 0.4f : 0.05f; gm.Pulse(0.3f); shake = 0.01f;
            yield return Wait(0.17f);
        }
        gm.flashAlpha = 0f;
        gm.SetDark(true); shake = 0f;
        gm.audio.Sfx("boom", 0.8f);
        yield return Wait(0.7f);
        // pasos que se acercan
        float gap = 0.55f;
        for (int i = 0; i < 6; i++)
        {
            gm.audio.Sfx("step", 0.4f + 0.1f * i, 0.8f); gm.audio.HeartSpeed(1f + i * 0.08f);
            yield return Wait(gap); gap = Mathf.Max(0.22f, gap * 0.82f);
        }
        // los ojos en la oscuridad
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        var fig = Track(Mon.Figure(new Vector3(camPos.x, floorY, camPos.z) + dir * 7f, -dir, new Color(0.01f, 0.01f, 0.01f), new Color(1f, 0.1f, 0.05f), 1.2f));
        fig.SetEyes(1f); gm.audio.Sfx("roar", 1f); gm.Pulse(1f);
        Vector3 s0 = fig.transform.position;
        yield return Tween(1.0f, u => { fig.transform.position = Vector3.Lerp(s0, new Vector3(camPos.x, floorY, camPos.z) + dir * 0.6f, u * u); shake = 0.02f + 0.1f * u; fov = Mathf.Lerp(72f, 88f, u); });
        gm.audio.Sfx("scream", 1f, 1.1f);
        Flash(new Color(0.9f, 0f, 0f), 0.9f, 0.3f);
        yield return Wait(0.6f);
    }
}
