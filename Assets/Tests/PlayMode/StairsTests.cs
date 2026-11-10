using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Regresión: la escalera se veía techada desde abajo y se atravesaba al subir (el piso del pasillo cubría el hueco).</summary>
public class StairsTests
{
    GameManager gm;
    [SetUp] public void SetUp() { gm = TestWorld.BootAndStart(); }
    [TearDown] public void TearDown() { TestWorld.Cleanup(); }

    static readonly Rect Hole = new Rect(12.4f, 3.9f, 15.85f - 12.4f, 10.0f - 3.9f);   // x, z, ancho, largo

    [UnityTest]
    public IEnumerator No_floor_covering_sits_over_the_stairwell_opening()
    {
        yield return null;
        foreach (var r in TestWorld.World().GetComponentsInChildren<Renderer>())
        {
            if (r.name != "PisoCuarto") continue;
            var b = r.bounds;
            if (b.max.y < 3.2f || b.min.y > 3.5f) continue;   // solo los pisos del segundo piso
            var xz = new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
            Assert.IsFalse(xz.Overlaps(Hole), "a second-floor floor covering overlaps the stairwell: " + xz);
        }
    }

    [UnityTest]
    public IEnumerator The_player_can_walk_up_the_stairs_and_arrives_on_the_second_floor()
    {
        gm.doorMap["reja"].Open(true);
        var pc = TestWorld.Player();
        pc.Teleport(new Vector3(14.1f, 0.05f, 3.0f), 0f);              // frente a la escalera, mirando hacia +z
        var cc = pc.GetComponent<CharacterController>();
        yield return null;
        float maxY = 0f;
        for (int i = 0; i < 420 && pc.transform.position.z < 11.5f; i++)
        {
            cc.Move(new Vector3(0f, -0.15f, 0.05f));                     // avanzar y gravedad, como el controlador
            maxY = Mathf.Max(maxY, pc.transform.position.y);
            yield return null;
        }
        Assert.Greater(pc.transform.position.z, 10.3f, "stuck on the stairs");
        Assert.That(pc.transform.position.y, Is.EqualTo(3.3f).Within(0.15f), "did not arrive on the second floor");
        Assert.Greater(maxY, 3.0f);
    }

    [UnityTest]
    public IEnumerator Every_step_is_solid_and_visible_from_the_hall()
    {
        yield return null;
        int steps = 0;
        foreach (var r in TestWorld.World().GetComponentsInChildren<Renderer>())
        {
            if (r.name != "Escalon") continue;
            steps++;
            Assert.IsTrue(r.enabled);
            Assert.IsNotNull(r.GetComponent<Collider>(), "a step without collider lets you walk through it");
        }
        Assert.AreEqual(11, steps);
    }
}
