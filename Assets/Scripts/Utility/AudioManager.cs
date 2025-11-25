using UnityEngine;
using System.Collections.Generic;

namespace VRGame
{
    /// <summary>
    /// 오디오 재생을 관리하는 매니저
    /// BGM, 효과음 등을 관리
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [Tooltip("BGM 재생용")]
        public AudioSource bgmSource;

        [Tooltip("효과음 재생용")]
        public AudioSource sfxSource;

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Range(0f, 1f)]
        public float bgmVolume = 0.5f;

        [Range(0f, 1f)]
        public float sfxVolume = 1f;

        [Header("Audio Clips")]
        [Tooltip("BGM 클립들")]
        public List<AudioClip> bgmClips = new List<AudioClip>();

        [Tooltip("효과음 클립들")]
        public List<AudioClip> sfxClips = new List<AudioClip>();

        private void Awake()
        {
            // 싱글톤 패턴
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupAudioSources();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetupAudioSources()
        {
            // BGM 소스 설정
            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("BGM Source");
                bgmObj.transform.SetParent(transform);
                bgmSource = bgmObj.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            // SFX 소스 설정
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }

            UpdateVolumes();
        }

        /// <summary>
        /// BGM 재생
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;

            bgmSource.clip = clip;
            bgmSource.Play();
        }

        /// <summary>
        /// BGM 재생 (인덱스)
        /// </summary>
        public void PlayBGM(int index)
        {
            if (index >= 0 && index < bgmClips.Count)
            {
                PlayBGM(bgmClips[index]);
            }
        }

        /// <summary>
        /// BGM 정지
        /// </summary>
        public void StopBGM()
        {
            bgmSource.Stop();
        }

        /// <summary>
        /// 효과음 재생
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;

            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }

        /// <summary>
        /// 효과음 재생 (인덱스)
        /// </summary>
        public void PlaySFX(int index)
        {
            if (index >= 0 && index < sfxClips.Count)
            {
                PlaySFX(sfxClips[index]);
            }
        }

        /// <summary>
        /// 특정 위치에서 3D 효과음 재생
        /// </summary>
        public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume);
        }

        /// <summary>
        /// 볼륨 업데이트
        /// </summary>
        public void UpdateVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = bgmVolume * masterVolume;
            }
        }

        /// <summary>
        /// 마스터 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        /// <summary>
        /// BGM 볼륨 설정
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        /// <summary>
        /// SFX 볼륨 설정
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
    }
}
