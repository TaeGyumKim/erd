using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 탈출문 - 최종 열쇠로 열 수 있는 탈출구
    /// </summary>
    public class ExitDoor : InteractableObject
    {
        [Header("Exit Door Settings")]
        [Tooltip("탈출문 ID")]
        public string exitDoorId = "exit_door";

        [Tooltip("문 피벗")]
        public Transform doorPivot;

        [Tooltip("열림 각도")]
        public float openAngle = 90f;

        [Tooltip("열리는 속도")]
        public float openSpeed = 2f;

        [Header("Requirements")]
        [Tooltip("최종 열쇠 필요")]
        public bool requiresFinalKey = true;

        [Tooltip("모든 단서 필요")]
        public bool requiresAllClues = true;

        [Header("Visual")]
        [Tooltip("문 잠금 아이콘")]
        public GameObject lockedIcon;

        [Tooltip("탈출 가능 이펙트")]
        public ParticleSystem exitReadyEffect;

        [Tooltip("탈출 이펙트")]
        public ParticleSystem escapeEffect;

        [Header("Audio")]
        public AudioClip lockedSound;
        public AudioClip unlockSound;
        public AudioClip openSound;
        public AudioClip escapeSound;

        [Header("Events")]
        public UnityEvent OnDoorLocked;
        public UnityEvent OnDoorUnlocked;
        public UnityEvent OnEscapeStart;
        public UnityEvent OnEscapeComplete;

        private bool isLocked = true;
        private bool isOpen = false;
        private bool isEscaping = false;
        private AudioSource audioSource;
        private Quaternion closedRotation;
        private Quaternion openRotation;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            if (doorPivot != null)
            {
                closedRotation = doorPivot.localRotation;
                openRotation = Quaternion.Euler(doorPivot.localEulerAngles + new Vector3(0, openAngle, 0));
            }

            // 초기 상태
            if (lockedIcon != null)
            {
                lockedIcon.SetActive(true);
            }
        }

        private void Update()
        {
            // 탈출 조건 체크
            CheckEscapeConditions();
        }

        /// <summary>
        /// 탈출 조건 확인
        /// </summary>
        private void CheckEscapeConditions()
        {
            if (!isLocked || StoryProgressManager.Instance == null) return;

            bool canUnlock = true;

            if (requiresFinalKey && !StoryProgressManager.Instance.hasFinalKey)
            {
                canUnlock = false;
            }

            if (requiresAllClues)
            {
                if (!StoryProgressManager.Instance.hasUSB ||
                    !StoryProgressManager.Instance.hasLighter ||
                    !StoryProgressManager.Instance.hasSecurityCard)
                {
                    canUnlock = false;
                }
            }

            // 탈출 준비 완료
            if (canUnlock && isLocked)
            {
                if (exitReadyEffect != null && !exitReadyEffect.isPlaying)
                {
                    exitReadyEffect.Play();
                }
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (isEscaping) return;

            if (isLocked)
            {
                TryUnlock();
            }
            else if (!isOpen)
            {
                OpenDoor();
            }
            else
            {
                // 문이 열려있으면 탈출
                StartEscape();
            }
        }

        /// <summary>
        /// 잠금 해제 시도
        /// </summary>
        public void TryUnlock()
        {
            if (StoryProgressManager.Instance == null)
            {
                // 스토리 매니저 없으면 바로 열기
                Unlock();
                return;
            }

            // 최종 열쇠 확인
            if (requiresFinalKey && !StoryProgressManager.Instance.hasFinalKey)
            {
                if (lockedSound != null)
                {
                    audioSource.PlayOneShot(lockedSound);
                }

                if (VRHUD.Instance != null)
                {
                    VRHUD.Instance.ShowStatus("탈출 열쇠가 필요합니다", 2f);
                }

                OnDoorLocked?.Invoke();
                return;
            }

            // 모든 단서 확인
            if (requiresAllClues)
            {
                if (!StoryProgressManager.Instance.hasUSB)
                {
                    ShowMissingClue("USB를 찾아야 합니다");
                    return;
                }
                if (!StoryProgressManager.Instance.hasLighter)
                {
                    ShowMissingClue("라이터를 찾아야 합니다");
                    return;
                }
                if (!StoryProgressManager.Instance.hasSecurityCard)
                {
                    ShowMissingClue("보안카드를 찾아야 합니다");
                    return;
                }
            }

            // 잠금 해제
            Unlock();
        }

        private void ShowMissingClue(string message)
        {
            if (lockedSound != null)
            {
                audioSource.PlayOneShot(lockedSound);
            }

            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus(message, 2f);
            }

            OnDoorLocked?.Invoke();
        }

        /// <summary>
        /// 잠금 해제
        /// </summary>
        public void Unlock()
        {
            if (!isLocked) return;

            isLocked = false;

            if (unlockSound != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }

            if (lockedIcon != null)
            {
                lockedIcon.SetActive(false);
            }

            if (exitReadyEffect != null)
            {
                exitReadyEffect.Stop();
            }

            OnDoorUnlocked?.Invoke();

            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus("잠금이 해제되었습니다!", 2f);
            }

            Debug.Log("[ExitDoor] 탈출문 잠금 해제!");

            // 바로 문 열기
            OpenDoor();
        }

        /// <summary>
        /// 문 열기
        /// </summary>
        public void OpenDoor()
        {
            if (isOpen || isLocked) return;

            isOpen = true;

            if (openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            // 문 열기 애니메이션
            StartCoroutine(OpenDoorAnimation());
        }

        private IEnumerator OpenDoorAnimation()
        {
            if (doorPivot == null) yield break;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * openSpeed;
                doorPivot.localRotation = Quaternion.Slerp(closedRotation, openRotation, t);
                yield return null;
            }

            doorPivot.localRotation = openRotation;

            // 탈출 유도
            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus("문이 열렸다! 탈출하세요!", 3f);
            }
        }

        /// <summary>
        /// 탈출 시작
        /// </summary>
        public void StartEscape()
        {
            if (isEscaping || isLocked || !isOpen) return;

            isEscaping = true;

            OnEscapeStart?.Invoke();

            if (escapeEffect != null)
            {
                escapeEffect.Play();
            }

            if (escapeSound != null)
            {
                audioSource.PlayOneShot(escapeSound);
            }

            // 스토리 매니저에 탈출 알림
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.TryEscape();
            }
            else if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.Victory();
            }

            OnEscapeComplete?.Invoke();
        }

        /// <summary>
        /// 문 앞 탈출 트리거 영역에 진입
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && isOpen && !isLocked)
            {
                StartEscape();
            }
        }

        /// <summary>
        /// 강제 잠금 해제 (디버그용)
        /// </summary>
        [ContextMenu("Force Unlock")]
        public void ForceUnlock()
        {
            requiresFinalKey = false;
            requiresAllClues = false;
            Unlock();
        }

        private void OnDrawGizmosSelected()
        {
            // 문 열림 방향 표시
            if (doorPivot != null)
            {
                Gizmos.color = Color.green;
                Vector3 openDirection = Quaternion.Euler(0, openAngle, 0) * doorPivot.forward;
                Gizmos.DrawRay(doorPivot.position, openDirection * 2f);
            }
        }
    }
}
