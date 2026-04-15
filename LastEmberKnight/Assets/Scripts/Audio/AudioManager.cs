using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LastEmberKnight
{
    // =========================================================================
    //  SoundType enum
    // =========================================================================

    public enum SoundType
    {
        Jump,
        Attack,
        HitEnemy,
        HitBoss,
        Dash,
        Death,
        ShardPickup,
        LevelComplete,
        Parry,
        SoulBlast,
        BossPhase,
        Shrine,
        MenuClick,
    }

    // =========================================================================
    //  AudioManager
    // =========================================================================

    /// <summary>
    /// Singleton MonoBehaviour that owns a pooled set of AudioSources.
    /// Plays procedural SFX clips and streamed music with fade support.
    /// Volume is separated into master, sfx, and music channels.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static AudioManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Pool")]
        [SerializeField] private int sfxPoolSize = 10;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume    = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume  = 0.6f;

        // ── Runtime ───────────────────────────────────────────────────────────
        private AudioSource[]       _sfxPool;
        private int                 _poolIndex = 0;
        private AudioSource         _musicSource;
        private Coroutine           _musicFadeCoroutine;

        private ProceduralSFX       _proceduralSFX;
        private readonly Dictionary<SoundType, AudioClip> _clipCache
            = new Dictionary<SoundType, AudioClip>();

        // ── Properties ────────────────────────────────────────────────────────
        public float MasterVolume
        {
            get => masterVolume;
            set { masterVolume = Mathf.Clamp01(value); RefreshAllVolumes(); }
        }
        public float SFXVolume
        {
            get => sfxVolume;
            set { sfxVolume = Mathf.Clamp01(value); }
        }
        public float MusicVolume
        {
            get => musicVolume;
            set { musicVolume = Mathf.Clamp01(value); RefreshMusicVolume(); }
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildPool();
            BuildMusicSource();

            // Generate procedural clips
            _proceduralSFX = gameObject.GetComponent<ProceduralSFX>()
                          ?? gameObject.AddComponent<ProceduralSFX>();
            _proceduralSFX.GenerateAllClips();
            CacheClips();
        }

        // =========================================================================
        //  Public API
        // =========================================================================

        /// <summary>Play a one-shot SFX from the pool.</summary>
        /// <param name="type">The sound to play.</param>
        /// <param name="volume">Relative volume (multiplied by sfx and master channels).</param>
        /// <param name="pitch">Pitch override (default 1.0).</param>
        public void PlaySound(SoundType type, float volume = 1f, float pitch = 1f)
        {
            if (!_clipCache.TryGetValue(type, out AudioClip clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] No clip for SoundType.{type}");
                return;
            }

            AudioSource src = NextPoolSource();
            src.pitch  = pitch;
            src.volume = Mathf.Clamp01(volume * sfxVolume * masterVolume);
            src.PlayOneShot(clip);
        }

        /// <summary>
        /// Start playing a music clip, cross-fading from the current one.
        /// </summary>
        public void PlayMusic(AudioClip clip, float fadeTime = 1f)
        {
            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(FadeToMusic(clip, fadeTime));
        }

        /// <summary>Stop music with a fade-out.</summary>
        public void StopMusic(float fadeTime = 1f)
        {
            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(FadeOutMusic(fadeTime));
        }

        /// <summary>Returns the AudioClip for a given SoundType (for external use).</summary>
        public AudioClip GetClip(SoundType type)
        {
            _clipCache.TryGetValue(type, out AudioClip clip);
            return clip;
        }

        // =========================================================================
        //  Private helpers
        // =========================================================================

        private void BuildPool()
        {
            _sfxPool = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                GameObject go = new GameObject($"SFXSource_{i}");
                go.transform.SetParent(transform);
                AudioSource src = go.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool[i] = src;
            }
        }

        private void BuildMusicSource()
        {
            GameObject go = new GameObject("MusicSource");
            go.transform.SetParent(transform);
            _musicSource = go.AddComponent<AudioSource>();
            _musicSource.loop        = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume      = musicVolume * masterVolume;
        }

        private void CacheClips()
        {
            if (_proceduralSFX == null) return;

            foreach (SoundType type in System.Enum.GetValues(typeof(SoundType)))
            {
                AudioClip clip = _proceduralSFX.GetClip(type);
                if (clip != null)
                    _clipCache[type] = clip;
            }
        }

        private AudioSource NextPoolSource()
        {
            AudioSource src = _sfxPool[_poolIndex];
            _poolIndex = (_poolIndex + 1) % sfxPoolSize;
            return src;
        }

        private void RefreshAllVolumes()
        {
            RefreshMusicVolume();
        }

        private void RefreshMusicVolume()
        {
            if (_musicSource != null)
                _musicSource.volume = musicVolume * masterVolume;
        }

        // ── Music coroutines ──────────────────────────────────────────────────

        private IEnumerator FadeToMusic(AudioClip newClip, float fadeTime)
        {
            float targetVol = musicVolume * masterVolume;

            // Fade out current
            if (_musicSource.isPlaying)
            {
                float start = _musicSource.volume;
                float elapsed = 0f;
                while (elapsed < fadeTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _musicSource.volume = Mathf.Lerp(start, 0f, elapsed / fadeTime);
                    yield return null;
                }
            }

            // Swap clip
            _musicSource.Stop();
            _musicSource.clip   = newClip;
            _musicSource.volume = 0f;
            _musicSource.Play();

            // Fade in
            {
                float elapsed = 0f;
                while (elapsed < fadeTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _musicSource.volume = Mathf.Lerp(0f, targetVol, elapsed / fadeTime);
                    yield return null;
                }
                _musicSource.volume = targetVol;
            }

            _musicFadeCoroutine = null;
        }

        private IEnumerator FadeOutMusic(float fadeTime)
        {
            float start   = _musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(start, 0f, elapsed / fadeTime);
                yield return null;
            }
            _musicSource.Stop();
            _musicFadeCoroutine = null;
        }
    }
}
