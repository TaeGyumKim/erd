using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// 숨을 수 있는 장소 (옷장, 침대 밑 등)
    ///
    /// 사용법:
    /// 1. 숨을 장소 오브젝트에 추가
    /// 2. hidePosition에 플레이어가 숨을 위치 설정
    /// 3. Collider (Trigger) 필요
    /// </summary>
    public class HidingSpot : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
    {
        [Header("Hiding Settings")]
        [Tooltip("숨는 위치")]
        public Transform hidePosition;

        [Tooltip("숨기 가능 여부")]
        public bool canHide = true;

        [Tooltip("숨는데 걸리는 시간")]
        public float hideTime = 0.5f;

        [Header("Visual")]
        [Tooltip("숨었을 때 보이는 오브젝트 (옷장 문 등)")]
        public GameObject hidingVisual;

        [Tooltip("문 닫힘 각도")]
        public float closedAngle = 0f;

        [Tooltip("문 열림 각도")]
        public float openAngle = 90f;

        [Header("Audio")]
        public AudioClip enterSound;
        public AudioClip exitSound;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnPlayerHide;
        public UnityEngine.Events.UnityEvent OnPlayerExit;

        public bool IsOccupied { get; private set; }

        private AudioSource audioSource;
        private VRPlayer hidingPlayer;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // 숨는 위치 자동 설정
            if (hidePosition == null)
            {
                GameObject hidePos = new GameObject("HidePosition");
                hidePos.transform.SetParent(transform);
                hidePos.transform.localPosition = Vector3.zero;
                hidePosition = hidePos.transform;
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (!canHide) return;

            // 플레이어 찾기
            var player = VRPlayer.Instance;
            if (player == null) return;

            if (IsOccupied)
            {
                // 이미 숨어있으면 나오기
                ExitHiding();
            }
            else
            {
                // 숨기
                EnterHiding(player);
            }
        }

        /// <summary>
        /// 숨기 시작
        /// </summary>
        public void EnterHiding(VRPlayer player)
        {
            if (!canHide || IsOccupied) return;

            IsOccupied = true;
            hidingPlayer = player;

            // 플레이어 숨기 상태로 전환
            player.StartHiding(hidePosition);

            // 비주얼 업데이트 (문 닫기 등)
            if (hidingVisual != null)
            {
                // 문 닫기 애니메이션
                StartCoroutine(RotateVisual(closedAngle));
            }

            // 사운드
            if (enterSound != null)
            {
                audioSource.PlayOneShot(enterSound);
            }

            OnPlayerHide?.Invoke();
            Debug.Log($"[HidingSpot] {player.name}이(가) {gameObject.name}에 숨음");
        }

        /// <summary>
        /// 숨기 종료
        /// </summary>
        public void ExitHiding()
        {
            if (!IsOccupied || hidingPlayer == null) return;

            // 플레이어 숨기 상태 해제
            hidingPlayer.StopHiding();

            // 비주얼 업데이트 (문 열기 등)
            if (hidingVisual != null)
            {
                StartCoroutine(RotateVisual(openAngle));
            }

            // 사운드
            if (exitSound != null)
            {
                audioSource.PlayOneShot(exitSound);
            }

            OnPlayerExit?.Invoke();
            Debug.Log($"[HidingSpot] {hidingPlayer.name}이(가) {gameObject.name}에서 나옴");

            IsOccupied = false;
            hidingPlayer = null;
        }

        private System.Collections.IEnumerator RotateVisual(float targetAngle)
        {
            if (hidingVisual == null) yield break;

            Quaternion startRotation = hidingVisual.transform.localRotation;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            float elapsed = 0;

            while (elapsed < hideTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / hideTime;
                hidingVisual.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            hidingVisual.transform.localRotation = targetRotation;
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

        private void OnDrawGizmosSelected()
        {
            // 숨는 위치 표시
            if (hidePosition != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(hidePosition.position, 0.3f);
                Gizmos.DrawLine(transform.position, hidePosition.position);
            }
        }
    }
}
