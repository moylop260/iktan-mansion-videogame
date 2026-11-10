using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Cada trampa dispara su escena de muerte, que termina en GAME OVER sin errores, y se puede reintentar.</summary>
public class DeathSceneTests
{
    [TearDown] public void TearDown() { TestWorld.Cleanup(); }

    static readonly object[][] Traps =
    {
        new object[] { "Espejo", 0 }, new object[] { "Refri", 0 }, new object[] { "Cama1", 0 },
        new object[] { "Closet", 0 }, new object[] { "Cortina", 0 }, new object[] { "LibroRojo", 0 },
        new object[] { "Tele", 0 }, new object[] { "Muneca", 0 }, new object[] { "Armario", 0 },
        new object[] { "CajaFuerte", 0 },
    };

    [UnityTest]
    public IEnumerator Every_trap_runs_its_scene_to_game_over_and_the_player_can_retry([ValueSource(nameof(Traps))] object[] trap)
    {
        var gm = TestWorld.BootAndStart();
        gm.FlashOn = true;                       // the doll scene needs light
        Time.timeScale = 10f;                    // scenes last several seconds
        TestWorld.Use(TestWorld.Prop((string)trap[0]), (int)trap[1]);

        Assert.AreEqual(Mode.Death, gm.mode, trap[0] + " should start a death scene");
        Assert.AreEqual(1, gm.Deaths);
        float waited = 0f;
        while (gm.mode == Mode.Death && waited < 30f) { waited += Time.unscaledDeltaTime; yield return null; }
        Assert.AreEqual(Mode.GameOver, gm.mode, trap[0] + " never reached GAME OVER");
        Assert.AreEqual(0f, gm.fadeAlpha, "the black fade must not cover the GAME OVER screen");

        Time.timeScale = 1f;
        gm.Retry();
        Assert.AreEqual(Mode.Playing, gm.mode);
        Assert.AreEqual(0f, gm.fadeAlpha);
        Assert.AreEqual(72f, TestWorld.Player().GetComponentInChildren<Camera>().fieldOfView, 0.01f);
    }

    [UnityTest]
    public IEnumerator A_second_death_while_dying_is_ignored()
    {
        var gm = TestWorld.BootAndStart();
        Time.timeScale = 10f;
        TestWorld.Use(TestWorld.Prop("Espejo"), 0);
        gm.Die("fridge", TestWorld.Prop("Refri"));
        Assert.AreEqual(1, gm.Deaths);
        yield return null;
    }

    [UnityTest]
    public IEnumerator The_closet_scene_works_even_when_standing_right_in_front_of_it()
    {
        var gm = TestWorld.BootAndStart();
        var closet = TestWorld.Prop("Closet");
        TestWorld.Player().Teleport(new Vector3(6.3f, 0.05f, 12.5f), 90f);   // point blank
        Time.timeScale = 10f;
        TestWorld.Use(closet, 0);
        float waited = 0f;
        while (gm.mode == Mode.Death && waited < 30f) { waited += Time.unscaledDeltaTime; yield return null; }
        Assert.AreEqual(Mode.GameOver, gm.mode);
    }

    [UnityTest]
    public IEnumerator The_alarm_scene_runs_without_a_focus_object()
    {
        var gm = TestWorld.BootAndStart();
        Time.timeScale = 10f;
        gm.Die("alarm", null);
        float waited = 0f;
        while (gm.mode == Mode.Death && waited < 30f) { waited += Time.unscaledDeltaTime; yield return null; }
        Assert.AreEqual(Mode.GameOver, gm.mode);
    }

    [UnityTest]
    public IEnumerator Lights_that_a_scene_turned_off_come_back_after_retry()
    {
        var gm = TestWorld.BootAndStart();
        Time.timeScale = 10f;
        TestWorld.Use(TestWorld.Prop("Tele"), 0);   // the TV scene switches the lights off
        float waited = 0f;
        while (gm.mode == Mode.Death && waited < 30f) { waited += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = 1f;
        gm.Retry();
        yield return null;
        var lights = TestWorld.World().GetComponentsInChildren<Light>();
        Assert.Greater(lights.Length, 10);
        Assert.IsTrue(System.Array.TrueForAll(lights, l => l.enabled), "all house lights must be on again");
    }
}
