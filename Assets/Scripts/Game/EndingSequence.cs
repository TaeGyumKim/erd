using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 게임 엔딩 시퀀스 (World Space UI)
    /// 탈출 성공 시 플레이어 앞에 텍스트 표시
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        public static EndingSequence Instance { get; private set; }

        [Header("World Space UI")]
        [Tooltip("World Space Canvas (플레이어 앞에 표시)")]
        public Canvas worldSpaceCanvas;

        [Tooltip("승리 메시지 텍스트")]
        public TextMeshProUGUI victoryText;

        [Tooltip("크레딧 텍스트")]
        public TextMeshProUGUI creditsText;

        [Tooltip("스킵 안내 텍스트")]
        public TextMeshProUGUI skipHintText;

        [Tooltip("메인 메뉴 버튼")]
        public Button mainMenuButton;

        [Tooltip("다시 시작 버튼")]
        public Button restartButton;

        [Header("UI Position Settings")]
        [Tooltip("플레이어로부터의 거리")]
        public float distanceFromPlayer = 2f;

        [Tooltip("UI 높이 오프셋 (카메라 높이 기준, 0 = 눈높이)")]
        public float uiHeightOffset = 0f;

        [Header("Messages")]
        [TextArea(3, 5)]
        public string[] victoryMessages = new string[]
        {
            "문이 열렸다...",
            "저택의 차가운 공기가 등 뒤로 사라져간다.",
            "그 악몽 같던 시간들...",
            "살인마의 발소리, 어둠 속의 숨바꼭질...",
            "모든 것이 끝났다.",
            "...",
            "하지만 의문은 남는다.",
            "왜 나였을까?",
            "그리고... 저 안에 갇힌 다른 이들은?",
            "...",
            "\"이제 자유다.\"",
            "정말... 그런 걸까?"
        };

        [Tooltip("크레딧 메시지")]
        [TextArea(5, 10)]
        public string creditsMessage = @"끝까지 달려라
Run 'Till the End

플레이해 주셔서 감사합니다.

