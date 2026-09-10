using System;
using UnityEngine;

namespace VRSim.Core
{
    /// <summary>
    /// Generates the scenario's audio cues in code.
    ///
    /// The prototype ships no sound files, and a silent emergency drill is a poor demonstration.
    /// Synthesising simple tones keeps the repository free of licensed audio while still giving
    /// the alarm, warnings and outcomes a distinct sound. Replace with recorded audio when
    /// licensed clips are available.
    ///
    /// Every cue is also subtitled by <see cref="AudioManager"/>, so nothing depends on hearing.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;

        /// <summary>A steady tone with short fades so it does not click on start or stop.</summary>
        public static AudioClip Tone(string name, float frequency, float seconds, float volume = 0.5f)
        {
            return Build(name, seconds, (t, _) => Mathf.Sin(2f * Mathf.PI * frequency * t) * volume);
        }

        /// <summary>A tone that slides from one frequency to another.</summary>
        public static AudioClip Sweep(string name, float fromHz, float toHz, float seconds,
            float volume = 0.5f)
        {
            return Build(name, seconds, (t, progress) =>
            {
                var frequency = Mathf.Lerp(fromHz, toHz, progress);
                return Mathf.Sin(2f * Mathf.PI * frequency * t) * volume;
            });
        }

        /// <summary>The two-tone note of a building fire alarm, built to loop seamlessly.</summary>
        public static AudioClip AlarmLoop()
        {
            const float half = 0.35f;
            return Build("Alarm Loop", half * 2f, (t, progress) =>
            {
                var frequency = progress < 0.5f ? 800f : 620f;
                var square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t));
                // Softened square wave: urgent without being painful over a long run.
                return square * 0.22f + Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.12f;
            }, fade: false);
        }

        /// <summary>Low double buzz for entering an unsafe area.</summary>
        public static AudioClip Warning() =>
            Build("Warning", 0.45f, (t, progress) =>
            {
                var gate = progress < 0.2f || (progress > 0.35f && progress < 0.55f) ? 1f : 0f;
                return Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.45f * gate;
            });

        /// <summary>Short blip confirming an object was used.</summary>
        public static AudioClip Interaction() => Tone("Interaction", 1200f, 0.07f, 0.3f);

        /// <summary>Rising two-note chime for a safe evacuation.</summary>
        public static AudioClip Success() =>
            Build("Success", 0.5f, (t, progress) =>
            {
                var frequency = progress < 0.5f ? 523f : 784f;   // C5 then G5
                return Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.4f;
            });

        /// <summary>Falling tone for a failed evacuation.</summary>
        public static AudioClip Failure() => Sweep("Failure", 400f, 150f, 0.6f, 0.4f);

        /// <param name="shape">Receives the time in seconds and the progress through the clip.</param>
        static AudioClip Build(string name, float seconds, Func<float, float, float> shape,
            bool fade = true)
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(seconds * SampleRate));
            var samples = new float[count];

            // A few milliseconds of fade removes the click a hard start or stop would make.
            var fadeSamples = fade ? Mathf.Min(count / 2, SampleRate / 200) : 0;

            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var progress = i / (float)count;
                var value = shape(t, progress);

                if (fadeSamples > 0)
                {
                    if (i < fadeSamples)
                        value *= i / (float)fadeSamples;
                    else if (i > count - fadeSamples)
                        value *= (count - i) / (float)fadeSamples;
                }

                samples[i] = Mathf.Clamp(value, -1f, 1f);
            }

            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
