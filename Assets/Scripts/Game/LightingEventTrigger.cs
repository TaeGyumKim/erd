using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 조명 이벤트 트리거
    /// 특정 이벤트 시 조명을 켜거나 끄는 연출
    ///
    /// 사용법:
    /// 1. 빈 오브젝트에 추가
    /// 2. lights 배열에 제어할 Light 컴포넌트 추가
    /// 3. TriggerLights() 호출하여 조명 켜기
    /// </summary>
    public class LightingEventTrigger : MonoBehaviour
    {
        [Header("Lights")]
        [Tooltip("제어할 조명들")]
        public List<Light> lights = new List<Light>();

        [Tooltip("초기 상태 (꺼짐/켜짐)")]
        public bool initiallyOff = true;

        [Header("Trigger Settings")]
        [Tooltip("순차적으로 켜기")]
        public bool sequential = true;

        [Tooltip("순차 켜기 간격 (초)")]
        public float sequentialDelay = 0.3f;

        [Tooltip("페이드 시간 (초)")]
        public float fadeTime = 0.5f;

        [Tooltip("최종 밝기")]
        public float targetIntensity = 1f;

        [Header("Flicker Effect")]
        [Tooltip("켜질 때 깜빡임 효과")]
        public bool flickerOnTrigger = true;

        [Tooltip("깜빡임 횟수")]
        public int flickerCount = 3;

        [Tooltip("깜빡임 속도")]
        public float flickerSpeed = 0.1f;

        [Header("Audio")]
        public AudioClip lightOnSound;
        public AudioClip electricBuzzSound;

        [Header("Events")]
        public UnityEvent OnLightsTriggered;
        public UnityEvent OnAllLightsOn;

        private AudioSource audioSource;
        private Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();
        private bool hasTriggered = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // lights 리스트가 비어있으면 자식에서 자동으로 찾기
            if (lights == null || lights.Count == 0)
            {
                lights = new List<Light>(GetComponentsInChildren<Light>(true));
                Debug.Log($"[LightingEventTrigger] {gameObject.name}: 자식에서 {lights.Count}개 Light 자동 발견");
            }

            // 원래 밝기 저장 및 초기 상태 설정
            foreach (var light in lights)
            {
                if (light != null)
                {
                    originalIntensities[light] = light.intensity;
                    if (initiallyOff)
                    {
                        light.enabled = false;
                        Debug.Log($"[LightingEventTrigger] {light.gameObject.name} 조명 꺼짐 (초기 상태)");
                    }
                }
            }
        }

        /// <summary>
        /// 조명 이벤트 트리거
        /// </summary>
        public void TriggerLights()
        {
            if (hasTriggered) return;
            hasTriggered = true;

            OnLightsTriggered?.Invoke();

            if (sequential)
            {
                StartCoroutine(TriggerLightsSequentially());
            }
            else
            {
                StartCoroutine(TriggerAllLights());
            }

            Debug.Log($"[LightingEventTrigger] {gameObject.name} 조명 이벤트 트리거");
        }

        private IEnumerator TriggerLightsSequentially()
        {
            for (int i = 0; i < lights.Count; i++)
            {
                var light = lights[i];
                if (light == null) continue;

                if (flickerOnTrigger)
                {
                    yield return StartCoroutine(FlickerAndTurnOn(light));
                }
                else
                {
                    yield return StartCoroutine(FadeInLight(light));
                }

                if (i < lights.Count - 1)
                {
                    yield return new WaitForSeconds(sequentialDelay);
                }
            }

            OnAllLightsOn?.Invoke();
            Debug.Log($"[LightingEventTrigger] 모든 조명 켜짐");
        }

        private IEnumerator TriggerAllLights()
        {
            // 모든 조명 동시에 켜기
            foreach (var light in lights)
            {
                if (light == null) continue;

                if (flickerOnTrigger)
                {
                    StartCoroutine(FlickerAndTurnOn(light));
                }
                else
                {
                    StartCoroutine(FadeInLight(light));
                }
            }

            // 가장 긴 시간 대기
            float waitTime = flickerOnTrigger ?
                (flickerCount * flickerSpeed * 2 + fadeTime) : fadeTime;
            yield return new WaitForSeconds(waitTime);

            OnAllLightsOn?.Invoke();
            Debug.Log($"[LightingEventTrigger] 모든 조명 켜짐");
        }

        private IEnumerator FlickerAndTurnOn(Light light)
        {
            // 깜빡임 효과
            light.enabled = true;
            float originalInt = originalIntensities.ContainsKey(light) ?
                originalIntensities[light] : targetIntensity;

            // 전기 버즈 사운드
            if (electricBuzzSound != null)
            {
                audioSource.PlayOneShot(electricBuzzSound, 0.5f);
            }

            for (int i = 0; i < flickerCount; i++)
            {
                light.intensity = originalInt * Random.Range(0.3f, 1f);
                yield return new WaitForSeconds(flickerSpeed);
                light.intensity = 0;
                yield return new WaitForSeconds(flickerSpeed * 0.5f);
            }

            // 조명 켜짐 사운드
            if (lightOnSound != null)
            {
                audioSource.PlayOneShot(lightOnSound);
            }

            // 페이드 인
            yield return StartCoroutine(FadeInLight(light, originalInt));
        }

        private IEnumerator FadeInLight(Light light, float targetInt = -1)
        {
            if (targetInt < 0)
            {
                targetInt = originalIntensities.ContainsKey(light) ?
                    originalIntensities[light] : targetIntensity;
            }

            light.enabled = true;
            light.intensity = 0;

            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(0, targetInt, elapsed / fadeTime);
                yield return null;
            }

            light.intensity = targetInt;
        }

        /// <summary>
        /// 조명 끄기
        /// </summary>
        public void TurnOffLights()
        {
            StartCoroutine(FadeOutAllLights());
        }

        private IEnumerator FadeOutAllLights()
        {
            foreach (var light in lights)
            {
                if (light == null) continue;
                StartCoroutine(FadeOutLight(light));
            }

            yield return new WaitForSeconds(fadeTime);
            hasTriggered = false;
        }

        private IEnumerator FadeOutLight(Light light)
        {
            float startInt = light.intensity;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(startInt, 0, elapsed / fadeTime);
                yield return null;
            }

            light.intensity = 0;
            light.enabled = false;
        }

        /// <summary>
        /// 리셋
        /// </summary>
        public void ResetLights()
        {
            StopAllCoroutines();
            hasTriggered = false;

            foreach (var light in lights)
            {
                if (light == null) continue;

                if (initiallyOff)
                {
                    light.enabled = false;
                }
                else
                {
                    light.enabled = true;
                    light.intensity = originalIntensities.ContainsKey(light) ?
                        originalIntensities[light] : targetIntensity;
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Test Trigger Lights")]
        private void TestTrigger()
        {
            if (Application.isPlaying)
            {
                hasTriggered = false;
                TriggerLights();
            }
        }

        [ContextMenu("Find Lights In Children")]
        private void FindLightsInChildren()
        {
            lights.Clear();
            lights.AddRange(GetComponentsInChildren<Light>());
            Debug.Log($"[LightingEventTrigger] {lights.Count}개의 조명을 찾았습니다.");
        }
#endif
    }
}
