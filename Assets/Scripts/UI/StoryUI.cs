using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 스토리 UI - 메시지 표시, 타이머, 단서 수집 현황
    /// VR 환경에서 월드 스페이스 캔버스로 표시
    /// </summary>
    public class StoryUI : MonoBehaviour
    {
        public static StoryUI Instance { get; private set; }

        [Header("Message Panel")]
        [Tooltip("메시지 패널")]
        public GameObject messagePanel;

        [Tooltip("메시지 텍스트")]
        public TextMeshProUGUI messageText;

        [Tooltip("메시지 배경")]
        public Image messageBackground;

        [Tooltip("메시지 표시 시간")]
        public float defaultMessageDuration = 4f;

        [Header("Timer Panel")]
        [Tooltip("타이머 패널")]
        public GameObject timerPanel;

        [Tooltip("타이머 텍스트")]
        public TextMeshProUGUI timerText;

        [Tooltip("위험 시간 (초)")]
        public float dangerTime = 60f;

        [Tooltip("위험 색상")]
        public Color dangerColor = Color.red;

        [Tooltip("일반 색상")]
        public Color normalColor = Color.white;

        [Header("Clue Panel")]
        [Tooltip("단서 패널")]
        public GameObject cluePanel;

        [Tooltip("USB 아이콘")]
        public Image usbIcon;

        [Tooltip("라이터 아이콘")]
        public Image lighterIcon;

        [Tooltip("보안카드 아이콘")]
        public Image securityCardIcon;

        [Tooltip("열쇠 아이콘")]
        public Image keyIcon;

        [Tooltip("미획득 색상")]
        public Color notCollectedColor = new Color(1f, 1f, 1f, 0.3f);

        [Tooltip("획득 색상")]
        public Color collectedColor = Color.white;

        [Header("Intro Panel")]
        [Tooltip("도입 패널")]
        public GameObject introPanel;

        [Tooltip("도입 텍스트")]
        public TextMeshProUGUI introText;

        [Tooltip("시작 버튼")]
        public Button startButton;

        [Header("Objective Panel")]
        [Tooltip("목표 패널")]
        public GameObject objectivePanel;

        [Tooltip("현재 목표 텍스트")]
        public TextMeshProUGUI objectiveText;

        [Header("Animation")]
        [Tooltip("페이드 속도")]
        public float fadeSpeed = 2f;

        [Header("Audio")]
        public AudioClip messageAppearSound;
        public AudioClip timerTickSound;
        public AudioClip clueCollectedSound;

        private AudioSource audioSource;
        private Coroutine currentMessageCoroutine;

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

            // 초기 상태
            HideAllPanels();
        }

        private void Start()
        {
            // 스토리 매니저 이벤트 연결
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.OnStoryMessage.AddListener(ShowMessage);
                StoryProgressManager.Instance.OnClueFound.AddListener(UpdateCluePanel);
                StoryProgressManager.Instance.OnIntroStart.AddListener(ShowIntro);
            }

            // 게임 매니저 이벤트 연결
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.OnTimeUpdate.AddListener(UpdateTimer);
            }

            // 단서 패널 초기화
            UpdateCluePanel();
        }

        private void HideAllPanels()
        {
            if (messagePanel != null) messagePanel.SetActive(false);
            if (introPanel != null) introPanel.SetActive(false);
            // 타이머와 단서 패널은 게임 중 표시
        }

        #region 메시지 표시

        /// <summary>
        /// 메시지 표시
        /// </summary>
        public void ShowMessage(string message)
        {
            ShowMessage(message, defaultMessageDuration);
        }

        /// <summary>
        /// 메시지 표시 (시간 지정)
        /// </summary>
        public void ShowMessage(string message, float duration)
        {
            if (currentMessageCoroutine != null)
            {
                StopCoroutine(currentMessageCoroutine);
            }

            currentMessageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
        }

        private IEnumerator ShowMessageCoroutine(string message, float duration)
        {
            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            if (messageAppearSound != null)
            {
                audioSource.PlayOneShot(messageAppearSound);
            }

            // 페이드 인
            yield return StartCoroutine(FadePanel(messagePanel, 0f, 1f));

            yield return new WaitForSeconds(duration);

            // 페이드 아웃
            yield return StartCoroutine(FadePanel(messagePanel, 1f, 0f));

            if (messagePanel != null)
            {
                messagePanel.SetActive(false);
            }

            currentMessageCoroutine = null;
        }

        #endregion

        #region 타이머

        /// <summary>
        /// 타이머 업데이트
        /// </summary>
        public void UpdateTimer(float remainingTime)
        {
            if (timerPanel != null)
            {
                timerPanel.SetActive(true);
            }

            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60);
                int seconds = Mathf.FloorToInt(remainingTime % 60);
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

                // 위험 시간 색상
                if (remainingTime <= dangerTime)
                {
                    timerText.color = dangerColor;

                    // 깜빡임
                    float blink = Mathf.PingPong(Time.time * 4f, 1f);
                    timerText.color = Color.Lerp(dangerColor, normalColor, blink);

                    // 틱 사운드
                    if (timerTickSound != null && remainingTime % 1f < 0.1f)
                    {
                        audioSource.PlayOneShot(timerTickSound, 0.5f);
                    }
                }
                else
                {
                    timerText.color = normalColor;
                }
            }
        }

        /// <summary>
        /// 타이머 숨기기
        /// </summary>
        public void HideTimer()
        {
            if (timerPanel != null)
            {
                timerPanel.SetActive(false);
            }
        }

        #endregion

        #region 단서 패널

        /// <summary>
        /// 단서 패널 업데이트
        /// </summary>
        public void UpdateCluePanel()
        {
            if (cluePanel != null)
            {
                cluePanel.SetActive(true);
            }

            if (StoryProgressManager.Instance == null) return;

            // USB
            if (usbIcon != null)
            {
                usbIcon.color = StoryProgressManager.Instance.hasUSB ? collectedColor : notCollectedColor;
            }

            // 라이터
            if (lighterIcon != null)
            {
                lighterIcon.color = StoryProgressManager.Instance.hasLighter ? collectedColor : notCollectedColor;
            }

            // 보안카드
            if (securityCardIcon != null)
            {
                securityCardIcon.color = StoryProgressManager.Instance.hasSecurityCard ? collectedColor : notCollectedColor;
            }

            // 열쇠
            if (keyIcon != null)
            {
                keyIcon.color = StoryProgressManager.Instance.hasFinalKey ? collectedColor : notCollectedColor;
            }

            // 사운드
            if (clueCollectedSound != null)
            {
                audioSource.PlayOneShot(clueCollectedSound);
            }
        }

        #endregion

        #region 도입부

        /// <summary>
        /// 도입부 표시
        /// </summary>
        public void ShowIntro()
        {
            StartCoroutine(ShowIntroSequence());
        }

        private IEnumerator ShowIntroSequence()
        {
            if (introPanel != null)
            {
                introPanel.SetActive(true);
            }

            if (introText != null)
            {
                introText.text = "어두운 저택에서 눈을 뜬다...\n\n" +
                                 "머리가 아프다. 무슨 일이 있었던 걸까?\n\n" +
                                 "이곳에서 빠져나가야 한다.";
            }

            yield return StartCoroutine(FadePanel(introPanel, 0f, 1f));

            // 시작 버튼 대기 또는 자동 진행
            if (startButton != null)
            {
                // 버튼 클릭 대기
                bool clicked = false;
                startButton.onClick.AddListener(() => clicked = true);

                while (!clicked)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(5f);
            }

            yield return StartCoroutine(FadePanel(introPanel, 1f, 0f));

            if (introPanel != null)
            {
                introPanel.SetActive(false);
            }

            // 탐색 시작
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.StartExploration();
            }
        }

        #endregion

        #region 목표 표시

        /// <summary>
        /// 현재 목표 표시
        /// </summary>
        public void ShowObjective(string objective)
        {
            if (objectivePanel != null)
            {
                objectivePanel.SetActive(true);
            }

            if (objectiveText != null)
            {
                objectiveText.text = objective;
            }
        }

        /// <summary>
        /// 목표 숨기기
        /// </summary>
        public void HideObjective()
        {
            if (objectivePanel != null)
            {
                objectivePanel.SetActive(false);
            }
        }

        #endregion

        #region 유틸리티

        private IEnumerator FadePanel(GameObject panel, float from, float to)
        {
            if (panel == null) yield break;

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * fadeSpeed;
                canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        #endregion

        /// <summary>
        /// 게임 오버 메시지
        /// </summary>
        public void ShowGameOver()
        {
            ShowMessage("잡혔다...\n\nGAME OVER", 5f);
        }

        /// <summary>
        /// 승리 메시지
        /// </summary>
        public void ShowVictory()
        {
            ShowMessage("탈출 성공!\n\n\"이제 자유다.\"", 5f);
        }
    }
}
