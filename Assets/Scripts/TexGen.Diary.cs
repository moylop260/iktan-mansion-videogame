using System;
using UnityEngine;

/// <summary>Hojas del diario de Javier: papel con renglones, letra manuscrita, manchas y el año 1973 encerrado en rojo.</summary>
public static partial class TexGen
{
    const int DiaryN = 256;

    public static Texture2D DiaryPage(bool left)
    {
        string key = left ? "diary_left" : "diary_right";
        Texture2D t;
        if (cache.TryGetValue(key, out t) && t != null) return t;

        var px = new Color[DiaryN * DiaryN];
        // papel amarillento con fibras
        for (int y = 0; y < DiaryN; y++)
            for (int x = 0; x < DiaryN; x++)
            {
                float n = Fbm(x, y, DiaryN, 16, 16);
                Color paper = new Color(0.93f, 0.88f, 0.74f);
                px[y * DiaryN + x] = Mul(paper, 0.86f + 0.2f * n);
            }
        // sombra del lomo en el borde interior de la hoja
        for (int y = 0; y < DiaryN; y++)
            for (int k = 0; k < 40; k++)
            {
                int x = left ? DiaryN - 1 - k : k;
                float d = 1f - k / 40f;
                px[y * DiaryN + x] = Mul(px[y * DiaryN + x], 1f - 0.28f * d * d);
            }
        // renglones azules y margen rojo
        for (int y = 20; y < DiaryN - 10; y += 14)
            for (int x = 8; x < DiaryN - 8; x++) px[y * DiaryN + x] = Mix(px[y * DiaryN + x], new Color(0.35f, 0.5f, 0.75f), 0.5f);
        int mx = left ? 34 : 24;
        for (int y = 0; y < DiaryN; y++) px[y * DiaryN + mx] = Mix(px[y * DiaryN + mx], new Color(0.8f, 0.25f, 0.25f), 0.55f);

        var rng = new System.Random(left ? 1993 : 1973);
        Color ink = new Color(0.08f, 0.1f, 0.3f);
        // renglones de letra a mano: palabras ondulantes con ascendentes
        int lines = left ? 15 : 10;
        for (int i = 0; i < lines; i++)
        {
            int baseY = 24 + i * 14 + 1;
            if (baseY > DiaryN - 14) break;
            float xcur = mx + 8;
            float endX = left ? DiaryN - 22 - rng.Next(0, 40) : DiaryN - 18 - rng.Next(10, 60);
            if (!left && i >= 8) endX = mx + 30 + rng.Next(0, 40);   // la última parte del texto se acorta
            while (xcur < endX)
            {
                float wordLen = 14 + rng.Next(0, 42);
                float phase = (float)rng.NextDouble() * 6.28f;
                for (float k = 0; k < wordLen && xcur + k < endX; k += 0.5f)
                {
                    float wave = Mathf.Sin(k * 0.65f + phase) * 3f;
                    float up = (Mathf.Sin(k * 0.21f + phase * 2f) > 0.85f) ? 5f : 0f;   // ascendentes
                    DiaryDot(px, (int)(xcur + k), (int)(baseY + 4 + wave + up), ink, 1);
                }
                xcur += wordLen + 7 + rng.Next(0, 6);
            }
        }
        if (left)
        {
            // fecha subrayada arriba y mancha de café
            for (int k = 0; k < 90; k++) { DiaryDot(px, 50 + k, 233, ink, 1); }
            DiaryRing(px, 80, 70, 26, 3, new Color(0.45f, 0.28f, 0.12f), 0.55f);
        }
        else
        {
            // "1973" grande con tinta roja, encerrado en un óvalo trazado a mano
            Color red = new Color(0.65f, 0.08f, 0.08f);
            int[][] segs = {
                new[]{0,1,1,0,0,0,0}, new[]{1,1,0,1,1,0,1}, new[]{1,1,1,1,0,0,1}, new[]{1,1,1,0,0,1,0}   // 1,9,7,3 en 7 segmentos
            };
            // 7 segmentos: 0 arriba, 1 sup-der, 2 inf-der, 3 abajo, 4 inf-izq, 5 sup-izq, 6 medio
            int[][] digits = {
                new[]{0,1,1,0,0,0,0},   // 1
                new[]{1,1,1,1,0,1,1},   // 9
                new[]{1,1,1,0,0,0,0},   // 7
                new[]{1,1,1,1,0,0,1}    // 3
            };
            int dx = 54, dy = 92, dw = 22, dh = 18;
            for (int d = 0; d < 4; d++)
            {
                int ox = dx + d * 34;
                var s = digits[d];
                if (s[0] == 1) DiaryLine(px, ox, dy + 2 * dh, ox + dw, dy + 2 * dh, red);
                if (s[1] == 1) DiaryLine(px, ox + dw, dy + dh, ox + dw, dy + 2 * dh, red);
                if (s[2] == 1) DiaryLine(px, ox + dw, dy, ox + dw, dy + dh, red);
                if (s[3] == 1) DiaryLine(px, ox, dy, ox + dw, dy, red);
                if (s[4] == 1) DiaryLine(px, ox, dy, ox, dy + dh, red);
                if (s[5] == 1) DiaryLine(px, ox, dy + dh, ox, dy + 2 * dh, red);
                if (s[6] == 1) DiaryLine(px, ox, dy + dh, ox + dw, dy + dh, red);
            }
            DiaryEllipse(px, 126, dy + dh, 90, 32, red);
            // gota de tinta
            DiaryRing(px, 200, 40, 7, 6, ink, 0.9f);
        }

        t = new Texture2D(DiaryN, DiaryN, TextureFormat.RGBA32, true);
        t.SetPixels(px);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear; t.anisoLevel = 4;
        t.Apply(true);
        cache[key] = t;
        return t;
    }

