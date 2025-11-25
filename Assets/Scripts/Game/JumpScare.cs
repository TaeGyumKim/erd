using UnityEngine;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 점프스케어 트리거
    /// 플레이어가 특정 위치에 도달하면 점프스케어 발동
    ///
    /// 사용법:
    /// 1. 빈 오브젝트에 추가
    /// 2. Box Collider (Trigger) 추가
    /// 3. 점프스케어 설정 (사운드, 이미지, 오브젝트 등)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class JumpScare : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [Tooltip("한 번만 발동")]
        public bool triggerOnce = true;

        [Tooltip("발동 확률 (0-1)")]
        [Range(0f, 1f)]
        public float triggerChance = 1f;

        [Tooltip("발동 딜레이")]
        public float triggerDelay = 0f;

        [Header("Visual")]
        [Tooltip("점프스케어 오브젝트 (활성화됨)")]
        public GameObject scareObject;

        [Tooltip("점프스케어 지속 시간")]
        public float scareDuration = 2f;

        [Tooltip("플레이어 앞에 나타날 위치 오프셋")]
        public Vector3 appearOffset = new Vector3(0, 0, 2f);

        [Tooltip("플레이어를 바라봄")]
        public bool lookAtPlayer = true;

        [Header("Audio")]
        [Tooltip("점프스케어 사운드")]
        public AudioClip scareSound;

        [Tooltip("사운드 볼륨")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Header("Screen Effect")]
        [Tooltip("화면 효과 사용")]
        public bool useScreenEffect = false;

        [Tooltip("화면 효과 색상")]
        public Color screenFlashColor = Color.red;

        [Tooltip("화면 효과 지속 시간")]
        public float flashDuration = 0.3f;

        [Header("Controller Vibration")]
        [Tooltip("컨트롤러 진동")]
        public bool useHaptics = true;

        [Tooltip("진동 강도")]
        [Range(0f, 1f)]
        public float hapticIntensity = 1f;

        [Tooltip("진동 지속 시간")]
        public float hapticDuration = 0.5f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnJumpScare;
        public UnityEngine.Events.UnityEvent OnJumpScareEnd;

        private bool hasTriggered;
        private AudioSource audioSource;
        private Transform playerTransform;

        private void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // 2D 사운드 (더 무섭게)
                audioSource.playOnAwake = false;
            }

            // 점프스케어 오브젝트 비활성화
            if (scareObject != null)
            {
                scareObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 이미 발동됨
            if (triggerOnce && hasTriggered) return;

            // 플레이어 확인
            var player = other.GetComponent<VRPlayer>();
            if (player == null)
            {
                player = other.GetComponentInParent<VRPlayer>();
            }

            if (player != null)
            {
                playerTransform = player.transform;

                // 확률 체크
                if (Random.value <= triggerChance)
                {
                    StartCoroutine(TriggerJumpScare());
                }
            }
        }

        private IEnumerator TriggerJumpScare()
        {
            hasTriggered = true;

            // 딜레이
            if (triggerDelay > 0)
            {
                yield return new WaitForSeconds(triggerDelay);
            }

            Debug.Log("[JumpScare] 점프스케어 발동!");

            // 사운드 재생
            if (scareSound != null)
            {
                audioSource.PlayOneShot(scareSound, volume);
            }

            // 오브젝트 표시
            if (scareObject != null)
            {
                // 플레이어 앞에 위치
                if (playerTransform != null)
                {
                    Vector3 spawnPos = playerTransform.position +
                        playerTransform.forward * appearOffset.z +
                        playerTransform.up * appearOffset.y +
                        playerTransform.right * appearOffset.x;

                    scareObject.transform.position = spawnPos;

                    if (lookAtPlayer)
                    {
                        scareObject.transform.LookAt(playerTransform);
                    }
                }

                scareObject.SetActive(true);
            }

            // 컨트롤러 진동
            if (useHaptics)
            {
                TriggerHaptics();
            }

            // 긴장도 증가
            if (HorrorAudioManager.Instance != null)
            {
                HorrorAudioManager.Instance.SetTension(HorrorAudioManager.TensionLevel.Tense);
            }

            OnJumpScare?.Invoke();

            // 지속 시간 대기
            yield return new WaitForSeconds(scareDuration);

            // 오브젝트 숨김
            if (scareObject != null)
            {
                scareObject.SetActive(false);
            }

            // 긴장도 복구
            if (HorrorAudioManager.Instance != null)
            {
                HorrorAudioManager.Instance.SetTension(HorrorAudioManager.TensionLevel.Uneasy);
            }

            OnJumpScareEnd?.Invoke();
        }

        private void TriggerHaptics()
        {
            // Quest 3 컨트롤러 진동
            var controllers = FindObjectsOfType<Quest3Controller>();
            foreach (var controller in controllers)
            {
                controller.Vibrate(hapticIntensity, hapticDuration);
            }
        }

        /// <summary>
        /// 수동으로 점프스케어 발동
        /// </summary>
        public void TriggerManually()
        {
            if (triggerOnce && hasTriggered) return;

            // 플레이어 찾기
            var player = VRPlayer.Instance;
            if (player != null)
            {
                playerTransform = player.transform;
            }

            StartCoroutine(TriggerJumpScare());
        }

        /// <summary>
        /// 점프스케어 리셋
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            if (scareObject != null)
            {
                scareObject.SetActive(false);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            var collider = GetComponent<BoxCollider>();
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(collider.center, collider.size);
                Gizmos.DrawWireCube(collider.center, collider.size);
            }

            // 점프스케어 아이콘
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
