using System;
using UnityEngine;

/// <summary>Texturas de la cocina: telarañas (con alfa) y etiquetas de latas vencidas.</summary>
public static partial class TexGen
{
    /// <summary>Telaraña de esquina (el origen del hilo está en la esquina inferior izquierda). Fondo transparente.</summary>
    public static Texture2D Cobweb()
    {
        const string key = "cobweb";
        Texture2D t;
        if (cache.TryGetValue(key, out t) && t != null) return t;
        const int n = 256;
        var px = new Color[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(0.9f, 0.9f, 0.88f, 0f);
        var rng = new System.Random(77);
        // hilos radiales desde la esquina
        int rays = 7;
        for (int r = 0; r < rays; r++)
        {
            float a = (r + 0.5f) / rays * Mathf.PI / 2f + (float)(rng.NextDouble() - 0.5) * 0.08f;
            float len = n * (0.82f + (float)rng.NextDouble() * 0.18f);
            for (float d = 0; d < len; d += 0.6f) WebDot(px, n, Mathf.Cos(a) * d, Mathf.Sin(a) * d, 0.9f);
        }
        // arcos que unen los hilos, curvados hacia la esquina (sagging)
        float[] radii = { 38, 70, 104, 140, 178, 214 };
        foreach (float rad in radii)
            for (int r = 0; r < rays - 1; r++)
            {
                float a0 = (r + 0.5f) / rays * Mathf.PI / 2f, a1 = (r + 1.5f) / rays * Mathf.PI / 2f;
                if (rng.NextDouble() < 0.12) continue;   // hilos rotos
                for (float k = 0; k <= 1f; k += 0.02f)
                {
                    float a = Mathf.Lerp(a0, a1, k);
                    float sag = Mathf.Sin(k * Mathf.PI) * rad * 0.06f;
                    float rr = rad - sag;
                    WebDot(px, n, Mathf.Cos(a) * rr, Mathf.Sin(a) * rr, 0.75f);
                }
            }
        t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        t.SetPixels(px);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        t.Apply(true);
        cache[key] = t;
        return t;
    }

    static void WebDot(Color[] px, int n, float fx, float fy, float alpha)
    {
        int x = Mathf.RoundToInt(fx), y = Mathf.RoundToInt(fy);
        for (int j = 0; j <= 1; j++)
            for (int i = 0; i <= 1; i++)
            {
                int xx = x + i, yy = y + j;
                if (xx < 0 || yy < 0 || xx >= n || yy >= n) continue;
                var c = px[yy * n + xx];
                px[yy * n + xx] = new Color(0.92f, 0.92f, 0.9f, Mathf.Max(c.a, alpha * (i + j == 0 ? 1f : 0.55f)));
            }
    }

    /// <summary>Etiqueta de lata (se enrolla en el cilindro): tomate, frijoles, maíz o sopa; óxido, manchas y fecha vencida.</summary>
    public static Texture2D CanLabel(int variant)
    {
        string key = "can" + variant;
        Texture2D t;
        if (cache.TryGetValue(key, out t) && t != null) return t;
        const int n = 128;
        Color[] bg = { new Color(0.72f, 0.12f, 0.1f), new Color(0.3f, 0.45f, 0.18f), new Color(0.85f, 0.7f, 0.15f), new Color(0.45f, 0.25f, 0.12f) };
        Color main = bg[variant % bg.Length];
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Color c = main;
                if (y < 14 || y > n - 15) c = new Color(0.75f, 0.76f, 0.78f);                 // aros de metal arriba y abajo
                else if (y < 28 || y > n - 29) c = new Color(0.92f, 0.9f, 0.85f);              // franja crema
                else
                {
                    // dibujo: círculo (fruta/verdura) y hojas
                    float dx = x - 64f, dy = y - 64f;
                    if (dx * dx + dy * dy < 20f * 20f) c = Color.Lerp(main, new Color(1f, 0.9f, 0.75f), 0.75f);
                    if (dx * dx + dy * dy < 12f * 12f) c = Color.Lerp(main, Color.white, 0.2f);
                }
                // óxido y manchas de humedad
                float rust = Fbm(x, y, n, 4, 4);
                if (rust > 0.62f) c = Color.Lerp(c, new Color(0.45f, 0.2f, 0.07f), Mathf.Clamp01((rust - 0.62f) * 5f));
                float grime = Fbm(x + 40, y, n, 8, 8);
                c = Mul(c, 0.78f + 0.2f * grime);
                px[y * n + x] = c;
            }
        // fecha de caducidad estampada en negro con dígitos de 7 segmentos: 1989
        int[][] d = {
            new[]{0,1,1,0,0,0,0}, new[]{1,1,1,1,0,1,1}, new[]{1,1,1,1,0,1,1}, new[]{1,1,1,1,0,1,1}
        };
        for (int k = 0; k < 4; k++)
        {
            int ox = 40 + k * 12;
            Seg7(px, n, ox, 18, d[k]);
        }
        t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        t.SetPixels(px);
        t.wrapMode = TextureWrapMode.Repeat; t.filterMode = FilterMode.Bilinear;
        t.Apply(true);
        cache[key] = t;
        return t;
    }

    static void Seg7(Color[] px, int n, int ox, int oy, int[] s)
    {
        Color ink = new Color(0.05f, 0.05f, 0.05f);
        int w = 6, h = 5;
        Action<int, int, int, int> L = (x0, y0, x1, y1) =>
        {
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
                for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
                    if (x >= 0 && y >= 0 && x < n && y < n) px[y * n + x] = ink;
        };
        if (s[0] == 1) L(ox, oy + 2 * h, ox + w, oy + 2 * h);
        if (s[1] == 1) L(ox + w, oy + h, ox + w, oy + 2 * h);
        if (s[2] == 1) L(ox + w, oy, ox + w, oy + h);
        if (s[3] == 1) L(ox, oy, ox + w, oy);
        if (s[4] == 1) L(ox, oy, ox, oy + h);
        if (s[5] == 1) L(ox, oy + h, ox, oy + 2 * h);
        if (s[6] == 1) L(ox, oy + h, ox + w, oy + h);
    }
}
