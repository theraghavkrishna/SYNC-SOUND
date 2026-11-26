using System.IO;
using System.Media;
using SyncSound.Core.Interfaces;

namespace SyncSound.App.Services;

public enum UiSoundCue
{
    Select,
    Deselect,
    Start,
    Stop,
    VolumeDetent,
    Mute,
    Reconnect,
    Disconnect
}

public interface ISoundFeedbackService
{
    void Play(UiSoundCue cue);
}

/// <summary>
/// Generates subtle, restrained acoustic UI feedback chimes in memory.
/// Respects user preferences and rate-limits continuous controls so volume adjustments never buzz.
/// </summary>
public sealed class SoundFeedbackService : ISoundFeedbackService
{
    private readonly ISettingsService _settingsService;
    private readonly Dictionary<UiSoundCue, byte[]> _wavCache = new();
    private readonly Lock _gate = new();
    private DateTime _lastDetentUtc = DateTime.MinValue;

    public SoundFeedbackService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        PrecomputeCues();
    }

    public void Play(UiSoundCue cue)
    {
        if (!_settingsService.Current.EnableUiSounds)
        {
            return;
        }

        if (cue == UiSoundCue.VolumeDetent)
        {
            var now = DateTime.UtcNow;
            if ((now - _lastDetentUtc).TotalMilliseconds < 90)
            {
                return;
            }

            _lastDetentUtc = now;
        }

        if (!_wavCache.TryGetValue(cue, out var wavBytes))
        {
            return;
        }

        _ = Task.Run(() =>
        {
            lock (_gate)
            {
                try
                {
                    using var ms = new MemoryStream(wavBytes, writable: false);
                    using var player = new SoundPlayer(ms);
                    player.Play();
                }
                catch
                {
                    // Never surface UI cue failures to the user.
                }
            }
        });
    }

    private void PrecomputeCues()
    {
        _wavCache[UiSoundCue.Select] = SynthesizeTone([(587.33, 0.055, 0.09)]);
        _wavCache[UiSoundCue.Deselect] = SynthesizeTone([(440.0, 0.048, 0.075)]);
        _wavCache[UiSoundCue.Start] = SynthesizeTone([(440.0, 0.070, 0.085), (659.25, 0.120, 0.095)]);
        _wavCache[UiSoundCue.Stop] = SynthesizeTone([(523.25, 0.065, 0.075), (392.0, 0.105, 0.07)]);
        _wavCache[UiSoundCue.VolumeDetent] = SynthesizeTone([(784.0, 0.022, 0.055)]);
        _wavCache[UiSoundCue.Mute] = SynthesizeTone([(329.63, 0.075, 0.07)]);
        _wavCache[UiSoundCue.Reconnect] = SynthesizeTone([(523.25, 0.060, 0.08), (783.99, 0.110, 0.085)]);
        _wavCache[UiSoundCue.Disconnect] = SynthesizeTone([(349.23, 0.085, 0.07), (261.63, 0.110, 0.065)]);
    }

    private static byte[] SynthesizeTone((double FreqHz, double DurationSec, double Gain)[] notes)
    {
        const int sampleRate = 24000;
        var totalSamples = notes.Sum(n => (int)(n.DurationSec * sampleRate));
        var pcmData = new short[totalSamples];

        var offset = 0;
        foreach (var (freq, dur, gain) in notes)
        {
            var count = (int)(dur * sampleRate);
            var attackSamples = Math.Max(1, (int)(0.005 * sampleRate));
            for (var i = 0; i < count; i++)
            {
                var t = i / (double)sampleRate;
                var env = i < attackSamples
                    ? i / (double)attackSamples
                    : Math.Pow(1.0 - (i - attackSamples) / (double)Math.Max(1, count - attackSamples), 1.8);
                var sample = Math.Sin(2.0 * Math.PI * freq * t) * gain * env;
                pcmData[offset + i] = (short)Math.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
            }
            offset += count;
        }

        using var ms = new MemoryStream(44 + pcmData.Length * 2);
        using var bw = new BinaryWriter(ms);
        bw.Write("RIFF"u8);
        bw.Write(36 + pcmData.Length * 2);
        bw.Write("WAVE"u8);
        bw.Write("fmt "u8);
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write("data"u8);
        bw.Write(pcmData.Length * 2);
        foreach (var s in pcmData)
        {
            bw.Write(s);
        }

        return ms.ToArray();
    }
}
