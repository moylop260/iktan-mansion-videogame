using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contenedores que se abren de verdad y muestran lo que el texto dice que hay dentro (ADR-0006):
/// cómoda con ropa vieja y partituras, mesita con pilas, cajón de la cocina con la linterna,
/// alacena con latas vencidas y telarañas, y los detalles del refrigerador (zumba y gotea).
/// </summary>
public static partial class MansionBuilder
{
    static readonly Color Faded1 = new Color(0.52f, 0.47f, 0.4f), Faded2 = new Color(0.38f, 0.42f, 0.52f), Faded3 = new Color(0.5f, 0.32f, 0.32f), Faded4 = new Color(0.4f, 0.45f, 0.36f);

    // ------------------------------------------------------------------ cajones
    /// <summary>
    /// Cajón que se desliza: pivote en su posición cerrada, frente con perillas y charola abierta. Devuelve el pivote para
    /// colgarle el contenido. (xc, yc) = centro del frente en el marco; zf = z del frente; slide = cuánto sale al abrirse.
    /// </summary>
    static GameObject AddDrawer(GameObject g, Openable op, KFrame f, string name, float xc, float yc, float zf, float w, float hh, float depth, float slide, Surf wood)
    {
        var pv = new GameObject(name);
        pv.transform.SetParent(g.transform, false);
        pv.transform.position = f.P(xc, yc, zf);
        pv.transform.rotation = f.Rot;
        var brass = MatOf(C(Gold, 0.8f));
        // frente con moldura interior y dos perillas
        KBoxP(pv, f, "FrenteCajon", xc, yc, zf, w, hh, 0.025f, wood, true);
        KBoxP(pv, f, "MolduraCajon", xc, yc, zf + 0.014f, w - 0.1f, hh - 0.08f, 0.006f, sDark, false);
        foreach (int s in new[] { -1, 1 })
        {
            var knob = KBall(g, f, "Perilla", xc + s * w * 0.28f, yc, zf + 0.035f, 0.035f, 0.035f, 0.03f, brass);
            knob.transform.SetParent(pv.transform, true);
        }
        // charola: fondo, costados y trasera (sin collider; el contenido se ve desde arriba)
        float cz = zf - depth / 2f - 0.01f;
        var inner = C(new Color(0.3f, 0.2f, 0.12f), 0.1f);
        KBoxP(pv, f, "FondoCajon", xc, yc - hh / 2f + 0.015f, cz, w - 0.05f, 0.015f, depth, inner, false);
        KBoxP(pv, f, "CostadoCajon", xc - (w - 0.05f) / 2f, yc - 0.01f, cz, 0.015f, hh - 0.04f, depth, inner, false);
        KBoxP(pv, f, "CostadoCajon", xc + (w - 0.05f) / 2f, yc - 0.01f, cz, 0.015f, hh - 0.04f, depth, inner, false);
        KBoxP(pv, f, "TraseraCajon", xc, yc - 0.01f, cz - depth / 2f, w - 0.05f, hh - 0.04f, 0.015f, inner, false);
        op.Add(pv.transform, pv.transform.position + f.fwd * slide, pv.transform.rotation);
        return pv;
    }

    /// <summary>Caja hija de un pivote, en el marco del mueble (mismo criterio que KBox).</summary>
    static GameObject KBoxP(GameObject pv, KFrame f, string name, float x, float y, float z, float sx, float sy, float sz, Surf s, bool collide, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        var b = BoxIn(pv, name, f.P(x, y, z), new Vector3(sx, sy, sz), s, collide);
        b.transform.rotation = f.Rot * Quaternion.Euler(rx, ry, rz);
        return b;
    }

    static GameObject PartP(GameObject pv, KFrame f, PrimitiveType type, float x, float y, float z, float sx, float sy, float sz, Material m, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        var g = Mon.Part(pv.transform, type, Vector3.zero, new Vector3(sx, sy, sz), m);
        g.transform.position = f.P(x, y, z);
        g.transform.rotation = f.Rot * Quaternion.Euler(rx, ry, rz);
        return g;
    }

