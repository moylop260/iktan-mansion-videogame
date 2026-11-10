using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Texturas procedurales (madera, mármol, papel tapiz...) e íconos de objetos. No usa archivos de imagen.</summary>
public static class TexGen
{
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    // ---------- ruido periódico (las texturas se repiten sin costuras)
    static float H(int x, int y, int px, int py)
    {
        x = ((x % px) + px) % px; y = ((y % py) + py) % py;
        int h = x * 374761393 + y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0xffff) / 65535f;
    }
    static float VN(float x, float y, int px, int py)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(H(ix, iy, px, py), H(ix + 1, iy, px, py), fx),
                          Mathf.Lerp(H(ix, iy + 1, px, py), H(ix + 1, iy + 1, px, py), fx), fy);
    }
    /// <summary>Ruido fractal periódico; fx/fy = repeticiones del patrón base en la textura.</summary>
    static float Fbm(int x, int y, int n, int fx, int fy)
    {
        float a = 0.5f, s = 0f;
        for (int o = 0; o < 4; o++)
        {
            int px = fx << o, py = fy << o;
            s += a * VN((float)x / n * px, (float)y / n * py, px, py);
            a *= 0.5f;
        }
        return s;
    }

    static Texture2D Make(string key, int n, Func<int, int, Color> f, bool repeat = true)
    {
        Texture2D t;
        if (cache.TryGetValue(key, out t) && t != null) return t;
        t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) px[y * n + x] = f(x, y);
        t.SetPixels(px);
        t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear; t.anisoLevel = 4;
        t.Apply(true);
        cache[key] = t;
        return t;
    }

    static Color Mul(Color c, float k) { return new Color(c.r * k, c.g * k, c.b * k, 1f); }
    static Color Mix(Color a, Color b, float t) { return Color.Lerp(a, b, t); }

    // ---------- superficies
    public static Texture2D Wood()
    {
        return Make("wood", 256, (x, y) =>
        {
            int row = y / 64, ry = y % 64;
            float tone = 0.8f + 0.4f * H(row, 3, 4, 4);
            float grain = Fbm(x + row * 53, y, 256, 2, 24);
            Color c = Mul(new Color(0.36f, 0.22f, 0.12f), tone * (0.7f + 0.6f * grain));
            if (ry < 2) c = Mul(c, 0.35f);
            if (((x + row * 97) % 256) < 2) c = Mul(c, 0.4f);
            return c;
        });
    }

    public static Texture2D DarkWood()
    {
        return Make("darkwood", 256, (x, y) =>
        {
            float g = Fbm(x, y, 256, 24, 2);
            Color c = Mul(new Color(0.20f, 0.12f, 0.07f), 0.7f + 0.7f * g);
            if (x % 64 < 2) c = Mul(c, 0.35f);
            return c;
        });
    }

    public static Texture2D Marble()
    {
        return Make("marble", 256, (x, y) =>
        {
            bool dark = ((x / 128) + (y / 128)) % 2 == 0;
            float vein = Mathf.Abs(Mathf.Sin((x + Fbm(x, y, 256, 4, 4) * 90f) * 0.05f));
            Color baseC = dark ? new Color(0.10f, 0.10f, 0.12f) : new Color(0.80f, 0.78f, 0.72f);
            Color c = Mix(baseC, dark ? new Color(0.3f, 0.3f, 0.33f) : new Color(0.55f, 0.53f, 0.5f), vein * 0.5f);
            if (x % 128 < 1 || y % 128 < 1) c = Mul(c, 0.5f);
            return c;
        });
    }

    public static Texture2D Wallpaper(string key, Color a, Color b)
    {
        return Make("wp_" + key, 256, (x, y) =>
        {
            bool stripe = (x / 32) % 2 == 0;
            Color c = stripe ? a : b;
            int dx = Mathf.Abs(x % 64 - 32), dy = Mathf.Abs(y % 64 - 32);
            if (dx + dy < 11 && dx + dy > 6) c = Mix(c, Color.white, 0.12f);
            if (dx + dy <= 3) c = Mix(c, Color.black, 0.18f);
            c = Mul(c, 0.9f + 0.2f * Fbm(x, y, 256, 16, 16));
            return c;
        });
    }

    public static Texture2D Panel()
    {
        return Make("panel", 256, (x, y) =>
        {
            float g = Fbm(x, y, 256, 16, 2);
            Color c = Mul(new Color(0.22f, 0.13f, 0.08f), 0.75f + 0.6f * g);
            if (x % 128 < 3 || x % 128 > 124) c = Mul(c, 0.4f);
            int px = x % 128, py = y % 256;
            bool inset = px > 14 && px < 114 && py > 30 && py < 226;
            if (inset && (px < 17 || px > 111 || py < 33 || py > 223)) c = Mul(c, 0.55f);
            return c;
        });
    }

    public static Texture2D Tile(string key, Color a, Color b)
    {
        return Make("tile_" + key, 256, (x, y) =>
        {
            bool alt = ((x / 32) + (y / 32)) % 2 == 0;
            Color c = alt ? a : b;
            if (x % 32 < 1 || y % 32 < 1) c = new Color(0.25f, 0.25f, 0.25f);
            return Mul(c, 0.92f + 0.12f * Fbm(x, y, 256, 8, 8));
        });
    }

    public static Texture2D Carpet()
    {
        return Make("carpet", 256, (x, y) =>
        {
            Color c = new Color(0.40f, 0.05f, 0.07f);
            int e = Mathf.Min(Mathf.Min(x, 255 - x), 256);
            if (e < 10) c = new Color(0.75f, 0.58f, 0.2f);
            else if (e < 16) c = new Color(0.2f, 0.02f, 0.03f);
            int dx = Mathf.Abs(x % 48 - 24), dy = Mathf.Abs(y % 48 - 24);
            if (e > 30 && dx + dy < 9 && dx + dy > 5) c = Mix(c, new Color(0.8f, 0.6f, 0.2f), 0.55f);
            return Mul(c, 0.85f + 0.3f * H(x, y, 256, 256));
        });
    }

    public static Texture2D Plaster()
    {
        return Make("plaster", 128, (x, y) => Mul(new Color(0.62f, 0.58f, 0.5f), 0.88f + 0.2f * Fbm(x, y, 128, 8, 8)));
    }

    public static Texture2D Stone()
    {
        return Make("stone", 256, (x, y) =>
        {
            int row = y / 32; int xx = (x + (row % 2) * 32) % 64;
            Color c = Mul(new Color(0.28f, 0.27f, 0.26f), 0.7f + 0.6f * H(x / 64 + row * 5, row, 8, 8)) ;
            c = Mul(c, 0.85f + 0.3f * Fbm(x, y, 256, 16, 16));
            if (y % 32 < 2 || xx < 2) c = Mul(c, 0.4f);
            return c;
        });
    }

    public static Texture2D Books()
    {
        return Make("books", 256, (x, y) =>
        {
            int shelf = y / 64, sy = y % 64;
            if (sy < 5) return new Color(0.2f, 0.12f, 0.07f);
            int bw = 10, idx = x / bw;
            float h = H(idx, shelf, 32, 8);
            if (sy > 14 + h * 44) return new Color(0.06f, 0.04f, 0.03f);
            Color[] pal = { new Color(0.5f, 0.1f, 0.1f), new Color(0.12f, 0.25f, 0.45f), new Color(0.15f, 0.35f, 0.2f), new Color(0.45f, 0.32f, 0.12f), new Color(0.3f, 0.1f, 0.3f) };
            Color c = pal[(int)(H(idx, shelf + 9, 32, 8) * 4.99f)];
            if (x % bw == 0) c = Mul(c, 0.4f);
            if (sy % 20 < 2) c = Mix(c, new Color(0.8f, 0.7f, 0.3f), 0.6f);
            return c;
        });
    }

    public static Texture2D Painting(int variant)
    {
        return Make("paint" + variant, 128, (x, y) =>
        {
            float u = x / 127f, v = y / 127f;
            Color bg = variant == 0 ? new Color(0.12f, 0.15f, 0.12f) : variant == 1 ? new Color(0.2f, 0.1f, 0.08f) : new Color(0.08f, 0.1f, 0.18f);
            Color c = Mix(bg, Mul(bg, 2.2f), v) ;
            float dx = u - 0.5f, dy = v - 0.62f;
            if (dx * dx / 0.05f + dy * dy / 0.07f < 1f) c = new Color(0.65f, 0.5f, 0.42f);       // cara
            if (v < 0.42f && Mathf.Abs(dx) < 0.28f - v * 0.2f) c = Mul(new Color(0.18f, 0.12f, 0.1f), 0.6f + 0.6f * v); // cuerpo
            if (dx * dx / 0.004f + (dy - 0.03f) * (dy - 0.03f) / 0.0008f < 1f && Mathf.Abs(dx) > 0.07f) c = Color.black;
            c = Mul(c, 0.9f + 0.2f * H(x, y, 128, 128));
            return c;
        }, false);
    }

    // ---------- íconos de inventario (64x64, transparentes)
    public static Texture2D Icon(string item)
    {
        return Make("icon_" + item, 64, (x, y) => Color.clear, false) == null ? null : BuildIcon(item);
    }

    static readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();

    static Texture2D BuildIcon(string item)
    {
        Texture2D t;
        if (icons.TryGetValue(item, out t) && t != null) return t;
        const int n = 64;
        var px = new Color[n * n];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        Action<int, int, int, int, Color> R = (x0, y0, w, h, c) => { for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) if (x >= 0 && x < n && y >= 0 && y < n) px[y * n + x] = c; };
        Action<int, int, float, Color> C = (cx, cy, r, c) => { for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) px[y * n + x] = c; };

        Color keyColor;
        switch (item)
        {
            case "Llave verde": keyColor = new Color(0.25f, 0.85f, 0.35f); break;
            case "Llave azul": keyColor = new Color(0.35f, 0.55f, 1f); break;
            case "Llave amarilla": keyColor = new Color(1f, 0.9f, 0.25f); break;
            case "Llave dorada": keyColor = new Color(1f, 0.72f, 0.12f); break;
            default: keyColor = Color.clear; break;
        }
        if (keyColor.a > 0f)
        {
            C(16, 32, 12, keyColor); C(16, 32, 5, Color.clear);
            R(26, 30, 32, 5, keyColor); R(46, 20, 5, 10, keyColor); R(54, 22, 5, 8, keyColor); R(38, 24, 5, 6, keyColor);
            if (item == "Llave dorada") C(16, 32, 3, new Color(0.9f, 0.1f, 0.2f));
        }
        else if (item == "Linterna")
        {
            R(8, 26, 30, 12, new Color(0.25f, 0.25f, 0.3f)); R(38, 20, 12, 24, new Color(0.45f, 0.45f, 0.5f));
            R(50, 22, 5, 20, new Color(1f, 0.95f, 0.6f)); R(14, 28, 4, 8, new Color(0.7f, 0.1f, 0.1f));
        }
        else if (item == "Pilas")
        {
            R(14, 14, 16, 34, new Color(0.7f, 0.7f, 0.75f)); R(18, 48, 8, 4, new Color(0.9f, 0.8f, 0.3f)); R(14, 14, 16, 9, new Color(0.9f, 0.5f, 0.1f));
            R(36, 14, 16, 34, new Color(0.7f, 0.7f, 0.75f)); R(40, 48, 8, 4, new Color(0.9f, 0.8f, 0.3f)); R(36, 14, 16, 9, new Color(0.9f, 0.5f, 0.1f));
        }
        else if (item == "Partituras")
        {
            R(12, 8, 40, 48, new Color(0.9f, 0.86f, 0.7f));
            for (int i = 0; i < 4; i++) R(16, 18 + i * 9, 32, 1, new Color(0.15f, 0.12f, 0.1f));
            C(24, 24, 3, Color.black); R(26, 24, 1, 14, Color.black); C(36, 33, 3, Color.black); R(38, 33, 1, 14, Color.black);
        }
        else { C(32, 32, 14, new Color(0.7f, 0.7f, 0.7f)); }

        t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.SetPixels(px); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; t.Apply();
        icons[item] = t;
        return t;
    }
}
