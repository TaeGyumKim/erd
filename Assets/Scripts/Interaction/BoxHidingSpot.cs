using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 상자에서 숨을 수 있는 기능
    /// 기존 HidingSpot보다 간단하게 상자에 추가할 수 있음
    ///
    /// 사용법:
    /// 1. 상자 오브젝트에 이 스크립트 추가
    /// 2. Collider (Trigger 권장) 필요
    /// 3. PCPlayerController로 클릭하면 숨기/나오기 가능
    /// </summary>
    public class BoxHidingSpot : MonoBehaviour
    {
        [Header("Hiding Settings")]
        [Tooltip("숨기 가능 여부")]
        public bool canHide = true;

        [Tooltip("숨을 수 있는 최대 거리")]
        public float maxHideDistance = 3f;

        [Tooltip("숨는 위치 오프셋 (상자 내부)")]
        public Vector3 hidePositionOffset = new Vector3(0f, 0.2f, 0f);

        [Tooltip("숨을 때 상자 뚜껑 열기 애니메이션 (옵션)")]
        public Transform lidTransform;

        [Tooltip("뚜껑 열림 각도")]
        public float lidOpenAngle = -90f;

        [Tooltip("뚜껑 닫힘 각도")]
        public float lidClosedAngle = 0f;

        [Tooltip("뚜껑 애니메이션 시간")]
        public float lidAnimationTime = 0.3f;

        [Header("Audio")]
        public AudioClip enterSound;
        public AudioClip exitSound;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnPlayerHide;
        public UnityEngine.Events.UnityEvent OnPlayerExit;

        /// <summary>
        /// 현재 플레이어가 숨어있는지
        /// </summary>
        public bool IsOccupied { get; private set; }

        private AudioSource audioSource;
        private PCPlayerController hidingPlayer;
        private Vector3 playerOriginalPosition;
        private Quaternion playerOriginalRotation;
        private Coroutine lidCoroutine;
        private CharacterController playerController;
        private BoxHidingSpot currentBoxHidingSpot; // PCPlayerController에서 참조용

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }

            // Collider가 없으면 추가
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
            }
        }

        /// <summary>
        /// 상호작용 (클릭 시 호출)
        /// </summary>
        public void Interact(PCPlayerController player)
        {
            if (!canHide) return;
            if (player == null) return;

            if (IsOccupied && hidingPlayer == player)
            {
                // 이미 숨어있으면 나오기
                ExitHiding();
            }
            else if (!IsOccupied)
            {
                // 숨기
                EnterHiding(player);
            }
        }

        /// <summary>
        /// 숨기 시작
        /// </summary>
        public void EnterHiding(PCPlayerController player)
        {
            if (!canHide || IsOccupied || player == null) return;

            // 거리 체크 - 너무 멀면 숨을 수 없음
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance > maxHideDistance)
            {
                Debug.Log($"[BoxHidingSpot] 너무 멀어서 숨을 수 없음 (거리: {distance:F1}m, 최대: {maxHideDistance}m)");
                return;
            }

            IsOccupied = true;
            hidingPlayer = player;

            // 원래 위치 저장
            playerOriginalPosition = player.transform.position;
            playerOriginalRotation = player.transform.rotation;

            // CharacterController 비활성화 (충돌 방지)
            playerController = player.GetComponent<CharacterController>();
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // 플레이어 위치를 상자 위로 이동 (상자 안이 아닌 위쪽 - 충돌 방지)
            // 플레이어는 보이지 않게 되므로 위치는 상관없음
            Vector3 hidePosition = transform.position + Vector3.up * 10f; // 상자 위 10m (보이지 않는 위치)
            player.transform.position = hidePosition;

            // 숨기 상태 설정
            player.StartHidingInBox(this);

            // 뚜껑 열기/닫기 애니메이션
            if (lidTransform != null)
            {
                if (lidCoroutine != null) StopCoroutine(lidCoroutine);
                lidCoroutine = StartCoroutine(AnimateLid(lidClosedAngle));
            }

            // 사운드
            if (enterSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(enterSound);
            }

            OnPlayerHide?.Invoke();
            Debug.Log($"[BoxHidingSpot] 플레이어가 {gameObject.name}에 숨음");
        }

        /// <summary>
        /// 숨기 종료
        /// </summary>
        public void ExitHiding()
        {
            if (!IsOccupied || hidingPlayer == null) return;

            var player = hidingPlayer;

            // 뚜껑 열기 애니메이션
            if (lidTransform != null)
            {
                if (lidCoroutine != null) StopCoroutine(lidCoroutine);
                lidCoroutine = StartCoroutine(AnimateLid(lidOpenAngle));
            }

            // 안전한 나가기 위치 찾기
            Vector3 exitPosition = FindSafeExitPosition();

            // 플레이어 위치 먼저 이동 (CharacterController 활성화 전)
            player.transform.position = exitPosition;

            // CharacterController 다시 활성화
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // 숨기 상태 해제
            player.StopHidingFromBox();

            // 사운드
            if (exitSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(exitSound);
            }

            OnPlayerExit?.Invoke();
            Debug.Log($"[BoxHidingSpot] 플레이어가 {gameObject.name}에서 나옴 (위치: {exitPosition})");

            IsOccupied = false;
            hidingPlayer = null;
            playerController = null;

            // 잠시 후 뚜껑 닫기
            StartCoroutine(CloseLidDelayed());
        }

        /// <summary>
        /// 안전한 나가기 위치 찾기 (충돌 없는 곳)
        /// </summary>
        private Vector3 FindSafeExitPosition()
        {
            // 여러 방향으로 나가기 시도
            Vector3[] directions = new Vector3[]
            {
                transform.forward,      // 앞
                -transform.forward,     // 뒤
                transform.right,        // 오른쪽
                -transform.right,       // 왼쪽
                (transform.forward + transform.right).normalized,   // 앞오른쪽
                (transform.forward - transform.right).normalized,   // 앞왼쪽
                (-transform.forward + transform.right).normalized,  // 뒤오른쪽
                (-transform.forward - transform.right).normalized   // 뒤왼쪽
            };

            float exitDistance = 1.5f;
            float playerHeight = playerOriginalPosition.y;

            foreach (var dir in directions)
            {
                Vector3 testPosition = transform.position + dir * exitDistance;
                testPosition.y = playerHeight;

                // 충돌 체크 (플레이어 크기 정도의 구체로 체크)
                if (!Physics.CheckSphere(testPosition, 0.4f, ~0, QueryTriggerInteraction.Ignore))
                {
                    return testPosition;
                }
            }

            // 모든 방향이 막혀있으면 원래 위치로
            Debug.LogWarning($"[BoxHidingSpot] 안전한 나가기 위치를 찾을 수 없음, 원래 위치로 복귀");
            return playerOriginalPosition;
        }

        /// <summary>
        /// 외부에서 강제로 플레이어 내보내기 (적이 발견했을 때 등)
        /// </summary>
        public void ForceExit()
        {
            if (IsOccupied)
            {
                ExitHiding();
            }
        }


        /// <summary>
        /// 뚜껑 애니메이션
        /// </summary>
        private System.Collections.IEnumerator AnimateLid(float targetAngle)
        {
            if (lidTransform == null) yield break;

            Quaternion startRotation = lidTransform.localRotation;
            Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
            float elapsed = 0f;

            while (elapsed < lidAnimationTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lidAnimationTime;
                t = 1f - Mathf.Pow(1f - t, 2f); // Ease out

                lidTransform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            lidTransform.localRotation = targetRotation;
        }

        /// <summary>
        /// 지연 후 뚜껑 닫기
        /// </summary>
        private System.Collections.IEnumerator CloseLidDelayed()
        {
            yield return new WaitForSeconds(0.5f);

            if (!IsOccupied && lidTransform != null)
            {
                if (lidCoroutine != null) StopCoroutine(lidCoroutine);
                lidCoroutine = StartCoroutine(AnimateLid(lidClosedAngle));
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 숨기 가능 거리 표시
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // 노란색 반투명
            Gizmos.DrawWireSphere(transform.position, maxHideDistance);

            // 숨는 위치 표시
            Gizmos.color = Color.cyan;
            Vector3 hidePos = transform.position + transform.TransformDirection(hidePositionOffset);
            Gizmos.DrawWireSphere(hidePos, 0.3f);
            Gizmos.DrawLine(transform.position, hidePos);

            // 나가는 위치 표시
            Gizmos.color = Color.green;
            Vector3 exitPos = transform.position + transform.forward * 1.2f;
            Gizmos.DrawWireSphere(exitPos, 0.2f);
        }
    }
}
