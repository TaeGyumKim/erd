using UnityEngine;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 조명 깜빡임 효과
    /// 공포 분위기 연출에 사용
    ///
    /// 사용법:
    /// 1. Light 컴포넌트가 있는 오브젝트에 추가
    /// 2. flickerType으로 효과 선택
    /// </summary>
    public class LightFlicker : MonoBehaviour
    {
        [Header("Light Reference")]
        [Tooltip("깜빡일 조명 (비워두면 자동 찾기)")]
        public Light targetLight;

        [Header("Flicker Settings")]
        public FlickerType flickerType = FlickerType.Random;

        [Tooltip("깜빡임 활성화")]
        public bool flickerEnabled = true;

        [Tooltip("최소 밝기 (0~1)")]
        [Range(0, 1)]
        public float minIntensity = 0.2f;

        [Tooltip("최대 밝기 (0~1)")]
        [Range(0, 1)]
        public float maxIntensity = 1f;

        [Tooltip("깜빡임 속도")]
        public float flickerSpeed = 5f;

        [Header("Random Flicker")]
        [Tooltip("랜덤 깜빡임 최소 간격")]
        public float randomMinInterval = 0.05f;

        [Tooltip("랜덤 깜빡임 최대 간격")]
        public float randomMaxInterval = 0.2f;

        [Header("Pattern Flicker")]
        [Tooltip("패턴 (0=꺼짐, 1=켜짐): 예 '10101111'")]
        public string flickerPattern = "11101110";

        [Tooltip("패턴 한 단계 시간")]
        public float patternStepTime = 0.1f;

        [Header("Candle Effect")]
        [Tooltip("촛불 흔들림 강도")]
        [Range(0, 1)]
        public float candleIntensityVariation = 0.3f;

        [Tooltip("촛불 색상 변화")]
        public bool candleColorShift = true;

        [Header("Broken Light")]
        [Tooltip("고장난 조명 - 꺼질 확률")]
        [Range(0, 1)]
        public float breakChance = 0.3f;

        [Tooltip("고장 시 꺼짐 시간")]
        public float breakDuration = 2f;

        [Header("Audio")]
        [Tooltip("깜빡임 사운드")]
        public AudioClip flickerSound;

        [Tooltip("사운드 볼륨")]
        [Range(0, 1)]
        public float soundVolume = 0.3f;

        public enum FlickerType
        {
            None,       // 깜빡임 없음
            Random,     // 랜덤 깜빡임
            Pattern,    // 패턴 깜빡임
            Candle,     // 촛불 효과
            Broken,     // 고장난 조명
            Pulse       // 맥박처럼 천천히
        }

        private float originalIntensity;
        private Color originalColor;
        private AudioSource audioSource;
        private int patternIndex;
        private float timer;
        private bool isBroken;

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            if (targetLight != null)
            {
                originalIntensity = targetLight.intensity;
                originalColor = targetLight.color;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && flickerSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            if (targetLight == null || !flickerEnabled) return;

            switch (flickerType)
            {
                case FlickerType.Random:
                    UpdateRandomFlicker();
                    break;
                case FlickerType.Pattern:
                    UpdatePatternFlicker();
                    break;
                case FlickerType.Candle:
                    UpdateCandleFlicker();
                    break;
                case FlickerType.Broken:
                    UpdateBrokenFlicker();
                    break;
                case FlickerType.Pulse:
                    UpdatePulseFlicker();
                    break;
            }
        }

        private void UpdateRandomFlicker()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                float randomIntensity = Random.Range(minIntensity, maxIntensity);
                targetLight.intensity = originalIntensity * randomIntensity;
                timer = Random.Range(randomMinInterval, randomMaxInterval);

                // 크게 변할 때 사운드
                if (randomIntensity < 0.5f && flickerSound != null)
                {
                    PlayFlickerSound();
                }
            }
        }

        private void UpdatePatternFlicker()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                if (string.IsNullOrEmpty(flickerPattern)) return;

                char state = flickerPattern[patternIndex];
                targetLight.intensity = state == '1' ? originalIntensity * maxIntensity : originalIntensity * minIntensity;

                patternIndex = (patternIndex + 1) % flickerPattern.Length;
                timer = patternStepTime;
            }
        }

        private void UpdateCandleFlicker()
        {
            // Perlin 노이즈로 부드러운 깜빡임
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0);
            float intensity = Mathf.Lerp(1 - candleIntensityVariation, 1, noise);
            targetLight.intensity = originalIntensity * intensity;

            // 색상 변화 (주황색 쪽으로)
            if (candleColorShift)
            {
                float colorNoise = Mathf.PerlinNoise(Time.time * flickerSpeed * 0.5f, 100);
                Color warmColor = new Color(1f, 0.8f, 0.6f);
                targetLight.color = Color.Lerp(originalColor, warmColor, colorNoise * 0.3f);
            }
        }

        private void UpdateBrokenFlicker()
        {
            if (isBroken) return;

            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                if (Random.value < breakChance)
                {
                    StartCoroutine(BreakSequence());
                }
                timer = Random.Range(1f, 5f);
            }
        }

        private IEnumerator BreakSequence()
        {
            isBroken = true;

            // 깜빡임 후 꺼짐
            for (int i = 0; i < Random.Range(2, 5); i++)
            {
                targetLight.intensity = originalIntensity * maxIntensity;
                PlayFlickerSound();
                yield return new WaitForSeconds(0.05f);
                targetLight.intensity = originalIntensity * minIntensity;
                yield return new WaitForSeconds(0.05f);
            }

            // 꺼짐
            targetLight.intensity = 0;
            yield return new WaitForSeconds(breakDuration);

            // 다시 켜짐
            targetLight.intensity = originalIntensity;
            isBroken = false;
        }

        private void UpdatePulseFlicker()
        {
            float pulse = (Mathf.Sin(Time.time * flickerSpeed) + 1) / 2;
            targetLight.intensity = originalIntensity * Mathf.Lerp(minIntensity, maxIntensity, pulse);
        }

        private void PlayFlickerSound()
        {
            if (audioSource != null && flickerSound != null)
            {
                audioSource.volume = soundVolume;
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(flickerSound);
            }
        }

        /// <summary>
        /// 조명 강제로 끄기
        /// </summary>
        public void TurnOff()
        {
            if (targetLight != null)
            {
                targetLight.intensity = 0;
            }
            flickerEnabled = false;
        }

        /// <summary>
        /// 조명 강제로 켜기
        /// </summary>
        public void TurnOn()
        {
            if (targetLight != null)
            {
                targetLight.intensity = originalIntensity;
            }
            flickerEnabled = true;
        }

        /// <summary>
        /// 깜빡임 타입 변경
        /// </summary>
        public void SetFlickerType(FlickerType type)
        {
            flickerType = type;
        }

        /// <summary>
        /// 깜빡이며 끄기 (이벤트용)
        /// </summary>
        public void FlickerAndTurnOff(float duration = 1f)
        {
            StartCoroutine(FlickerOffSequence(duration));
        }

        private IEnumerator FlickerOffSequence(float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                targetLight.intensity = Random.value > 0.5f ? originalIntensity : 0;
                PlayFlickerSound();
                yield return new WaitForSeconds(0.05f);
                elapsed += 0.05f;
            }
            targetLight.intensity = 0;
            flickerEnabled = false;
        }
    }
}
