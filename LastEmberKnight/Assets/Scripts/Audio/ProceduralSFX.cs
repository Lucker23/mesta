using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    /// <summary>
    /// Generates all game SFX AudioClips at runtime using mathematical waveforms.
    /// No audio asset files required. Called once on Awake by AudioManager.
    /// </summary>
    public class ProceduralSFX : MonoBehaviour
    {
        private const int   SampleRate  = 44100;
        private const float TwoPi       = 2f * Mathf.PI;

        private readonly Dictionary<SoundType, AudioClip> _clips
            = new Dictionary<SoundType, AudioClip>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Generate (or re-generate) all clips. Safe to call multiple times.</summary>
        public void GenerateAllClips()
        {
            _clips[SoundType.Jump]          = GenerateJump();
            _clips[SoundType.Attack]        = GenerateAttack();
            _clips[SoundType.HitEnemy]      = GenerateHitEnemy();
            _clips[SoundType.HitBoss]       = GenerateHitBoss();
            _clips[SoundType.Dash]          = GenerateDash();
            _clips[SoundType.Death]         = GenerateDeath();
            _clips[SoundType.ShardPickup]   = GenerateShardPickup();
            _clips[SoundType.LevelComplete] = GenerateLevelComplete();
            _clips[SoundType.Parry]         = GenerateParry();
            _clips[SoundType.SoulBlast]     = GenerateSoulBlast();
            _clips[SoundType.BossPhase]     = GenerateBossPhase();
            _clips[SoundType.Shrine]        = GenerateShrine();
            _clips[SoundType.MenuClick]     = GenerateMenuClick();
        }

        /// <summary>Returns the generated AudioClip for the given SoundType, or null.</summary>
        public AudioClip GetClip(SoundType type)
        {
            _clips.TryGetValue(type, out AudioClip clip);
            return clip;
        }

        // =========================================================================
        //  Individual sound generators
        // =========================================================================

        // ── Jump: rising sine sweep 200 Hz → 600 Hz over 0.1 s ───────────────
        private static AudioClip GenerateJump()
        {
            float duration  = 0.10f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            float startFreq = 200f;
            float endFreq   = 600f;
            float phase     = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t    = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float env  = Mathf.Sin(t * Mathf.PI);           // bell envelope
                phase += TwoPi * freq / SampleRate;
                data[i] = Mathf.Sin(phase) * env * 0.5f;
            }

            return MakeClip("Jump", data);
        }

        // ── Attack: white-noise burst 0.05 s with high-pass effect ───────────
        private static AudioClip GenerateAttack()
        {
            float duration  = 0.05f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            System.Random rng = new System.Random(42);
            float prev = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t     = (float)i / samples;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                // Simple first-order high-pass: y[n] = x[n] - x[n-1]
                float hp    = noise - prev;
                prev        = noise;
                float env   = Mathf.Pow(1f - t, 2f);           // fast decay
                data[i]     = hp * env * 0.6f;
            }

            return MakeClip("Attack", data);
        }

        // ── HitEnemy: sine pulse 80 Hz, 0.08 s, fast decay ───────────────────
        private static AudioClip GenerateHitEnemy()
        {
            float duration  = 0.08f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            const float freq = 80f;
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / samples;
                float env = Mathf.Exp(-t * 20f);                // exponential decay
                phase    += TwoPi * freq / SampleRate;
                data[i]   = Mathf.Sin(phase) * env * 0.7f;
            }

            return MakeClip("HitEnemy", data);
        }

        // ── HitBoss: deeper sine 50 Hz, 0.12 s ───────────────────────────────
        private static AudioClip GenerateHitBoss()
        {
            float duration  = 0.12f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            const float freq = 50f;
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / samples;
                float env = Mathf.Exp(-t * 12f);
                phase    += TwoPi * freq / SampleRate;
                // Add a slight 2nd harmonic for thickness
                float harmonics = Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.3f;
                data[i] = harmonics * env * 0.55f;
            }

            return MakeClip("HitBoss", data);
        }

        // ── Dash: filtered noise sweep low → high over 0.12 s ────────────────
        private static AudioClip GenerateDash()
        {
            float duration  = 0.12f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            System.Random rng = new System.Random(99);
            float prevLP = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t     = (float)i / samples;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);

                // Low-pass cutoff rises from 0.02 to 0.35 (fraction of Nyquist)
                float cutoff = Mathf.Lerp(0.02f, 0.35f, t);
                prevLP       = prevLP + cutoff * (noise - prevLP);  // simple RC low-pass

                float env    = Mathf.Sin(t * Mathf.PI);             // bell envelope
                data[i]      = prevLP * env * 0.65f;
            }

            return MakeClip("Dash", data);
        }

        // ── Death: descending sine 400 Hz → 50 Hz over 0.5 s ─────────────────
        private static AudioClip GenerateDeath()
        {
            float duration  = 0.50f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            float startFreq = 400f;
            float endFreq   = 50f;
            float phase     = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t    = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t * t);    // exponential sweep down
                float env  = Mathf.Pow(1f - t, 1.5f);
                phase     += TwoPi * freq / SampleRate;
                data[i]    = Mathf.Sin(phase) * env * 0.65f;
            }

            return MakeClip("Death", data);
        }

        // ── ShardPickup: sine at 800 Hz, 0.05 s ──────────────────────────────
        private static AudioClip GenerateShardPickup()
        {
            float duration  = 0.05f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            const float freq = 800f;
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / samples;
                float env = Mathf.Exp(-t * 30f);
                phase    += TwoPi * freq / SampleRate;
                data[i]   = Mathf.Sin(phase) * env * 0.5f;
            }

            return MakeClip("ShardPickup", data);
        }

        // ── LevelComplete: ascending arpeggio C-E-G-C over 0.4 s ─────────────
        //   C4=261.63, E4=329.63, G4=392.00, C5=523.25 Hz
        private static AudioClip GenerateLevelComplete()
        {
            float duration  = 0.40f;
            int   totalSamples = (int)(SampleRate * duration);
            float[] data    = new float[totalSamples];

            float[] freqs   = { 261.63f, 329.63f, 392.00f, 523.25f };
            int   noteCount = freqs.Length;
            int   noteLen   = totalSamples / noteCount;

            for (int n = 0; n < noteCount; n++)
            {
                float freq  = freqs[n];
                float phase = 0f;
                int   start = n * noteLen;
                int   end   = Mathf.Min(start + noteLen, totalSamples);

                for (int i = start; i < end; i++)
                {
                    float localT = (float)(i - start) / noteLen;
                    float env    = Mathf.Sin(localT * Mathf.PI);        // per-note bell
                    phase       += TwoPi * freq / SampleRate;
                    data[i]     += Mathf.Sin(phase) * env * 0.45f;
                }
            }

            return MakeClip("LevelComplete", data);
        }

        // ── Parry: sharp mid-frequency click / clang ──────────────────────────
        private static AudioClip GenerateParry()
        {
            float duration  = 0.06f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            const float freq1 = 320f;
            const float freq2 = 640f;
            float phase1 = 0f, phase2 = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / samples;
                float env = Mathf.Exp(-t * 40f);
                phase1   += TwoPi * freq1 / SampleRate;
                phase2   += TwoPi * freq2 / SampleRate;
                data[i]   = (Mathf.Sin(phase1) + Mathf.Sin(phase2) * 0.5f) * env * 0.55f;
            }

            return MakeClip("Parry", data);
        }

        // ── SoulBlast: rising whoosh + low thump ──────────────────────────────
        private static AudioClip GenerateSoulBlast()
        {
            float duration  = 0.20f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            System.Random rng = new System.Random(77);
            float prevLP = 0f;
            float phase  = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t     = (float)i / samples;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float cutoff = Mathf.Lerp(0.05f, 0.5f, t);
                prevLP = prevLP + cutoff * (noise - prevLP);

                // Add base sine "boom"
                float freq = Mathf.Lerp(120f, 200f, t);
                phase += TwoPi * freq / SampleRate;
                float boom = Mathf.Sin(phase) * Mathf.Exp(-t * 10f);

                float env  = Mathf.Sin(t * Mathf.PI);
                data[i]    = (prevLP * 0.4f + boom * 0.6f) * env * 0.7f;
            }

            return MakeClip("SoulBlast", data);
        }

        // ── BossPhase: dramatic low impact + rising tone ──────────────────────
        private static AudioClip GenerateBossPhase()
        {
            float duration  = 0.60f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t    = (float)i / samples;
                float freq = Mathf.Lerp(60f, 300f, Mathf.Sqrt(t));
                float env  = t < 0.05f
                             ? t / 0.05f
                             : Mathf.Pow(1f - (t - 0.05f) / 0.95f, 1.2f);
                phase     += TwoPi * freq / SampleRate;
                // Fundamental + 2nd harmonic for richness
                data[i]    = (Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.4f) * env * 0.6f;
            }

            return MakeClip("BossPhase", data);
        }

        // ── Shrine: warm ascending chime ─────────────────────────────────────
        private static AudioClip GenerateShrine()
        {
            float duration  = 0.35f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            float phase1 = 0f, phase2 = 0f;
            const float freq1 = 440f;    // A4
            const float freq2 = 660f;    // E5 (perfect fifth)

            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / samples;
                float env = Mathf.Sin(t * Mathf.PI) * Mathf.Pow(1f - t, 0.5f);
                phase1   += TwoPi * freq1 / SampleRate;
                phase2   += TwoPi * freq2 / SampleRate;
                data[i]   = (Mathf.Sin(phase1) * 0.6f + Mathf.Sin(phase2) * 0.4f) * env * 0.5f;
            }

            return MakeClip("Shrine", data);
        }

        // ── MenuClick: crisp short tick ───────────────────────────────────────
        private static AudioClip GenerateMenuClick()
        {
            float duration  = 0.03f;
            int   samples   = (int)(SampleRate * duration);
            float[] data    = new float[samples];

            const float freq = 900f;
            float phase = 0f;

            for (int i = 0; i < samples; i++)
            {
                float t   = (float)i / samples;
                float env = Mathf.Exp(-t * 60f);
                phase    += TwoPi * freq / SampleRate;
                data[i]   = Mathf.Sin(phase) * env * 0.4f;
            }

            return MakeClip("MenuClick", data);
        }

        // =========================================================================
        //  Factory helper
        // =========================================================================

        /// <summary>
        /// Create a mono AudioClip from a float[] sample array at 44100 Hz.
        /// </summary>
        private static AudioClip MakeClip(string clipName, float[] samples)
        {
            AudioClip clip = AudioClip.Create(
                clipName,
                samples.Length,
                channels: 1,
                frequency: SampleRate,
                stream: false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