    // ------------------------------------------------------------------ contenido de los cajones
    /// <summary>Ropa vieja doblada y arrugada, de colores desteñidos, con una prenda colgando por el frente si spill.</summary>
    static void OldClothes(GameObject pv, KFrame f, float x, float y, float z, int seed, bool spill)
    {
        var rng = new System.Random(seed);
        System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);
        Color[] cols = { Faded1, Faded2, Faded3, Faded4 };
        for (int i = 0; i < 4; i++)
        {
            var col = cols[(i + seed) % 4];
            float zz = z + R(-0.05f, 0.05f);
            // camisa doblada: cuerpo, mangas dobladas, cuello
            KBoxP(pv, f, "RopaDoblada", x + R(-0.03f, 0.03f), y + i * 0.045f, zz, 0.34f, 0.04f, 0.26f, C(col, 0.05f), false, R(-3f, 3f), R(-9f, 9f), R(-2f, 2f));
            KBoxP(pv, f, "Manga", x - 0.14f, y + i * 0.045f + 0.025f, zz, 0.1f, 0.03f, 0.2f, C(Color.Lerp(col, Color.black, 0.12f), 0.05f), false, 0f, R(-10f, 10f), 0f);
            KBoxP(pv, f, "Cuello", x + 0.05f, y + i * 0.045f + 0.03f, zz - 0.1f, 0.1f, 0.015f, 0.05f, C(Color.Lerp(col, Color.white, 0.2f), 0.05f), false);
        }
        // calcetines hechos bola y un botón suelto
        PartP(pv, f, PrimitiveType.Sphere, x + 0.2f, y + 0.03f, z + 0.1f, 0.09f, 0.06f, 0.1f, Mon.Lit(Faded2, 0.05f));
        PartP(pv, f, PrimitiveType.Sphere, x + 0.26f, y + 0.03f, z - 0.02f, 0.08f, 0.055f, 0.09f, Mon.Lit(Faded3, 0.05f));
        PartP(pv, f, PrimitiveType.Cylinder, x - 0.22f, y + 0.012f, z + 0.12f, 0.02f, 0.003f, 0.02f, Mon.Lit(new Color(0.8f, 0.75f, 0.6f), 0.4f));
        // agujeros de polilla: manchitas oscuras sobre la prenda de arriba
        for (int k = 0; k < 4; k++)
            KBoxP(pv, f, "AgujeroPolilla", x + R(-0.12f, 0.12f), y + 3 * 0.045f + 0.022f, z + R(-0.08f, 0.08f), 0.02f, 0.002f, 0.02f, C(new Color(0.08f, 0.06f, 0.05f), 0f), false);
        if (spill)
            KBoxP(pv, f, "VestidoColgando", x + 0.02f, y - 0.1f, z + 0.17f, 0.4f, 0.28f, 0.015f, C(Faded3, 0.05f), false, 8f, 0f, 3f);
    }

    /// <summary>Pila de partituras (hojas con pentagramas) atada con un listón. Desaparece al tomarlas.</summary>
    static GameObject SheetMusicStack(GameObject pv, KFrame f, float x, float y, float z, float yaw)
    {
        var root = new GameObject("Partituras");
        root.transform.SetParent(pv.transform, false);
        root.AddComponent<HideWhenOwned>().item = "Partituras";
        var paper = Mon.Lit(new Color(0.93f, 0.88f, 0.75f), 0.1f);
        for (int i = 0; i < 6; i++)
        {
            var sh = PartP(pv, f, PrimitiveType.Cube, x + 0.004f * (i % 2), y + i * 0.004f, z, 0.21f, 0.004f, 0.297f, paper, 0f, yaw + (i - 3) * 2.2f, 0f);
            sh.transform.SetParent(root.transform, true);
        }
        // hoja de arriba con la música escrita (quad con textura)
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.Destroy(q.GetComponent<Collider>());
        q.name = "HojaConMusica";
        q.transform.SetParent(root.transform, false);
        q.transform.position = f.P(x, y + 6 * 0.004f + 0.002f, z);
        q.transform.rotation = f.Rot * Quaternion.Euler(90f, yaw + 4f, 0f);
        q.transform.localScale = new Vector3(0.21f, 0.297f, 1f);
        q.GetComponent<Renderer>().sharedMaterial = MatOf(S(TexGen.SheetMusic(), 1f, 0.1f));
        // listón rojo que las ata
        var ribbon = PartP(pv, f, PrimitiveType.Cube, x, y + 6 * 0.004f + 0.003f, z + 0.03f, 0.22f, 0.003f, 0.012f, Mon.Lit(new Color(0.6f, 0.05f, 0.07f), 0.4f), 0f, yaw, 0f);
        ribbon.transform.SetParent(root.transform, true);
        return root;
    }

    /// <summary>Dos pilas cilíndricas de cobre con la tapa negra y un envoltorio arrugado. Desaparecen al tomarlas.</summary>
    static GameObject BatteriesProp(GameObject pv, KFrame f, float x, float y, float z)
    {
        var root = new GameObject("Pilas");
        root.transform.SetParent(pv.transform, false);
        root.AddComponent<HideWhenOwned>().item = "Pilas";
        var copper = Mon.Lit(new Color(0.78f, 0.45f, 0.2f), 0.6f);
        var cap = Mon.Lit(new Color(0.12f, 0.12f, 0.13f), 0.5f);
        var tip = Mon.Lit(new Color(0.8f, 0.8f, 0.82f), 0.8f);
        for (int i = 0; i < 2; i++)
        {
            float zz = z + (i - 0.5f) * 0.075f;
            var body = PartP(pv, f, PrimitiveType.Cylinder, x, y + 0.025f, zz, 0.045f, 0.06f, 0.045f, copper, 0f, 0f, 90f);
            var band = PartP(pv, f, PrimitiveType.Cylinder, x + 0.045f, y + 0.025f, zz, 0.046f, 0.012f, 0.046f, cap, 0f, 0f, 90f);
            var nub = PartP(pv, f, PrimitiveType.Cylinder, x + 0.112f, y + 0.025f, zz, 0.016f, 0.008f, 0.016f, tip, 0f, 0f, 90f);
            foreach (var p in new[] { body, band, nub }) p.transform.SetParent(root.transform, true);
        }
        var wrap = PartP(pv, f, PrimitiveType.Cube, x - 0.18f, y + 0.008f, z, 0.1f, 0.012f, 0.14f, Mon.Lit(new Color(0.75f, 0.7f, 0.2f), 0.2f), 0f, 20f, 0f);
        wrap.transform.SetParent(root.transform, true);
        return root;
    }

    /// <summary>Linterna vieja tirada en el cajón. Desaparece al tomarla.</summary>
    static GameObject FlashlightProp(GameObject pv, KFrame f, float x, float y, float z)
    {
        var root = new GameObject("LinternaEnCajon");
        root.transform.SetParent(pv.transform, false);
        root.AddComponent<HideWhenOwned>().item = "Linterna";
        var body = Mon.Lit(new Color(0.55f, 0.1f, 0.1f), 0.5f);
        var metal = Mon.Lit(new Color(0.6f, 0.6f, 0.63f), 0.85f);
        var parts = new List<GameObject>
        {
            PartP(pv, f, PrimitiveType.Cylinder, x, y + 0.03f, z, 0.05f, 0.1f, 0.05f, body, 0f, 10f, 90f),                          // mango
            PartP(pv, f, PrimitiveType.Cylinder, x + 0.125f, y + 0.035f, z - 0.018f, 0.075f, 0.03f, 0.075f, metal, 0f, 10f, 90f),     // cabeza
            PartP(pv, f, PrimitiveType.Cylinder, x + 0.157f, y + 0.035f, z - 0.024f, 0.068f, 0.004f, 0.068f, Mon.Glow(new Color(1f, 0.95f, 0.7f), 0.5f), 0f, 10f, 90f), // lente
            PartP(pv, f, PrimitiveType.Cube, x + 0.02f, y + 0.062f, z + 0.005f, 0.04f, 0.012f, 0.02f, Mon.Lit(new Color(0.1f, 0.1f, 0.1f), 0.3f), 0f, 10f, 0f),         // interruptor
            PartP(pv, f, PrimitiveType.Cylinder, x - 0.105f, y + 0.03f, z + 0.018f, 0.052f, 0.006f, 0.052f, metal, 0f, 10f, 90f),     // tapa trasera
        };
        foreach (var p in parts) p.transform.SetParent(root.transform, true);
        return root;
    }

    // ------------------------------------------------------------------ interacción común
    /// <summary>
    /// Opciones de un contenedor: cerrado = "Abrir"; abierto = tomar el objeto (si falta) o revisar, y cerrar.
    /// Abrir muestra el contenido; tomarlo lo hace desaparecer del cajón (HideWhenOwned).
    /// </summary>
    static void ContainerInteract(GameObject g, string title, Openable op, string item, string openLabel, string openSay,
                                  string takeLabel, string takeSay, string emptySay, string closeLabel)
    {
        Interactable.Add(g, title, () =>
        {
            var l = new List<Opt>();
            if (!op.IsOpen)
            {
                l.Add(O(openLabel, () => { op.SetOpen(true); gm.audio.At("creak", g.transform.position + Vector3.up, 0.5f, 1.5f); gm.Say(openSay); }));
                return l;
            }
            if (item != null && !gm.Has(item)) l.Add(O(takeLabel, () => Take(item, takeSay, emptySay)));
            else l.Add(O("Revisar", () => gm.Say(emptySay)));
            l.Add(O(closeLabel, () => { op.SetOpen(false); gm.audio.At("creak", g.transform.position + Vector3.up, 0.4f, 1.7f); }));
            return l;
        });
    }

    // ------------------------------------------------------------------ cómoda
    /// <summary>Cómoda de 4 cajones con perillas, tablero con moldura y espejo opcional; sus cajones muestran ropa vieja y las partituras.</summary>
    static GameObject DresserWithContents(string name, float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        var g = Group(name);
        var op = g.AddComponent<Openable>();
        var f = new KFrame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        // armazón: costados, trasera, base con patas, tablero y moldura
        KBox(g, f, "Costado", -hw + 0.02f, h / 2f, 0f, 0.04f, h - 0.1f, d, sDark);
        KBox(g, f, "Costado", hw - 0.02f, h / 2f, 0f, 0.04f, h - 0.1f, d, sDark);
        KBox(g, f, "Trasera", 0f, h / 2f, -hd + 0.01f, w - 0.08f, h - 0.12f, 0.02f, sDark);
        KBox(g, f, "Fondo", 0f, 0.1f, 0f, w - 0.08f, 0.03f, d - 0.04f, sDark);
        foreach (int sx in new[] { -1, 1 })
            foreach (int sz in new[] { -1, 1 })
                KBox(g, f, "Pata", sx * (hw - 0.05f), 0.05f, sz * (hd - 0.06f), 0.07f, 0.1f, 0.07f, sDark);
        KBox(g, f, "Tablero", 0f, h - 0.015f, 0.01f, w + 0.04f, 0.04f, d + 0.04f, sWood);
        KBox(g, f, "MolduraTablero", 0f, h - 0.05f, hd + 0.02f, w, 0.03f, 0.02f, sDark, false);
        // 4 cajones: los de arriba más bajos
        float[] hs = { 0.2f, 0.2f, 0.22f, 0.26f };
        float y = h - 0.06f;
        for (int i = 0; i < 4; i++)
        {
            float hh = hs[i] - 0.012f;
            float yc = y - hs[i] / 2f;
            var pv = AddDrawer(g, op, f, "Cajon" + i, 0f, yc, hd - 0.012f, w - 0.1f, hh, d - 0.12f, 0.46f, sWood);
            float cy = yc - hh / 2f + 0.03f;
            if (i == 0) OldClothes(pv, f, -0.35f, cy, hd - 0.3f, 3, true);
            if (i == 0) OldClothes(pv, f, 0.3f, cy, hd - 0.3f, 7, false);
            if (i == 1) { OldClothes(pv, f, -0.45f, cy, hd - 0.3f, 11, false); SheetMusicStack(pv, f, 0.25f, cy, hd - 0.3f, 12f); }
            if (i == 2) OldClothes(pv, f, 0f, cy, hd - 0.3f, 21, false);
            if (i == 3) KBoxP(pv, f, "PapelViejo", 0.1f, cy, hd - 0.3f, 0.3f, 0.003f, 0.2f, C(new Color(0.7f, 0.66f, 0.55f), 0f), false, 0f, 25f, 0f);
            y -= hs[i];
        }
        // encima: tapete de encaje, un frasco y una foto enmarcada
        KBox(g, f, "Tapete", -0.45f, h + 0.008f, 0f, 0.5f, 0.004f, 0.3f, C(new Color(0.9f, 0.88f, 0.8f), 0.05f), false);
        KBall(g, f, "FrascoPolvo", -0.45f, h + 0.07f, 0f, 0.09f, 0.12f, 0.09f, Mon.Lit(new Color(0.3f, 0.5f, 0.55f), 0.8f));
        KBox(g, f, "FotoMarco", 0.5f, h + 0.1f, -0.15f, 0.2f, 0.2f, 0.02f, C(Gold, 0.6f), false, -8f, 0f, 0f);
        KBox(g, f, "Foto", 0.5f, h + 0.1f, -0.138f, 0.16f, 0.16f, 0.004f, C(new Color(0.35f, 0.3f, 0.28f), 0.1f), false, -8f, 0f, 0f);
        return g;
    }

    /// <summary>Mesita de noche con un cajón (con las pilas, un rosario y papeles) y un estante abajo.</summary>
    static GameObject NightstandWithBatteries(string name, float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        var g = Group(name);
        var op = g.AddComponent<Openable>();
        var f = new KFrame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        KBox(g, f, "Costado", -hw + 0.015f, h / 2f, 0f, 0.03f, h, d, sWood);
        KBox(g, f, "Costado", hw - 0.015f, h / 2f, 0f, 0.03f, h, d, sWood);
        KBox(g, f, "Trasera", 0f, h / 2f, -hd + 0.01f, w - 0.06f, h - 0.1f, 0.02f, sDark);
        KBox(g, f, "Tablero", 0f, h - 0.015f, 0.005f, w + 0.03f, 0.03f, d + 0.03f, sWood);
        KBox(g, f, "Estante", 0f, 0.18f, 0f, w - 0.06f, 0.02f, d - 0.04f, sWood);
        KBox(g, f, "Base", 0f, 0.04f, 0f, w - 0.04f, 0.08f, d - 0.04f, sDark);
        // un libro y un frasco en el estante
        KBox(g, f, "LibroEstante", 0f, 0.23f, 0f, 0.18f, 0.05f, 0.24f, C(new Color(0.2f, 0.25f, 0.35f), 0.2f), false, 0f, 12f, 0f);
        var pv = AddDrawer(g, op, f, "Cajon", 0f, h - 0.17f, hd - 0.012f, w - 0.07f, 0.2f, d - 0.1f, 0.3f, sWood);
        float cy = h - 0.17f - 0.1f + 0.03f;
        BatteriesProp(pv, f, 0.04f, cy, hd - 0.25f);
        // rosario y papeles viejos
        for (int i = 0; i < 12; i++)
        {
            float a = i * 0.52f;
            PartP(pv, f, PrimitiveType.Sphere, -0.12f + Mathf.Cos(a) * 0.04f, cy + 0.008f, hd - 0.2f + Mathf.Sin(a) * 0.04f, 0.012f, 0.012f, 0.012f, Mon.Lit(new Color(0.35f, 0.15f, 0.1f), 0.5f));
        }
        KBoxP(pv, f, "Recibos", -0.1f, cy + 0.003f, hd - 0.3f, 0.12f, 0.003f, 0.09f, C(new Color(0.85f, 0.82f, 0.7f), 0f), false, 0f, 20f, 0f);
        // lámpara de buró encima
        KBall(g, f, "BaseLampara", 0.05f, h + 0.06f, 0f, 0.13f, 0.1f, 0.13f, MatOf(C(Gold, 0.8f)));
        KBox(g, f, "TalloLampara", 0.05f, h + 0.17f, 0f, 0.025f, 0.15f, 0.025f, C(Gold, 0.8f), false);
        var shade = KBox(g, f, "PantallaLampara", 0.05f, h + 0.3f, 0f, 0.24f, 0.16f, 0.24f, C(Color.white, 0.1f), false);
        shade.GetComponent<Renderer>().sharedMaterial = EmitMat(new Color(1f, 0.85f, 0.6f), 0.5f);
        return g;
    }

    /// <summary>Cajón del mostrador de la cocina que se desliza y deja ver la linterna, cubiertos, fósforos y cinta.</summary>
    static GameObject KitchenDrawerUnit(string name, float cx, float y0, float cz, Vector3 facing, float w, float h)
    {
        var g = Group(name);
        var op = g.AddComponent<Openable>();
        var f = new KFrame(cx, y0, cz, facing);
        var pv = AddDrawer(g, op, f, "Cajon", 0f, 0.74f, 0.016f, w, h, 0.5f, 0.42f, sWood);
        float cy = 0.74f - h / 2f + 0.03f;
        // charola de cubiertos con divisiones
        KBoxP(pv, f, "DivisionCubiertos", -0.1f, cy + 0.015f, -0.25f, 0.01f, 0.03f, 0.4f, sDark, false);
        for (int i = 0; i < 3; i++)
            KBoxP(pv, f, "Cuchara", -0.3f + i * 0.05f, cy + 0.012f, -0.15f, 0.025f, 0.01f, 0.18f, C(new Color(0.7f, 0.7f, 0.72f), 0.85f), false, 0f, i * 6f - 6f, 0f);
        KBoxP(pv, f, "Fosforos", 0.2f, cy + 0.015f, -0.35f, 0.07f, 0.03f, 0.04f, C(new Color(0.8f, 0.3f, 0.1f), 0.2f), false, 0f, 15f, 0f);
        KBoxP(pv, f, "Cinta", 0.3f, cy + 0.015f, -0.2f, 0.06f, 0.03f, 0.06f, C(new Color(0.75f, 0.7f, 0.45f), 0.3f), false);
        FlashlightProp(pv, f, 0.0f, cy, -0.32f);
        return g;
    }

    // ------------------------------------------------------------------ alacena
    /// <summary>
    /// Alacena de cocina: armazón, 4 entrepaños, dos puertas con tableros que SE ABREN y por dentro latas vencidas
    /// (etiqueta desteñida, óxido, fecha de caducidad estampada, tapas hinchadas, abolladas y caídas), frascos oscuros,
    /// polvo y telarañas en las esquinas.
    /// </summary>
    static GameObject OpenablePantry(string name, float cx, float y0, float cz, Vector3 facing, float w, float d, float h)
    {
        var g = Group(name);
        var op = g.AddComponent<Openable>();
        var f = new KFrame(cx, y0, cz, facing);
        float hw = w / 2f, hd = d / 2f;
        var wood = sWood;
        // armazón
        KBox(g, f, "Costado", -hw + 0.015f, h / 2f, 0f, 0.03f, h, d, wood);
        KBox(g, f, "Costado", hw - 0.015f, h / 2f, 0f, 0.03f, h, d, wood);
        KBox(g, f, "Trasera", 0f, h / 2f, -hd + 0.01f, w - 0.06f, h - 0.14f, 0.02f, sDark);
        KBox(g, f, "Techo", 0f, h - 0.02f, 0f, w + 0.02f, 0.04f, d + 0.02f, wood);
        KBox(g, f, "Cornisa", 0f, h + 0.02f, hd + 0.01f, w + 0.06f, 0.05f, 0.05f, sDark, false);
        KBox(g, f, "Zoclo", 0f, 0.06f, hd - 0.04f, w, 0.12f, 0.04f, sDark);
        KBox(g, f, "Base", 0f, 0.1f, 0f, w - 0.06f, 0.03f, d - 0.04f, wood);
        // entrepaños (con polvo gris encima)
        float[] shelves = { 0.46f, 0.84f, 1.22f, 1.6f };
        var dust = C(new Color(0.55f, 0.54f, 0.5f), 0f);
        foreach (float sy in shelves)
        {
            KBox(g, f, "Entrepano", 0f, sy, 0f, w - 0.06f, 0.025f, d - 0.05f, wood);
            KBox(g, f, "PolvoEntrepano", 0f, sy + 0.014f, 0f, w - 0.08f, 0.002f, d - 0.08f, dust, false);
        }
        float[] tops = { 0.1f + 0.015f, 0.46f + 0.0125f, 0.84f + 0.0125f, 1.22f + 0.0125f, 1.6f + 0.0125f };
        // contenido: latas en tres entrepaños, frascos oscuros arriba
        var rng = new System.Random(89);
        System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);
        var steel = Mon.Lit(new Color(0.62f, 0.63f, 0.66f), 0.7f);
        var rust = Mon.Lit(new Color(0.4f, 0.2f, 0.1f), 0.2f);
        for (int level = 0; level < 4; level++)
        {
            float top = tops[level];
            if (level == 3)
            {
                // frascos de vidrio oscuro con tapa
                for (int i = 0; i < 5; i++)
                {
                    float x = -hw + 0.15f + i * 0.22f;
                    KBall(g, f, "Frasco", x, top + 0.08f, R(-0.1f, 0.1f), 0.11f, 0.16f, 0.11f, Mon.Lit(new Color(0.18f, 0.1f, 0.04f), 0.85f));
                    PartG(g, f, PrimitiveType.Cylinder, x, top + 0.175f, 0f, 0.075f, 0.012f, 0.075f, Mon.Lit(new Color(0.5f, 0.45f, 0.2f), 0.6f));
                }
                continue;
            }
            for (int row = 0; row < 2; row++)
            {
                float z = row == 0 ? -0.18f : 0.05f;
                for (int i = 0; i < 6; i++)
                {
                    if (rng.NextDouble() < 0.12) continue;   // hueco
                    float x = -hw + 0.12f + i * 0.095f + (float)rng.NextDouble() * 0.01f;
                    int variant = rng.Next(0, 4);
                    var label = MatOf(S(TexGen.CanLabel(variant), 1f, 0.35f));
                    bool fallen = rng.NextDouble() < 0.09;
                    bool dented = rng.NextDouble() < 0.28;
                    bool swollen = rng.NextDouble() < 0.28;
                    float hCan = dented ? 0.1f : 0.11f;
                    if (fallen)
                    {
                        PartG(g, f, PrimitiveType.Cylinder, x, top + 0.04f, z, 0.075f, hCan / 2f, 0.075f, label, 0f, R(0f, 90f), 90f);
                        continue;
                    }
                    PartG(g, f, PrimitiveType.Cylinder, x, top + hCan / 2f, z, 0.075f, hCan / 2f, 0.075f, label, R(-4f, 4f), 0f, R(-4f, 4f));
                    PartG(g, f, PrimitiveType.Cylinder, x, top + hCan + 0.003f, z, 0.078f, 0.004f, 0.078f, swollen ? rust : steel);
                    if (swollen) PartG(g, f, PrimitiveType.Sphere, x, top + hCan + 0.008f, z, 0.066f, 0.03f, 0.066f, steel);   // tapa hinchada
                }
            }
        }
        // telarañas: esquinas de la trasera, entre costados y entrepaños, y colgando de la cornisa
        for (int i = 0; i < shelves.Length; i++)
        {
            float topY = (i + 1 < shelves.Length ? shelves[i + 1] : h - 0.04f) - 0.02f;
            float sz = 0.28f + 0.07f * (i % 2);
            Cobweb(g, f.P(-hw + 0.03f + sz / 2f, topY - sz / 2f, -hd + 0.025f), f.fwd, sz, false, true);
            if (i % 2 == 0) Cobweb(g, f.P(hw - 0.03f - sz / 2f, topY - sz / 2f, -hd + 0.025f), f.fwd, sz * 0.9f, true, true);
        }
        // las dos puertas con tableros en relieve y manijas; cada una gira sobre su bisagra
        float doorW = w / 2f - 0.01f;
        for (int s = -1; s <= 1; s += 2)
        {
            var piv = new GameObject(s < 0 ? "PuertaIzq" : "PuertaDer");
            piv.transform.SetParent(g.transform, false);
            piv.transform.position = f.P(s * (hw - 0.005f), 0f, hd + 0.01f);
            piv.transform.rotation = f.Rot;
            float cxl = -s * doorW / 2f;          // la hoja crece hacia el centro
            KBoxP(piv, f, "HojaPuerta", s * (hw - 0.005f) + cxl, h / 2f - 0.02f, hd + 0.01f, doorW, h - 0.1f, 0.03f, wood, true);
            KBoxP(piv, f, "TableroSup", s * (hw - 0.005f) + cxl, h * 0.72f, hd + 0.027f, doorW - 0.12f, h * 0.38f, 0.012f, sDark, false);
            KBoxP(piv, f, "TableroInf", s * (hw - 0.005f) + cxl, h * 0.28f, hd + 0.027f, doorW - 0.12f, h * 0.38f, 0.012f, sDark, false);
            var knob = PartP(piv, f, PrimitiveType.Sphere, s * (hw - 0.005f) + cxl * 1.8f, 1.0f, hd + 0.06f, 0.045f, 0.045f, 0.045f, MatOf(C(Gold, 0.85f)));
            // al abrirse giran hacia afuera, alrededor de su bisagra
            op.Add(piv.transform, piv.transform.position, f.Rot * Quaternion.Euler(0f, s * 105f, 0f));
        }
        return g;
    }

    /// <summary>Pieza decorativa hija del grupo en el marco del mueble (sin collider).</summary>
    static GameObject PartG(GameObject g, KFrame f, PrimitiveType type, float x, float y, float z, float sx, float sy, float sz, Material m, float rx = 0f, float ry = 0f, float rz = 0f)
    {
        var p = Mon.Part(g.transform, type, Vector3.zero, new Vector3(sx, sy, sz), m);
        p.transform.position = f.P(x, y, z);
        p.transform.rotation = f.Rot * Quaternion.Euler(rx, ry, rz);
        return p;
    }

    // ------------------------------------------------------------------ refrigerador
    /// <summary>Detalles del refrigerador: zumba fuerte (audio 3D en bucle) y gotea un líquido oscuro sobre un charco.</summary>
    static void FridgeDetails(float cx, float y0, float cz, Vector3 facing, float w, float d)
    {
        var g = Group("RefriDetalles");
        var f = new KFrame(cx, y0, cz, facing);
        // zumbido eléctrico
        var hum = new GameObject("Zumbido");
        hum.transform.SetParent(g.transform, false);
        hum.transform.position = f.P(0f, 1.0f, 0f);
        var src = hum.AddComponent<AudioSource>();
        src.clip = AudioGen.Get("fridgehum"); src.loop = true; src.spatialBlend = 1f; src.volume = 0.55f;
        src.rolloffMode = AudioRolloffMode.Linear; src.minDistance = 1.2f; src.maxDistance = 9f;
        src.Play();
        // charco oscuro y brillante frente a la puerta
        var puddle = Mon.Lit(new Color(0.04f, 0.01f, 0.02f), 0.95f);
        PartG(g, f, PrimitiveType.Cylinder, 0.1f, 0.012f, d / 2f + 0.3f, 0.5f, 0.004f, 0.36f, puddle);
        PartG(g, f, PrimitiveType.Cylinder, -0.12f, 0.012f, d / 2f + 0.12f, 0.22f, 0.004f, 0.16f, puddle);
        // gota que cae de la puerta al charco
        var drop = Mon.Part(g.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.025f, 0.04f, 0.025f), puddle);
        var dr = g.AddComponent<Dripper>();
        dr.drop = drop.transform;
        dr.from = f.P(0.1f, 0.6f, d / 2f + 0.04f);
        dr.to = f.P(0.1f, 0.02f, d / 2f + 0.3f);
        dr.period = 2.3f;
        // mancha oscura que escurre por la puerta
        PartG(g, f, PrimitiveType.Cube, 0.1f, 0.35f, d / 2f + 0.01f, 0.05f, 0.5f, 0.004f, puddle);
    }
}
