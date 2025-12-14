using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 비밀번호로 열리는 패널/문
    /// 클릭 시 힌트 메시지 표시 후 비밀번호 입력 UI 표시
    /// </summary>
    public class PasswordPanel : InteractableObject
    {
        [Header("Password Settings")]
        [Tooltip("비밀번호")]
        public string correctPassword = "1234";

        [Tooltip("비밀번호 자릿수")]
        public int passwordLength = 4;

        [Header("Hint Settings")]
        [Tooltip("힌트 메시지 표시 (첫 상호작용 시에만)")]
        public bool showHint = true;

        [Tooltip("힌트 팝업 제목")]
        public string hintTitle = "잠긴 패널";

        [Tooltip("힌트 메시지")]
        [TextArea(2, 4)]
        public string hintMessage = "비밀번호가 필요한 것 같다..";

        [Tooltip("힌트 표시 시간")]
        public float hintDisplayTime = 2f;

        // 힌트 표시 여부 (첫 상호작용 후 false)
        private bool hasShownHint = false;

        [Header("Password Input UI")]
        [Tooltip("비밀번호 입력 UI 제목")]
        public string passwordInputTitle = "비밀번호를 입력하세요";

        [Header("State")]
        [Tooltip("열림 상태")]
        public bool isOpen = false;

        [Tooltip("잠금 상태")]
        public bool isLocked = true;

        [Header("Open Animation")]
        [Tooltip("패널이 열릴 때 이동할 오브젝트 (없으면 자신)")]
        public Transform panelToOpen;

        [Tooltip("열림 방향 (로컬)")]
        public Vector3 openDirection = Vector3.up;

        [Tooltip("열림 거리")]
        public float openDistance = 2f;

        [Tooltip("열림 속도")]
        public float openSpeed = 2f;

        [Tooltip("열릴 때 오브젝트 비활성화 (슬라이딩 대신 사라짐)")]
        public bool deactivateOnOpen = false;

        [Header("Contents")]
        [Tooltip("패널 뒤의 아이템들 (열리면 활성화)")]
        public GameObject[] hiddenItems;

        [Header("Audio")]
        public AudioClip correctSound;
        public AudioClip wrongSound;
        public AudioClip openSound;

        [Header("Events")]
        public UnityEvent OnPasswordCorrect;
        public UnityEvent OnPasswordWrong;
        public UnityEvent OnPanelOpen;

        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isAnimating = false;

        protected override void Awake()
        {
            base.Awake();

            // 패널 오브젝트 설정
            if (panelToOpen == null)
            {
                panelToOpen = transform;
            }

            // 위치 저장
            closedPosition = panelToOpen.localPosition;
            openPosition = closedPosition + openDirection.normalized * openDistance;

            // 숨겨진 아이템 비활성화
            if (hiddenItems != null)
            {
                foreach (var item in hiddenItems)
                {
                    if (item != null)
                    {
                        item.SetActive(false);
                    }
                }
            }
        }

        public override void Interact()
        {
            if (!canInteract) return;
            if (isOpen) return;
            if (isAnimating) return;

            if (isLocked)
            {
                // VRPasswordKeypad가 있으면 바로 키패드 표시 (힌트 없이)
                if (VRPasswordKeypad.Instance != null)
                {
                    ShowPasswordInput();
                }
                // GamePopupUI만 있으면 힌트 표시 후 비밀번호 입력
                else if (showHint && !hasShownHint && GamePopupUI.Instance != null)
                {
                    hasShownHint = true;
                    // 힌트 표시 후 비밀번호 입력 UI 표시
                    GamePopupUI.Instance.autoClose = true;
                    GamePopupUI.Instance.autoCloseTime = hintDisplayTime;
                    GamePopupUI.Instance.ShowPopup(hintTitle, hintMessage, ShowPasswordInput);
                    Debug.Log($"[PasswordPanel] {gameObject.name} 힌트 표시: {hintMessage}");
                }
                else
                {
                    // 힌트 없이 바로 비밀번호 입력
                    ShowPasswordInput();
                }
            }
            else
            {
                // 잠금 해제되었으면 열기
                OpenPanel();
            }
        }

        /// <summary>
        /// 비밀번호 입력 UI 표시
        /// </summary>
        private void ShowPasswordInput()
        {
            // VR 키패드 우선 사용
            if (VRPasswordKeypad.Instance != null)
            {
                VRPasswordKeypad.Instance.Open(
                    passwordInputTitle,
                    passwordLength,
                    OnPasswordSubmit,
                    transform // 이 오브젝트 앞에 키패드 표시
                );
                Debug.Log($"[PasswordPanel] {gameObject.name} VR 비밀번호 키패드 표시 (타겟 전달)");
            }
            // 폴백: GamePopupUI 사용
            else if (GamePopupUI.Instance != null)
            {
                GamePopupUI.Instance.ShowPasswordInput(
                    passwordInputTitle,
                    passwordLength,
                    OnPasswordSubmit
                );
                Debug.Log($"[PasswordPanel] {gameObject.name} 비밀번호 입력 UI 표시 (GamePopupUI)");
            }
            else
            {
                Debug.LogWarning("[PasswordPanel] VRPasswordKeypad와 GamePopupUI 모두 없습니다!");
            }
        }

        /// <summary>
        /// 비밀번호 제출 콜백
        /// </summary>
        private void OnPasswordSubmit(string password)
        {
            if (password == correctPassword)
            {
                // 정답
                isLocked = false;

                if (correctSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(correctSound);
                }

                // 정답 메시지 표시
                if (VRHUD.Instance != null)
                {
                    VRHUD.Instance.ShowStatus("비밀번호가 맞았습니다!", 2f);
                }

                OnPasswordCorrect?.Invoke();
                Debug.Log($"[PasswordPanel] {gameObject.name} 비밀번호 정답!");

                // 자동으로 열기
                StartCoroutine(DelayedOpen(0.5f));
            }
            else
            {
                // 오답
                if (wrongSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(wrongSound);
                }

                OnPasswordWrong?.Invoke();
                Debug.Log($"[PasswordPanel] {gameObject.name} 비밀번호 오답: {password}");

                // 오답 메시지 표시 (VR HUD 우선)
                if (VRHUD.Instance != null)
                {
                    VRHUD.Instance.ShowStatus("비밀번호가 틀렸습니다.", 2f);
                }
                else if (GamePopupUI.Instance != null)
                {
                    GamePopupUI.Instance.ShowMessage("비밀번호가 틀렸습니다.", 1.5f);
                }
            }
        }

        private IEnumerator DelayedOpen(float delay)
        {
            yield return new WaitForSeconds(delay);
            OpenPanel();
        }

        /// <summary>
        /// 패널 열기
        /// </summary>
        public void OpenPanel()
        {
            if (isOpen || isLocked || isAnimating) return;

            isOpen = true;

            // 사운드
            if (openSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            // 애니메이션
            if (deactivateOnOpen)
            {
                panelToOpen.gameObject.SetActive(false);
            }
            else
            {
                StartCoroutine(OpenAnimation());
            }

            // 숨겨진 아이템 활성화
            if (hiddenItems != null)
            {
                foreach (var item in hiddenItems)
                {
                    if (item != null)
                    {
                        item.SetActive(true);
                    }
                }
            }

            OnPanelOpen?.Invoke();
            Debug.Log($"[PasswordPanel] {gameObject.name} 열림");
        }

        private IEnumerator OpenAnimation()
        {
            isAnimating = true;

            float elapsed = 0f;
            Vector3 startPos = panelToOpen.localPosition;

            while (elapsed < 1f / openSpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed * openSpeed;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                panelToOpen.localPosition = Vector3.Lerp(startPos, openPosition, t);
                yield return null;
            }

            panelToOpen.localPosition = openPosition;
            isAnimating = false;
        }

        /// <summary>
        /// 외부에서 비밀번호 설정 (PasswordBookTrigger에서 호출)
        /// </summary>
        public void SetPassword(string newPassword)
        {
            correctPassword = newPassword;
            passwordLength = newPassword.Length;
            Debug.Log($"[PasswordPanel] {gameObject.name} 비밀번호 설정: {newPassword}");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform panel = panelToOpen != null ? panelToOpen : transform;

            // 현재 위치
            Gizmos.color = isOpen ? Color.green : (isLocked ? Color.red : Color.yellow);
            Gizmos.DrawWireCube(panel.position, Vector3.one * 0.5f);

            // 열린 위치
            if (!isOpen)
            {
                Vector3 openPos = panel.position + panel.TransformDirection(openDirection.normalized * openDistance);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(openPos, Vector3.one * 0.3f);
                Gizmos.DrawLine(panel.position, openPos);
            }
        }
#endif
    }
}
