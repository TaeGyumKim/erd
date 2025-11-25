using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorrorGame
{
    /// <summary>
    /// VR 메뉴 UI
    /// 일시정지, 게임오버, 승리 화면 등
    ///
    /// 사용법:
    /// 1. World Space Canvas에 추가
    /// 2. 각 패널과 버튼 연결
    /// </summary>
    public class VRMenuUI : MonoBehaviour
    {
        public static VRMenuUI Instance { get; private set; }

        [Header("Panels")]
        [Tooltip("일시정지 메뉴 패널")]
        public GameObject pausePanel;

        [Tooltip("게임오버 패널")]
        public GameObject gameOverPanel;

        [Tooltip("승리 패널")]
        public GameObject victoryPanel;

        [Header("Pause Menu")]
        [Tooltip("재개 버튼")]
        public Button resumeButton;

        [Tooltip("재시작 버튼")]
        public Button restartButton;

        [Tooltip("메인메뉴 버튼")]
        public Button mainMenuButton;

        [Tooltip("종료 버튼")]
        public Button quitButton;

        [Header("Game Over")]
        [Tooltip("게임오버 제목")]
        public TextMeshProUGUI gameOverTitle;

        [Tooltip("게임오버 메시지")]
        public TextMeshProUGUI gameOverMessage;

        [Tooltip("게임오버 재시작 버튼")]
        public Button gameOverRestartButton;

        [Header("Victory")]
        [Tooltip("승리 제목")]
        public TextMeshProUGUI victoryTitle;

        [Tooltip("승리 메시지")]
        public TextMeshProUGUI victoryMessage;

        [Tooltip("클리어 시간 텍스트")]
        public TextMeshProUGUI clearTimeText;

        [Header("Settings")]
        [Tooltip("플레이어 카메라")]
        public Transform playerCamera;

        [Tooltip("메뉴 거리")]
        public float menuDistance = 2f;

        private Canvas canvas;
        private float playTime;
        private bool isTimerRunning;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
            }

            // 버튼 이벤트 연결
            SetupButtons();

            // 모든 패널 숨기기
            HideAllPanels();
        }

        private void Start()
        {
            // 플레이어 카메라 자동 찾기
            if (playerCamera == null)
            {
                // Main Camera 찾기
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerCamera = mainCam.transform;
                }
                // 없으면 VRPlayer 위치 사용
                else if (VRPlayer.Instance != null)
                {
                    playerCamera = VRPlayer.Instance.transform;
                }
            }

            // 게임 이벤트 연결
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.OnGameOver.AddListener(ShowGameOver);
                HorrorGameManager.Instance.OnVictory.AddListener(ShowVictory);
            }

            isTimerRunning = true;
        }

        private void Update()
        {
            if (isTimerRunning)
            {
                playTime += Time.deltaTime;
            }

            // 메뉴 위치 업데이트
            UpdateMenuPosition();
        }

        private void SetupButtons()
        {
            // 일시정지 메뉴 버튼
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }

            // 게임오버 버튼
            if (gameOverRestartButton != null)
            {
                gameOverRestartButton.onClick.AddListener(RestartGame);
            }
        }

        private void UpdateMenuPosition()
        {
            if (playerCamera == null) return;

            // 활성화된 패널이 있으면 플레이어 앞에 위치
            if (pausePanel != null && pausePanel.activeSelf ||
                gameOverPanel != null && gameOverPanel.activeSelf ||
                victoryPanel != null && victoryPanel.activeSelf)
            {
                transform.position = playerCamera.position + playerCamera.forward * menuDistance;
                transform.rotation = Quaternion.LookRotation(transform.position - playerCamera.position);
            }
        }

        private void HideAllPanels()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
        }

        /// <summary>
        /// 일시정지 메뉴 표시
        /// </summary>
        public void ShowPauseMenu()
        {
            HideAllPanels();

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.PauseGame();
            }

            Debug.Log("[VRMenuUI] 일시정지 메뉴 표시");
        }

        /// <summary>
        /// 게임오버 화면 표시
        /// </summary>
        public void ShowGameOver()
        {
            HideAllPanels();
            isTimerRunning = false;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (gameOverTitle != null)
            {
                gameOverTitle.text = "GAME OVER";
            }

            if (gameOverMessage != null)
            {
                gameOverMessage.text = "당신은 잡혔습니다...";
            }

            Debug.Log("[VRMenuUI] 게임오버 화면 표시");
        }

        /// <summary>
        /// 승리 화면 표시
        /// </summary>
        public void ShowVictory()
        {
            HideAllPanels();
            isTimerRunning = false;

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (victoryTitle != null)
            {
                victoryTitle.text = "탈출 성공!";
            }

            if (victoryMessage != null)
            {
                victoryMessage.text = "당신은 무사히 탈출했습니다!";
            }

            if (clearTimeText != null)
            {
                int minutes = Mathf.FloorToInt(playTime / 60);
                int seconds = Mathf.FloorToInt(playTime % 60);
                clearTimeText.text = $"클리어 시간: {minutes:00}:{seconds:00}";
            }

            Debug.Log("[VRMenuUI] 승리 화면 표시");
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            HideAllPanels();

            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.ResumeGame();
            }

            Debug.Log("[VRMenuUI] 게임 재개");
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.RestartGame();
            }
        }

        /// <summary>
        /// 메인 메뉴로
        /// </summary>
        public void GoToMainMenu()
        {
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.GoToMainMenu();
            }
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 일시정지 토글
        /// </summary>
        public void TogglePause()
        {
            if (pausePanel != null && pausePanel.activeSelf)
            {
                ResumeGame();
            }
            else
            {
                ShowPauseMenu();
            }
        }
    }
}
