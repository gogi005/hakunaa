using Godot;

namespace AimTrainer
{
    /// <summary>
    /// Generates a tiny procedural "pop" sound at runtime so the prototype
    /// needs no audio files. Drop a real WAV/OGG into res://audio/ and assign
    /// it to the ShootAudio node's Stream to replace this placeholder.
    /// </summary>
    public static class SoundGenerator
    {
        public static AudioStreamWav CreatePopSound(
            float durationSeconds = 0.07f, float frequencyHz = 700f, float volume = 0.35f)
        {
            int sampleRate = 22050;
            int sampleCount = (int)(sampleRate * durationSeconds);
            byte[] data = new byte[sampleCount * 2]; // 16-bit mono

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / durationSeconds);       // quick decay
                float sample = Mathf.Sin(t * frequencyHz * Mathf.Tau) * envelope * volume;
                short s = (short)(sample * short.MaxValue);

                data[i * 2] = (byte)(s & 0xFF);
                data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }

            AudioStreamWav wav = new AudioStreamWav
            {
                Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = sampleRate,
                Data = data,
            };
            return wav;
        }
    }
}
