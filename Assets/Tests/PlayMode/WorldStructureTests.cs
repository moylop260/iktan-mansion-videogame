using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WorldStructureTests
{
    GameManager gm;

    [SetUp] public void SetUp() { gm = TestWorld.Boot(); }
    [TearDown] public void TearDown() { TestWorld.Cleanup(); }

    [UnityTest]
    public IEnumerator The_house_has_all_doors_checkpoints_and_rooms()
    {
        yield return null;
        foreach (var id in new[] { "principal", "reja", "biblioteca", "oficina" })
            Assert.IsTrue(gm.doorMap.ContainsKey(id), "missing door " + id);
        CollectionAssert.AreEquivalent(new[] { "Vestíbulo", "Biblioteca", "Segundo piso", "Oficina" },
                                       gm.checkpoints.Select(c => c.name).ToArray());
        foreach (var z in new[] { "Sala", "Cuarto 1", "Cocina", "Biblioteca", "Baño", "Vestíbulo", "Cuarto 3",
                                  "Salón del piano", "Cuarto 4", "Oficina", "Pasillo del segundo piso" })
            Assert.IsTrue(gm.zones.Any(x => x.name == z), "missing room " + z);
    }

    [UnityTest]
    public IEnumerator Every_interactable_can_be_aimed_at_and_has_options()
    {
        yield return null;
        var all = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        Assert.GreaterOrEqual(all.Length, 25, "the house should be full of things to interact with");
        foreach (var it in all)
        {
            Assert.IsFalse(string.IsNullOrEmpty(it.title), it.name);
            Assert.IsNotNull(it.options, it.name);
            Assert.Greater(it.options().Count, 0, it.title);
            Assert.IsNotNull(it.GetComponentInChildren<Collider>(), it.title + " has no collider to aim at");
        }
    }

    [UnityTest]
    public IEnumerator The_key_props_exist_by_name()
    {
        yield return null;
        foreach (var n in new[] { "Mostrador", "Mesita", "Lavabo", "LibroAzul", "LibroRojo", "Comoda", "Piano", "CajaFuerte",
                                  "Espejo", "Refri", "Cama1", "Closet", "Cortina", "Tele", "Muneca", "Armario" })
            Assert.IsNotNull(TestWorld.Prop(n), "missing prop " + n);
    }

    // ADR-0006: the colour named by the text must be the colour of the lock.
    static Color Emission(GameObject leaf)
    {
        Color best = Color.black;
        foreach (var r in leaf.GetComponentsInChildren<Renderer>(true))
        {
            var m = r.sharedMaterial;
            if (m == null || !m.HasProperty("_EmissionColor")) continue;
            var c = m.GetColor("_EmissionColor");
            if (c.maxColorComponent > best.maxColorComponent) best = c;
        }
        return best;
    }

    [UnityTest]
    public IEnumerator Locks_are_visibly_the_colour_of_their_key()
    {
        yield return null;
        var green = Emission(gm.doorMap["biblioteca"].leaf);
        Assert.IsTrue(green.g > green.r && green.g > green.b, "library lock must be green: " + green);
        var blue = Emission(gm.doorMap["reja"].leaf);
        Assert.IsTrue(blue.b > blue.r && blue.b > blue.g, "stair gate padlock must be blue: " + blue);
        var yellow = Emission(gm.doorMap["oficina"].leaf);
        Assert.IsTrue(yellow.r > yellow.b && yellow.g > yellow.b && yellow.g > 0.7f * yellow.r, "office lock must be yellow: " + yellow);
        var gold = Emission(gm.doorMap["principal"].leaf);
        Assert.IsTrue(gold.r > gold.g && gold.g > gold.b, "front door lock must be golden: " + gold);
    }

    [UnityTest]
    public IEnumerator Door_leaves_are_interactable_and_start_closed()
    {
        yield return null;
        foreach (var d in gm.doorMap.Values)
        {
            Assert.IsFalse(d.IsOpen, d.id);
            Assert.IsNotNull(d.leaf.GetComponent<Interactable>(), d.id);
        }
    }

    [UnityTest]
    public IEnumerator Opening_the_gate_hides_it_and_a_door_swings_and_both_are_remembered()
    {
        yield return null;
        var gate = gm.doorMap["reja"];
        gate.Open(true);
        Assert.IsTrue(gate.IsOpen);
        Assert.IsFalse(gate.leaf.activeSelf);
        Assert.IsTrue(gm.doors.Contains("reja"));
        gate.Open(true);   // idempotent
        Assert.AreEqual(1, gm.doors.Count(x => x == "reja"));
        var bib = gm.doorMap["biblioteca"];
        bib.Open();
        yield return new WaitForSeconds(0.9f);
        Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(0f, bib.transform.localEulerAngles.y)), 80f, "the door should have swung open");
        Assert.IsFalse(bib.leaf.GetComponent<Interactable>().enabled);
    }
}
