using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CheckpointTests
{
    GameManager gm;
    [SetUp] public void SetUp() { gm = TestWorld.BootAndStart(); }
    [TearDown] public void TearDown() { TestWorld.Cleanup(); }

    IEnumerator TouchLamp(string name)
    {
        var cp = gm.checkpoints.Find(c => c.name == name);
        TestWorld.Player().Teleport(cp.lampPos + new Vector3(0.5f, 0.05f, 0f), 0f);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Walking_near_a_lamp_saves_progress_and_turns_it_active()
    {
        gm.Give("Linterna");
        yield return TouchLamp("Vestíbulo");
        Assert.AreEqual("Vestíbulo", gm.CurrentCheckpoint);
        Assert.IsTrue(PlayerPrefs.HasKey(TestWorld.SaveKey));
        var save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(TestWorld.SaveKey));
        Assert.AreEqual("Vestíbulo", save.checkpoint);
        CollectionAssert.Contains(save.items, "Linterna");
    }

    [UnityTest]
    public IEnumerator Retry_goes_back_to_the_checkpoint_state_not_to_the_death_state()
    {
        gm.Give("Linterna");
        gm.doorMap["reja"].Open(true);
        yield return TouchLamp("Segundo piso");

        gm.Give("Pilas");                              // picked up after the checkpoint
        gm.doorMap["biblioteca"].Open(true);
        Time.timeScale = 10f;
        gm.Die("alarm", null);
        float waited = 0f;
        while (gm.mode == Mode.Death && waited < 30f) { waited += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = 1f;
        gm.Retry();

        Assert.IsTrue(gm.Has("Linterna"));
        Assert.IsFalse(gm.Has("Pilas"), "items found after the checkpoint are lost");
        Assert.IsTrue(gm.doorMap["reja"].IsOpen, "doors opened before the checkpoint stay open");
        Assert.IsFalse(gm.doorMap["biblioteca"].IsOpen, "doors opened after the checkpoint close again");
        Assert.AreEqual(1, gm.Deaths, "deaths keep counting across retries");
        var p = TestWorld.Player().transform.position;
        Assert.Greater(p.y, 3f, "the player respawns upstairs at the second-floor lamp");
    }

    [UnityTest]
    public IEnumerator Continue_restores_the_saved_game()
    {
        gm.Give("Llave verde");
        yield return TouchLamp("Vestíbulo");
        gm.Give("Pilas");
        gm.Continue();
        Assert.AreEqual(Mode.Playing, gm.mode);
        Assert.IsTrue(gm.Has("Llave verde"));
        Assert.IsFalse(gm.Has("Pilas"));
    }

    [UnityTest]
    public IEnumerator A_new_game_deletes_the_save()
    {
        yield return TouchLamp("Vestíbulo");
        Assert.IsTrue(PlayerPrefs.HasKey(TestWorld.SaveKey));
        gm.NewGame();
        Assert.IsFalse(PlayerPrefs.HasKey(TestWorld.SaveKey) && JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(TestWorld.SaveKey)).checkpoint == "Vestíbulo");
        Assert.AreEqual(0, gm.items.Count);
        Assert.AreEqual(0, gm.doors.Count);
    }

    [UnityTest]
    public IEnumerator Death_count_is_saved_when_you_die()
    {
        yield return TouchLamp("Vestíbulo");
        Time.timeScale = 10f;
        gm.Die("alarm", null);
        var save = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(TestWorld.SaveKey));
        Assert.AreEqual(1, save.deaths);
        float waited = 0f;
        while (gm.mode == Mode.Death && waited < 30f) { waited += Time.unscaledDeltaTime; yield return null; }
    }
}
