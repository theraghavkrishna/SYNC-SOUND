using SyncSound.Audio.Generators;
using SyncSound.Core.Models;

namespace SyncSound.Tests.AudioTests;

public sealed class TestSignalSampleProviderTests
{
    [Fact]
    public void ContinuousTone_GeneratesBoundedNonZeroStereoSamples()
    {
        var provider = new TestSignalSampleProvider(AudioTestMode.ContinuousTone440Hz, sampleRate: 48000, channels: 2, gain: 0.25f);
        var buffer = new float[4800 * 2]; // 100 ms

        var read = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(buffer.Length, read);
        Assert.Contains(buffer, s => Math.Abs(s) > 0.01f);
        Assert.All(buffer, s => Assert.InRange(s, -0.30f, 0.30f));

        // Left and right channels should match
        for (var i = 0; i < buffer.Length; i += 2)
        {
            Assert.Equal(buffer[i], buffer[i + 1]);
        }
    }

    [Fact]
    public void RhythmicPulse_HasAudibleAttackAndQuietDecayTail()
    {
        var provider = new TestSignalSampleProvider(AudioTestMode.RhythmicChimePulse, sampleRate: 48000, channels: 2, gain: 0.25f);
        var buffer = new float[24000 * 2]; // 500 ms = 1 full beat at 120 BPM

        var read = provider.Read(buffer, 0, buffer.Length);

        Assert.Equal(buffer.Length, read);

        // First 100 ms contains chime attack
        var attackPeak = buffer.Take(4800 * 2).Max(Math.Abs);
        // Final 100 ms of the 500 ms beat should be silent (0.0)
        var tailPeak = buffer.Skip(19200 * 2).Max(Math.Abs);

        Assert.True(attackPeak > 0.05f, $"Expected audible attack peak, got {attackPeak}");
        Assert.Equal(0.0f, tailPeak);
    }
}
