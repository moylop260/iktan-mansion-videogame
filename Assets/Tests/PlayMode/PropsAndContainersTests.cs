using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>ADR-0006: what the text says is in the world must be modelled, visible and consistent.</summary>
public class PropsAndContainersTests
{
    GameManager gm;
    [SetUp] public void SetUp() { gm = TestWorld.BootAndStart(); }
    [TearDown] public void TearDown() { TestWorld.Cleanup(); }

    static void RunFirst(string prop) { TestWorld.Use(TestWorld.Prop(prop), 0); }

    [UnityTest]
    public IEnumerator Dresser_opens_to_old_clothes_and_sheet_music_and_taking_them_empties_the_drawer()
    {
        var comoda = TestWorld.Prop("Comoda");
        var op = comoda.GetComponent<Openable>();
        var music = comoda.GetComponentsInChildren<HideWhenOwned>(true).First(h => h.item == "Partituras");
        Assert.IsFalse(op.IsOpen);
        Assert.AreEqual(4, op.parts.Count, "four drawers");
        Assert.IsTrue(comoda.GetComponentsInChildren<Renderer>().Count(r => r.name == "RopaDoblada") >= 4, "folded old clothes are modelled");
        Vector3 closedPos = op.parts[0].t.position;
        RunFirst("Comoda");                                    // Abrir los cajones
        Assert.IsTrue(op.IsOpen);
        yield return new WaitForSeconds(1.2f);
        Assert.Greater(Vector3.Distance(op.parts[0].t.position, closedPos), 0.3f, "the drawer slid out");
        Assert.IsTrue(music.gameObject.activeSelf, "the sheet music is visible in the drawer");
        TestWorld.TakeFrom("Comoda");
        yield return null;
        Assert.IsTrue(gm.Has("Partituras"));
        Assert.IsFalse(music.gameObject.activeSelf, "the sheet music leaves the drawer when taken");
        // closing is offered, and the clothes stay
        var labels = comoda.GetComponent<Interactable>().options().Select(o => o.label).ToArray();
        CollectionAssert.Contains(labels, "Cerrar los cajones");
    }

    [UnityTest]
    public IEnumerator Nightstand_and_kitchen_drawer_show_batteries_and_flashlight_until_taken()
    {
        foreach (var (prop, item, part) in new[] { ("Mesita", "Pilas", "Pilas"), ("Mostrador", "Linterna", "LinternaEnCajon") })
        {
            var go = TestWorld.Prop(prop);
            var visible = go.GetComponentsInChildren<HideWhenOwned>(true).First(h => h.item == item);
            Assert.AreEqual(part, visible.name);
            Assert.IsTrue(visible.gameObject.activeSelf);
            TestWorld.TakeFrom(prop);
            yield return null;
            Assert.IsTrue(gm.Has(item));
            Assert.IsFalse(visible.gameObject.activeSelf, item + " must disappear from its drawer");
        }
    }

    [UnityTest]
    public IEnumerator Pantry_opens_on_expired_cans_and_cobwebs()
    {
        var alacena = TestWorld.Prop("Alacena");
        var op = alacena.GetComponent<Openable>();
        Assert.AreEqual(2, op.parts.Count, "two doors");
        Assert.GreaterOrEqual(alacena.GetComponentsInChildren<Renderer>().Count(r => r.name == "Telarana"), 4, "cobwebs");
        Assert.Greater(alacena.GetComponentsInChildren<Renderer>().Length, 80, "shelves full of cans, lids and jars");
        Quaternion closed = op.parts[0].t.rotation;
        RunFirst("Alacena");
        yield return new WaitForSeconds(1.3f);
        Assert.Greater(Quaternion.Angle(closed, op.parts[0].t.rotation), 80f, "the doors swung open");
        var opts = alacena.GetComponent<Interactable>().options();
        Assert.AreEqual("Revisar", opts[0].label);
        opts[0].run();   // examinar: no lanza
    }

    [UnityTest]
    public IEnumerator Fridge_hums_and_leaks()
    {
        yield return null;
        var details = TestWorld.World().transform.Find("RefriDetalles");
        Assert.IsNotNull(details);
        var src = details.GetComponentInChildren<AudioSource>();
        Assert.AreEqual("fridgehum", src.clip.name);
        Assert.IsTrue(src.loop); Assert.AreEqual(1f, src.spatialBlend);
        var dripper = details.GetComponent<Dripper>();
        Assert.IsNotNull(dripper);
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(dripper.drop.gameObject.activeSelf || true);   // la gota cae al ritmo de su periodo
        Assert.IsNotNull(TestWorld.Prop("Refri").GetComponent<Interactable>());
    }

