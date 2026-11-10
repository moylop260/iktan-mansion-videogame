using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extra procedural textures for the detailed furniture: clock face, upholstery, leather, curtains,
/// lace, quilts, brushed steel, book rows and a globe map. All go through the shared Make() cache.
/// </summary>
public static partial class TexGen
{
    // ---------- tiny rasterizer (used by the clock face)
    static void Line(Color[] buf, int n, float x0, float y0, float x1, float y1, float w, Color c)
    {
        float hw = w * 0.5f;
        int ax = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(x0, x1) - hw - 1f)), bx = Mathf.Min(n - 1, Mathf.CeilToInt(Mathf.Max(x0, x1) + hw + 1f));
        int ay = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(y0, y1) - hw - 1f)), by = Mathf.Min(n - 1, Mathf.CeilToInt(Mathf.Max(y0, y1) + hw + 1f));
        float dx = x1 - x0, dy = y1 - y0, len2 = dx * dx + dy * dy;
        for (int y = ay; y <= by; y++)
            for (int x = ax; x <= bx; x++)
            {
                float px = x + 0.5f - x0, py = y + 0.5f - y0;
                float t = len2 > 0f ? Mathf.Clamp01((px * dx + py * dy) / len2) : 0f;
                float ex = px - t * dx, ey = py - t * dy;
                float dist = Mathf.Sqrt(ex * ex + ey * ey);
                int i = y * n + x;
                if (dist <= hw) buf[i] = c;
                else if (dist < hw + 1f) buf[i] = Color.Lerp(c, buf[i], dist - hw);
            }
    }

    static void Disc(Color[] buf, int n, float cx, float cy, float r, Color c)
    {
        int ax = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1f)), bx = Mathf.Min(n - 1, Mathf.CeilToInt(cx + r + 1f));
        int ay = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1f)), by = Mathf.Min(n - 1, Mathf.CeilToInt(cy + r + 1f));
        for (int y = ay; y <= by; y++)
            for (int x = ax; x <= bx; x++)
            {
                float dist = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                int i = y * n + x;
                if (dist <= r) buf[i] = c;
                else if (dist < r + 1f) buf[i] = Color.Lerp(c, buf[i], dist - r);
            }
    }

    static void Ring(Color[] buf, int n, float cx, float cy, float r0, float r1, Color c)
    {
        int ax = Mathf.Max(0, Mathf.FloorToInt(cx - r1 - 1f)), bx = Mathf.Min(n - 1, Mathf.CeilToInt(cx + r1 + 1f));
        int ay = Mathf.Max(0, Mathf.FloorToInt(cy - r1 - 1f)), by = Mathf.Min(n - 1, Mathf.CeilToInt(cy + r1 + 1f));
        for (int y = ay; y <= by; y++)
            for (int x = ax; x <= bx; x++)
            {
                float dist = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                int i = y * n + x;
                if (dist >= r0 && dist <= r1) buf[i] = c;
                else if (dist > r1 && dist < r1 + 1f) buf[i] = Color.Lerp(c, buf[i], dist - r1);
                else if (dist < r0 && dist > r0 - 1f) buf[i] = Color.Lerp(c, buf[i], r0 - dist);
            }
    }

    /// <summary>Draws one Roman numeral glyph (I, V or X) with strokes. Returns its advance width in px.</summary>
    static float Glyph(Color[] buf, int n, char ch, float x, float yBottom, float hgt, float stroke, Color c)
    {
        if (ch == 'I')
        {
            float w = hgt * 0.32f, mx = x + w / 2f;
            Line(buf, n, mx, yBottom, mx, yBottom + hgt, stroke, c);
            Line(buf, n, x, yBottom, x + w, yBottom, stroke * 0.8f, c);
            Line(buf, n, x, yBottom + hgt, x + w, yBottom + hgt, stroke * 0.8f, c);
            return w;
        }
        if (ch == 'V')
        {
            float w = hgt * 0.78f;
            Line(buf, n, x, yBottom + hgt, x + w / 2f, yBottom, stroke, c);
            Line(buf, n, x + w, yBottom + hgt, x + w / 2f, yBottom, stroke * 0.7f, c);
            Line(buf, n, x - stroke * 0.3f, yBottom + hgt, x + stroke * 1.4f, yBottom + hgt, stroke * 0.8f, c);
            Line(buf, n, x + w - stroke * 1.4f, yBottom + hgt, x + w + stroke * 0.3f, yBottom + hgt, stroke * 0.8f, c);
            return w;
        }
        if (ch == 'X')
        {
            float w = hgt * 0.78f;
            Line(buf, n, x, yBottom + hgt, x + w, yBottom, stroke, c);
            Line(buf, n, x, yBottom, x + w, yBottom + hgt, stroke * 0.7f, c);
            return w;
        }
        return hgt * 0.3f;
    }

    static float NumeralWidth(string s, float hgt)
    {
        float w = 0f;
        for (int i = 0; i < s.Length; i++) w += (s[i] == 'I' ? 0.32f : 0.78f) * hgt + (i > 0 ? hgt * 0.14f : 0f);
        return w;
    }

    /// <summary>
    /// 256x256 clamp texture: ivory dial with Roman numerals, minute ticks, a thin gold ring and two black hands
    /// stopped at 3:00 (hour hand on the III, minute hand on the XII). y = 0 is the bottom of the face.
    /// </summary>
    public static Texture2D ClockFace()
    {
        Texture2D cached;
        if (cache.TryGetValue("clockface", out cached) && cached != null) return cached;
        const int n = 256;
        float c = n / 2f;
        var buf = new Color[n * n];
        Color ivory = new Color(0.93f, 0.89f, 0.78f), black = new Color(0.06f, 0.05f, 0.05f);
        Color gold = new Color(0.82f, 0.64f, 0.26f), brass = new Color(0.55f, 0.42f, 0.18f), outer = new Color(0.15f, 0.1f, 0.06f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c, r = Mathf.Sqrt(dx * dx + dy * dy) / c;
                Color col = r > 0.98f ? outer : Mul(ivory, 1.02f - 0.18f * r * r);
                col = Mul(col, 0.95f + 0.1f * H(x, y, n, n));   // paper grain
                if (r < 0.98f && H(x / 3, y / 3, n, n) > 0.985f) col = Mix(col, brass, 0.5f);   // age spots
                buf[y * n + x] = col;
            }
        // chapter rings
        Ring(buf, n, c, c, c * 0.955f, c * 0.98f, outer);
        Ring(buf, n, c, c, c * 0.925f, c * 0.955f, gold);
        Ring(buf, n, c, c, c * 0.905f, c * 0.915f, black);
        Ring(buf, n, c, c, c * 0.70f, c * 0.712f, black);
        // minute and hour ticks
        for (int i = 0; i < 60; i++)
        {
            float a = i * 6f * Mathf.Deg2Rad;
            bool hour = i % 5 == 0;
            float r0 = hour ? 0.80f : 0.845f, r1 = 0.90f;
            Line(buf, n, c + Mathf.Cos(a) * c * r0, c + Mathf.Sin(a) * c * r0, c + Mathf.Cos(a) * c * r1, c + Mathf.Sin(a) * c * r1, hour ? 3.2f : 1.4f, black);
        }
        // Roman numerals (upright), XII at the top, III at the right
        string[] nums = { "XII", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI" };
        float gh = n * 0.085f, stroke = 2.4f;
        for (int hr = 0; hr < 12; hr++)
        {
            float ang = (90f - hr * 30f) * Mathf.Deg2Rad;
            float cxn = c + Mathf.Cos(ang) * c * 0.60f, cyn = c + Mathf.Sin(ang) * c * 0.60f;
            string s = nums[hr];
            float x = cxn - NumeralWidth(s, gh) / 2f, yb = cyn - gh / 2f;
            for (int k = 0; k < s.Length; k++)
            {
                float adv = Glyph(buf, n, s[k], x, yb, gh, stroke, black);
                x += adv + gh * 0.14f;
            }
        }
        // winding arbors (two small brass-rimmed holes), a maker's cartouche below the centre
        Disc(buf, n, c - c * 0.3f, c + c * 0.12f, 5.5f, brass);
        Disc(buf, n, c - c * 0.3f, c + c * 0.12f, 3.5f, black);
        Disc(buf, n, c + c * 0.3f, c + c * 0.12f, 5.5f, brass);
        Disc(buf, n, c + c * 0.3f, c + c * 0.12f, 3.5f, black);
        Line(buf, n, c - 22f, c - c * 0.36f, c + 22f, c - c * 0.36f, 1.2f, brass);
        Line(buf, n, c - 16f, c - c * 0.40f, c + 16f, c - c * 0.40f, 1.0f, brass);
        // hands: hour hand -> III (pointing +x), minute hand -> XII (pointing +y)
        Line(buf, n, c, c, c + c * 0.52f, c, 6f, black);
        Line(buf, n, c + c * 0.52f, c, c + c * 0.60f, c, 2.5f, black);
        Disc(buf, n, c + c * 0.38f, c, 6.5f, black);
        Line(buf, n, c, c, c, c + c * 0.78f, 4.2f, black);
        Line(buf, n, c, c, c, c - c * 0.14f, 3f, black);        // minute hand tail
        Line(buf, n, c, c, c + c * 0.2f, c, 3f, black);         // hour hand counterweight hidden under hub
        Disc(buf, n, c, c, 8f, black);
        Disc(buf, n, c, c, 3f, gold);
        return Make("clockface", n, (x, y) => buf[y * n + x], false);
    }

    /// <summary>Damask upholstery: diamond lattice with a floral medallion, woven thread noise. Seamless, 256 px.</summary>
    public static Texture2D Fabric(string key, Color a, Color b)
    {
        return Make("fabric_" + key, 256, (x, y) =>
        {
            int cell = 128;
            float dx = ((x % cell) - cell / 2f) / (cell / 2f), dy = ((y % cell) - cell / 2f) / (cell / 2f);
            float diamond = Mathf.Abs(dx) + Mathf.Abs(dy);
            float r = Mathf.Sqrt(dx * dx + dy * dy), ang = Mathf.Atan2(dy, dx);
            Color c = a;
            if (diamond > 0.86f && diamond < 0.96f) c = Mix(a, b, 0.75f);            // lattice line
            float petal = 0.28f + 0.14f * Mathf.Abs(Mathf.Cos(ang * 3f));
            if (r < petal) c = Mix(a, b, 0.85f);                                        // medallion
            if (r < petal && r > petal - 0.06f) c = Mix(c, Color.white, 0.08f);         // highlight edge
            if (r < 0.07f) c = Mix(c, Color.white, 0.12f);                              // centre dot
            // small secondary buds between medallions
            float ox = Mathf.Abs(dx) - 1f, oy = dy;
            float r2 = Mathf.Sqrt(ox * ox + oy * oy);
            if (r2 < 0.12f) c = Mix(a, b, 0.6f);
            // weave
            if (x % 4 == 0 || y % 4 == 0) c = Mul(c, 0.92f);
            c = Mul(c, 0.9f + 0.2f * Fbm(x, y, 256, 16, 16));
            return c;
        });
    }

    /// <summary>Worn leather: warm brown with crackle creases and pores.</summary>
    public static Texture2D Leather()
    {
        return Make("leather", 256, (x, y) =>
        {
            Color baseC = new Color(0.33f, 0.17f, 0.09f);
            float g = Fbm(x, y, 256, 24, 24);
            Color c = Mul(baseC, 0.7f + 0.6f * g);
            float crease = Fbm(x + 31, y + 17, 256, 6, 6);
            if (Mathf.Abs(crease - 0.5f) < 0.012f) c = Mul(c, 0.55f);        // creases follow noise contours
            if (Mathf.Abs(crease - 0.38f) < 0.008f) c = Mul(c, 0.7f);
            if (H(x, y, 256, 256) > 0.93f) c = Mul(c, 0.8f);                   // pores
            float wear = Fbm(x + 90, y + 50, 256, 2, 2);
            c = Mix(c, new Color(0.5f, 0.3f, 0.16f), Mathf.Clamp01((wear - 0.55f) * 2f) * 0.4f);  // worn patches
            return c;
        });
    }

    /// <summary>Vertical pleated curtain: soft shading per pleat, two alternating shades and a fine weave.</summary>
    public static Texture2D Curtain(string key, Color a, Color b)
    {
        return Make("curtain_" + key, 256, (x, y) =>
        {
            float ph = x / 256f * Mathf.PI * 2f * 8f;          // 8 pleats per tile
            float shade = 0.62f + 0.38f * (0.5f + 0.5f * Mathf.Sin(ph));
            int pleat = (x * 16 / 256) % 2;
            Color c = pleat == 0 ? a : b;
            c = Mul(c, shade);
            if (x % 3 == 0) c = Mul(c, 0.95f);                 // thread
            c = Mul(c, 0.94f + 0.12f * Fbm(x, y, 256, 4, 32));
            return c;
        });
    }

    /// <summary>White lace / doily: cream cloth with a regular pattern of small holes and a floral grid.</summary>
    public static Texture2D Lace()
    {
        return Make("lace", 256, (x, y) =>
        {
            Color cloth = new Color(0.94f, 0.92f, 0.86f), hole = new Color(0.45f, 0.4f, 0.32f);
            int dx = x % 16 - 8, dy = y % 16 - 8;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            Color c = cloth;
            if (r < 3.2f) c = hole;
            int ex = x % 64 - 32, ey = y % 64 - 32;
            float rr = Mathf.Sqrt(ex * ex + ey * ey), aa = Mathf.Atan2(ey, ex);
            float flower = 14f + 6f * Mathf.Abs(Mathf.Sin(aa * 4f));
            if (rr < flower && rr > flower - 2.5f) c = Mix(c, hole, 0.5f);
            if (rr < 4f) c = Mix(c, hole, 0.6f);
            return Mul(c, 0.93f + 0.14f * H(x, y, 256, 256));
        });
    }

    /// <summary>Patchwork quilt: 64 px squares in two colours with stitch lines and a diagonal motif.</summary>
    public static Texture2D Quilt(string key, Color a, Color b)
    {
        return Make("quilt_" + key, 256, (x, y) =>
        {
            int sx = x / 64, sy = y / 64, px = x % 64, py = y % 64;
            bool alt = (sx + sy) % 2 == 0;
            Color c = alt ? a : b;
            if (!alt && Mathf.Abs(px - py) < 3) c = Mix(c, a, 0.5f);                   // diagonal
            if (!alt && Mathf.Abs(px + py - 63) < 3) c = Mix(c, a, 0.5f);
            if (alt && ((px / 8 + py / 8) % 2 == 0)) c = Mul(c, 0.9f);                 // check
            if (px < 2 || py < 2) c = Mix(c, Color.white, 0.35f);                        // stitching
            else if (px == 3 || py == 3) c = Mul(c, 0.8f);
            return Mul(c, 0.92f + 0.16f * Fbm(x, y, 256, 16, 16));
        });
    }

    /// <summary>Brushed stainless steel: horizontal streaks.</summary>
    public static Texture2D Steel()
    {
        return Make("steel", 256, (x, y) =>
        {
            float streak = Fbm(x, y, 256, 2, 64);
            Color c = Mul(new Color(0.66f, 0.67f, 0.69f), 0.82f + 0.3f * streak);
            if (H(0, y, 256, 256) > 0.9f) c = Mul(c, 0.9f);
            return c;
        });
    }

    /// <summary>
    /// One row of book spines filling the texture vertically, with the row's bottom/top boundary on the v = 0.5
    /// line. Box UVs are centred on the face, so a face of height H with tile = H shows exactly one shelf of books
    /// (spines standing on the bottom edge, dark shadow above the short ones). Spines are 36-63 px wide, i.e.
    /// 4-7 cm on a 0.3 m tile.
    /// </summary>
    public static Texture2D BookRow(string key)
    {
        return Make("bookrow_" + key, 256, (x, y) =>
        {
            int v = (y + 128) % 256;                 // 0 = bottom of the row (seam), 255 = top
            int seed = 0;
            for (int i = 0; i < key.Length; i++) seed = (seed * 31 + key[i]) & 0xffff;
            int bx = 0, idx = 0, w = 40;
            while (bx + w <= x) { bx += w; idx++; w = 36 + (int)(H(idx, seed, 64, 64) * 27f); }
            float hgt = 120f + H(idx, seed + 3, 64, 64) * 95f;
            Color back = new Color(0.07f, 0.045f, 0.03f);
            if (v > hgt) return Mul(back, 0.7f + 0.3f * (v / 256f));
            Color[] pal = { new Color(0.52f, 0.1f, 0.1f), new Color(0.12f, 0.22f, 0.42f), new Color(0.14f, 0.34f, 0.2f), new Color(0.45f, 0.3f, 0.12f), new Color(0.28f, 0.1f, 0.3f), new Color(0.2f, 0.18f, 0.16f), new Color(0.6f, 0.52f, 0.36f) };
            Color c = pal[(int)(H(idx, seed + 9, 64, 64) * 6.99f)];
            int lx = x - bx;
            if (lx < 2 || lx > w - 3) c = Mul(c, 0.45f);                                     // spine edges
            else if (lx < 5 || lx > w - 6) c = Mul(c, 0.8f);                                 // rounded spine shading
            if (v < 6) c = Mul(c, 0.6f);
            if ((v > hgt - 18 && v < hgt - 14) || (v > 20 && v < 24)) c = Mix(c, new Color(0.85f, 0.7f, 0.3f), 0.7f);  // gilt bands
            if (v > 50 && v < hgt - 40 && Mathf.Abs(lx - w / 2) < 4 && (v % 14) < 7 && H(idx, seed + 5, 64, 64) > 0.4f) c = Mix(c, new Color(0.85f, 0.75f, 0.4f), 0.45f);  // title lettering
            return Mul(c, 0.9f + 0.2f * Fbm(x, y, 256, 32, 32));
        });
    }

    /// <summary>Globe map: ocean with noise continents, horizontally seamless for a sphere.</summary>
    public static Texture2D GlobeMap()
    {
        return Make("globe", 256, (x, y) =>
        {
            float land = Fbm(x, y, 256, 3, 3) + 0.35f * Fbm(x + 77, y + 33, 256, 12, 12) - 0.2f;
            float lat = Mathf.Abs(y / 255f - 0.5f) * 2f;
            Color ocean = new Color(0.55f, 0.6f, 0.52f), paper = new Color(0.78f, 0.7f, 0.5f), ink = new Color(0.3f, 0.22f, 0.14f);
            Color c = land > 0.52f ? paper : ocean;
            if (land > 0.52f && land < 0.55f) c = ink;                                     // coast line
            if (lat > 0.9f) c = Mix(c, new Color(0.9f, 0.9f, 0.88f), 0.6f);                // polar caps
            if (y % 32 == 0 || x % 32 == 0) c = Mix(c, ink, 0.3f);                          // graticule
            return Mul(c, 0.9f + 0.2f * Fbm(x, y, 256, 16, 16));
        });
    }
}
