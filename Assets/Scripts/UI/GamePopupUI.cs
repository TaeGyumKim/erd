using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 게임 팝업 UI 시스템
    /// 스토리 메시지, 비밀번호 입력, 확인 다이얼로그 등
    /// </summary>
    public class GamePopupUI : MonoBehaviour
    {
        public static GamePopupUI Instance { get; private set; }

        [Header("Main Popup")]
        [Tooltip("팝업 캔버스")]
        public Canvas popupCanvas;

        [Tooltip("팝업 패널")]
        public GameObject popupPanel;

        [Tooltip("팝업 제목")]
        public TextMeshProUGUI titleText;

        [Tooltip("팝업 내용")]
        public TextMeshProUGUI contentText;

        [Tooltip("확인 버튼")]
        public Button confirmButton;

        [Tooltip("닫기 버튼")]
        public Button closeButton;

        [Header("Password Input")]
        [Tooltip("비밀번호 입력 패널")]
        public GameObject passwordPanel;

        [Tooltip("비밀번호 입력 필드")]
        public TMP_InputField passwordInput;

        [Tooltip("비밀번호 표시 텍스트")]
        public TextMeshProUGUI passwordDisplayText;

        [Tooltip("숫자 버튼들 (0-9)")]
        public Button[] numberButtons;

        [Tooltip("지우기 버튼")]
        public Button clearButton;

        [Tooltip("백스페이스 버튼")]
        public Button backspaceButton;

        [Tooltip("비밀번호 확인 버튼")]
        public Button passwordSubmitButton;

        [Tooltip("비밀번호 패널 닫기 버튼")]
        public Button passwordCloseButton;

        [Header("Animation")]
        [Tooltip("팝업 애니메이터")]
        public Animator popupAnimator;

        [Tooltip("페이드 인/아웃 시간")]
        public float fadeDuration = 0.3f;

        [Header("Auto Close")]
        [Tooltip("자동 닫기 활성화")]
        public bool autoClose = false;

        [Tooltip("자동 닫기 시간")]
        public float autoCloseTime = 5f;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip buttonClickSound;

        [Header("Events")]
        public UnityEvent OnPopupOpen;
        public UnityEvent OnPopupClose;

        private AudioSource audioSource;
        private Action currentCloseCallback;
        private Action<string> currentPasswordCallback;
        private string currentPasswordInput = "";
        private int currentPasswordLength = 4;
        private Coroutine autoCloseCoroutine;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            canvasGroup = popupPanel?.GetComponent<CanvasGroup>();
            if (canvasGroup == null && popupPanel != null)
            {
                canvasGroup = popupPanel.AddComponent<CanvasGroup>();
            }

            AutoFindKeypadElements();
            SetupButtons();
            HideAllPanels();
        }

        /// <summary>
        /// 키패드 UI 요소 자동 검색
        /// </summary>
        private void AutoFindKeypadElements()
        {
            if (passwordPanel == null) return;

            Transform keypadBg = passwordPanel.transform.Find("KeypadBackground");
            if (keypadBg == null) return;

            // 닫기 버튼
            if (passwordCloseButton == null)
            {
                var closeBtn = keypadBg.Find("CloseButton");
                if (closeBtn != null) passwordCloseButton = closeBtn.GetComponent<Button>();
            }

            // 비밀번호 디스플레이
            if (passwordDisplayText == null)
            {
                var display = keypadBg.Find("PasswordDisplay/DisplayText");
                if (display != null) passwordDisplayText = display.GetComponent<TMPro.TextMeshProUGUI>();
            }

            // 숫자 그리드
            Transform grid = keypadBg.Find("NumberGrid");
            if (grid != null)
            {
                // 숫자 버튼 배열 초기화
                if (numberButtons == null || numberButtons.Length < 10)
                {
                    numberButtons = new Button[10];
                }

                // 숫자 버튼 찾기
                for (int i = 0; i < 10; i++)
                {
                    if (numberButtons[i] == null)
                    {
                        var btn = grid.Find($"Button_{i}");
                        if (btn != null) numberButtons[i] = btn.GetComponent<Button>();
                    }
                }

                // 백스페이스 버튼
                if (backspaceButton == null)
                {
                    var backspace = grid.Find("BackspaceButton");
                    if (backspace != null) backspaceButton = backspace.GetComponent<Button>();
                }

                // Enter 버튼
                if (passwordSubmitButton == null)
                {
                    var enter = grid.Find("EnterButton");
                    if (enter != null) passwordSubmitButton = enter.GetComponent<Button>();
                }
            }

            Debug.Log("[GamePopupUI] 키패드 UI 요소 자동 검색 완료");
        }

        private void SetupButtons()
        {
            // 확인/닫기 버튼
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClick);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePopup);
            }

            // 숫자 버튼들
            if (numberButtons != null)
            {
                for (int i = 0; i < numberButtons.Length && i < 10; i++)
                {
                    int number = i;
                    if (numberButtons[i] != null)
                    {
                        numberButtons[i].onClick.AddListener(() => OnNumberClick(number));
                    }
                }
            }

            // 비밀번호 관련 버튼
            if (clearButton != null)
            {
                clearButton.onClick.AddListener(ClearPasswordInput);
            }

            if (backspaceButton != null)
            {
                backspaceButton.onClick.AddListener(BackspacePassword);
            }

            if (passwordSubmitButton != null)
            {
                passwordSubmitButton.onClick.AddListener(SubmitPassword);
            }

            if (passwordCloseButton != null)
            {
                passwordCloseButton.onClick.AddListener(ClosePasswordInput);
            }
        }

        private void HideAllPanels()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
            if (passwordPanel != null) passwordPanel.SetActive(false);
        }

        #region General Popup

        /// <summary>
        /// 일반 팝업 표시
        /// </summary>
        public void ShowPopup(string title, string content, Action onClose = null)
        {
            if (popupPanel == null) return;

            // 기존 팝업 닫기
            if (popupPanel.activeSelf)
            {
                ClosePopup();
            }

            // 콜백 저장
            currentCloseCallback = onClose;

            // 내용 설정
            if (titleText != null) titleText.text = title;
            if (contentText != null) contentText.text = content;

            // 비밀번호 패널 숨기기
            if (passwordPanel != null) passwordPanel.SetActive(false);

            // 팝업 표시
            popupPanel.SetActive(true);

            // 사운드
            if (openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            // 애니메이션
            if (popupAnimator != null)
            {
                popupAnimator.SetTrigger("Open");
            }
            else
            {
                StartCoroutine(FadeIn());
            }

            // 자동 닫기
            if (autoClose)
            {
                if (autoCloseCoroutine != null)
                {
                    StopCoroutine(autoCloseCoroutine);
                }
                autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
            }

            OnPopupOpen?.Invoke();
            Debug.Log($"[GamePopupUI] 팝업 열림: {title}");
        }

        /// <summary>
        /// 자동 닫기 대기
        /// </summary>
        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(autoCloseTime);
            ClosePopup();
        }

        /// <summary>
        /// 확인 버튼 클릭
        /// </summary>
        private void OnConfirmClick()
        {
            PlayButtonSound();
            ClosePopup();
        }

        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void ClosePopup()
        {
            if (popupPanel == null || !popupPanel.activeSelf) return;

            // 자동 닫기 취소
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }

            // 사운드
            if (closeSound != null)
            {
                audioSource.PlayOneShot(closeSound);
            }

            // 애니메이션
            if (popupAnimator != null)
            {
                popupAnimator.SetTrigger("Close");
                StartCoroutine(HidePanelAfterAnimation());
            }
            else
            {
                StartCoroutine(FadeOut());
            }

            // 콜백 실행
            currentCloseCallback?.Invoke();
            currentCloseCallback = null;

            OnPopupClose?.Invoke();
            Debug.Log("[GamePopupUI] 팝업 닫힘");
        }

        private IEnumerator HidePanelAfterAnimation()
        {
            yield return new WaitForSeconds(0.3f);
            popupPanel.SetActive(false);
        }

        #endregion

        #region Password Input

        /// <summary>
        /// 비밀번호 입력 팝업 표시
        /// VRPasswordKeypad가 있으면 이 메서드는 아무것도 하지 않음
        /// </summary>
        public void ShowPasswordInput(string title, int passwordLength, Action<string> onSubmit)
        {
            // VRPasswordKeypad가 있으면 VR 키패드 사용 (이 UI 사용 안함)
            if (VRPasswordKeypad.Instance != null)
            {
                Debug.Log("[GamePopupUI] VRPasswordKeypad가 있으므로 GamePopupUI 비밀번호 입력 건너뜀");
                return;
            }

            if (popupPanel == null) return;

            currentPasswordLength = passwordLength;
            currentPasswordCallback = onSubmit;
            currentPasswordInput = "";

            // 내용 설정
            if (titleText != null) titleText.text = title;
            if (contentText != null) contentText.text = "숫자를 입력하세요";

            // 비밀번호 패널 표시
            if (passwordPanel != null) passwordPanel.SetActive(true);

            UpdatePasswordDisplay();

            // 팝업 표시
            popupPanel.SetActive(true);

            // 사운드
            if (openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            // 애니메이션
            if (popupAnimator != null)
            {
                popupAnimator.SetTrigger("Open");
            }
            else
            {
                StartCoroutine(FadeIn());
            }

            OnPopupOpen?.Invoke();
            Debug.Log($"[GamePopupUI] 비밀번호 입력 팝업 열림: {passwordLength}자리");
        }

        /// <summary>
        /// 숫자 버튼 클릭
        /// </summary>
        private void OnNumberClick(int number)
        {
            if (currentPasswordInput.Length >= currentPasswordLength) return;

            PlayButtonSound();
            currentPasswordInput += number.ToString();
            UpdatePasswordDisplay();

            // 자릿수 완료 시 자동 제출
            if (currentPasswordInput.Length >= currentPasswordLength)
            {
                StartCoroutine(AutoSubmitAfterDelay(0.3f));
            }
        }

        private IEnumerator AutoSubmitAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SubmitPassword();
        }

        /// <summary>
        /// 비밀번호 지우기
        /// </summary>
        private void ClearPasswordInput()
        {
            PlayButtonSound();
            currentPasswordInput = "";
            UpdatePasswordDisplay();
        }

        /// <summary>
        /// 백스페이스
        /// </summary>
        private void BackspacePassword()
        {
            if (currentPasswordInput.Length > 0)
            {
                PlayButtonSound();
                currentPasswordInput = currentPasswordInput.Substring(0, currentPasswordInput.Length - 1);
                UpdatePasswordDisplay();
            }
        }

        /// <summary>
        /// 비밀번호 제출
        /// </summary>
        private void SubmitPassword()
        {
            PlayButtonSound();

            // 콜백 실행
            currentPasswordCallback?.Invoke(currentPasswordInput);

            // 팝업 닫기
            ClosePopup();
            currentPasswordCallback = null;
        }

        /// <summary>
        /// 비밀번호 입력 취소 (X 버튼)
        /// </summary>
        public void ClosePasswordInput()
        {
            PlayButtonSound();

            // 콜백 없이 팝업 닫기
            currentPasswordCallback = null;
            currentPasswordInput = "";

            // 팝업 닫기
            ClosePopup();
            Debug.Log("[GamePopupUI] 비밀번호 입력 취소됨");
        }

        /// <summary>
        /// 비밀번호 디스플레이 업데이트
        /// </summary>
        private void UpdatePasswordDisplay()
        {
            if (passwordDisplayText != null)
            {
                string display = "";
                for (int i = 0; i < currentPasswordLength; i++)
                {
                    if (i < currentPasswordInput.Length)
                    {
                        display += "●";
                    }
                    else
                    {
                        display += "○";
                    }

                    if (i < currentPasswordLength - 1) display += " ";
                }
                passwordDisplayText.text = display;
            }

            if (passwordInput != null)
            {
                passwordInput.text = currentPasswordInput;
            }
        }

        #endregion

        #region Fade Effects

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = elapsed / fadeDuration;
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null)
            {
                popupPanel.SetActive(false);
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            popupPanel.SetActive(false);
        }

        #endregion

        private void PlayButtonSound()
        {
            if (buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }

        /// <summary>
        /// 간단한 메시지 표시 (자동 닫힘)
        /// </summary>
        public void ShowMessage(string message, float duration = 3f)
        {
            bool originalAutoClose = autoClose;
            float originalAutoCloseTime = autoCloseTime;

            autoClose = true;
            autoCloseTime = duration;

            ShowPopup("", message);

            // 원래 설정 복원
            autoClose = originalAutoClose;
            autoCloseTime = originalAutoCloseTime;
        }

        /// <summary>
        /// 확인 다이얼로그 표시
        /// </summary>
        public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null)
        {
            // TODO: 확인/취소 버튼이 있는 다이얼로그 구현
            ShowPopup(title, message, onConfirm);
        }
    }
}
