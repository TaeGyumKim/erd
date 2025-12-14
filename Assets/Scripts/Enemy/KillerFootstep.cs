using UnityEngine;
using UnityEngine.AI;

namespace HorrorGame
{
    /// <summary>
    /// 살인마 발소리 시스템
    /// 걷기/뛰기에 따라 다른 발소리 재생
    ///
    /// 사용법:
    /// 1. Killer 오브젝트에 이 스크립트 추가
    /// 2. 발소리 AudioClip 배열 설정
    /// 3. KillerAnimator와 연동하여 속도에 따른 발소리 재생
    /// </summary>
    public class KillerFootstep : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("발소리 재생용 AudioSource (자동 생성됨)")]
        public AudioSource footstepAudioSource;

        [Tooltip("KillerAI (자동 탐색됨)")]
        public KillerAI killerAI;

        [Tooltip("NavMeshAgent (자동 탐색됨)")]
        public NavMeshAgent agent;

        [Header("Walk Footsteps - 걷기 발소리")]
        [Tooltip("걷기 발소리 클립들")]
        public AudioClip[] walkFootsteps;

        [Tooltip("걷기 발소리 간격")]
        public float walkStepInterval = 0.5f;

        [Tooltip("걷기 발소리 볼륨")]
        [Range(0f, 1f)]
        public float walkVolume = 0.4f;

        [Header("Run Footsteps - 뛰기 발소리")]
        [Tooltip("뛰기 발소리 클립들")]
        public AudioClip[] runFootsteps;

        [Tooltip("뛰기 발소리 간격")]
        public float runStepInterval = 0.28f;

        [Tooltip("뛰기 발소리 볼륨")]
        [Range(0f, 1f)]
        public float runVolume = 0.7f;

        [Header("Speed Thresholds")]
        [Tooltip("이동 감지 최소 속도")]
        public float minMoveSpeed = 0.1f;

        [Tooltip("뛰기 판정 속도 임계값")]
        public float runSpeedThreshold = 3f;

        [Header("Audio Settings")]
        [Tooltip("피치 랜덤 범위 최소")]
        [Range(0.8f, 1f)]
        public float pitchMin = 0.9f;

        [Tooltip("피치 랜덤 범위 최대")]
        [Range(1f, 1.2f)]
        public float pitchMax = 1.1f;

        [Tooltip("3D 사운드 블렌드 (0=2D, 1=3D)")]
        [Range(0f, 1f)]
        public float spatialBlend = 1f;

        [Tooltip("최대 청취 거리")]
        public float maxDistance = 20f;

        private float stepTimer;
        private int lastWalkClipIndex = -1;
        private int lastRunClipIndex = -1;
        private float currentSpeed;
        private bool isMoving;

        private void Awake()
        {
            // AudioSource 설정
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }

            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.spatialBlend = spatialBlend;
            footstepAudioSource.maxDistance = maxDistance;
            footstepAudioSource.rolloffMode = AudioRolloffMode.Linear;

            // 컴포넌트 자동 탐색
            if (killerAI == null)
            {
                killerAI = GetComponent<KillerAI>();
            }

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }

        private void Update()
        {
            UpdateMovementState();
            UpdateFootsteps();
        }

        /// <summary>
        /// 이동 상태 업데이트
        /// </summary>
        private void UpdateMovementState()
        {
            if (agent != null && agent.enabled)
            {
                currentSpeed = agent.velocity.magnitude;
            }
            else
            {
                currentSpeed = 0f;
            }

            isMoving = currentSpeed > minMoveSpeed;
        }

        /// <summary>
        /// 발소리 업데이트
        /// </summary>
        private void UpdateFootsteps()
        {
            if (!isMoving)
            {
                stepTimer = 0f;
                return;
            }

            // 타이머 감소
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = GetStepInterval();
            }
        }

        /// <summary>
        /// 현재 속도에 따른 발소리 간격 반환
        /// </summary>
        private float GetStepInterval()
        {
            bool isRunning = currentSpeed >= runSpeedThreshold;
            return isRunning ? runStepInterval : walkStepInterval;
        }

        /// <summary>
        /// 발소리 재생
        /// </summary>
        private void PlayFootstep()
        {
            bool isRunning = currentSpeed >= runSpeedThreshold;
            AudioClip clip = GetFootstepClip(isRunning);

            if (clip == null) return;

            // 피치 랜덤화
            footstepAudioSource.pitch = Random.Range(pitchMin, pitchMax);

            // 볼륨 설정
            float volume = isRunning ? runVolume : walkVolume;
            footstepAudioSource.volume = volume;

            // 재생
            footstepAudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// 발소리 클립 선택 (연속 재생 방지)
        /// </summary>
        private AudioClip GetFootstepClip(bool isRunning)
        {
            AudioClip[] clips = isRunning ? runFootsteps : walkFootsteps;

            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            // 마지막 인덱스 참조
            ref int lastIndex = ref (isRunning ? ref lastRunClipIndex : ref lastWalkClipIndex);

            // 같은 클립 연속 재생 방지
            int clipIndex;
            int attempts = 0;
            do
            {
                clipIndex = Random.Range(0, clips.Length);
                attempts++;
            } while (clipIndex == lastIndex && clips.Length > 1 && attempts < 5);

            lastIndex = clipIndex;
            return clips[clipIndex];
        }

        /// <summary>
        /// 수동으로 발소리 재생
        /// </summary>
        public void PlayFootstepManual(bool isRunning = false)
        {
            AudioClip clip = GetFootstepClip(isRunning);
            if (clip == null) return;

            footstepAudioSource.pitch = Random.Range(pitchMin, pitchMax);
            footstepAudioSource.volume = isRunning ? runVolume : walkVolume;
            footstepAudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// 발소리 정지
        /// </summary>
        public void StopFootsteps()
        {
            footstepAudioSource.Stop();
            stepTimer = 0f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (killerAI == null)
            {
                killerAI = GetComponent<KillerAI>();
            }
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 청취 범위 표시
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, maxDistance);
        }
#endif
    }
}
