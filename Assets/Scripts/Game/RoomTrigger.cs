using UnityEngine;
using UnityEngine.Events;

namespace HorrorGame
{
    /// <summary>
    /// 방 진입 감지 트리거
    /// 플레이어가 특정 영역에 들어왔을 때 이벤트 발생
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RoomTrigger : MonoBehaviour
    {
        [Header("Room Settings")]
        [Tooltip("방 ID")]
        public string roomId = "room1";

        [Tooltip("방 이름 (표시용)")]
        public string roomName = "Room 1";

        [Header("Trigger Settings")]
        [Tooltip("한 번만 트리거")]
        public bool triggerOnce = false;

        [Tooltip("트리거 활성화 상태")]
        public bool isActive = true;

        [Tooltip("필요한 게임 스테이지 (옵션)")]
        public RoomProgressManager.GameStage requiredStage = RoomProgressManager.GameStage.Room1_Start;

        [Tooltip("스테이지 체크 활성화")]
        public bool checkStage = false;

        [Header("Events")]
        public UnityEvent OnPlayerEnter;
        public UnityEvent OnPlayerExit;
        public UnityEvent<string> OnEnterWithRoomId;

        [Header("Debug")]
        [SerializeField] private bool playerInside = false;
        [SerializeField] private bool hasTriggered = false;

        public bool PlayerInside => playerInside;
        public bool HasTriggered => hasTriggered;

        private void Awake()
        {
            // Collider가 Trigger인지 확인
            var collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
                Debug.LogWarning($"[RoomTrigger] {gameObject.name}의 Collider를 Trigger로 설정했습니다.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;
            if (triggerOnce && hasTriggered) return;

            // 플레이어 체크
            if (!IsPlayer(other)) return;

            // 스테이지 체크
            if (checkStage && RoomProgressManager.Instance != null)
            {
                if (RoomProgressManager.Instance.CurrentStage != requiredStage)
                {
                    return;
                }
            }

            playerInside = true;
            hasTriggered = true;

            // 이벤트 발생
            OnPlayerEnter?.Invoke();
            OnEnterWithRoomId?.Invoke(roomId);

            // RoomProgressManager에 알림
            NotifyRoomProgress();

            Debug.Log($"[RoomTrigger] 플레이어가 {roomName}에 진입");
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;

            playerInside = false;
            OnPlayerExit?.Invoke();

            Debug.Log($"[RoomTrigger] 플레이어가 {roomName}에서 나감");
        }

        /// <summary>
        /// 플레이어인지 확인
        /// </summary>
        private bool IsPlayer(Collider other)
        {
            // VR 플레이어
            if (other.GetComponent<VRPlayer>() != null) return true;
            if (other.GetComponentInParent<VRPlayer>() != null) return true;

            // PC 플레이어
            if (other.GetComponent<PCPlayerController>() != null) return true;
            if (other.GetComponentInParent<PCPlayerController>() != null) return true;

            // CharacterController (플레이어)
            if (other.GetComponent<CharacterController>() != null)
            {
                // Player 태그 확인
                if (other.CompareTag("Player")) return true;
            }

            // 태그 확인
            if (other.CompareTag("Player")) return true;

            return false;
        }

        /// <summary>
        /// RoomProgressManager에 방 진입 알림
        /// </summary>
        private void NotifyRoomProgress()
        {
            if (RoomProgressManager.Instance == null) return;

            switch (roomId.ToLower())
            {
                case "room1":
                    // Room1은 시작 지점이므로 별도 처리 없음
                    break;

                case "room2":
                    // Room2 (복도) 진입
                    break;

                case "room3":
                    // Room3 진입
                    break;

                case "room4":
                    RoomProgressManager.Instance.OnEnterRoom4();
                    break;

                case "room5":
                    // Room5 (탈출구) 진입 - 탈출 시퀀스는 SlidingDoor에서 처리
                    break;

                case "hiding_spot":
                    RoomProgressManager.Instance.OnEnterHidingSpot();
                    break;
            }
        }

        /// <summary>
        /// 트리거 리셋
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            playerInside = false;
        }

        /// <summary>
        /// 트리거 활성화/비활성화
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var collider = GetComponent<Collider>();
            if (collider == null) return;

            // 상태에 따른 색상
            if (playerInside)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            }
            else if (!isActive || (triggerOnce && hasTriggered))
            {
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            }
            else
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            }

            // Box Collider
            if (collider is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
            // Sphere Collider
            else if (collider is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 방 이름 표시
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"{roomName}\n({roomId})");
        }
#endif
    }
}
