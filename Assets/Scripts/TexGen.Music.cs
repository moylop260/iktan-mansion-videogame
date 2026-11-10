using System;
using UnityEngine;

/// <summary>Hoja de partitura de piano escrita a mano.</summary>
public static partial class TexGen
{
    public static Texture2D SheetMusic()
    {
        const string key = "sheetmusic";
        Texture2D t;
        if (cache.TryGetValue(key, out t) && t != null) return t;
        const int n = 256;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                px[y * n + x] = Mul(new Color(0.94f, 0.9f, 0.78f), 0.86f + 0.2f * Fbm(x, y, n, 16, 16));
        // manchas de humedad en las esquinas
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float e = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y)) / 40f;
                if (e < 1f) px[y * n + x] = Mul(px[y * n + x], 0.78f + 0.22f * e);
            }
        var rng = new System.Random(1973);
        Color ink = new Color(0.07f, 0.07f, 0.1f);
        // título manuscrito arriba y las iniciales "J." abajo a la derecha
        for (float x = 70; x < 190; x += 0.7f) MDot(px, n, x, 238f + Mathf.Sin(x * 0.4f) * 2f, ink);
        for (float x = 210; x < 222; x += 0.7f) MDot(px, n, x, 12f + Mathf.Sin(x * 0.9f) * 3f, ink);
        // cuatro sistemas de cinco líneas con claves, compases y notas
        for (int s = 0; s < 4; s++)
        {
            int top = 215 - s * 52;
            for (int l = 0; l < 5; l++)
                for (int x = 18; x < n - 18; x++) MDot(px, n, x, top - l * 5f, ink * 0.85f);
            for (int y = top - 20; y <= top; y++) { MDot(px, n, 18, y, ink); MDot(px, n, n - 18, y, ink); }
            // clave de sol aproximada: espiral + trazo
            for (float a = 0; a < 9f; a += 0.1f) MDot(px, n, 30 + Mathf.Cos(a) * (2f + a * 0.5f), top - 12 + Mathf.Sin(a) * (2f + a * 0.5f), ink);
            for (int y = top - 28; y < top + 8; y++) MDot(px, n, 30, y, ink);
            // barras de compás y notas
            for (int bar = 1; bar < 4; bar++) for (int y = top - 20; y <= top; y++) MDot(px, n, 18 + bar * 56, y, ink);
            for (int k = 0; k < 14; k++)
            {
                float nx = 50 + k * 14f + (float)rng.NextDouble() * 3f;
                float ny = top - 2f - rng.Next(0, 9) * 2.5f;
                for (float a = 0; a < 6.3f; a += 0.3f) MDot(px, n, nx + Mathf.Cos(a) * 3.2f, ny + Mathf.Sin(a) * 2.4f, ink);   // cabeza
                for (int y = 0; y < 17; y++) MDot(px, n, nx + 3f, ny + y, ink);                                                  // plica
            }
        }
        t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        t.SetPixels(px);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; t.anisoLevel = 4;
        t.Apply(true);
        cache[key] = t;
        return t;
    }

    static void MDot(Color[] px, int n, float fx, float fy, Color c)
    {
        int x = Mathf.RoundToInt(fx), y = Mathf.RoundToInt(fy);
        if (x < 0 || y < 0 || x >= n || y >= n) return;
        px[y * n + x] = Color.Lerp(px[y * n + x], c, 0.9f);
    }
}
