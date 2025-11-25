using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 공포 분위기 오디오 관리
    /// 배경음악, 환경음, 긴장감 조절
    ///
    /// 사용법:
    /// 1. 빈 게임오브젝트에 추가
    /// 2. 오디오 클립들 설정
    /// </summary>
    public class HorrorAudioManager : MonoBehaviour
    {
        public static HorrorAudioManager Instance { get; private set; }

        [Header("Background Music")]
        [Tooltip("평상시 배경음악")]
        public AudioClip normalAmbient;

        [Tooltip("긴장 상황 음악")]
        public AudioClip tenseMusic;

        [Tooltip("추격 상황 음악")]
        public AudioClip chaseMusic;

        [Tooltip("배경음악 볼륨")]
        [Range(0f, 1f)]
        public float musicVolume = 0.5f;

        [Header("Random Sounds")]
        [Tooltip("랜덤 환경음 목록")]
        public AudioClip[] randomSounds;

        [Tooltip("랜덤 사운드 최소 간격")]
        public float minRandomInterval = 30f;

        [Tooltip("랜덤 사운드 최대 간격")]
        public float maxRandomInterval = 120f;

        [Tooltip("랜덤 사운드 볼륨")]
        [Range(0f, 1f)]
        public float randomSoundVolume = 0.3f;

        [Header("Heartbeat")]
        [Tooltip("심장박동 소리")]
        public AudioClip heartbeatSound;

        [Tooltip("심장박동 볼륨")]
        [Range(0f, 1f)]
        public float heartbeatVolume = 0.5f;

        [Header("Breathing")]
        [Tooltip("숨소리")]
        public AudioClip breathingSound;

        [Tooltip("숨소리 볼륨")]
        [Range(0f, 1f)]
        public float breathingVolume = 0.3f;

        public enum TensionLevel
        {
            Calm,       // 평온
            Uneasy,     // 불안
            Tense,      // 긴장
            Chase       // 추격
        }

        private AudioSource musicSource;
        private AudioSource ambientSource;
        private AudioSource heartbeatSource;
        private AudioSource breathingSource;
        private AudioSource sfxSource;

        private TensionLevel currentTension = TensionLevel.Calm;
        private float randomSoundTimer;
        private Coroutine tensionCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupAudioSources();
        }

        private void SetupAudioSources()
        {
            // 음악 소스
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.playOnAwake = false;

            // 환경음 소스
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.volume = musicVolume;
            ambientSource.playOnAwake = false;

            // 심장박동 소스
            heartbeatSource = gameObject.AddComponent<AudioSource>();
            heartbeatSource.loop = true;
            heartbeatSource.volume = 0;
            heartbeatSource.playOnAwake = false;

            // 숨소리 소스
            breathingSource = gameObject.AddComponent<AudioSource>();
            breathingSource.loop = true;
            breathingSource.volume = 0;
            breathingSource.playOnAwake = false;

            // 효과음 소스
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        private void Start()
        {
            // 기본 환경음 시작
            if (normalAmbient != null)
            {
                ambientSource.clip = normalAmbient;
                ambientSource.Play();
            }

            // 심장박동 준비
            if (heartbeatSound != null)
            {
                heartbeatSource.clip = heartbeatSound;
                heartbeatSource.Play();
            }

            // 숨소리 준비
            if (breathingSound != null)
            {
                breathingSource.clip = breathingSound;
                breathingSource.Play();
            }

            // 랜덤 사운드 타이머 시작
            randomSoundTimer = Random.Range(minRandomInterval, maxRandomInterval);
        }

        private void Update()
        {
            UpdateRandomSounds();
        }

        private void UpdateRandomSounds()
        {
            if (randomSounds == null || randomSounds.Length == 0) return;

            randomSoundTimer -= Time.deltaTime;
            if (randomSoundTimer <= 0)
            {
                PlayRandomSound();
                randomSoundTimer = Random.Range(minRandomInterval, maxRandomInterval);
            }
        }

        /// <summary>
        /// 랜덤 환경음 재생
        /// </summary>
        public void PlayRandomSound()
        {
            if (randomSounds.Length == 0) return;

            var clip = randomSounds[Random.Range(0, randomSounds.Length)];
            sfxSource.PlayOneShot(clip, randomSoundVolume);
            Debug.Log($"[HorrorAudioManager] 랜덤 사운드 재생: {clip.name}");
        }

        /// <summary>
        /// 긴장 레벨 설정
        /// </summary>
        public void SetTension(TensionLevel level)
        {
            if (currentTension == level) return;

            currentTension = level;

            if (tensionCoroutine != null)
            {
                StopCoroutine(tensionCoroutine);
            }

            tensionCoroutine = StartCoroutine(TransitionTension(level));
            Debug.Log($"[HorrorAudioManager] 긴장도 변경: {level}");
        }

        private IEnumerator TransitionTension(TensionLevel level)
        {
            float duration = 2f;
            float elapsed = 0;

            float startHeartbeat = heartbeatSource.volume;
            float startBreathing = breathingSource.volume;

            float targetHeartbeat = 0;
            float targetBreathing = 0;
            AudioClip targetMusic = null;

            switch (level)
            {
                case TensionLevel.Calm:
                    targetHeartbeat = 0;
                    targetBreathing = 0;
                    targetMusic = null;
                    break;

                case TensionLevel.Uneasy:
                    targetHeartbeat = heartbeatVolume * 0.3f;
                    targetBreathing = breathingVolume * 0.3f;
                    targetMusic = null;
                    break;

                case TensionLevel.Tense:
                    targetHeartbeat = heartbeatVolume * 0.7f;
                    targetBreathing = breathingVolume * 0.7f;
                    targetMusic = tenseMusic;
                    break;

                case TensionLevel.Chase:
                    targetHeartbeat = heartbeatVolume;
                    targetBreathing = breathingVolume;
                    targetMusic = chaseMusic;
                    break;
            }

            // 음악 전환
            if (targetMusic != null && musicSource.clip != targetMusic)
            {
                musicSource.clip = targetMusic;
                musicSource.Play();
            }
            else if (targetMusic == null)
            {
                musicSource.Stop();
            }

            // 볼륨 전환
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                heartbeatSource.volume = Mathf.Lerp(startHeartbeat, targetHeartbeat, t);
                breathingSource.volume = Mathf.Lerp(startBreathing, targetBreathing, t);

                yield return null;
            }

            heartbeatSource.volume = targetHeartbeat;
            breathingSource.volume = targetBreathing;
        }

        /// <summary>
        /// 특정 위치에서 3D 사운드 재생
        /// </summary>
        public void PlaySoundAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        /// <summary>
        /// 2D 사운드 재생
        /// </summary>
        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            sfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 모든 오디오 정지
        /// </summary>
        public void StopAll()
        {
            musicSource.Stop();
            ambientSource.Stop();
            heartbeatSource.volume = 0;
            breathingSource.volume = 0;
        }

        /// <summary>
        /// 마스터 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = volume;
        }
    }
}
