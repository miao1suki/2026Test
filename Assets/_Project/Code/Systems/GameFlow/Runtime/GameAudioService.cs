using System.Collections;
using UnityEngine;

namespace Project.GameFlow
{
    [DisallowMultipleComponent]
    public sealed class GameAudioService : MonoBehaviour, IGameSystemService
    {
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource soundEffectSource;

        public int InitializationOrder => 0;
        public bool IsInitialized { get; private set; }
        public AudioSource MusicSource => musicSource;
        public AudioSource SoundEffectSource => soundEffectSource;

        public void Configure(AudioSource music, AudioSource soundEffects)
        {
            musicSource = music;
            soundEffectSource = soundEffects;
        }

        public IEnumerator Initialize()
        {
            EnsureSources();
            IsInitialized = true;
            yield break;
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            EnsureSources();
            musicSource.clip = clip;
            musicSource.loop = loop;
            if (clip != null)
            {
                musicSource.Play();
            }
        }

        public void StopMusic()
        {
            musicSource?.Stop();
        }

        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
            {
                return;
            }

            EnsureSources();
            soundEffectSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private void Awake()
        {
            EnsureSources();
        }

        private void EnsureSources()
        {
            if (musicSource == null)
            {
                GameObject music = new GameObject("Music");
                music.transform.SetParent(transform, false);
                musicSource = music.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            if (soundEffectSource == null)
            {
                GameObject soundEffects = new GameObject("SoundEffects");
                soundEffects.transform.SetParent(transform, false);
                soundEffectSource = soundEffects.AddComponent<AudioSource>();
                soundEffectSource.playOnAwake = false;
            }
        }
    }
}
