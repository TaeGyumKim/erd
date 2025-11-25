using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 탈출 구역
    /// 플레이어가 이 구역에 진입하면 탈출 시도
    ///
    /// 사용법:
    /// 1. 탈출 지점에 빈 오브젝트 생성
    /// 2. 이 스크립트 추가
    /// 3. Box Collider (Trigger) 추가
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EscapeZone : MonoBehaviour
    {
        [Header("Escape Settings")]
        [Tooltip("탈출에 필요한 열쇠 수 (0이면 GameManager 설정 사용)")]
        public int requiredKeys = 0;

        [Tooltip("자동 탈출 (조건 충족 시 바로 탈출)")]
        public bool autoEscape = true;

        [Header("Locked Door (Optional)")]
        [Tooltip("탈출구 문 (열쇠 조건 충족 시 열림)")]
        public Door escapeDoor;

        [Header("Visual")]
        [Tooltip("탈출 가능 시 활성화될 오브젝트")]
        public GameObject escapeAvailableIndicator;

        [Tooltip("탈출 불가 시 활성화될 오브젝트")]
        public GameObject escapeLockedIndicator;

        [Header("Audio")]
        public AudioClip cannotEscapeSound;
        public AudioClip escapeSound;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnEscapeAttempt;
        public UnityEngine.Events.UnityEvent OnEscapeFailed;
        public UnityEngine.Events.UnityEvent OnEscapeSuccess;

        private AudioSource audioSource;

        /// <summary>
        /// 플레이어가 탈출 구역 안에 있는지 여부
        /// </summary>
        public bool IsPlayerInZone { get; private set; }

        private void Awake()
        {
            // Collider를 Trigger로 설정
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }
        }

        private void Start()
        {
            UpdateVisuals();
        }

        private void Update()
        {
            // 시각적 표시 업데이트
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            bool canEscape = CanEscape();

            if (escapeAvailableIndicator != null)
            {
                escapeAvailableIndicator.SetActive(canEscape);
            }

            if (escapeLockedIndicator != null)
            {
                escapeLockedIndicator.SetActive(!canEscape);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 플레이어 확인
            var player = other.GetComponent<VRPlayer>();
            if (player == null)
            {
                player = other.GetComponentInParent<VRPlayer>();
            }

            if (player != null)
            {
                IsPlayerInZone = true;
                Debug.Log("[EscapeZone] 플레이어가 탈출 구역에 진입");

                if (autoEscape)
                {
                    TryEscape();
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<VRPlayer>();
            if (player == null)
            {
                player = other.GetComponentInParent<VRPlayer>();
            }

            if (player != null)
            {
                IsPlayerInZone = false;
                Debug.Log("[EscapeZone] 플레이어가 탈출 구역에서 나감");
            }
        }

        /// <summary>
        /// 탈출 가능 여부 확인
        /// </summary>
        public bool CanEscape()
        {
            int keysNeeded = requiredKeys > 0 ? requiredKeys :
                (HorrorGameManager.Instance != null ? HorrorGameManager.Instance.requiredKeysToEscape : 0);

            if (PlayerInventory.Instance != null)
            {
                return PlayerInventory.Instance.GetKeyCount() >= keysNeeded;
            }

            if (HorrorGameManager.Instance != null)
            {
                return HorrorGameManager.Instance.CanEscape();
            }

            return true;
        }

        /// <summary>
        /// 탈출 시도
        /// </summary>
        public void TryEscape()
        {
            OnEscapeAttempt?.Invoke();

            if (CanEscape())
            {
                // 탈출 성공
                if (escapeSound != null)
                {
                    audioSource.PlayOneShot(escapeSound);
                }

                // 탈출구 문 열기
                if (escapeDoor != null && !escapeDoor.isOpen)
                {
                    escapeDoor.Unlock();
                    escapeDoor.OpenDoor();
                }

                OnEscapeSuccess?.Invoke();

                // 게임 매니저에 알림
                if (HorrorGameManager.Instance != null)
                {
                    HorrorGameManager.Instance.Victory();
                }

                Debug.Log("[EscapeZone] 탈출 성공!");
            }
            else
            {
                // 탈출 실패
                if (cannotEscapeSound != null)
                {
                    audioSource.PlayOneShot(cannotEscapeSound);
                }

                OnEscapeFailed?.Invoke();

                int keysNeeded = requiredKeys > 0 ? requiredKeys :
                    (HorrorGameManager.Instance != null ? HorrorGameManager.Instance.requiredKeysToEscape : 0);
                int currentKeys = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetKeyCount() : 0;

                Debug.Log($"[EscapeZone] 탈출 실패 - 열쇠가 더 필요합니다 ({currentKeys}/{keysNeeded})");
            }
        }

        private void OnDrawGizmos()
        {
            // 탈출 구역 표시
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            var collider = GetComponent<BoxCollider>();
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(collider.center, collider.size);
                Gizmos.DrawWireCube(collider.center, collider.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1f);
            }
        }
    }
}
