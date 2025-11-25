using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace HorrorGame
{
    /// <summary>
    /// 심장박동 효과
    /// 위험할 때 시각/청각 효과로 긴장감 고조
    ///
    /// 사용법:
    /// 1. 플레이어 또는 매니저에 추가
    /// 2. SetDangerLevel()로 위험도 설정
    /// 3. heartbeatClip에 심장박동 사운드 추가
    /// </summary>
    public class HeartbeatEffect : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("심장박동 사운드")]
        public AudioClip heartbeatClip;

        [Tooltip("오디오 소스")]
        public AudioSource heartbeatAudioSource;

        [Tooltip("최소 볼륨")]
        [Range(0, 1)]
        public float minVolume = 0.1f;

        [Tooltip("최대 볼륨")]
        [Range(0, 1)]
        public float maxVolume = 0.8f;

        [Header("Heartbeat Rate")]
        [Tooltip("평온할 때 BPM")]
        public float calmBPM = 60f;

        [Tooltip("최대 긴장 시 BPM")]
        public float maxBPM = 140f;

        [Tooltip("현재 BPM")]
        public float currentBPM;

        [Header("Visual Effect")]
        [Tooltip("화면 가장자리 비네트 효과 (URP 필요)")]
        public bool useVignette = false;

        [Tooltip("최대 비네트 강도")]
        [Range(0, 1)]
        public float maxVignetteIntensity = 0.5f;

        [Tooltip("비네트 색상")]
        public Color vignetteColor = new Color(0.5f, 0, 0, 1);

        [Header("Controller Haptics")]
        [Tooltip("컨트롤러 진동 사용")]
        public bool useHaptics = true;

        [Tooltip("최대 진동 강도")]
        [Range(0, 1)]
        public float maxHapticIntensity = 0.3f;

        [Header("Danger Detection")]
        [Tooltip("자동 위험 감지")]
        public bool autoDetectDanger = true;

        [Tooltip("위험 감지 반경")]
        public float dangerDetectionRadius = 15f;

        [Tooltip("살인마 레이어")]
        public LayerMask enemyLayer;

        // 0 = 안전, 1 = 최대 위험
        private float dangerLevel = 0;
        private float targetDangerLevel = 0;
        private float heartbeatTimer;
#if UNITY_URP
        private Volume postProcessVolume;
        private Vignette vignette;
