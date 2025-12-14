using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
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

        [Header("Pivot Settings")]
        [Tooltip("경첩(피벗) 위치 오프셋 - 문 메시의 로컬 좌표 기준")]
        public Vector3 pivotOffset = Vector3.zero;

        [Tooltip("자동으로 자식 메시의 피벗 조정 (Awake에서 실행)")]
        public bool autoAdjustChildPivot = true;

        [Header("Lock Settings")]
        [Tooltip("잠금 상태")]
        public bool isLocked = false;

        [Tooltip("필요한 열쇠 ID")]
        public string requiredKeyId = "";

        [Header("Locked Door Hint")]
        [Tooltip("잠긴 문 힌트 메시지 표시")]
        public bool showLockedHint = true;

        [Tooltip("힌트 팝업 제목")]
        public string lockedHintTitle = "잠긴 문";

        [Tooltip("힌트 메시지")]
        [TextArea(2, 4)]
        public string lockedHintMessage = "문이 잠겨 있다..\n열쇠가 필요할 것 같다..";

        [Tooltip("힌트 표시 시간")]
        public float hintDisplayTime = 2f;

        [Header("Peek Settings")]
        [Tooltip("엿보기 각도 (DoorPeek에서 설정)")]
        public float peekAngle = 0f;

        // 프로퍼티
        public bool IsOpen => isOpen;
        public bool IsLocked => isLocked;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip lockedSound;
        public AudioClip unlockSound;
        [Tooltip("문 두드리는 소리 (잠긴 문 상호작용 시)")]
        public AudioClip knockSound;

        [Header("NavMesh Settings")]
        [Tooltip("문에 연결된 NavMeshLink (자동 검색)")]
        public NavMeshLink navMeshLink;

        [Tooltip("NavMeshLink 자동 생성")]
        public bool autoCreateNavMeshLink = true;

        [Tooltip("NavMeshLink 시작점 오프셋")]
        public Vector3 linkStartOffset = new Vector3(0, 0, -1f);

        [Tooltip("NavMeshLink 끝점 오프셋")]
        public Vector3 linkEndOffset = new Vector3(0, 0, 1f);

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

            // AudioSource 확인/생성 (Door는 여러 사운드를 사용하므로 반드시 필요)
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1f; // 3D 사운드
                    audioSource.playOnAwake = false;
                    audioSource.minDistance = 1f;
                    audioSource.maxDistance = 15f;
                    Debug.Log($"[Door] {gameObject.name}에 AudioSource 생성됨");
                }
            }

            // 자식 메시의 피벗 자동 조정
            if (autoAdjustChildPivot && pivotOffset != Vector3.zero)
            {
                AdjustChildPivot();
            }

            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            // NavMeshLink 설정
            SetupNavMeshLink();

            if (isOpen)
            {
                transform.localRotation = openRotation;
                // 문이 열린 상태로 시작하면 NavMeshLink 활성화
                if (navMeshLink != null)
                {
                    navMeshLink.enabled = true;
                }
            }
        }

        /// <summary>
        /// NavMeshLink 설정 (자동 검색 또는 생성)
        /// </summary>
        private void SetupNavMeshLink()
        {
            // 이미 할당되어 있으면 스킵
            if (navMeshLink != null)
            {
                // 초기 상태: 문이 닫혀있으면 비활성화
                navMeshLink.enabled = isOpen;
                return;
            }

            // 자식에서 NavMeshLink 검색
            navMeshLink = GetComponentInChildren<NavMeshLink>();

            // 없으면 자동 생성
            if (navMeshLink == null && autoCreateNavMeshLink)
            {
                navMeshLink = gameObject.AddComponent<NavMeshLink>();

                // Link 오프셋 설정
                // linkStartOffset/linkEndOffset이 기본값이 아니면 그대로 사용
                // 기본값이면 문의 로컬 Z축 방향으로 설정 (문을 통과하는 방향)
                float linkDistance = 1.2f;

                if (linkStartOffset == new Vector3(0, 0, -1f) && linkEndOffset == new Vector3(0, 0, 1f))
                {
                    // 기본값 - 로컬 Z축 방향 사용
                    navMeshLink.startPoint = new Vector3(0, 0, -linkDistance);
                    navMeshLink.endPoint = new Vector3(0, 0, linkDistance);
                }
                else
                {
                    // 사용자 지정 값 사용
                    navMeshLink.startPoint = linkStartOffset;
                    navMeshLink.endPoint = linkEndOffset;
                }

                navMeshLink.width = 1.5f;
                navMeshLink.bidirectional = true;
                navMeshLink.autoUpdate = true;

                Debug.Log($"[Door] {gameObject.name}에 NavMeshLink 자동 생성됨 (start: {navMeshLink.startPoint}, end: {navMeshLink.endPoint})");
            }

            // 초기 상태: 문이 닫혀있으면 비활성화
            if (navMeshLink != null)
            {
                navMeshLink.enabled = isOpen;
            }
        }

        /// <summary>
        /// 자식 오브젝트들의 로컬 위치를 조정하여 피벗 변경 효과
        /// </summary>
        private void AdjustChildPivot()
        {
            foreach (Transform child in transform)
            {
                child.localPosition -= pivotOffset;
            }
            // 부모의 월드 위치도 보정
            transform.position += transform.TransformDirection(pivotOffset);
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
                    // 문 두드리는 소리 재생 (knockSound가 있으면 우선, 없으면 lockedSound)
                    if (audioSource != null)
                    {
                        if (knockSound != null)
                        {
                            audioSource.PlayOneShot(knockSound);
                        }
                        else if (lockedSound != null)
                        {
                            audioSource.PlayOneShot(lockedSound);
                        }
                    }

                    // 힌트 팝업 표시
                    if (showLockedHint && GamePopupUI.Instance != null)
                    {
                        GamePopupUI.Instance.autoClose = true;
                        GamePopupUI.Instance.autoCloseTime = hintDisplayTime;
                        GamePopupUI.Instance.ShowPopup(lockedHintTitle, lockedHintMessage);
                        Debug.Log($"[Door] {gameObject.name} 힌트 표시: {lockedHintMessage}");
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

            // NavMeshLink 활성화/비활성화
            if (navMeshLink != null)
            {
                navMeshLink.enabled = opening;
                Debug.Log($"[Door] {gameObject.name} NavMeshLink {(opening ? "활성화" : "비활성화")}");
            }

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

        /// <summary>
        /// 엿보기 각도 설정 (DoorPeek에서 호출)
        /// </summary>
        public void SetPeekAngle(float angle)
        {
            if (isOpen) return; // 이미 열려있으면 무시

            peekAngle = Mathf.Clamp(angle, 0, openAngle);

            // 엿보기 회전 적용
            Quaternion peekRotation = closedRotation * Quaternion.AngleAxis(peekAngle, rotationAxis);
            transform.localRotation = peekRotation;
        }

        /// <summary>
        /// 플레이어가 키를 들고 문에 부딪히면 문 열기/제거
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!isLocked) return;

            // PC 플레이어 체크
            var pcPlayer = other.GetComponent<PCPlayerController>();
            if (pcPlayer != null && pcPlayer.IsHoldingItem)
            {
                var heldItem = pcPlayer.HeldItem;
                if (heldItem is KeyItem keyItem && keyItem.keyId == requiredKeyId)
                {
                    Debug.Log($"[Door] 키로 문 열림 (충돌): {keyItem.keyId}");
                    pcPlayer.DropHeldItem(true); // 키 사용 후 파괴

                    // 문 제거
                    StartCoroutine(DestroyDoorWithDelay());
                    return;
                }
            }

            // VR 플레이어 체크 (인벤토리)
            if (other.CompareTag("Player"))
            {
                if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasKey(requiredKeyId))
                {
                    Debug.Log($"[Door] 키로 문 열림 (충돌, 인벤토리): {requiredKeyId}");
                    StartCoroutine(DestroyDoorWithDelay());
                }
            }
        }

        /// <summary>
        /// 플레이어가 키를 들고 문과 접촉 중이면 문 열기/제거
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (!isLocked) return;

            // PC 플레이어 체크
            var pcPlayer = collision.gameObject.GetComponent<PCPlayerController>();
            if (pcPlayer != null && pcPlayer.IsHoldingItem)
            {
                var heldItem = pcPlayer.HeldItem;
                if (heldItem is KeyItem keyItem && keyItem.keyId == requiredKeyId)
                {
                    Debug.Log($"[Door] 키로 문 열림 (물리 충돌): {keyItem.keyId}");
                    pcPlayer.DropHeldItem(true); // 키 사용 후 파괴

                    // 문 제거
                    StartCoroutine(DestroyDoorWithDelay());
                    return;
                }
            }
        }

        private IEnumerator DestroyDoorWithDelay()
        {
            // 잠금 해제 사운드 재생
            if (unlockSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }

            OnDoorUnlocked?.Invoke();

            // NavMeshLink 활성화 (문이 제거되어도 AI가 통과 가능)
            if (navMeshLink != null)
            {
                navMeshLink.enabled = true;
                // NavMeshLink를 씬 루트로 이동 (문이 제거되어도 유지)
                navMeshLink.transform.SetParent(null);
                Debug.Log($"[Door] {gameObject.name} NavMeshLink 활성화 및 분리");
            }

            // 잠시 대기 후 문 제거
            yield return new WaitForSeconds(0.3f);

            Debug.Log($"[Door] {gameObject.name} 제거됨");
            Destroy(gameObject);
        }
    }
}
