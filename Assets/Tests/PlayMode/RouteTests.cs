using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Recorre la ruta ganadora completa usando solo los objetos del mundo, como lo haría el jugador.</summary>
public class RouteTests
{
    GameManager gm;
    [SetUp] public void SetUp() { gm = TestWorld.BootAndStart(); }
    [TearDown] public void TearDown() { TestWorld.Cleanup(); }

    [UnityTest]
    public IEnumerator New_game_asks_to_buy_the_mansion_and_buying_starts_playing()
    {
        TestWorld.Cleanup();
        gm = TestWorld.Boot();
        gm.NewGame();
        Assert.AreEqual(Mode.Choice, gm.mode);
        Assert.AreEqual(2, gm.ChoiceCount);
        gm.PickOption(0);
        Assert.AreEqual(Mode.Playing, gm.mode);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Refusing_to_buy_gives_the_first_ending()
    {
        TestWorld.Cleanup();
        gm = TestWorld.Boot();
        gm.NewGame();
        gm.PickOption(1);
        Assert.AreEqual(Mode.Ended, gm.mode);
        yield return null;
    }

    [UnityTest]
    public IEnumerator The_whole_winning_route_works_and_each_step_is_gated()
    {
        yield return null;
        Assert.AreEqual(Mode.Playing, gm.mode);
        Assert.IsFalse(gm.FlashReady);

        TestWorld.TakeFrom("Mostrador");
        Assert.IsTrue(gm.Has("Linterna")); Assert.IsFalse(gm.FlashReady);
        TestWorld.TakeFrom("Mesita");
        Assert.IsTrue(gm.Has("Pilas")); Assert.IsTrue(gm.FlashReady);

        // library door needs the green key
        var bib = gm.doorMap["biblioteca"];
        TestWorld.Use(bib.leaf);
        Assert.IsFalse(bib.IsOpen, "the library must stay locked without the green key");
        TestWorld.Use(TestWorld.Prop("Lavabo"));
        Assert.IsTrue(gm.Has("Llave verde"));
        TestWorld.Use(bib.leaf);
        Assert.IsTrue(bib.IsOpen);

        // blue key opens the stair gate
        var gate = gm.doorMap["reja"];
        TestWorld.Use(gate.leaf);
        Assert.IsFalse(gate.IsOpen);
        TestWorld.Use(TestWorld.Prop("LibroAzul"));
        Assert.IsTrue(gm.Has("Llave azul"));
        TestWorld.Use(gate.leaf);
        Assert.IsTrue(gate.IsOpen);

        // sheet music, then the piano (dark room: flashlight must be on)
        TestWorld.TakeFrom("Comoda");
        Assert.IsTrue(gm.Has("Partituras"));
        gm.FlashOn = false;
        TestWorld.Use(TestWorld.Prop("Piano"));
        Assert.IsFalse(gm.Has("Llave amarilla"), "the piano must need the flashlight");
        gm.FlashOn = true;
        TestWorld.Use(TestWorld.Prop("Piano"));
        Assert.IsTrue(gm.Has("Llave amarilla"));

        // office and safe (option 1 is the correct code 1973)
        var ofi = gm.doorMap["oficina"];
        TestWorld.Use(ofi.leaf);
        Assert.IsTrue(ofi.IsOpen);
        var safe = TestWorld.Prop("CajaFuerte").GetComponent<Interactable>();
        Assert.AreEqual(3, safe.options().Count);
        StringAssert.Contains("1973", safe.options()[1].label);
        safe.options()[1].run();
        Assert.IsTrue(gm.Has("Llave dorada"));
        Assert.AreEqual("Mirar", safe.options()[0].label, "the emptied safe only offers to look");

        // front door wins
        TestWorld.Use(gm.doorMap["principal"].leaf);
        Assert.AreEqual(Mode.Win, gm.mode);
    }

    [UnityTest]
    public IEnumerator The_front_door_stays_locked_without_the_golden_key()
    {
        yield return null;
        TestWorld.Use(gm.doorMap["principal"].leaf);
        Assert.AreEqual(Mode.Playing, gm.mode);
    }

    [UnityTest]
    public IEnumerator Items_are_given_once_and_keep_their_order()
    {
        yield return null;
        gm.Give("Linterna"); gm.Give("Pilas"); gm.Give("Linterna");
        CollectionAssert.AreEqual(new[] { "Linterna", "Pilas" }, gm.items);
    }

    [UnityTest]
    public IEnumerator The_hint_follows_the_progress()
    {
        yield return null;
        StringAssert.Contains("luz", gm.Hint());
        gm.Give("Linterna"); gm.Give("Pilas");
        StringAssert.Contains("baño", gm.Hint());
        gm.Give("Llave verde");
        StringAssert.Contains("biblioteca", gm.Hint());
        gm.Give("Llave azul");
        StringAssert.Contains("reja", gm.Hint());
        gm.Give("Partituras");
        StringAssert.Contains("piano", gm.Hint());
        gm.Give("Llave amarilla");
        StringAssert.Contains("oficina", gm.Hint());
        gm.Give("Llave dorada");
        StringAssert.Contains("puerta principal", gm.Hint());
    }
}
