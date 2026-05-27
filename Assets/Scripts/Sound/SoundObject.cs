using StockGame.Scripts.Define;
using UnityEngine.Audio;
using UnityEngine;

namespace StockGame.Scripts.Sounds
{
    public class SoundObject : MonoBehaviour
    {
        [SerializeField] private AudioSource soundSource;

        public void Initialize(AudioMixerGroup group)
        {
            if (soundSource == null)
            {
                soundSource = gameObject.AddComponent<AudioSource>();
            }
            soundSource.outputAudioMixerGroup = group;
        }
        public void PlaySound(AudioClip clip, SoundType type)
        {
            if (clip == null) return;
            switch (type)
            {
                case SoundType.BGM:
                    if (soundSource.isPlaying) soundSource?.Stop();
                    soundSource.loop = true;
                    soundSource.clip = clip;
                    soundSource.Play();
                    break;
                case SoundType.SFX:
                    soundSource.loop = false;
                    soundSource.PlayOneShot(clip);
                    break;
            }
        }

        public void StopSound()
        {
            if (!soundSource.isPlaying) return;
            soundSource?.Stop();
        }
    }
}