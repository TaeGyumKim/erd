using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;

namespace HorrorGame
{
    /// <summary>
    /// 상호작용 가능한 책
    /// 읽으면 스토리 팝업 표시 및 게임 진행 트리거
    /// </summary>
    public class InteractableBook : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
    {
        [Header("Book Content")]
        [Tooltip("책 제목")]
        public string bookTitle = "오래된 일기";

        [TextArea(5, 10)]
        [Tooltip("책 내용 (팝업에 표시)")]
        public string bookContent = "이곳에서 끔찍한 일이 일어났다...\n\n비밀번호: 1234";

        [Tooltip("읽기 완료 여부")]
        public bool hasBeenRead = false;

        [Header("Clue Information")]
        [Tooltip("비밀번호 힌트 포함 여부")]
        public bool containsPasswordHint = true;

        [Tooltip("비밀번호 값 (Room4 상자용)")]
        public string passwordHint = "1234";

        [Header("Visual")]
        [Tooltip("책 열림 애니메이션")]
        public Animator bookAnimator;

        [Tooltip("책이 빛나는 효과")]
        public GameObject glowEffect;

        [Tooltip("읽기 후 빛나는 효과 끄기")]
        public bool disableGlowAfterRead = true;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip pageFlipSound;
        public AudioClip closeSound;

        [Header("Events")]
        public UnityEvent OnBookOpened;
        public UnityEvent OnBookRead;
        public UnityEvent OnBookClosed;
        public UnityEvent<string> OnShowContent;

        private AudioSource audioSource;
        private bool isOpen = false;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (!isOpen)
            {
                OpenBook();
            }
            else
            {
                CloseBook();
            }
        }

        /// <summary>
        /// 책 열기
        /// </summary>
        public void OpenBook()
        {
            if (isOpen) return;

            isOpen = true;

            // 사운드
            if (openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            // 애니메이션
            if (bookAnimator != null)
            {
                bookAnimator.SetTrigger("Open");
            }

            OnBookOpened?.Invoke();

            // 내용 표시
            ShowBookContent();
        }

        /// <summary>
        /// 책 내용 표시
        /// </summary>
        private void ShowBookContent()
        {
            // 페이지 넘기는 소리
            if (pageFlipSound != null)
            {
                audioSource.PlayOneShot(pageFlipSound);
            }

            // 팝업 표시
            string fullContent = $"<b>{bookTitle}</b>\n\n{bookContent}";
            OnShowContent?.Invoke(fullContent);

            // GamePopupUI 연동
            if (GamePopupUI.Instance != null)
            {
                GamePopupUI.Instance.ShowPopup(bookTitle, bookContent, OnPopupClosed);
            }
            else
            {
                // 대체: VRHUD 사용
                if (VRHUD.Instance != null)
                {
                    VRHUD.Instance.ShowStatus(fullContent, 10f);
                }
            }

            // 읽기 완료 처리
            if (!hasBeenRead)
            {
                hasBeenRead = true;
                OnBookRead?.Invoke();

                // 빛나는 효과 끄기
                if (disableGlowAfterRead && glowEffect != null)
                {
                    glowEffect.SetActive(false);
                }

                // RoomProgressManager에 알림
                if (RoomProgressManager.Instance != null)
                {
                    RoomProgressManager.Instance.OnBookRead();
                }

                Debug.Log($"[InteractableBook] '{bookTitle}' 읽기 완료");
            }
        }

        /// <summary>
        /// 팝업 닫힐 때 콜백
        /// </summary>
        private void OnPopupClosed()
        {
            // 팝업이 닫히면 다음 단계로 진행
            Debug.Log("[InteractableBook] 팝업 닫힘 - 다음 단계 진행");
        }

        /// <summary>
        /// 책 닫기
        /// </summary>
        public void CloseBook()
        {
            if (!isOpen) return;

            isOpen = false;

            // 사운드
            if (closeSound != null)
            {
                audioSource.PlayOneShot(closeSound);
            }

            // 애니메이션
            if (bookAnimator != null)
            {
                bookAnimator.SetTrigger("Close");
            }

            OnBookClosed?.Invoke();
        }

        /// <summary>
        /// 비밀번호 힌트 가져오기
        /// </summary>
        public string GetPasswordHint()
        {
            if (containsPasswordHint && hasBeenRead)
            {
                return passwordHint;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 책 위치 표시
            Gizmos.color = hasBeenRead ? Color.gray : Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
        }
#endif
    }
}
