using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 호러 사운드 매니저
    /// 공포 분위기 조성을 위한 사운드 시스템
    /// - 환경음, 발소리, 속삭임, 경고음 등
    /// </summary>
    public class HorrorSoundManager : MonoBehaviour
    {
        public static HorrorSoundManager Instance { get; private set; }

        [Header("Ambient Audio")]
        [Tooltip("배경 음악 소스")]
        public AudioSource ambientSource;

        [Tooltip("평상시 배경음")]
        public AudioClip normalAmbient;

        [Tooltip("긴장 배경음")]
        public AudioClip tensionAmbient;

        [Tooltip("추격 배경음")]
        public AudioClip chaseAmbient;

        [Header("Effect Audio")]
        [Tooltip("효과음 소스")]
        public AudioSource effectSource;

        [Header("3D Sound Sources")]
        [Tooltip("발소리용 오디오 소스")]
        public AudioSource footstepSource;

        [Header("Footstep Sounds")]
        public AudioClip[] footstepSounds;
        public AudioClip[] runFootstepSounds;
        public AudioClip[] crouchFootstepSounds;

        [Header("Horror Sounds")]
        public AudioClip[] whisperSounds;
        public AudioClip[] creekyDoorSounds;
        public AudioClip[] breathingSounds;
        public AudioClip heartbeatSound;
        public AudioClip jumpScareSound;

        [Header("UI Sounds")]
        public AudioClip itemPickupSound;
        public AudioClip objectiveCompleteSound;
        public AudioClip warningSound;
        public AudioClip timerTickSound;

        [Header("Settings")]
        [Tooltip("발소리 간격")]
        public float footstepInterval = 0.5f;

        [Tooltip("달리기 발소리 간격")]
        public float runFootstepInterval = 0.3f;

        [Tooltip("속삭임 간격 범위")]
        public Vector2 whisperIntervalRange = new Vector2(20f, 60f);

        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Range(0f, 1f)]
        public float ambientVolume = 0.5f;

        [Range(0f, 1f)]
        public float effectVolume = 1f;

        [Range(0f, 1f)]
        public float footstepVolume = 0.6f;

        private float lastFootstepTime;
        private float nextWhisperTime;
        private bool isChasing = false;
        private Coroutine heartbeatCoroutine;

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

        private void Start()
        {
            // 배경음 시작
            PlayAmbient(normalAmbient);

            // 랜덤 속삭임 예약
            ScheduleNextWhisper();

            // 이벤트 연결
            ConnectEvents();
        }

        private void SetupAudioSources()
        {
            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.SetParent(transform);
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.spatialBlend = 0f;
            }

            if (effectSource == null)
            {
                GameObject effectObj = new GameObject("EffectSource");
                effectObj.transform.SetParent(transform);
                effectSource = effectObj.AddComponent<AudioSource>();
                effectSource.spatialBlend = 0f;
            }

            if (footstepSource == null)
            {
                GameObject footstepObj = new GameObject("FootstepSource");
                footstepObj.transform.SetParent(transform);
                footstepSource = footstepObj.AddComponent<AudioSource>();
                footstepSource.spatialBlend = 1f;
            }
        }

        private void ConnectEvents()
        {
            // 킬러 이벤트 연결
            var killers = FindObjectsOfType<KillerAI>();
            foreach (var killer in killers)
            {
                killer.OnPlayerSpotted.AddListener(OnKillerSpottedPlayer);
                killer.OnPlayerLost.AddListener(OnKillerLostPlayer);
            }

            // 게임 매니저 이벤트 연결
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.OnGameOver.AddListener(StopAllSounds);
                HorrorGameManager.Instance.OnVictory.AddListener(StopAllSounds);
            }
        }

        private void Update()
        {
            // 속삭임 체크
            if (Time.time >= nextWhisperTime)
            {
                PlayRandomWhisper();
                ScheduleNextWhisper();
            }

            // 플레이어 발소리
            UpdatePlayerFootsteps();
        }

        #region 배경음

        /// <summary>
        /// 배경음 재생
        /// </summary>
        public void PlayAmbient(AudioClip clip)
        {
            if (clip == null || ambientSource == null) return;

            ambientSource.clip = clip;
            ambientSource.volume = ambientVolume * masterVolume;
            ambientSource.Play();
        }

        /// <summary>
        /// 긴장 모드 시작
        /// </summary>
        public void StartTensionMode()
        {
            if (tensionAmbient != null)
            {
                StartCoroutine(CrossfadeAmbient(tensionAmbient, 2f));
            }
        }

        /// <summary>
        /// 추격 모드 시작
        /// </summary>
        public void StartChaseMode()
        {
            isChasing = true;

            if (chaseAmbient != null)
            {
                StartCoroutine(CrossfadeAmbient(chaseAmbient, 0.5f));
            }

            // 심장 박동 시작
            StartHeartbeat();
        }

        /// <summary>
        /// 평상시 모드로 복귀
        /// </summary>
        public void ReturnToNormalMode()
        {
            isChasing = false;

            if (normalAmbient != null)
            {
                StartCoroutine(CrossfadeAmbient(normalAmbient, 3f));
            }

            // 심장 박동 중지
            StopHeartbeat();
        }

        private IEnumerator CrossfadeAmbient(AudioClip newClip, float duration)
        {
            float startVolume = ambientSource.volume;

            // 페이드 아웃
            float t = 0f;
            while (t < duration / 2f)
            {
                t += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(startVolume, 0f, t / (duration / 2f));
                yield return null;
            }

            // 클립 변경
            ambientSource.clip = newClip;
            ambientSource.Play();

            // 페이드 인
            t = 0f;
            while (t < duration / 2f)
            {
                t += Time.deltaTime;
                ambientSource.volume = Mathf.Lerp(0f, ambientVolume * masterVolume, t / (duration / 2f));
                yield return null;
            }
        }

        #endregion

        #region 발소리

        private void UpdatePlayerFootsteps()
        {
            if (VRPlayer.Instance == null) return;

            // 숨어있거나 잡혔으면 발소리 안남
            if (VRPlayer.Instance.currentState == VRPlayer.PlayerState.Hiding ||
                VRPlayer.Instance.currentState == VRPlayer.PlayerState.Caught)
            {
                return;
            }

            // 이동 중인지 확인 (간단히 위치 변화로)
            // 실제로는 VRPlayer에서 이동 이벤트를 받아야 함
        }

        /// <summary>
        /// 발소리 재생
        /// </summary>
        public void PlayFootstep(bool isRunning = false, bool isCrouching = false)
        {
            float interval = isRunning ? runFootstepInterval : footstepInterval;

            if (Time.time - lastFootstepTime < interval) return;

            lastFootstepTime = Time.time;

            AudioClip[] clips = isCrouching ? crouchFootstepSounds :
                               (isRunning ? runFootstepSounds : footstepSounds);

            if (clips != null && clips.Length > 0)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                float volume = isCrouching ? footstepVolume * 0.3f :
                              (isRunning ? footstepVolume * 1.5f : footstepVolume);

                PlaySound(clip, volume * masterVolume);
            }
        }

        #endregion

        #region 공포 효과음

        /// <summary>
        /// 랜덤 속삭임 재생
        /// </summary>
        public void PlayRandomWhisper()
        {
            if (whisperSounds != null && whisperSounds.Length > 0)
            {
                AudioClip clip = whisperSounds[Random.Range(0, whisperSounds.Length)];
                PlaySound(clip, 0.3f * masterVolume);
            }
        }

        private void ScheduleNextWhisper()
        {
            nextWhisperTime = Time.time + Random.Range(whisperIntervalRange.x, whisperIntervalRange.y);
        }

        /// <summary>
        /// 삐걱거리는 문 소리
        /// </summary>
        public void PlayCreekyDoor()
        {
            if (creekyDoorSounds != null && creekyDoorSounds.Length > 0)
            {
                AudioClip clip = creekyDoorSounds[Random.Range(0, creekyDoorSounds.Length)];
                PlaySound(clip, effectVolume * masterVolume);
            }
        }

        /// <summary>
        /// 숨소리 재생
        /// </summary>
        public void PlayBreathing()
        {
            if (breathingSounds != null && breathingSounds.Length > 0)
            {
                AudioClip clip = breathingSounds[Random.Range(0, breathingSounds.Length)];
                PlaySound(clip, 0.5f * masterVolume);
            }
        }

        /// <summary>
        /// 심장 박동 시작
        /// </summary>
        public void StartHeartbeat()
        {
            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
            }
            heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
        }

        /// <summary>
        /// 심장 박동 중지
        /// </summary>
        public void StopHeartbeat()
        {
            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
                heartbeatCoroutine = null;
            }
        }

        private IEnumerator HeartbeatLoop()
        {
            while (true)
            {
                if (heartbeatSound != null)
                {
                    PlaySound(heartbeatSound, 0.7f * masterVolume);
                }
                yield return new WaitForSeconds(isChasing ? 0.5f : 1f);
            }
        }

        /// <summary>
        /// 점프스케어 사운드
        /// </summary>
        public void PlayJumpScare()
        {
            if (jumpScareSound != null)
            {
                PlaySound(jumpScareSound, 1f * masterVolume);
            }
        }

        #endregion

        #region UI 사운드

        public void PlayItemPickup()
        {
            if (itemPickupSound != null)
            {
                PlaySound(itemPickupSound, effectVolume * masterVolume);
            }
        }

        public void PlayObjectiveComplete()
        {
            if (objectiveCompleteSound != null)
            {
                PlaySound(objectiveCompleteSound, effectVolume * masterVolume);
            }
        }

        public void PlayWarning()
        {
            if (warningSound != null)
            {
                PlaySound(warningSound, effectVolume * masterVolume);
            }
        }

        public void PlayTimerTick()
        {
            if (timerTickSound != null)
            {
                PlaySound(timerTickSound, 0.3f * masterVolume);
            }
        }

        #endregion

        #region 이벤트 핸들러

        private void OnKillerSpottedPlayer()
        {
            StartChaseMode();
            PlayJumpScare();
        }

        private void OnKillerLostPlayer()
        {
            StartTensionMode();
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 사운드 재생
        /// </summary>
        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null || effectSource == null) return;
            effectSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 3D 위치에서 사운드 재생
        /// </summary>
        public void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null) return;
            AudioSource.PlayClipAtPoint(clip, position, volume * masterVolume);
        }

        /// <summary>
        /// 모든 사운드 중지
        /// </summary>
        public void StopAllSounds()
        {
            ambientSource?.Stop();
            effectSource?.Stop();
            StopHeartbeat();
        }

        /// <summary>
        /// 볼륨 설정
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);

            if (ambientSource != null)
            {
                ambientSource.volume = ambientVolume * masterVolume;
            }
        }

        #endregion
    }
}
