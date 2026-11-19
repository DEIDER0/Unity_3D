using System.Collections.Generic;
using UnityEngine;

namespace Dungeon.View
{
    /// <summary>Efectos de sonido disponibles.</summary>
    public enum Sfx
    {
        Swing,
        Hit,
        Dodge,
        EnemyDeath,
        PlayerDeath,
        ChestOpen,
        DoorOpen,
        BossRoar,
        Slam,
        Victory,
        Defeat
    }

    /// <summary>
    /// VISTA. Reproduce efectos de sonido. Son placeholders sintetizados por código
    /// (no hay archivos de audio); si consiguen sonidos reales, basta con cambiar CreateClip().
    /// Se crea solo la primera vez que se usa y sobrevive entre escenas.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private static SfxPlayer _instance;
        private static bool _quitting;

        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        private AudioSource _source;
        private System.Random _random;

        public static float Volume { get; set; } = 0.6f;

        public static void Play(Sfx sfx, float volume = 1f)
        {
            if (_quitting || !Application.isPlaying) return;
            if (_instance == null)
            {
                var go = new GameObject("SfxPlayer");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<SfxPlayer>();
            }
            _instance.PlayInternal(sfx, volume);
        }

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _random = new System.Random(1234);
        }

        private void OnApplicationQuit() => _quitting = true;

        private void PlayInternal(Sfx sfx, float volume)
        {
            if (!_clips.TryGetValue(sfx, out AudioClip clip))
            {
                clip = CreateClip(sfx);
                _clips[sfx] = clip;
            }
            _source.pitch = 1f + ((float)_random.NextDouble() - 0.5f) * 0.1f;
            _source.PlayOneShot(clip, volume * Volume);
        }

        // ------------------------------------------------------------------
        // Síntesis de los sonidos (ruido, ondas y envolventes simples)
        // ------------------------------------------------------------------
        private AudioClip CreateClip(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Swing: return Build("Swing", 0.18f, (t, d) => Noise() * Env(t, d, 0.3f) * 0.5f, lowPass: 0.25f);
                case Sfx.Hit: return Build("Hit", 0.15f, (t, d) => (Sine(150f - 400f * t, t) * 0.8f + Noise() * 0.4f) * Env(t, d, 0.02f));
                case Sfx.Dodge: return Build("Dodge", 0.22f, (t, d) => Noise() * Env(t, d, 0.5f) * 0.35f, lowPass: 0.12f);
                case Sfx.EnemyDeath: return Build("EnemyDeath", 0.45f, (t, d) => Square(300f - 500f * t, t) * Env(t, d, 0.05f) * 0.3f);
                case Sfx.PlayerDeath: return Build("PlayerDeath", 0.8f, (t, d) => Square(220f - 180f * t, t) * Env(t, d, 0.05f) * 0.3f);
                case Sfx.ChestOpen: return Build("ChestOpen", 0.6f, (t, d) => Arpeggio(t, new[] { 523f, 659f, 784f, 1047f }, 0.12f) * Env(t, d, 0.02f) * 0.35f);
                case Sfx.DoorOpen: return Build("DoorOpen", 1.4f, (t, d) => (Noise() * 0.6f + Sine(55f, t) * 0.5f) * Env(t, d, 0.2f) * 0.5f, lowPass: 0.04f);
                case Sfx.BossRoar: return Build("BossRoar", 1.0f, (t, d) => (Saw(110f - 40f * t + Mathf.Sin(t * 40f) * 8f, t) * 0.6f + Noise() * 0.3f) * Env(t, d, 0.15f) * 0.45f, lowPass: 0.15f);
                case Sfx.Slam: return Build("Slam", 0.7f, (t, d) => (Sine(70f - 40f * t, t) + Noise() * 0.5f) * Env(t, d, 0.01f) * 0.7f, lowPass: 0.08f);
                case Sfx.Victory: return Build("Victory", 1.4f, (t, d) => Arpeggio(t, new[] { 523f, 659f, 784f, 1047f, 1319f }, 0.16f) * Env(t, d, 0.01f) * 0.35f);
                default: return Build("Defeat", 1.4f, (t, d) => Arpeggio(t, new[] { 392f, 349f, 311f, 262f }, 0.3f) * Env(t, d, 0.01f) * 0.35f);
            }
        }

        private delegate float Generator(float time, float duration);

        private AudioClip Build(string name, float duration, Generator generator, float lowPass = 1f)
        {
            int samples = Mathf.CeilToInt(duration * SampleRate);
            var data = new float[samples];
            float filtered = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float raw = generator(t, duration);
                filtered += (raw - filtered) * lowPass; // filtro paso bajo de un polo
                data[i] = Mathf.Clamp(filtered, -1f, 1f);
            }
            var clip = AudioClip.Create("Sfx_" + name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private float Noise() => (float)_random.NextDouble() * 2f - 1f;
        private static float Sine(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t);
        private static float Square(float freq, float t) => Sine(freq, t) >= 0f ? 1f : -1f;
        private static float Saw(float freq, float t) => 2f * (t * freq - Mathf.Floor(0.5f + t * freq));

        private static float Arpeggio(float t, float[] notes, float noteLength)
        {
            int index = Mathf.Min((int)(t / noteLength), notes.Length - 1);
            return Sine(notes[index], t) * 0.7f + Sine(notes[index] * 2f, t) * 0.2f;
        }

        /// <summary>Envolvente: ataque corto y caída hasta el final.</summary>
        private static float Env(float t, float duration, float attackFraction)
        {
            float attack = Mathf.Max(0.001f, duration * attackFraction);
            if (t < attack) return t / attack;
            return Mathf.Clamp01(1f - (t - attack) / (duration - attack));
        }
    }
}