- THE END -";

        [Tooltip("메시지 간 대기 시간")]
        public float messageDelay = 1f;

        [Header("Skip Settings")]
        [Tooltip("스킵 가능 여부")]
        public bool canSkip = true;

        [Header("Audio")]
        public AudioClip victoryMusic;
        public AudioClip doorOpenSound;

        [Header("Scene Settings")]
        [Tooltip("메인 메뉴 씬 이름")]
        public string mainMenuSceneName = "MainMenu";

        [Tooltip("게임 씬 이름 (재시작용)")]
        public string gameSceneName = "SampleScene";

        [Header("Events")]
        public UnityEvent OnVictoryStart;
        public UnityEvent OnVictoryComplete;

        private AudioSource audioSource;
        private bool isEndingPlaying = false;
        private bool skipRequested = false;
        private bool isShowingMessages = false;
        private Transform playerTransform;
        private Camera playerCamera;

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
            // World Space Canvas 초기화
            if (worldSpaceCanvas != null)
            {
                worldSpaceCanvas.gameObject.SetActive(false);
            }

            // 버튼 이벤트 연결
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(GoToMainMenu);
                mainMenuButton.gameObject.SetActive(false);
            }
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
                restartButton.gameObject.SetActive(false);
            }
            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // 스킵 입력 감지 (메시지 표시 중일 때만)
            if (isShowingMessages && canSkip && !skipRequested)
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
                {
                    skipRequested = true;
                    Debug.Log("[EndingSequence] 스킵 요청됨");
                }
            }

            // World Space Canvas가 항상 플레이어를 향하도록 (Y축 회전만)
            if (isEndingPlaying && worldSpaceCanvas != null && playerCamera != null)
            {
                Vector3 lookDirection = playerCamera.transform.position - worldSpaceCanvas.transform.position;
                lookDirection.y = 0; // Y축 회전만 (수평 유지)
                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    worldSpaceCanvas.transform.rotation = Quaternion.LookRotation(-lookDirection);
                }
            }
        }

        /// <summary>
        /// 승리 시퀀스 시작
        /// </summary>
        public void PlayVictorySequence()
        {
            if (isEndingPlaying) return;

            isEndingPlaying = true;
            OnVictoryStart?.Invoke();

            // 플레이어 찾기
            FindPlayer();

            // 킬러 AI 중지
            StopKillerAI();

            StartCoroutine(VictorySequenceCoroutine());
        }

        private void FindPlayer()
        {
            // VR 플레이어 카메라 찾기
            if (VRPlayer.Instance != null)
            {
                playerTransform = VRPlayer.Instance.transform;
                playerCamera = VRPlayer.Instance.GetComponentInChildren<Camera>();
            }

            // PC 플레이어 찾기
            if (playerCamera == null)
            {
                var pcPlayer = FindObjectOfType<PCPlayerController>();
                if (pcPlayer != null)
                {
                    playerTransform = pcPlayer.transform;
                    playerCamera = pcPlayer.GetComponentInChildren<Camera>();
                }
            }

            // 메인 카메라 폴백
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera != null)
                {
                    playerTransform = playerCamera.transform;
                }
            }
        }

        /// <summary>
        /// 킬러 AI 중지
        /// </summary>
        private void StopKillerAI()
        {
            // 모든 KillerAI 찾아서 비활성화
            var killers = FindObjectsOfType<KillerAI>();
            foreach (var killer in killers)
            {
                killer.enabled = false;
                Debug.Log($"[EndingSequence] 킬러 AI 중지: {killer.name}");
            }

            // NavMeshAgent도 중지
            var agents = FindObjectsOfType<UnityEngine.AI.NavMeshAgent>();
            foreach (var agent in agents)
            {
                if (agent.GetComponent<KillerAI>() != null)
                {
                    agent.isStopped = true;
                    agent.enabled = false;
                }
            }
        }

        private void PositionWorldSpaceCanvas()
        {
            if (worldSpaceCanvas == null || playerCamera == null) return;

            // 플레이어 앞에 배치 (카메라가 바라보는 방향)
            Vector3 forward = playerCamera.transform.forward;
            forward.y = 0; // 수평으로만 (위/아래 기울기 무시)

            // forward가 0이면 (완전히 위나 아래를 보고 있으면) 기본 방향 사용
            if (forward.sqrMagnitude < 0.01f)
            {
                forward = Vector3.forward;
            }
            forward.Normalize();

            // 플레이어 눈 위치에서 앞으로 distanceFromPlayer만큼 떨어진 위치
            Vector3 position = playerCamera.transform.position + forward * distanceFromPlayer;

            // Y 위치는 카메라와 같은 높이 + 오프셋
            position.y = playerCamera.transform.position.y + uiHeightOffset;

            worldSpaceCanvas.transform.position = position;

            // 플레이어를 바라보도록 회전 (Y축만)
            Vector3 lookDirection = playerCamera.transform.position - position;
            lookDirection.y = 0; // 수평으로만
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                worldSpaceCanvas.transform.rotation = Quaternion.LookRotation(-lookDirection);
            }

            Debug.Log($"[EndingSequence] Canvas 위치: {position}, 플레이어: {playerCamera.transform.position}");
        }

        private IEnumerator VictorySequenceCoroutine()
        {
            skipRequested = false;

            Debug.Log("[EndingSequence] 승리 시퀀스 시작");

            // 플레이어 이동 비활성화
            DisablePlayer();

            // 커서 표시 (PC 모드에서 버튼 클릭 가능하게)
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 문 열리는 소리
            if (doorOpenSound != null)
            {
                audioSource.PlayOneShot(doorOpenSound);
            }

            yield return new WaitForSeconds(0.5f);

            // 승리 음악
            if (victoryMusic != null)
            {
                audioSource.clip = victoryMusic;
                audioSource.loop = true;
                audioSource.volume = 0.5f;
                audioSource.Play();
            }

            // World Space Canvas 생성 또는 활성화
            if (worldSpaceCanvas == null)
            {
                CreateWorldSpaceCanvas();
            }

            // Canvas 위치 설정
            PositionWorldSpaceCanvas();

            // Canvas 활성화
            worldSpaceCanvas.gameObject.SetActive(true);

            // 텍스트 초기화
            if (victoryText != null)
            {
                victoryText.text = "";
                victoryText.gameObject.SetActive(true);
            }

            // 스킵 안내 표시
            if (skipHintText != null && canSkip)
            {
                skipHintText.text = "아무 키나 눌러서 스킵";
                skipHintText.gameObject.SetActive(true);
            }

            // 메시지 표시 시작
            isShowingMessages = true;

            Debug.Log("[EndingSequence] 메시지 표시 시작");

            // 승리 메시지 순차적 표시 (1초마다)
            foreach (string message in victoryMessages)
            {
                if (skipRequested) break;

                // 메시지 페이드 인
                yield return StartCoroutine(FadeInText(victoryText, message, 0.3f));

                // 메시지 표시 유지 (1초)
                float waitTime = 0f;
                while (waitTime < messageDelay && !skipRequested)
                {
                    waitTime += Time.deltaTime;
                    yield return null;
                }

                if (skipRequested) break;

                // 메시지 페이드 아웃
                yield return StartCoroutine(FadeOutText(victoryText, 0.3f));

                // 다음 메시지 전 짧은 대기
                yield return new WaitForSeconds(0.2f);
            }

            // 스킵 안내 숨기기
            if (skipHintText != null)
            {
                skipHintText.gameObject.SetActive(false);
            }

            isShowingMessages = false;

            // 텍스트 클리어
            if (victoryText != null)
            {
                victoryText.text = "";
                victoryText.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(0.5f);

            // 크레딧 표시
            if (creditsText != null)
            {
                creditsText.gameObject.SetActive(true);
                yield return StartCoroutine(FadeInText(creditsText, creditsMessage, 0.5f));
            }

            yield return new WaitForSeconds(2f);

            // 버튼 표시
            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(true);
                StartCoroutine(FadeInButton(mainMenuButton));
            }
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
                StartCoroutine(FadeInButton(restartButton));
            }

            OnVictoryComplete?.Invoke();
            Debug.Log("[EndingSequence] 승리 시퀀스 완료");
        }

        private void CreateWorldSpaceCanvas()
        {
            // World Space Canvas 생성
            GameObject canvasObj = new GameObject("EndingWorldSpaceCanvas");
            worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;

            // Canvas Scaler
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            // Graphic Raycaster
            canvasObj.AddComponent<GraphicRaycaster>();

            // RectTransform 설정
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800, 600);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // 반투명 배경 패널
            GameObject bgPanel = new GameObject("BackgroundPanel");
            bgPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform bgRect = bgPanel.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bgPanel.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);

            // 승리 텍스트
            GameObject textObj = new GameObject("VictoryText");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.6f);
            textRect.anchorMax = new Vector2(0.5f, 0.6f);
            textRect.sizeDelta = new Vector2(700, 200);
            textRect.anchoredPosition = Vector2.zero;

            victoryText = textObj.AddComponent<TextMeshProUGUI>();
            victoryText.text = "";
            victoryText.fontSize = 40;
            victoryText.color = Color.white;
            victoryText.alignment = TextAlignmentOptions.Center;

            // 크레딧 텍스트
            GameObject creditsObj = new GameObject("CreditsText");
            creditsObj.transform.SetParent(canvasObj.transform, false);
            RectTransform creditsRect = creditsObj.AddComponent<RectTransform>();
            creditsRect.anchorMin = new Vector2(0.5f, 0.5f);
            creditsRect.anchorMax = new Vector2(0.5f, 0.5f);
            creditsRect.sizeDelta = new Vector2(700, 300);
            creditsRect.anchoredPosition = Vector2.zero;

            creditsText = creditsObj.AddComponent<TextMeshProUGUI>();
            creditsText.text = "";
            creditsText.fontSize = 32;
            creditsText.color = Color.white;
            creditsText.alignment = TextAlignmentOptions.Center;
            creditsObj.SetActive(false);

            // 스킵 힌트 텍스트
            GameObject skipObj = new GameObject("SkipHintText");
            skipObj.transform.SetParent(canvasObj.transform, false);
            RectTransform skipRect = skipObj.AddComponent<RectTransform>();
            skipRect.anchorMin = new Vector2(0.5f, 0.1f);
            skipRect.anchorMax = new Vector2(0.5f, 0.1f);
            skipRect.sizeDelta = new Vector2(400, 50);
            skipRect.anchoredPosition = Vector2.zero;

            skipHintText = skipObj.AddComponent<TextMeshProUGUI>();
            skipHintText.text = "아무 키나 눌러서 스킵";
            skipHintText.fontSize = 20;
            skipHintText.fontStyle = FontStyles.Italic;
            skipHintText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            skipHintText.alignment = TextAlignmentOptions.Center;
            skipObj.SetActive(false);

            // 버튼 컨테이너
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(canvasObj.transform, false);
            RectTransform buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
            buttonContainerRect.anchorMin = new Vector2(0.5f, 0.15f);
            buttonContainerRect.anchorMax = new Vector2(0.5f, 0.15f);
            buttonContainerRect.sizeDelta = new Vector2(500, 80);
            buttonContainerRect.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup layout = buttonContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 30;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // 메인 메뉴 버튼
            mainMenuButton = CreateButton(buttonContainer.transform, "MainMenuButton", "메인 메뉴");
            mainMenuButton.onClick.AddListener(GoToMainMenu);
            mainMenuButton.gameObject.SetActive(false);

            // 다시 시작 버튼
            restartButton = CreateButton(buttonContainer.transform, "RestartButton", "다시 시작");
            restartButton.onClick.AddListener(RestartGame);
            restartButton.gameObject.SetActive(false);

            Debug.Log("[EndingSequence] World Space Canvas 생성 완료");
        }

        private Button CreateButton(Transform parent, string name, string text)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(180, 50);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            button.colors = colors;

            // 버튼 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            return button;
        }

        /// <summary>
        /// 텍스트 페이드 인
        /// </summary>
        private IEnumerator FadeInText(TextMeshProUGUI textComponent, string text, float duration = 0.5f)
        {
            if (textComponent == null) yield break;

            textComponent.text = text;
            Color color = textComponent.color;
            color.a = 0f;
            textComponent.color = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(0f, 1f, elapsed / duration);
                textComponent.color = color;
                yield return null;
            }

            color.a = 1f;
            textComponent.color = color;
        }

        /// <summary>
        /// 텍스트 페이드 아웃
        /// </summary>
        private IEnumerator FadeOutText(TextMeshProUGUI textComponent, float duration = 0.5f)
        {
            if (textComponent == null) yield break;

            Color color = textComponent.color;
            float startAlpha = color.a;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                textComponent.color = color;
                yield return null;
            }

            color.a = 0f;
            textComponent.color = color;
        }

        /// <summary>
        /// 버튼 페이드 인
        /// </summary>
        private IEnumerator FadeInButton(Button button, float duration = 0.5f)
        {
            if (button == null) yield break;

            CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }

        private void DisablePlayer()
        {
            if (VRPlayer.Instance != null)
            {
                VRPlayer.Instance.enabled = false;
            }

            var pcPlayer = FindObjectOfType<PCPlayerController>();
            if (pcPlayer != null)
            {
                pcPlayer.enabled = false;
            }
        }

        /// <summary>
        /// 메인 메뉴로 이동
        /// </summary>
        public void GoToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
