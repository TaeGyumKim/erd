using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

namespace HorrorGame
{
    public class GameOverUI : MonoBehaviour
    {
        public static GameOverUI Instance { get; private set; }

        [Header("UI References")]
        public GameObject gameOverPanel;
        public TextMeshProUGUI deathText;
        public TextMeshProUGUI difficultyChangeText;
        public Button restartButton;
        public Button mainMenuButton;
        public CanvasGroup buttonContainer;

        [Header("Settings")]
        public string deathMessage = "YOU DIED";
        public float textFadeInDuration = 2f;
        public float buttonDelayTime = 1.5f;
        public float buttonFadeInDuration = 0.5f;

        [Header("Audio")]
        public AudioClip deathSound;
        public AudioClip deathMusic;

        private AudioSource audioSource;
        private bool isGameOver = false;
        private bool difficultyReduced = false;
        private string previousDifficulty = "";
        private string newDifficulty = "";

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

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ShowGameOver()
        {
            if (isGameOver) return;
            isGameOver = true;

            Debug.Log("[GameOverUI] Game Over!");

            // 난이도 하향 처리
            HandleDifficultyReduction();

            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            StartCoroutine(FadeInSequence());
        }

        /// <summary>
        /// 난이도 하향 처리
        /// </summary>
        private void HandleDifficultyReduction()
        {
            var difficultyManager = DifficultyManager.Instance;
            if (difficultyManager != null && difficultyManager.CanReduceDifficulty())
            {
                // 이전 난이도 저장
                previousDifficulty = difficultyManager.GetDifficultyName();

                // 난이도 하향
                difficultyReduced = difficultyManager.ReduceDifficulty();

                // 새 난이도 저장
                newDifficulty = difficultyManager.GetDifficultyName();

                Debug.Log($"[GameOverUI] 난이도 하향: {previousDifficulty} → {newDifficulty}");
            }
            else
            {
                difficultyReduced = false;
                Debug.Log("[GameOverUI] 난이도 하향 불가 (이미 쉬움 또는 DifficultyManager 없음)");
            }
        }

        private IEnumerator FadeInSequence()
        {
            if (deathText != null)
            {
                deathText.text = deathMessage;
                Color textColor = deathText.color;
                textColor.a = 0f;
                deathText.color = textColor;

                float elapsed = 0f;
                while (elapsed < textFadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(0f, 1f, elapsed / textFadeInDuration);
                    textColor.a = alpha;
                    deathText.color = textColor;
                    yield return null;
                }

                textColor.a = 1f;
                deathText.color = textColor;
            }

            // 난이도 하향 메시지 표시
            if (difficultyReduced && difficultyChangeText != null)
            {
                difficultyChangeText.text = $"난이도가 하향되었습니다\n{previousDifficulty} → {newDifficulty}";
                difficultyChangeText.gameObject.SetActive(true);

                // 페이드 인
                Color changeTextColor = difficultyChangeText.color;
                changeTextColor.a = 0f;
                difficultyChangeText.color = changeTextColor;

                float changeElapsed = 0f;
                float changeFadeDuration = 1f;
                while (changeElapsed < changeFadeDuration)
                {
                    changeElapsed += Time.unscaledDeltaTime;
                    float alpha = Mathf.Lerp(0f, 1f, changeElapsed / changeFadeDuration);
                    changeTextColor.a = alpha;
                    difficultyChangeText.color = changeTextColor;
                    yield return null;
                }

                changeTextColor.a = 1f;
                difficultyChangeText.color = changeTextColor;
            }
            else if (difficultyChangeText != null)
            {
                // 난이도 하향이 없으면 텍스트 숨김
                difficultyChangeText.gameObject.SetActive(false);
            }

            yield return new WaitForSecondsRealtime(buttonDelayTime);

            if (buttonContainer != null)
            {
                buttonContainer.alpha = 0f;
                buttonContainer.interactable = false;
                buttonContainer.blocksRaycasts = false;

                float elapsed = 0f;
                while (elapsed < buttonFadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    buttonContainer.alpha = Mathf.Lerp(0f, 1f, elapsed / buttonFadeInDuration);
                    yield return null;
                }

                buttonContainer.alpha = 1f;
                buttonContainer.interactable = true;
                buttonContainer.blocksRaycasts = true;
            }
        }

        public void OnRestartClicked()
        {
            if (difficultyReduced)
            {
                Debug.Log($"[GameOverUI] Restart (난이도 {newDifficulty}로 재시작)");
            }
            else
            {
                Debug.Log("[GameOverUI] Restart");
            }

            Time.timeScale = 1f;
            isGameOver = false;
            difficultyReduced = false;

            string currentScene = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentScene);
        }

        public void OnMainMenuClicked()
        {
            Debug.Log("[GameOverUI] Go to Main Menu");

            Time.timeScale = 1f;
            isGameOver = false;

            if (SceneExists("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Debug.LogWarning("[GameOverUI] MainMenu scene not found. Restarting current scene.");
                OnRestartClicked();
            }
        }

        private bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (name == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsGameOver => isGameOver;
    }
}
