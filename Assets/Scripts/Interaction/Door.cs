using UnityEngine;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 열고 닫을 수 있는 문
    /// 잠금 기능 및 열쇠 시스템 지원
    /// </summary>
    public class Door : InteractableObject
    {
        [Header("Door Settings")]
        [Tooltip("문 열림 상태")]
        public bool isOpen = false;

        [Tooltip("문 열림 각도")]
        public float openAngle = 90f;

        [Tooltip("문 열림/닫힘 속도")]
        public float doorSpeed = 2f;

        [Tooltip("문 회전 축 (로컬)")]
        public Vector3 rotationAxis = Vector3.up;

        [Header("Lock Settings")]
        [Tooltip("잠금 상태")]
        public bool isLocked = false;

        [Tooltip("필요한 열쇠 ID")]
        public string requiredKeyId = "";

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip lockedSound;
        public AudioClip unlockSound;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnDoorOpen;
        public UnityEngine.Events.UnityEvent OnDoorClose;
        public UnityEngine.Events.UnityEvent OnDoorLocked;
        public UnityEngine.Events.UnityEvent OnDoorUnlocked;

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Coroutine doorCoroutine;

        protected override void Awake()
        {
            base.Awake();

            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            if (isOpen)
            {
                transform.localRotation = openRotation;
            }
        }

        public override void Interact()
        {
            if (!canInteract) return;

            // 잠금 상태 체크
            if (isLocked)
            {
                // 플레이어가 열쇠를 가지고 있는지 확인
                if (PlayerInventory.Instance != null &&
                    PlayerInventory.Instance.HasKey(requiredKeyId))
                {
                    Unlock();
                }
                else
                {
                    // 잠겨있음 알림
                    if (lockedSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(lockedSound);
                    }
                    OnDoorLocked?.Invoke();
                    Debug.Log($"[Door] {gameObject.name} 잠겨있습니다. 열쇠 필요: {requiredKeyId}");
                    return;
                }
            }

            // 문 열기/닫기 토글
            ToggleDoor();
        }

        /// <summary>
        /// 문 열기/닫기 토글
        /// </summary>
        public void ToggleDoor()
        {
            if (isOpen)
                CloseDoor();
            else
                OpenDoor();
        }

        /// <summary>
        /// 문 열기
        /// </summary>
        public void OpenDoor()
        {
            if (isOpen || isLocked) return;

            if (doorCoroutine != null)
                StopCoroutine(doorCoroutine);

            doorCoroutine = StartCoroutine(RotateDoor(openRotation, true));
        }

        /// <summary>
        /// 문 닫기
        /// </summary>
        public void CloseDoor()
        {
            if (!isOpen) return;

            if (doorCoroutine != null)
                StopCoroutine(doorCoroutine);

            doorCoroutine = StartCoroutine(RotateDoor(closedRotation, false));
        }

        private IEnumerator RotateDoor(Quaternion targetRotation, bool opening)
        {
            // 사운드 재생
            AudioClip sound = opening ? openSound : closeSound;
            if (sound != null && audioSource != null)
            {
                audioSource.PlayOneShot(sound);
            }

            // 문 회전
            while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
            {
                transform.localRotation = Quaternion.Slerp(
                    transform.localRotation,
                    targetRotation,
                    Time.deltaTime * doorSpeed
                );
                yield return null;
            }

            transform.localRotation = targetRotation;
            isOpen = opening;

            if (opening)
            {
                OnDoorOpen?.Invoke();
                Debug.Log($"[Door] {gameObject.name} 열림");
            }
            else
            {
                OnDoorClose?.Invoke();
                Debug.Log($"[Door] {gameObject.name} 닫힘");
            }
        }

        /// <summary>
        /// 문 잠금
        /// </summary>
        public void Lock()
        {
            isLocked = true;
            Debug.Log($"[Door] {gameObject.name} 잠금");
        }

        /// <summary>
        /// 문 잠금 해제
        /// </summary>
        public void Unlock()
        {
            if (!isLocked) return;

            isLocked = false;

            if (unlockSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }

            OnDoorUnlocked?.Invoke();
            Debug.Log($"[Door] {gameObject.name} 잠금 해제");
        }

        /// <summary>
        /// 열쇠 ID 설정
        /// </summary>
        public void SetRequiredKey(string keyId)
        {
            requiredKeyId = keyId;
            isLocked = !string.IsNullOrEmpty(keyId);
        }
    }
}
