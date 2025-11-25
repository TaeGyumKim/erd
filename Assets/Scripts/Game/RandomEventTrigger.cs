using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 랜덤 이벤트 트리거
    /// 무작위 공포 이벤트 발생 시스템
    ///
    /// 사용법:
    /// 1. 빈 오브젝트에 추가
    /// 2. 이벤트 목록 설정
    /// 3. 트리거 영역 또는 시간 기반 활성화
    /// </summary>
    public class RandomEventTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [Tooltip("트리거 방식")]
        public TriggerType triggerType = TriggerType.OnEnter;

        [Tooltip("발생 확률 (0~1)")]
        [Range(0, 1)]
        public float triggerChance = 0.5f;

        [Tooltip("쿨다운 시간")]
        public float cooldownTime = 30f;

        [Tooltip("한 번만 발생")]
        public bool triggerOnce = false;

        [Header("Time-Based Trigger")]
        [Tooltip("시간 기반 - 최소 간격")]
        public float minInterval = 30f;

        [Tooltip("시간 기반 - 최대 간격")]
        public float maxInterval = 120f;

        [Header("Events")]
        public List<RandomEvent> randomEvents = new List<RandomEvent>();

        [Header("Global Events")]
        public UnityEvent OnEventTriggered;
        public UnityEvent OnEventFinished;

        [System.Serializable]
        public class RandomEvent
        {
            public string eventName;

            [Tooltip("이벤트 가중치 (높을수록 자주 발생)")]
            public float weight = 1f;

            [Tooltip("실행할 이벤트들")]
            public UnityEvent onEventStart;

            [Header("Audio")]
            public AudioClip eventSound;
            public float soundDelay = 0f;

            [Header("Object Control")]
            public GameObject objectToActivate;
            public float activeDuration = 3f;

            [Header("Light Control")]
            public LightFlicker[] lightsToFlicker;
            public float flickerDuration = 2f;

            [Header("Haptic")]
            public bool triggerHaptic = false;
            public float hapticIntensity = 0.5f;
        }

        public enum TriggerType
        {
            OnEnter,        // 영역 진입 시
            OnStay,         // 영역 내 체류 시
            TimeBased,      // 시간 기반 자동
            Manual          // 수동 호출
        }

        private float lastTriggerTime;
        private float nextEventTime;
        private bool hasTriggered;
        private AudioSource audioSource;
        private bool isEventRunning;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }
        }

        private void Start()
        {
            if (triggerType == TriggerType.TimeBased)
            {
                ScheduleNextEvent();
            }
        }

        private void Update()
        {
            if (triggerType == TriggerType.TimeBased && !hasTriggered)
            {
                if (Time.time >= nextEventTime)
                {
                    TryTriggerEvent();
                    ScheduleNextEvent();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType != TriggerType.OnEnter) return;
            if (other.GetComponent<VRPlayer>() == null) return;

            TryTriggerEvent();
        }

        private void OnTriggerStay(Collider other)
        {
            if (triggerType != TriggerType.OnStay) return;
            if (other.GetComponent<VRPlayer>() == null) return;

            // 쿨다운 체크 후 발생
            if (Time.time - lastTriggerTime >= cooldownTime)
            {
                TryTriggerEvent();
            }
        }

        /// <summary>
        /// 이벤트 발생 시도
        /// </summary>
        public void TryTriggerEvent()
        {
            if (hasTriggered && triggerOnce) return;
            if (Time.time - lastTriggerTime < cooldownTime) return;
            if (isEventRunning) return;

            // 확률 체크
            if (Random.value > triggerChance) return;

            // 이벤트 선택 및 실행
            RandomEvent selectedEvent = SelectRandomEvent();
            if (selectedEvent != null)
            {
                StartCoroutine(ExecuteEvent(selectedEvent));
            }

            lastTriggerTime = Time.time;
            hasTriggered = true;
        }

        /// <summary>
        /// 강제로 이벤트 발생
        /// </summary>
        public void ForceTrigerEvent()
        {
            RandomEvent selectedEvent = SelectRandomEvent();
            if (selectedEvent != null)
            {
                StartCoroutine(ExecuteEvent(selectedEvent));
            }
        }

        /// <summary>
        /// 특정 이벤트 발생
        /// </summary>
        public void TriggerSpecificEvent(string eventName)
        {
            var ev = randomEvents.Find(e => e.eventName == eventName);
            if (ev != null)
            {
                StartCoroutine(ExecuteEvent(ev));
            }
        }

        private RandomEvent SelectRandomEvent()
        {
            if (randomEvents.Count == 0) return null;

            // 가중치 기반 랜덤 선택
            float totalWeight = 0;
            foreach (var ev in randomEvents)
            {
                totalWeight += ev.weight;
            }

            float randomValue = Random.Range(0, totalWeight);
            float currentWeight = 0;

            foreach (var ev in randomEvents)
            {
                currentWeight += ev.weight;
                if (randomValue <= currentWeight)
                {
                    return ev;
                }
            }

            return randomEvents[0];
        }

        private System.Collections.IEnumerator ExecuteEvent(RandomEvent ev)
        {
            isEventRunning = true;
            OnEventTriggered?.Invoke();

            Debug.Log($"[RandomEventTrigger] 이벤트 발생: {ev.eventName}");

            // 이벤트 시작
            ev.onEventStart?.Invoke();

            // 사운드 재생
            if (ev.eventSound != null)
            {
                if (ev.soundDelay > 0)
                {
                    yield return new WaitForSeconds(ev.soundDelay);
                }
                audioSource.PlayOneShot(ev.eventSound);
            }

            // 오브젝트 활성화
            if (ev.objectToActivate != null)
            {
                ev.objectToActivate.SetActive(true);

                if (ev.activeDuration > 0)
                {
                    yield return new WaitForSeconds(ev.activeDuration);
                    ev.objectToActivate.SetActive(false);
                }
            }

            // 조명 깜빡임
            if (ev.lightsToFlicker != null && ev.lightsToFlicker.Length > 0)
            {
                foreach (var light in ev.lightsToFlicker)
                {
                    if (light != null)
                    {
                        light.flickerEnabled = true;
                        light.FlickerAndTurnOff(ev.flickerDuration);
                    }
                }
            }

            // 햅틱
            if (ev.triggerHaptic)
            {
                var controllers = FindObjectsOfType<Quest3Controller>();
                foreach (var controller in controllers)
                {
                    controller?.Vibrate(ev.hapticIntensity, 0.3f);
                }
            }

            OnEventFinished?.Invoke();
            isEventRunning = false;
        }

        private void ScheduleNextEvent()
        {
            nextEventTime = Time.time + Random.Range(minInterval, maxInterval);
        }

        /// <summary>
        /// 쿨다운 리셋
        /// </summary>
        public void ResetCooldown()
        {
            lastTriggerTime = 0;
            hasTriggered = false;
        }

        private void OnDrawGizmosSelected()
        {
            // 트리거 영역 표시
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
                Gizmos.matrix = transform.localToWorldMatrix;

                if (collider is BoxCollider box)
                {
                    Gizmos.DrawCube(box.center, box.size);
                }
                else if (collider is SphereCollider sphere)
                {
                    Gizmos.DrawSphere(sphere.center, sphere.radius);
                }
            }
        }
    }
}
