using System.IO;
using System.Media;
using System.Text;

namespace MiniPiano.Audio;

/// <summary>
/// Tạo 13 âm thanh WAV từ sóng sin + hòa âm.
/// Không cần tải file WAV và không cần cài NuGet package.
/// SoundPlayer phát từng nốt đơn (monophonic), không phù hợp để chơi hợp âm.
/// </summary>
public sealed class PianoSoundEngine : IDisposable
{
    public static readonly IReadOnlyDictionary<string, double> Frequencies =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["C4"] = 261.63, ["C#4"] = 277.18, ["D4"] = 293.66,
            ["D#4"] = 311.13, ["E4"] = 329.63, ["F4"] = 349.23,
            ["F#4"] = 369.99, ["G4"] = 392.00, ["G#4"] = 415.30,
            ["A4"] = 440.00, ["A#4"] = 466.16, ["B4"] = 493.88,
            ["C5"] = 523.25
        };

    private const int SampleRate = 44100;
    private const double DurationSeconds = 0.52;
    private readonly Dictionary<string, SoundPlayer> _players = new();
    private readonly List<MemoryStream> _waveStreams = new();

    public PianoSoundEngine()
    {
        foreach (var (pitch, frequency) in Frequencies)
        {
            var stream = CreateWave(frequency);
            var player = new SoundPlayer(stream);
            player.Load();
            _waveStreams.Add(stream);
            _players.Add(pitch, player);
        }
    }

    public void Play(string pitch)
    {
        if (_players.TryGetValue(pitch, out var player))
            player.Play();
    }

    public void Stop()
    {
        foreach (var player in _players.Values)
            player.Stop();
    }

    private static MemoryStream CreateWave(double frequency)
    {
        var count = (int)(SampleRate * DurationSeconds);
        var bytes = count * sizeof(short);
        var stream = new MemoryStream(capacity: 44 + bytes);
        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            // WAV PCM: 1 kênh, 16 bit/sample, 44.1 kHz.
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + bytes);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);                 // PCM fmt size
            writer.Write((short)1);           // PCM format
            writer.Write((short)1);           // mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);     // bytes/s
            writer.Write((short)2);           // block align
            writer.Write((short)16);          // bit depth
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(bytes);

            for (var i = 0; i < count; i++)
            {
                var time = (double)i / SampleRate;
                var attack = Math.Min(1.0, time / 0.01);
                var decay = Math.Exp(-3.5 * time);
                var release = Math.Min(1.0, (DurationSeconds - time) / 0.055);
                var envelope = attack * decay * Math.Max(0, release);
                var wave = 0.78 * Math.Sin(2 * Math.PI * frequency * time)
                         + 0.17 * Math.Sin(4 * Math.PI * frequency * time)
                         + 0.05 * Math.Sin(6 * Math.PI * frequency * time);
                var sample = (short)(Math.Clamp(wave * envelope * 0.50, -1.0, 1.0) * short.MaxValue);
                writer.Write(sample);
            }
        }
        stream.Position = 0;
        return stream;
    }

    public void Dispose()
    {
        foreach (var player in _players.Values)
            player.Dispose();
        foreach (var stream in _waveStreams)
            stream.Dispose();
    }
}
