using UnityEngine;

/// <summary>
/// Reproduce los sonidos: ambiente (zumbido grave), efectos 2D/3D, latido, y sustos aleatorios
/// (susurros, crujidos, goteo, golpes lejanos) para dar terror psicológico.
/// </summary>
public class AudioManager : MonoBehaviour
{
    AudioSource amb, heart;
    AudioSource[] pool;
    int poolIdx;
    float ambTarget = 0.3f, heartTarget;
    float nextScare;

    public void Init()
    {
        AudioGen.PreloadAll();
        amb = gameObject.AddComponent<AudioSource>();
        amb.clip = AudioGen.Get("drone"); amb.loop = true; amb.volume = 0f; amb.spatialBlend = 0f; amb.Play();
        heart = gameObject.AddComponent<AudioSource>();
        heart.clip = AudioGen.Get("heart"); heart.loop = true; heart.volume = 0f; heart.spatialBlend = 0f; heart.Play();
        pool = new AudioSource[14];
        for (int i = 0; i < pool.Length; i++) { pool[i] = gameObject.AddComponent<AudioSource>(); pool[i].spatialBlend = 0f; pool[i].playOnAwake = false; }
        nextScare = Time.time + 15f;
    }

    public void SetAmbient(float v) { ambTarget = v; }
    public void SetHeart(float v) { heartTarget = v; }
    public void HeartSpeed(float pitch) { if (heart != null) heart.pitch = pitch; }

    void Update()
    {
        if (amb == null) return;
        amb.volume = Mathf.MoveTowards(amb.volume, ambTarget, Time.unscaledDeltaTime * 0.6f);
        heart.volume = Mathf.MoveTowards(heart.volume, heartTarget, Time.unscaledDeltaTime * 1.5f);
    }

    /// <summary>Efecto 2D (sin posición).</summary>
    public AudioSource Sfx(string name, float vol = 1f, float pitch = 1f)
    {
        if (pool == null) return null;
        var s = pool[poolIdx++ % pool.Length];
        s.Stop();
        s.clip = AudioGen.Get(name); s.volume = vol; s.pitch = pitch; s.spatialBlend = 0f;
        s.Play();
        return s;
    }

    /// <summary>Efecto 3D en una posición del mundo.</summary>
    public void At(string name, Vector3 pos, float vol = 1f, float pitch = 1f)
    {
        var clip = AudioGen.Get(name);
        var g = new GameObject("Sonido_" + name);
        g.transform.position = pos;
        var s = g.AddComponent<AudioSource>();
        s.clip = clip; s.volume = vol; s.pitch = pitch; s.spatialBlend = 1f; s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 1.5f; s.maxDistance = 18f;
        s.Play();
        Destroy(g, clip.length / Mathf.Max(0.1f, pitch) + 0.2f);
    }

    /// <summary>Sustos de ambiente al azar mientras se explora.</summary>
    public void Tick(bool exploring, Vector3 playerPos)
    {
        if (!exploring) return;
        if (Time.time < nextScare) return;
        nextScare = Time.time + Random.Range(16f, 38f);
        Vector2 d = Random.insideUnitCircle.normalized * Random.Range(5f, 9f);
        Vector3 p = playerPos + new Vector3(d.x, 1.4f, d.y);
        switch (Random.Range(0, 5))
        {
            case 0: At("whisper", p, 0.8f); break;
            case 1: At("creak", p, 0.9f, Random.Range(0.8f, 1.1f)); break;
            case 2: At("drip", p, 0.9f); break;
            case 3: At("boom", p, 0.55f, Random.Range(0.8f, 1.3f)); break;
            default: At("giggle", p, 0.45f, Random.Range(0.8f, 1f)); break;
        }
    }
}
