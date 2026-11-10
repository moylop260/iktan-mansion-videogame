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