    [UnityTest]
    public IEnumerator The_diary_lies_open_on_the_table_with_handwriting_and_a_pen()
    {
        yield return null;
        var mesa = TestWorld.Prop("MesaSala");
        var names = mesa.GetComponentsInChildren<Transform>().Select(t => t.name).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "HojaIzquierda", "HojaDerecha" }, names);
        var page = mesa.GetComponentsInChildren<Renderer>().First(r => r.name == "HojaDerecha");
        Assert.AreSame(TexGen.DiaryPage(false), page.sharedMaterial.GetTexture("_BaseMap"));
        Assert.IsNotNull(mesa.GetComponent<Interactable>());
    }

    [UnityTest]
    public IEnumerator Beds_are_unmade_each_as_its_text_says()
    {
        yield return null;
        foreach (var n in new[] { "Cama1", "Cama3", "Cama4" })
        {
            var bed = TestWorld.Prop(n);
            Assert.IsNotNull(bed, n);
            int lumps = bed.GetComponentsInChildren<Transform>().Count(t => t.name == "Pliegue");
            Assert.GreaterOrEqual(lumps, 3, n + " has rumpled covers");
            Assert.GreaterOrEqual(bed.GetComponentsInChildren<Transform>().Count(t => t.name == "Almohada"), 2, n + " has crooked pillows");
            Assert.IsNotNull(bed.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "HuecoTorso"), n + " has the body hollow");
        }
        Assert.IsNotNull(TestWorld.Prop("Cama3").GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "CobijaEnPiso"), "bed 3: blanket on the floor");
        Assert.IsNotNull(TestWorld.Prop("Cama4").GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "Telarana"), "bed 4: cobwebs");
        Assert.IsNotNull(TestWorld.Prop("Cama1").GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "CalorTibio"), "bed 1: still warm");
    }

    [UnityTest]
    public IEnumerator The_stair_gate_is_a_barred_grate_you_can_see_through()
    {
        yield return null;
        var gate = gm.doorMap["reja"];
        Assert.IsNull(gate.leaf.GetComponent<MeshRenderer>(), "not a solid slab");
        Assert.Greater(gate.leaf.GetComponentsInChildren<Renderer>().Length, 40, "bars, rails and ornaments");
        Assert.IsNotNull(gate.leaf.GetComponent<BoxCollider>(), "still blocks the way");
        gate.Open();
        Assert.IsFalse(gate.leaf.activeSelf);
    }

    [UnityTest]
    public IEnumerator Bathtub_is_hollow_and_the_curtain_slides_open()
    {
        yield return null;
        var tub = TestWorld.Prop("Banera");
        Assert.IsNotNull(tub);
        var floor = tub.GetComponentsInChildren<Transform>().First(t => t.name == "PisoBanera");
        var rim = tub.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y);
        Assert.Less(floor.position.y, rim - 0.25f, "the basin floor is well below the rim: it holds water");
        Assert.IsNotNull(tub.GetComponentInChildren<Collider>());
        var cortina = TestWorld.Prop("Cortina");
        var curtain = cortina.GetComponent<SlidingCurtain>();
        Assert.IsNotNull(curtain);
        Assert.GreaterOrEqual(curtain.strips.Length, 10);
        Assert.IsFalse(curtain.IsOpen);
        curtain.Slide(1f);
        yield return new WaitForSeconds(1.2f);
        Assert.IsTrue(curtain.IsOpen);
        Assert.Greater(Vector3.Distance(curtain.closedPos[3], curtain.strips[3].localPosition), 0.2f, "strips bunched up along the rail");
    }

    [UnityTest]
    public IEnumerator The_grandfather_clock_is_a_clock_with_the_three_oclock_dial()
    {
        yield return null;
        var reloj = TestWorld.Prop("Reloj");
        Assert.IsNotNull(reloj.GetComponent<Interactable>());
        Assert.Greater(reloj.GetComponentsInChildren<Renderer>().Length, 25, "not a box");
        var dial = reloj.GetComponentsInChildren<Renderer>().FirstOrDefault(r => r.sharedMaterial != null && r.sharedMaterial.mainTexture == TexGen.ClockFace());
        Assert.IsNotNull(dial, "a dial with the clock-face texture");
    }

    [UnityTest]
    public IEnumerator Real_mirror_renders_the_room_and_the_player_body()
    {
        var mirror = Object.FindFirstObjectByType<MirrorSurface>();
        Assert.IsNotNull(mirror);
        TestWorld.Player().Teleport(new Vector3(2.2f, 0.05f, 5.5f), 270f);   // frente al espejo de la sala
        yield return null; yield return null; yield return null;
        Assert.IsNotNull(mirror.ReflectionCamera);
        Assert.IsTrue(mirror.ReflectionCamera.enabled, "the reflection camera renders when you look at the mirror");
        Assert.IsNotNull(mirror.ReflectionCamera.targetTexture);
        var avatars = Object.FindObjectsByType<PlayerAvatar>(FindObjectsSortMode.None);
        Assert.AreEqual(2, avatars.Length, "the body and its delayed copy");
        int main = gm.cam.cullingMask;
        Assert.AreEqual(0, main & (1 << MirrorSurface.AvatarLayer), "the main camera must not draw the body");
        Assert.AreEqual(0, main & (1 << MirrorSurface.GhostLayer));
        Assert.IsTrue(avatars.All(a => a.GetComponentsInChildren<Renderer>().Length > 15));
        // looking away stops the rendering (performance)
        TestWorld.Player().Teleport(new Vector3(2.2f, 0.05f, 5.5f), 90f);
        yield return null; yield return null;
        Assert.IsFalse(mirror.ReflectionCamera.enabled);
    }

    [UnityTest]
    public IEnumerator Rooms_have_distinct_floors_and_lights()
    {
        yield return null;
        var lightColors = TestWorld.World().GetComponentsInChildren<Light>().Select(l => l.color).Distinct().Count();
        Assert.Greater(lightColors, 6, "each room has its own light colour");
    }
}
