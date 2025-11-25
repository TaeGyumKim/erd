using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 호흡 시스템
    /// 긴장, 달리기 상태에 따른 호흡 사운드
    ///
    /// 사용법:
    /// 1. 플레이어에 추가
    /// 2. 호흡 사운드 클립 설정
    /// </summary>
    public class BreathingSystem : MonoBehaviour
    {
        [Header("Audio Sources")]
        [Tooltip("호흡 오디오 소스")]
        public AudioSource breathingAudioSource;

        [Header("Breathing Sounds")]
        public AudioClip normalBreathing;
        public AudioClip heavyBreathing;
        public AudioClip scaredBreathing;
        public AudioClip exhaustedBreathing;
        public AudioClip recoveringBreathing;

        [Header("Settings")]
        [Tooltip("호흡 볼륨")]
        [Range(0, 1)]
        public float breathVolume = 0.5f;

        [Tooltip("스태미나 임계값 (이 이하면 지친 호흡)")]
        [Range(0, 1)]
        public float exhaustedThreshold = 0.2f;

        [Tooltip("호흡 전환 속도")]
        public float transitionSpeed = 2f;

        [Header("Heartbeat Integration")]
        [Tooltip("심장박동 효과와 연동")]
        public bool syncWithHeartbeat = true;

        public enum BreathState
        {
            Normal,     // 평상시
            Heavy,      // 달리는 중
            Scared,     // 무서운 상황
            Exhausted,  // 스태미나 바닥
            Recovering  // 회복 중
        }

        public BreathState CurrentState { get; private set; }

        private VRPlayer vrPlayer;
        private HeartbeatEffect heartbeatEffect;
        private float targetVolume;
        private AudioClip currentClip;
        private bool wasExhausted;

        private void Awake()
        {
            if (breathingAudioSource == null)
            {
                breathingAudioSource = gameObject.AddComponent<AudioSource>();
                breathingAudioSource.spatialBlend = 0; // 2D
                breathingAudioSource.loop = true;
                breathingAudioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            vrPlayer = GetComponent<VRPlayer>();
            if (vrPlayer == null)
            {
                vrPlayer = VRPlayer.Instance;
            }

            if (syncWithHeartbeat)
            {
                heartbeatEffect = FindObjectOfType<HeartbeatEffect>();
            }

            // 기본 호흡 시작
            SetBreathState(BreathState.Normal);
        }

        private void Update()
        {
            if (vrPlayer == null) return;

            UpdateBreathState();
            UpdateAudio();
        }

        private void UpdateBreathState()
        {
            BreathState newState = BreathState.Normal;

            // 숨어있으면 조용히
            if (vrPlayer.IsHiding)
            {
                targetVolume = 0.1f;
                return;
            }

            // 스태미나 체크
            float staminaRatio = vrPlayer.currentStamina / vrPlayer.maxStamina;

            if (staminaRatio <= exhaustedThreshold)
            {
                newState = BreathState.Exhausted;
                wasExhausted = true;
            }
            else if (wasExhausted && staminaRatio < 0.5f)
            {
                newState = BreathState.Recovering;
            }
            else if (vrPlayer.IsSprinting)
            {
                newState = BreathState.Heavy;
                wasExhausted = false;
            }
            else
            {
                wasExhausted = false;

                // 심장박동 연동으로 공포 상태 확인
                if (syncWithHeartbeat && heartbeatEffect != null)
                {
                    if (heartbeatEffect.currentBPM > 100)
                    {
                        newState = BreathState.Scared;
                    }
                }
            }

            if (newState != CurrentState)
            {
                SetBreathState(newState);
            }
        }

        private void SetBreathState(BreathState state)
        {
            CurrentState = state;
            AudioClip newClip = GetClipForState(state);
            float newVolume = GetVolumeForState(state);

            if (newClip != currentClip && newClip != null)
            {
                currentClip = newClip;
                breathingAudioSource.clip = newClip;
                breathingAudioSource.Play();
            }

            targetVolume = newVolume;

            Debug.Log($"[BreathingSystem] 호흡 상태: {state}");
        }

        private AudioClip GetClipForState(BreathState state)
        {
            switch (state)
            {
                case BreathState.Normal:
                    return normalBreathing;
                case BreathState.Heavy:
                    return heavyBreathing;
                case BreathState.Scared:
                    return scaredBreathing;
                case BreathState.Exhausted:
                    return exhaustedBreathing;
                case BreathState.Recovering:
                    return recoveringBreathing;
                default:
                    return normalBreathing;
            }
        }

        private float GetVolumeForState(BreathState state)
        {
            switch (state)
            {
                case BreathState.Normal:
                    return breathVolume * 0.3f;
                case BreathState.Heavy:
                    return breathVolume * 0.7f;
                case BreathState.Scared:
                    return breathVolume * 0.8f;
                case BreathState.Exhausted:
                    return breathVolume * 1f;
                case BreathState.Recovering:
                    return breathVolume * 0.6f;
                default:
                    return breathVolume * 0.3f;
            }
        }

        private void UpdateAudio()
        {
            // 볼륨 부드럽게 전환
            breathingAudioSource.volume = Mathf.Lerp(
                breathingAudioSource.volume,
                targetVolume,
                Time.deltaTime * transitionSpeed
            );
        }

        /// <summary>
        /// 공포 호흡 강제 발동
        /// </summary>
        public void TriggerScaredBreathing(float duration = 3f)
        {
            SetBreathState(BreathState.Scared);
            Invoke(nameof(ResetToNormal), duration);
        }

        /// <summary>
        /// 호흡 정상화
        /// </summary>
        public void ResetToNormal()
        {
            SetBreathState(BreathState.Normal);
        }

        /// <summary>
        /// 호흡 일시 정지 (숨 참기)
        /// </summary>
        public void HoldBreath()
        {
            targetVolume = 0;
            breathingAudioSource.Pause();
        }

        /// <summary>
        /// 호흡 재개
        /// </summary>
        public void ResumeBreathing()
        {
            breathingAudioSource.UnPause();
        }

        /// <summary>
        /// 볼륨 설정
        /// </summary>
        public void SetVolume(float volume)
        {
            breathVolume = Mathf.Clamp01(volume);
        }
    }
}