    static void DiaryDot(Color[] px, int x, int y, Color c, int r)
    {
        for (int j = -r; j <= r; j++)
            for (int i = -r; i <= r; i++)
            {
                int xx = x + i, yy = y + j;
                if (xx < 0 || yy < 0 || xx >= DiaryN || yy >= DiaryN) continue;
                if (i * i + j * j > r * r + 1) continue;
                px[yy * DiaryN + xx] = Color.Lerp(px[yy * DiaryN + xx], c, 0.9f);
            }
    }

    static void DiaryLine(Color[] px, int x0, int y0, int x1, int y1, Color c)
    {
        int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) * 2 + 1;
        for (int i = 0; i <= steps; i++)
        {
            float u = (float)i / steps;
            float wob = Mathf.Sin(u * 9f) * 0.8f;   // temblor de la mano
            DiaryDot(px, Mathf.RoundToInt(Mathf.Lerp(x0, x1, u) + wob), Mathf.RoundToInt(Mathf.Lerp(y0, y1, u)), c, 1);
        }
    }

    static void DiaryEllipse(Color[] px, int cx, int cy, int rx, int ry, Color c)
    {
        for (float a = 0f; a < 6.4f; a += 0.02f)
        {
            float r = 1f + 0.04f * Mathf.Sin(a * 3f);
            DiaryDot(px, Mathf.RoundToInt(cx + Mathf.Cos(a) * rx * r), Mathf.RoundToInt(cy + Mathf.Sin(a) * ry * r), c, 1);
        }
    }

    static void DiaryRing(Color[] px, int cx, int cy, int radius, int thick, Color c, float alpha)
    {
        for (int y = -radius - thick; y <= radius + thick; y++)
            for (int x = -radius - thick; x <= radius + thick; x++)
            {
                float d = Mathf.Sqrt(x * x + y * y);
                if (Mathf.Abs(d - radius) > thick) continue;
                int xx = cx + x, yy = cy + y;
                if (xx < 0 || yy < 0 || xx >= DiaryN || yy >= DiaryN) continue;
                px[yy * DiaryN + xx] = Color.Lerp(px[yy * DiaryN + xx], c, alpha * (1f - Mathf.Abs(d - radius) / (thick + 1f)));
            }
    }
}
