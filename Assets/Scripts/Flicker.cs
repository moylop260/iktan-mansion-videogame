using UnityEngine;

/// <summary>Parpadeo de fuego/velas en una luz.</summary>
public class Flicker : MonoBehaviour
{
    public float baseIntensity = 1f, amount = 0.25f, speed = 6f;
    Light l; float seed;
    void Awake() { l = GetComponent<Light>(); seed = Random.value * 100f; }
    void Update()
    {
        if (l == null) return;
        l.intensity = baseIntensity * (1f + (Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f) * 2f * amount);
    }
}
