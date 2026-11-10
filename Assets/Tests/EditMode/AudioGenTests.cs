using NUnit.Framework;
using UnityEngine;

public class AudioGenTests
{
    static readonly string[] Names =
    {
        "drone", "heart", "roar", "scream", "screech", "stinger", "boom", "skitter", "static", "siren",
        "giggle", "whisper", "creak", "glass", "drip", "step", "pickup", "checkpoint", "piano", "win", "click"
    };

    [TestCaseSource(nameof(Names))]
    public void Every_clip_is_audible_finite_and_not_clipping(string name)
    {
        var clip = AudioGen.Get(name);
        Assert.IsNotNull(clip, name);
        Assert.Greater(clip.length, 0.04f, name + " is too short");
        var data = new float[clip.samples];
        Assert.IsTrue(clip.GetData(data, 0));
        float peak = 0f;
        foreach (var v in data)
        {
            Assert.IsFalse(float.IsNaN(v) || float.IsInfinity(v), name + " has NaN/Inf samples");
            peak = Mathf.Max(peak, Mathf.Abs(v));
        }
        Assert.Greater(peak, 0.05f, name + " is silent");
        Assert.LessOrEqual(peak, 1.0f, name + " clips");
    }

    [Test]
    public void Clips_are_cached()
    {
        Assert.AreSame(AudioGen.Get("roar"), AudioGen.Get("roar"));
    }

    [Test]
    public void Unknown_name_gives_a_short_silent_clip_instead_of_failing()
    {
        var clip = AudioGen.Get("this-does-not-exist");
        Assert.IsNotNull(clip);
        Assert.Greater(clip.samples, 0);
    }

    [Test]
    public void Scream_is_longer_than_screech_and_roar_is_the_longest_beast_sound()
    {
        Assert.Greater(AudioGen.Get("scream").length, AudioGen.Get("screech").length);
        Assert.Greater(AudioGen.Get("roar").length, 3f);
    }

    [Test]
    public void Heartbeat_is_one_beat_cycle()
    {
        Assert.That(AudioGen.Get("heart").length, Is.EqualTo(0.95f).Within(0.02f));
    }

    [Test]
    public void Drone_loops_without_a_click_at_the_seam()
    {
        var clip = AudioGen.Get("drone");
        var d = new float[clip.samples];
        clip.GetData(d, 0);
        Assert.Less(Mathf.Abs(d[0] - d[d.Length - 1]), 0.1f);
        Assert.That(clip.length, Is.EqualTo(8f).Within(0.05f));
    }

    [Test]
    public void PreloadAll_generates_every_clip_used_by_the_game()
    {
        AudioGen.PreloadAll();
        foreach (var n in new[] { "drone", "heart", "roar", "scream", "stinger", "boom", "skitter", "static", "siren",
                                  "giggle", "whisper", "creak", "glass", "drip", "step", "pickup", "checkpoint", "piano", "win", "click", "screech" })
            Assert.IsNotNull(AudioGen.Get(n), n);
    }
}
