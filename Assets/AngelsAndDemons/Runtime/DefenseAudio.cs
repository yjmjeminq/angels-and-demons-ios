using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AngelsAndDemons
{
    public sealed class DefenseHaptics
    {
        public bool Enabled { get; private set; } = true;
        float nextTime;

        public DefenseHaptics()
        {
            Enabled = PlayerPrefs.GetInt("angels.haptics", 1) == 1;
        }

        public void Toggle()
        {
            Enabled = !Enabled;
            PlayerPrefs.SetInt("angels.haptics", Enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Trigger(int signal)
        {
            if (!Enabled || Time.unscaledTime < nextTime) return;
            nextTime = Time.unscaledTime + 0.12f;
#if UNITY_IOS && !UNITY_EDITOR
            AngelsHaptic(signal);
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void AngelsHaptic(int signal);
#endif
    }

    public sealed class DefenseAudio : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly AudioSource[] sources = new AudioSource[10];
        AudioSource bgm;
        int nextSource;
        float lastHitTime;

        public bool Muted { get; private set; }

        public void EnsureInitialized()
        {
            if (bgm != null) return;
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] == null)
                {
                    sources[i] = gameObject.AddComponent<AudioSource>();
                    sources[i].playOnAwake = false;
                }
            }
            bgm = gameObject.AddComponent<AudioSource>();
            bgm.loop = true;
            bgm.volume = 0.20f;
            Muted = PlayerPrefs.GetInt("angels.muted", 0) == 1;
            AudioListener.volume = Muted ? 0f : 0.78f;
        }

        void Awake()
        {
            EnsureInitialized();
        }

        AudioClip GetClip(string name)
        {
            if (!clips.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("AngelsAndDemons/Audio/" + name);
                clips[name] = clip;
            }
            return clip;
        }

        public void SetMusic(bool inBattle)
        {
            EnsureInitialized();
            var clip = GetClip(inBattle ? "bgm_battle" : "bgm_harbor");
            if (clip != null && bgm != null && bgm.clip != clip)
            {
                bgm.clip = clip;
                bgm.Play();
            }
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            AudioListener.volume = Muted ? 0f : 0.78f;
            PlayerPrefs.SetInt("angels.muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Play(string name, float volume = 0.5f, float pitch = 1.0f)
        {
            var clip = GetClip(name);
            if (clip == null) return;
            var s = sources[nextSource++ % sources.Length];
            s.clip = clip;
            s.volume = volume;
            s.pitch = pitch;
            s.Play();
        }

        public void PlayCue(BattleCue cue, DefenseHaptics haptics)
        {
            switch (cue.kind)
            {
                case CueKind.Spawn:
                    Play("summon", 0.42f, 1.08f);
                    haptics?.Trigger(1);
                    break;
                case CueKind.Hit:
                    if (Time.unscaledTime - lastHitTime > 0.07f)
                    {
                        lastHitTime = Time.unscaledTime;
                        Play("enemy_hit", 0.34f, Random.Range(0.92f, 1.12f));
                    }
                    break;
                case CueKind.Critical:
                    Play("attack_slash", 0.55f, 0.95f);
                    haptics?.Trigger(2);
                    break;
                case CueKind.Heal:
                    Play("magic", 0.38f, 1.2f);
                    break;
                case CueKind.Skill:
                    Play("haki", 0.65f, 0.95f);
                    Play("merge", 0.52f, 1.05f);
                    haptics?.Trigger(3);
                    break;
                case CueKind.EconomyUp:
                    Play("research", 0.55f, 1.1f);
                    haptics?.Trigger(1);
                    break;
                case CueKind.WaveWarn:
                    Play("boss_spawn", 0.62f, 1.0f);
                    haptics?.Trigger(3);
                    break;
                case CueKind.Victory:
                    Play("victory", 0.7f, 1.0f);
                    haptics?.Trigger(4);
                    break;
                case CueKind.Defeat:
                    Play("defeat", 0.65f, 1.0f);
                    haptics?.Trigger(5);
                    break;
            }
        }
    }
}
