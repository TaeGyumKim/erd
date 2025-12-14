using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 게임 도입 시퀀스
    /// 플레이어가 방에서 깨어나는 연출
    /// - 화면 페이드 인
    /// - 도입 메시지
    /// - YES 버튼 클릭 후 게임 시작
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        public static IntroSequence Instance { get; private set; }

        [Header("Screen Fade")]
        [Tooltip("페이드 이미지 (검은 화면)")]
        public Image fadeImage;

        [Tooltip("페이드 인 시간")]
        public float fadeInDuration = 3f;

        [Tooltip("시작 시 검은 화면")]
        public bool startWithBlackScreen = true;

        [Header("Intro UI")]
        [Tooltip("도입 UI 캔버스")]
        public Canvas introCanvas;

        [Tooltip("도입 메시지 텍스트")]
        public TextMeshProUGUI introText;

        [Tooltip("YES 버튼")]
        public Button yesButton;

        [Tooltip("버튼 텍스트")]
        public TextMeshProUGUI yesButtonText;

        [Header("Messages")]
        [TextArea(3, 5)]
        public string[] introMessages = new string[]
        {
            "...",
            "어디지...?",
            "눈을 떴다...",
            "어둡다...\n\n아무것도 보이지 않는다.",
            "여기서 나가야 해...",
            "탈출해야 한다."
        };

        [TextArea(2, 3)]
        public string buttonPrompt = "눈을 뜨시겠습니까?";

        [Tooltip("메시지 간 대기 시간")]
        public float messageDelay = 2f;

        [Tooltip("타이핑 효과 속도")]
        public float typingSpeed = 0.05f;

        [Header("Audio")]
        public AudioClip heartbeatSound;
        public AudioClip breathingSound;
        public AudioClip ambientSound;
        public AudioClip buttonClickSound;

        [Tooltip("심장 박동 속도")]
        public float heartbeatInterval = 1f;

        [Header("Player Control")]
        [Tooltip("도입 중 플레이어 이동 비활성화")]
        public bool disablePlayerDuringIntro = true;

        [Header("Skip")]
        [Tooltip("스킵 버튼")]
        public Button skipButton;

        [Tooltip("스킵 버튼 텍스트")]
        public string skipButtonLabel = "스킵 (Space)";

        [Tooltip("스킵 키 (Space 또는 Escape)")]
        public bool allowKeyboardSkip = true;

        [Header("Events")]
        public UnityEvent OnIntroStart;
        public UnityEvent OnIntroMessageShow;
        public UnityEvent OnButtonPromptShow;
        public UnityEvent OnButtonClicked;
        public UnityEvent OnIntroComplete;

        private AudioSource audioSource;
        private bool isIntroPlaying = false;
        private bool introCompleted = false;
        private Coroutine heartbeatCoroutine;
        private Button createdSkipButton; // 자동 생성된 스킵 버튼

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
        }

        private void Start()
        {
            // UI 초기화
            if (yesButton != null)
            {
                yesButton.onClick.AddListener(OnYesButtonClicked);
                yesButton.gameObject.SetActive(false);
            }

            if (introText != null)
            {
                introText.text = "";
            }

            // 스킵 버튼 설정
            SetupSkipButton();

            // 검은 화면으로 시작
            if (startWithBlackScreen && fadeImage != null)
            {
                fadeImage.color = Color.black;
                fadeImage.gameObject.SetActive(true);
            }

            // 자동 시작
            StartIntro();
        }

        private void Update()
        {
            // 키보드로 스킵
            if (allowKeyboardSkip && isIntroPlaying && !introCompleted)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                {
                    SkipIntro();
                }
            }
        }

        /// <summary>
        /// 스킵 버튼 설정
        /// </summary>
        private void SetupSkipButton()
        {
            // 이미 스킵 버튼이 있으면 사용
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipIntro);
                return;
            }

            // 캔버스 찾기 또는 생성
            Canvas canvas = introCanvas;
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }
            if (canvas == null) return;

            // 스킵 버튼 생성
            GameObject skipObj = new GameObject("SkipButton");
            skipObj.transform.SetParent(canvas.transform, false);

            // RectTransform 설정 (우측 하단)
            RectTransform rectTransform = skipObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(1, 0);
            rectTransform.anchoredPosition = new Vector2(-30, 30);
            rectTransform.sizeDelta = new Vector2(150, 40);

            // Image (버튼 배경)
            Image buttonImage = skipObj.AddComponent<Image>();
            buttonImage.color = new Color(0, 0, 0, 0.5f);

            // Button
            createdSkipButton = skipObj.AddComponent<Button>();
            createdSkipButton.onClick.AddListener(SkipIntro);

            // 텍스트 생성
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(skipObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = skipButtonLabel;
            buttonText.fontSize = 18;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;

            Debug.Log("[IntroSequence] 스킵 버튼 생성됨");
        }

        /// <summary>
        /// 도입 시퀀스 시작
        /// </summary>
        public void StartIntro()
        {
            if (isIntroPlaying) return;

            isIntroPlaying = true;
            introCompleted = false;
            OnIntroStart?.Invoke();

            // 플레이어 이동 비활성화
            if (disablePlayerDuringIntro)
            {
                DisablePlayerMovement();
            }

            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            // 심장 박동 시작
            if (heartbeatSound != null)
            {
                heartbeatCoroutine = StartCoroutine(PlayHeartbeat());
            }

            // 잠시 대기 (암전 상태)
            yield return new WaitForSeconds(1f);

            // 서서히 밝아지기 (약간만)
            if (fadeImage != null)
            {
                yield return StartCoroutine(FadeScreen(1f, 0.7f, 2f));
            }

            // 도입 메시지들 표시
            foreach (string message in introMessages)
            {
                OnIntroMessageShow?.Invoke();
                yield return StartCoroutine(TypeText(message));
                yield return new WaitForSeconds(messageDelay);
            }

            // 메시지 지우기
            if (introText != null)
            {
                introText.text = "";
            }

            yield return new WaitForSeconds(1f);

            // 버튼 프롬프트 표시
            OnButtonPromptShow?.Invoke();
            yield return StartCoroutine(TypeText(buttonPrompt));

            // YES 버튼 표시
            if (yesButton != null)
            {
                yesButton.gameObject.SetActive(true);

                // 버튼 페이드 인 효과
                CanvasGroup buttonGroup = yesButton.GetComponent<CanvasGroup>();
                if (buttonGroup == null)
                {
                    buttonGroup = yesButton.gameObject.AddComponent<CanvasGroup>();
                }
                buttonGroup.alpha = 0f;

                float elapsed = 0f;
                while (elapsed < 1f)
                {
                    elapsed += Time.deltaTime;
                    buttonGroup.alpha = elapsed;
                    yield return null;
                }
                buttonGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// YES 버튼 클릭
        /// </summary>
        private void OnYesButtonClicked()
        {
            if (introCompleted) return;

            introCompleted = true;
            OnButtonClicked?.Invoke();

            // 버튼 클릭 사운드
            if (buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }

            StartCoroutine(CompleteIntro());
        }

        private IEnumerator CompleteIntro()
        {
            // 심장 박동 중지
            if (heartbeatCoroutine != null)
            {
                StopCoroutine(heartbeatCoroutine);
            }

            // 버튼 숨기기
            if (yesButton != null)
            {
                yesButton.gameObject.SetActive(false);
            }

            // 스킵 버튼 숨기기
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(false);
            }
            if (createdSkipButton != null)
            {
                createdSkipButton.gameObject.SetActive(false);
            }

            // 메시지 지우기
            if (introText != null)
            {
                introText.text = "";
            }

            yield return new WaitForSeconds(0.5f);

            // 완전히 밝아지기
            if (fadeImage != null)
            {
                yield return StartCoroutine(FadeScreen(fadeImage.color.a, 0f, fadeInDuration));
                fadeImage.gameObject.SetActive(false);
            }

            // 도입 UI 숨기기
            if (introCanvas != null)
            {
                introCanvas.gameObject.SetActive(false);
            }

            // 플레이어 이동 활성화
            EnablePlayerMovement();

            isIntroPlaying = false;
            OnIntroComplete?.Invoke();

            // 스토리 진행 관리자에게 알림
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.StartExploration();
            }

            // 배경음 시작
            if (ambientSound != null)
            {
                audioSource.clip = ambientSound;
                audioSource.loop = true;
                audioSource.volume = 0.3f;
                audioSource.Play();
            }

            Debug.Log("[IntroSequence] 도입 완료, 게임 시작");
        }

        /// <summary>
        /// 타이핑 효과
        /// </summary>
        private IEnumerator TypeText(string text)
        {
            if (introText == null) yield break;

            introText.text = "";

            foreach (char c in text)
            {
                introText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        /// <summary>
        /// 화면 페이드
        /// </summary>
        private IEnumerator FadeScreen(float from, float to, float duration)
        {
            if (fadeImage == null) yield break;

            float elapsed = 0f;
            Color color = fadeImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                color.a = Mathf.Lerp(from, to, t);
                fadeImage.color = color;
                yield return null;
            }

            color.a = to;
            fadeImage.color = color;
        }

        /// <summary>
        /// 심장 박동 재생
        /// </summary>
        private IEnumerator PlayHeartbeat()
        {
            while (true)
            {
                if (heartbeatSound != null)
                {
                    audioSource.PlayOneShot(heartbeatSound, 0.5f);
                }
                yield return new WaitForSeconds(heartbeatInterval);
            }
        }

        private void DisablePlayerMovement()
        {
            // VR 플레이어
            if (VRPlayer.Instance != null)
            {
                VRPlayer.Instance.enabled = false;
            }

            // PC 플레이어
            var pcPlayer = FindObjectOfType<PCPlayerController>();
            if (pcPlayer != null)
            {
                pcPlayer.enabled = false;
            }

            // PC에서 커서 표시 (UI 클릭 가능하도록)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void EnablePlayerMovement()
        {
            // VR 플레이어
            if (VRPlayer.Instance != null)
            {
                VRPlayer.Instance.enabled = true;
            }

            // PC 플레이어
            var pcPlayer = FindObjectOfType<PCPlayerController>();
            if (pcPlayer != null)
            {
                pcPlayer.enabled = true;

                // PC에서 커서 잠금 (게임 플레이 모드)
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// 도입 스킵 (디버그용)
        /// </summary>
        public void SkipIntro()
        {
            if (!isIntroPlaying) return;

            StopAllCoroutines();
            StartCoroutine(CompleteIntro());
        }

        /// <summary>
        /// 도입 완료 여부
        /// </summary>
        public bool IsIntroCompleted => introCompleted;

        /// <summary>
        /// 도입 진행 중 여부
        /// </summary>
        public bool IsIntroPlaying => isIntroPlaying;
    }
}
