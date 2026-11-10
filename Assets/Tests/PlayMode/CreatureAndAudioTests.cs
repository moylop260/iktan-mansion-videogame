using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CreatureAndAudioTests
{
    [TearDown] public void TearDown() { TestWorld.Cleanup(); }

    [Test]
    public void Figure_has_glowing_eyes_a_jaw_and_arms_that_reach()
    {
        var f = Mon.Figure(Vector3.zero, Vector3.forward, Color.black, Color.red, 1f);
        Assert.AreEqual(2, f.eyes.Length);
        Assert.IsNotNull(f.jaw); Assert.IsNotNull(f.head); Assert.IsNotNull(f.armL); Assert.IsNotNull(f.armR);
        f.Reach(0f);
        float rest = f.armL.localScale.z;
        f.Reach(1f, 3f);
        Assert.Greater(f.armL.localScale.z, rest);
        float closed = f.jaw.localScale.y;
        f.Mouth(1f);
        Assert.Greater(f.jaw.localScale.y, closed);
        f.SetEyes(0f);
        Assert.IsFalse(f.eyes[0].enabled);
        f.SetEyes(1f);
        Assert.IsTrue(f.eyes[0].enabled);
        Object.Destroy(f.gameObject);
    }

    [Test]
    public void Figure_scale_and_facing_are_applied()
    {
        var f = Mon.Figure(new Vector3(3, 0, 4), Vector3.left, Color.black, Color.red, 1.5f);
        Assert.AreEqual(1.5f, f.transform.localScale.x, 0.001f);
        Assert.That(Vector3.Dot(f.transform.forward, Vector3.left), Is.GreaterThan(0.99f));
        Assert.AreEqual(new Vector3(3, 0, 4), f.transform.position);
        Object.Destroy(f.gameObject);
    }

    [Test]
    public void Spider_has_eight_legs_and_four_eyes()
    {
        var s = Mon.Spider(Vector3.zero, Vector3.forward, 1f);
        Assert.AreEqual(8, s.legs.Length);
        Assert.AreEqual(4, s.eyes.Length);
        s.Wiggle(1.2f);   // must not throw
        Object.Destroy(s.gameObject);
    }

    [Test]
    public void Doll_has_a_head_that_can_spin_and_eyes_that_light_up()
    {
        var d = Mon.Doll(Vector3.zero, Vector3.forward, 1f);
        Assert.IsNotNull(d.head);
        d.head.localRotation = Quaternion.Euler(0, 180, 0);
        Assert.AreEqual(2, d.eyes.Length);
        Object.Destroy(d.gameObject);
    }

    [Test]
    public void PaleArm_extends_to_the_requested_length()
    {
        var a = Mon.PaleArm(Vector3.zero, Vector3.forward);
        a.Extend(2f);
        Assert.AreEqual(2f, a.armL.localScale.z, 0.001f);
        Object.Destroy(a.gameObject);
    }

    [UnityTest]
    public IEnumerator AudioManager_plays_2D_sounds_and_places_3D_sounds_that_clean_up()
    {
        var gm = TestWorld.Boot();
        Assert.IsNotNull(gm.audio);
        var src = gm.audio.Sfx("roar", 0.5f, 1.2f);
        Assert.IsNotNull(src);
        Assert.AreEqual("roar", src.clip.name);
        Assert.AreEqual(1.2f, src.pitch, 0.001f);

        gm.audio.At("click", new Vector3(1, 1, 1), 1f);
        Assert.IsNotNull(GameObject.Find("Sonido_click"));
        yield return new WaitForSeconds(0.6f);
        Assert.IsNull(GameObject.Find("Sonido_click"), "temporary 3D sounds must be destroyed");
    }

    [UnityTest]
    public IEnumerator Picking_up_an_item_shows_its_card_and_the_game_keeps_running()
    {
        var gm = TestWorld.BootAndStart();
        gm.Give("Llave verde");
        yield return null;
        Assert.AreEqual(Mode.Playing, gm.mode);
        StringAssert.Contains("Llave verde", string.Join(",", gm.items));
    }
}
