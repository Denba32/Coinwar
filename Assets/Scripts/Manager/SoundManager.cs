using StockGame.Scripts.Sounds;
using StockGame.Scripts.Define;
using Cysharp.Threading.Tasks;
using UnityEngine.Audio;
using Denba.Common;
using UnityEngine;
using System.Threading;

namespace StockGame.Scripts.Manager
{
    public class SoundManager : MonoSingleton<SoundManager>
    {
        #region CONSTANTS
        private const string MIXER_PATH = "Mixer/MainMixer";
        #endregion CONSTANTS

        private AudioMixer audioMixer;

        // BGM
        [SerializeField] private SoundObject bgmAudio;

        // SFX
        [SerializeField] private SoundObject footStepAudio;
        [SerializeField] private SoundObject skillAudio;
        [SerializeField] private SoundObject uiAudio;
        [SerializeField] private SoundObject missionAudio;

        public override void Initialize()
        {
            base.Initialize();
            var token = Managers.Token.GetToken(this, nameof(InitAsync));
            InitAsync(token).Forget();
        }

        private async UniTask InitAsync(CancellationToken token)
        {
            await UniTask.Yield(cancellationToken:token); // AudioMixer 볼륨 미반영 방지를 위한 1Frame 대기
            // Mixer 데이터 가져옴
            audioMixer = await Managers.Resource.LoadAsync<AudioMixer>(MIXER_PATH, ResourceDirectory.Sounds);
            bgmAudio?.Initialize(GetAudioMixerGroup(SoundType.BGM));
            footStepAudio?.Initialize(GetAudioMixerGroup(SoundType.SFX, SoundEffectType.FootStep));
            skillAudio?.Initialize(GetAudioMixerGroup(SoundType.SFX, SoundEffectType.Skill));
            uiAudio?.Initialize(GetAudioMixerGroup(SoundType.SFX, SoundEffectType.UI));
            missionAudio?.Initialize(GetAudioMixerGroup(SoundType.SFX, SoundEffectType.Mission));

            SetVolume(SoundType.Master, Managers.ConfigData.MasterVolume);
            SetVolume(SoundType.BGM, Managers.ConfigData.BgmVolume);
            SetVolume(SoundType.SFX, Managers.ConfigData.SfxVolume);
        }

        private SoundObject CreateSoundObject(Transform root = null)
        {
            GameObject go = new GameObject { name = "SoundObject" };

            if (root == null) go.transform.SetParent(transform);
            else go.transform.SetParent(root);

            SoundObject so = go.AddComponent<SoundObject>();
            return so;
        }

        public void PlayBgm(string path, bool isLoop = true)
        {
            string _path = $"BGM/{path}";
            var clip = ResourceManager.Instance.Load<AudioClip>(_path, ResourceDirectory.Sounds);

            if (clip == null)
            {
                Debug.Log($"{path} is null");
                return;
            }

            PlayBgm(clip, isLoop);
        }
        public void PlayBgm(AudioClip clip, bool isLoop = true)
        {
            if (clip == null || bgmAudio == null)
            {
                Debug.Log("Clip or BgmAudio is Null");
                return;
            }

            if (bgmAudio == null)
            {
                bgmAudio = CreateSoundObject();
                bgmAudio?.Initialize(GetAudioMixerGroup(SoundType.BGM));
            }

            bgmAudio?.PlaySound(clip, SoundType.BGM);
        }

        public void PlaySound(string path, SoundType soundType, SoundEffectType soundEffectType = SoundEffectType.None, Vector3 position = default)
        {
            if (string.IsNullOrEmpty(path)) return;
            var clip = ResourceManager.Instance.Load<AudioClip>(path, ResourceDirectory.Sounds);

            if (clip == null)
            {
                Debug.Log($"{path} is null");
                return;
            }
            
            switch(soundType)
            {
                case SoundType.BGM: PlayBgm(clip); break;
                case SoundType.SFX: PlaySE(clip, soundType, soundEffectType); break;
                default: return;
            }

            //if (soundType == SoundType.SFX_3D)
            //{
            //    PlaySound3D(clip, position);
            //    return;
            //}
        }

        public void PlaySound(AudioClip clip, SoundType soundType)
        {
            if (clip == null)
            {
                Debug.Log("Clip is None");
                return;
            }

            if (soundType == SoundType.BGM)
            {
                bgmAudio?.PlaySound(clip, soundType);
            }
        }

        public void PlaySE(AudioClip clip, SoundType soundType, SoundEffectType soundEffectType)
        {
            if (clip == null) return;
            switch(soundEffectType)
            {
                case SoundEffectType.FootStep: footStepAudio?.PlaySound(clip, soundType); break;
                case SoundEffectType.UI: uiAudio?.PlaySound(clip, soundType); break;
                case SoundEffectType.Skill: skillAudio?.PlaySound(clip, soundType); break;
                case SoundEffectType.Mission: missionAudio?.PlaySound(clip, soundType); break;
            }
        }
        public void PlaySound3D(AudioClip clip, Vector3 position){}

        public void StopBgm()
        {
            if (bgmAudio == null) return;
            bgmAudio?.StopSound();
        }

        public void StopSE(SoundEffectType soundEffectType)
        {
            switch(soundEffectType)
            {
                case SoundEffectType.FootStep: footStepAudio?.StopSound(); break;
                case SoundEffectType.Skill: skillAudio?.StopSound(); break;
                case SoundEffectType.UI: uiAudio?.StopSound(); break;
                case SoundEffectType.Mission: missionAudio?.StopSound(); break;
            }
        }

        private AudioMixerGroup GetAudioMixerGroup(SoundType type, SoundEffectType soundEffectType = SoundEffectType.None)
        {
            string name = type.ToString();
            if(type == SoundType.SFX)
            {
                name = soundEffectType.ToString();
            }
            return audioMixer.FindMatchingGroups(name)[0];
        }

        public void SetVolume(SoundType type, float value)
        {
            if(audioMixer == null)
            {
                Debug.Log("AudioMixer is Null");
                return;
            }

            SaveVolume(type, value);

            float db = LinearToDb(value);
            Debug.Log($"Mixer에 적용되는 수치 : {db}");
            audioMixer.SetFloat(type.ToString(), db);
        }

        public void SaveVolume(SoundType type, float value)
        {
            Debug.Log($"Save Volume : {value}");
            Managers.ConfigData.SetVolume(value, type);
        }

        private float LinearToDb(float value)
        {
            if (value <= 0f) return -80f;
            return Mathf.Log10(value) * 20f;
        }

        public override void Clear()
        {
            base.Clear();
            StopBgm();
        }

        void OnDestroy()
        {
            Clear();
            audioMixer = null;
            bgmAudio = null;
            Managers.Token.CancelAll(this);
        }
    }
}