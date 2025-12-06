using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 슬라이딩 문 (위로 열리는 탈출문)
    /// Room5 탈출문용
    /// </summary>
    public class SlidingDoor : MonoBehaviour
    {
        [Header("Door Settings")]
        [Tooltip("열림 상태")]
        public bool isOpen = false;

        [Tooltip("잠금 상태")]
        public bool isLocked = true;

        [Tooltip("필요한 아이템 ID")]
        public string requiredItemId = "escape_key";

        [Header("Movement")]
        [Tooltip("슬라이드 방향 (로컬)")]
        public Vector3 slideDirection = Vector3.up;

        [Tooltip("열림 거리")]
        public float openDistance = 3f;

        [Tooltip("열리는 속도 (일반)")]
        public float normalOpenSpeed = 2f;

        [Tooltip("열리는 속도 (천천히)")]
        public float slowOpenSpeed = 0.5f;

        [Tooltip("열리는 시간 (천천히 열기용)")]
        public float slowOpenDuration = 5f;

        [Header("Audio")]
        public AudioClip openStartSound;
        public AudioClip openLoopSound;
        public AudioClip openEndSound;
        public AudioClip lockedSound;

        [Header("Events")]
        public UnityEvent OnDoorOpen;
        public UnityEvent OnDoorFullyOpen;
        public UnityEvent OnDoorLocked;
        public UnityEvent OnDoorUnlock;

        private AudioSource audioSource;
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private Coroutine openCoroutine;
        private bool isOpening = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // 위치 계산
            closedPosition = transform.localPosition;
            openPosition = closedPosition + slideDirection.normalized * openDistance;
        }

        /// <summary>
        /// 상호작용 시도
        /// </summary>
        public void TryInteract()
        {
            if (isOpen || isOpening) return;

            if (isLocked)
            {
                // 필요 아이템 확인
                if (PlayerInventory.Instance != null &&
                    PlayerInventory.Instance.HasKey(requiredItemId))
                {
                    Unlock();
                    OpenDoor();
                }
                else
                {
                    // 잠김 알림
                    if (lockedSound != null)
                    {
                        audioSource.PlayOneShot(lockedSound);
                    }
                    OnDoorLocked?.Invoke();

                    if (VRHUD.Instance != null)
                    {
                        VRHUD.Instance.ShowStatus("열쇠가 필요하다...", 3f);
                    }

                    Debug.Log("[SlidingDoor] 잠겨있습니다.");
                }
            }
            else
            {
                OpenDoor();
            }
        }

        /// <summary>
        /// 잠금 해제
        /// </summary>
        public void Unlock()
        {
            if (!isLocked) return;

            isLocked = false;
            OnDoorUnlock?.Invoke();
            Debug.Log("[SlidingDoor] 잠금 해제");
        }

        /// <summary>
        /// 문 열기 (일반 속도)
        /// </summary>
        public void OpenDoor()
        {
            if (isOpen || isOpening || isLocked) return;

            if (openCoroutine != null)
            {
                StopCoroutine(openCoroutine);
            }

            openCoroutine = StartCoroutine(OpenSequence(normalOpenSpeed));
        }

        /// <summary>
        /// 문 천천히 열기 (탈출 연출용)
        /// </summary>
        public void OpenSlowly()
        {
            if (isOpen || isOpening) return;

            // 잠금 상태여도 강제로 열기 (탈출 시퀀스)
            isLocked = false;

            if (openCoroutine != null)
            {
                StopCoroutine(openCoroutine);
            }

            openCoroutine = StartCoroutine(OpenSlowlySequence());
        }

        private IEnumerator OpenSequence(float speed)
        {
            isOpening = true;

            // 시작 사운드
            if (openStartSound != null)
            {
                audioSource.PlayOneShot(openStartSound);
            }

            OnDoorOpen?.Invoke();

            // 루프 사운드
            if (openLoopSound != null)
            {
                audioSource.clip = openLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // 이동
            while (Vector3.Distance(transform.localPosition, openPosition) > 0.01f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    openPosition,
                    speed * Time.deltaTime
                );
                yield return null;
            }

            transform.localPosition = openPosition;

            // 루프 사운드 정지
            audioSource.Stop();

            // 완료 사운드
            if (openEndSound != null)
            {
                audioSource.PlayOneShot(openEndSound);
            }

            isOpen = true;
            isOpening = false;

            OnDoorFullyOpen?.Invoke();
            Debug.Log("[SlidingDoor] 문 완전히 열림");
        }

        private IEnumerator OpenSlowlySequence()
        {
            isOpening = true;

            // 시작 사운드
            if (openStartSound != null)
            {
                audioSource.PlayOneShot(openStartSound);
            }

            OnDoorOpen?.Invoke();

            // 루프 사운드
            if (openLoopSound != null)
            {
                audioSource.clip = openLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // 천천히 이동 (지정된 시간 동안)
            float elapsed = 0f;
            Vector3 startPos = transform.localPosition;

            while (elapsed < slowOpenDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / slowOpenDuration;
                t = t * t; // Ease in (처음엔 천천히, 나중에 빠르게)

                transform.localPosition = Vector3.Lerp(startPos, openPosition, t);
                yield return null;
            }

            transform.localPosition = openPosition;

            // 루프 사운드 정지
            audioSource.Stop();

            // 완료 사운드
            if (openEndSound != null)
            {
                audioSource.PlayOneShot(openEndSound);
            }

            isOpen = true;
            isOpening = false;

            OnDoorFullyOpen?.Invoke();

            // RoomProgressManager에 알림
            if (RoomProgressManager.Instance != null)
            {
                RoomProgressManager.Instance.OnUseExitDoor();
            }

            Debug.Log("[SlidingDoor] 탈출문 완전히 열림");
        }

        /// <summary>
        /// 문 닫기
        /// </summary>
        public void CloseDoor()
        {
            if (!isOpen) return;

            if (openCoroutine != null)
            {
                StopCoroutine(openCoroutine);
            }

            openCoroutine = StartCoroutine(CloseSequence());
        }

        private IEnumerator CloseSequence()
        {
            isOpening = false;

            while (Vector3.Distance(transform.localPosition, closedPosition) > 0.01f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    closedPosition,
                    normalOpenSpeed * Time.deltaTime
                );
                yield return null;
            }

            transform.localPosition = closedPosition;
            isOpen = false;

            Debug.Log("[SlidingDoor] 문 닫힘");
        }

        /// <summary>
        /// 열림 진행도 (0~1)
        /// </summary>
        public float GetOpenProgress()
        {
            float totalDistance = Vector3.Distance(closedPosition, openPosition);
            float currentDistance = Vector3.Distance(closedPosition, transform.localPosition);
            return Mathf.Clamp01(currentDistance / totalDistance);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 closedPos = Application.isPlaying ? closedPosition : transform.localPosition;
            Vector3 openPos = closedPos + slideDirection.normalized * openDistance;

            // 월드 좌표로 변환
            if (transform.parent != null)
            {
                closedPos = transform.parent.TransformPoint(closedPos);
                openPos = transform.parent.TransformPoint(openPos);
            }

            // 닫힌 위치
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(closedPos, Vector3.one * 0.5f);

            // 열린 위치
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(openPos, Vector3.one * 0.5f);

            // 이동 경로
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(closedPos, openPos);
        }
#endif
    }
}
