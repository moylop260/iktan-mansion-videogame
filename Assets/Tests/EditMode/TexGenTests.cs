using NUnit.Framework;
using UnityEngine;

public class TexGenTests
{
    static float Range(Texture2D t)
    {
        var px = t.GetPixels();
        float lo = 1f, hi = 0f;
        foreach (var c in px) { float g = (c.r + c.g + c.b) / 3f; lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g); }
        return hi - lo;
    }

    static Texture2D[] Tiling()
    {
        return new[]
        {
            TexGen.Wood(), TexGen.DarkWood(), TexGen.Marble(), TexGen.Panel(), TexGen.Carpet(),
            TexGen.Plaster(), TexGen.Stone(), TexGen.Books(),
            TexGen.Wallpaper("t", Color.red, Color.blue), TexGen.Tile("t", Color.white, Color.gray)
        };
    }

    [Test]
    public void Tiling_textures_are_square_power_of_two_repeating_and_have_detail()
    {
        foreach (var t in Tiling())
        {
            Assert.IsNotNull(t);
            Assert.AreEqual(t.width, t.height);
            Assert.IsTrue(Mathf.IsPowerOfTwo(t.width), t.name);
            Assert.AreEqual(TextureWrapMode.Repeat, t.wrapMode);
            Assert.Greater(Range(t), 0.05f, "texture is flat");
        }
    }

    [Test]
    public void Paintings_are_clamped_and_distinct()
    {
        var a = TexGen.Painting(0); var b = TexGen.Painting(1);
        Assert.AreEqual(TextureWrapMode.Clamp, a.wrapMode);
        Assert.AreNotSame(a, b);
        Assert.AreNotEqual(a.GetPixel(5, 5), b.GetPixel(5, 5));   // fondo de cada cuadro
    }

    [Test]
    public void Textures_are_cached_by_key()
    {
        Assert.AreSame(TexGen.Wood(), TexGen.Wood());
        Assert.AreSame(TexGen.Wallpaper("same", Color.red, Color.blue), TexGen.Wallpaper("same", Color.green, Color.white));
        Assert.AreNotSame(TexGen.Wallpaper("one", Color.red, Color.blue), TexGen.Wallpaper("two", Color.red, Color.blue));
    }

    [TestCase("Linterna")]
    [TestCase("Pilas")]
    [TestCase("Llave verde")]
    [TestCase("Llave azul")]
    [TestCase("Llave amarilla")]
    [TestCase("Llave dorada")]
    [TestCase("Partituras")]
    [TestCase("algo que no existe")]
    public void Icons_are_64px_with_a_drawn_shape_on_a_transparent_background(string item)
    {
        var icon = TexGen.Icon(item);
        Assert.IsNotNull(icon);
        Assert.AreEqual(64, icon.width);
        Assert.AreEqual(64, icon.height);
        int opaque = 0, clear = 0;
        foreach (var c in icon.GetPixels()) { if (c.a > 0.5f) opaque++; else clear++; }
        Assert.Greater(opaque, 100, "nothing drawn");
        Assert.Greater(clear, 100, "no transparent background");
    }

    [Test]
    public void Key_icons_have_different_colors()
    {
        Color Dominant(string n) { foreach (var c in TexGen.Icon(n).GetPixels()) if (c.a > 0.5f) return c; return Color.clear; }
        Assert.AreNotEqual(Dominant("Llave verde"), Dominant("Llave azul"));
        Assert.AreNotEqual(Dominant("Llave amarilla"), Dominant("Llave dorada"));
    }
}

public class TexGenFurnitureTests
{
    static float Lum(Color c) { return (c.r + c.g + c.b) / 3f; }

    static int DarkCount(Texture2D t, int x0, int x1, int y0, int y1)
    {
        int n = 0;
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) if (Lum(t.GetPixel(x, y)) < 0.25f) n++;
        return n;
    }

    // ADR-0006: the grandfather clock is stopped at 3:00 (hour hand on the 3, minute hand on the 12).
    [Test]
    public void Clock_face_shows_three_oclock()
    {
        var t = TexGen.ClockFace();
        Assert.AreEqual(TextureWrapMode.Clamp, t.wrapMode);
        int c = t.width / 2;
        int right = DarkCount(t, c + 10, c + 70, c - 4, c + 4);   // aguja de las horas hacia las 3
        int left = DarkCount(t, c - 70, c - 10, c - 4, c + 4);
        int up = DarkCount(t, c - 4, c + 4, c + 10, c + 90);      // aguja de los minutos hacia las 12
        int down = DarkCount(t, c - 4, c + 4, c - 90, c - 10);
        Assert.Greater(right, left + 30, "the hour hand must point to the 3");
        Assert.Greater(up, down + 30, "the minute hand must point to the 12");
    }

    [Test]
    public void Clock_face_has_an_ivory_dial_and_dark_markings()
    {
        var t = TexGen.ClockFace();
        Assert.Greater(Lum(t.GetPixel(t.width / 2 + 40, t.height / 2 - 40)), 0.6f, "ivory dial");
        int dark = DarkCount(t, 0, t.width - 1, 0, t.height - 1);
        Assert.Greater(dark, 400, "numerals, ticks and hands");
    }

    [Test]
    public void Kitchen_textures_cobweb_has_alpha_and_cans_have_four_labels()
    {
        var web = TexGen.Cobweb();
        int opaque = 0, clear = 0;
        foreach (var c in web.GetPixels()) { if (c.a > 0.5f) opaque++; else clear++; }
        Assert.Greater(opaque, 800); Assert.Greater(clear, opaque * 5, "mostly transparent");
        var seen = new System.Collections.Generic.HashSet<Color>();
        for (int v = 0; v < 4; v++) seen.Add(TexGen.CanLabel(v).GetPixel(60, 64));
        Assert.AreEqual(4, seen.Count, "four different can labels");
        Assert.AreSame(TexGen.CanLabel(2), TexGen.CanLabel(2));
    }

    [Test]
    public void Diary_pages_and_sheet_music_are_drawn()
    {
        foreach (var t in new[] { TexGen.DiaryPage(true), TexGen.DiaryPage(false), TexGen.SheetMusic() })
        {
            Assert.AreEqual(TextureWrapMode.Clamp, t.wrapMode);
            Assert.Greater(DarkCount(t, 0, t.width - 1, 0, t.height - 1), 300, "ink on the page");
        }
        // the right page carries the code 1973 circled in red ink
        int red = 0;
        foreach (var c in TexGen.DiaryPage(false).GetPixels()) if (c.r > 0.5f && c.g < 0.25f && c.b < 0.25f) red++;
        Assert.Greater(red, 150, "red ink with the year");
    }

    [Test]
    public void Fabric_leather_and_curtain_textures_tile()
    {
        var a = TexGen.Fabric("t1", Color.red, Color.yellow);
        Assert.AreEqual(TextureWrapMode.Repeat, a.wrapMode);
        Assert.AreSame(a, TexGen.Fabric("t1", Color.red, Color.yellow));
        Assert.AreNotSame(a, TexGen.Fabric("t2", Color.red, Color.yellow));
        Assert.AreEqual(TextureWrapMode.Repeat, TexGen.Leather().wrapMode);
        Assert.AreEqual(TextureWrapMode.Repeat, TexGen.Curtain("c1", Color.white, Color.gray).wrapMode);
    }
}