#endif
        private Quest3Controller[] controllers;
        private bool isPulsing;

        private void Awake()
        {
            // 오디오 소스 설정
            if (heartbeatAudioSource == null)
            {
                heartbeatAudioSource = gameObject.AddComponent<AudioSource>();
                heartbeatAudioSource.spatialBlend = 0; // 2D 사운드
                heartbeatAudioSource.loop = false;
                heartbeatAudioSource.playOnAwake = false;
            }

            currentBPM = calmBPM;
        }

        private void Start()
        {
#if UNITY_URP
            // Post Processing 찾기
            if (useVignette)
            {
                postProcessVolume = FindObjectOfType<Volume>();
                if (postProcessVolume != null)
                {
                    postProcessVolume.profile.TryGet(out vignette);
                }
            }
#endif

            // 컨트롤러 찾기
            if (useHaptics)
            {
                controllers = FindObjectsOfType<Quest3Controller>();
            }
        }

        private void Update()
        {
            if (autoDetectDanger)
            {
                DetectDanger();
            }

            // 부드럽게 위험도 변경
            dangerLevel = Mathf.Lerp(dangerLevel, targetDangerLevel, Time.deltaTime * 2f);

            // BPM 계산
            currentBPM = Mathf.Lerp(calmBPM, maxBPM, dangerLevel);

            // 심장박동 타이밍
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0 && dangerLevel > 0.1f)
            {
                PlayHeartbeat();
                heartbeatTimer = 60f / currentBPM;
            }

            // 비주얼 효과 업데이트
            UpdateVisuals();
        }

        private void DetectDanger()
        {
            if (VRPlayer.Instance == null) return;

            // 플레이어가 숨어있으면 안전
            if (VRPlayer.Instance.IsHiding)
            {
                targetDangerLevel = Mathf.Max(0, targetDangerLevel - Time.deltaTime);
                return;
            }

            // 주변 적 감지
            Collider[] enemies = Physics.OverlapSphere(
                VRPlayer.Instance.transform.position,
                dangerDetectionRadius,
                enemyLayer
            );

            if (enemies.Length == 0)
            {
                targetDangerLevel = 0;
                return;
            }

            // 가장 가까운 적과의 거리로 위험도 계산
            float closestDistance = float.MaxValue;
            foreach (var enemy in enemies)
            {
                float dist = Vector3.Distance(
                    VRPlayer.Instance.transform.position,
                    enemy.transform.position
                );
                closestDistance = Mathf.Min(closestDistance, dist);
            }

            // 거리에 반비례하는 위험도
            targetDangerLevel = 1 - (closestDistance / dangerDetectionRadius);
            targetDangerLevel = Mathf.Clamp01(targetDangerLevel);

            // 추적 중이면 위험도 증가
            var killer = enemies[0].GetComponent<KillerAI>();
            if (killer != null && killer.currentState == KillerAI.AIState.Chase)
            {
                targetDangerLevel = Mathf.Max(targetDangerLevel, 0.8f);
            }
        }

        private void PlayHeartbeat()
        {
            if (heartbeatClip == null) return;

            // 볼륨 설정
            float volume = Mathf.Lerp(minVolume, maxVolume, dangerLevel);
            heartbeatAudioSource.volume = volume;

            // 피치 조절 (빠를수록 높게)
            heartbeatAudioSource.pitch = Mathf.Lerp(0.9f, 1.2f, dangerLevel);

            heartbeatAudioSource.PlayOneShot(heartbeatClip);

            // 햅틱 펄스
            if (useHaptics && controllers != null)
            {
                float hapticIntensity = maxHapticIntensity * dangerLevel;
                foreach (var controller in controllers)
                {
                    if (controller != null)
                    {
                        controller.Vibrate(hapticIntensity, 0.1f);
                    }
                }
            }

            // 비네트 펄스
            if (useVignette)
            {
                StartCoroutine(VignettePulse());
            }
        }

        private System.Collections.IEnumerator VignettePulse()
        {
#if UNITY_URP
            if (vignette == null) yield break;
            if (isPulsing) yield break;

            isPulsing = true;

            float baseIntensity = maxVignetteIntensity * dangerLevel * 0.5f;
            float peakIntensity = maxVignetteIntensity * dangerLevel;

            // 펄스 업
            float duration = 0.1f;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                vignette.intensity.value = Mathf.Lerp(baseIntensity, peakIntensity, elapsed / duration);
                yield return null;
            }

            // 펄스 다운
            elapsed = 0;
            duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                vignette.intensity.value = Mathf.Lerp(peakIntensity, baseIntensity, elapsed / duration);
                yield return null;
            }

            isPulsing = false;
#else
            yield break;
#endif
        }

        private void UpdateVisuals()
        {
#if UNITY_URP
            if (!useVignette || vignette == null) return;

            // 기본 비네트 강도
            float baseIntensity = maxVignetteIntensity * dangerLevel * 0.5f;
            if (!isPulsing)
            {
                vignette.intensity.value = baseIntensity;
            }
            vignette.color.value = vignetteColor;
#endif
        }

        /// <summary>
        /// 위험도 수동 설정 (0~1)
        /// </summary>
        public void SetDangerLevel(float level)
        {
            targetDangerLevel = Mathf.Clamp01(level);
        }

        /// <summary>
        /// 자동 감지 활성화/비활성화
        /// </summary>
        public void SetAutoDetect(bool enabled)
        {
            autoDetectDanger = enabled;
        }

        /// <summary>
        /// 즉시 최대 위험 상태로 (점프스케어 등)
        /// </summary>
        public void TriggerMaxDanger(float duration = 2f)
        {
            StartCoroutine(MaxDangerSequence(duration));
        }

        private System.Collections.IEnumerator MaxDangerSequence(float duration)
        {
            float originalTarget = targetDangerLevel;
            targetDangerLevel = 1f;
            dangerLevel = 1f;

            yield return new WaitForSeconds(duration);

            targetDangerLevel = originalTarget;
        }
    }
}
