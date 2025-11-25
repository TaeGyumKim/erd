using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 공포 게임 전체 관리
    /// 게임 상태, 승리/패배 조건 관리
    ///
    /// 사용법:
    /// 1. 빈 게임오브젝트에 추가
    /// 2. HorrorGameManager.Instance로 접근
    /// </summary>
    public class HorrorGameManager : MonoBehaviour
    {
        public static HorrorGameManager Instance { get; private set; }

        [Header("Game State")]
        public GameState currentState = GameState.Playing;

        [Header("Win Condition")]
        [Tooltip("탈출에 필요한 열쇠 수")]
        public int requiredKeysToEscape = 3;

        [Tooltip("현재 수집한 열쇠 수")]
        public int collectedKeys = 0;

        [Header("Game Over Settings")]
        [Tooltip("게임오버 후 재시작까지 대기 시간")]
        public float gameOverDelay = 3f;

        [Tooltip("게임오버 씬 이름 (비어있으면 현재 씬 재시작)")]
        public string gameOverSceneName = "";

        [Header("Victory Settings")]
        [Tooltip("승리 후 대기 시간")]
        public float victoryDelay = 5f;

        [Tooltip("승리 씬 이름 (비어있으면 현재 씬)")]
        public string victorySceneName = "";

        [Header("Timer (Optional)")]
        [Tooltip("제한 시간 사용")]
        public bool useTimeLimit = false;

        [Tooltip("제한 시간 (초)")]
        public float timeLimit = 600f;

        [Tooltip("남은 시간")]
        public float remainingTime;

        [Header("Audio")]
        public AudioClip gameOverSound;
        public AudioClip victorySound;
        public AudioClip ambientMusic;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnGameStart;
        public UnityEngine.Events.UnityEvent OnGameOver;
        public UnityEngine.Events.UnityEvent OnVictory;
        public UnityEngine.Events.UnityEvent OnKeyCollected;
        public UnityEngine.Events.UnityEvent<float> OnTimeUpdate;

        public enum GameState
        {
            Menu,
            Playing,
            Paused,
            GameOver,
            Victory
        }

        private AudioSource audioSource;

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
            StartGame();
        }

        private void Update()
        {
            if (currentState == GameState.Playing && useTimeLimit)
            {
                UpdateTimer();
            }
        }

        /// <summary>
        /// 게임 시작
        /// </summary>
        public void StartGame()
        {
            currentState = GameState.Playing;
            collectedKeys = 0;
            remainingTime = timeLimit;

            // 배경 음악 재생
            if (ambientMusic != null)
            {
                audioSource.clip = ambientMusic;
                audioSource.loop = true;
                audioSource.Play();
            }

            OnGameStart?.Invoke();
            Debug.Log("[HorrorGameManager] 게임 시작");
        }

        /// <summary>
        /// 타이머 업데이트
        /// </summary>
        private void UpdateTimer()
        {
            remainingTime -= Time.deltaTime;
            OnTimeUpdate?.Invoke(remainingTime);

            if (remainingTime <= 0)
            {
                remainingTime = 0;
                GameOver("시간 초과!");
            }
        }

        /// <summary>
        /// 열쇠 수집
        /// </summary>
        public void CollectKey()
        {
            collectedKeys++;
            OnKeyCollected?.Invoke();
            Debug.Log($"[HorrorGameManager] 열쇠 수집: {collectedKeys}/{requiredKeysToEscape}");
        }

        /// <summary>
        /// 탈출 가능 여부 확인
        /// </summary>
        public bool CanEscape()
        {
            return collectedKeys >= requiredKeysToEscape;
        }

        /// <summary>
        /// 탈출 시도
        /// </summary>
        public void TryEscape()
        {
            if (CanEscape())
            {
                Victory();
            }
            else
            {
                Debug.Log($"[HorrorGameManager] 탈출 불가 - 열쇠가 더 필요합니다 ({collectedKeys}/{requiredKeysToEscape})");
            }
        }

        /// <summary>
        /// 게임 오버
        /// </summary>
        public void GameOver(string reason = "")
        {
            if (currentState == GameState.GameOver) return;

            currentState = GameState.GameOver;

            // 음악 정지
            audioSource.Stop();

            // 게임오버 사운드
            if (gameOverSound != null)
            {
                audioSource.PlayOneShot(gameOverSound);
            }

            OnGameOver?.Invoke();
            Debug.Log($"[HorrorGameManager] 게임 오버: {reason}");

            // 재시작
            StartCoroutine(RestartAfterDelay());
        }

        /// <summary>
        /// 승리
        /// </summary>
        public void Victory()
        {
            if (currentState == GameState.Victory) return;

            currentState = GameState.Victory;

            // 음악 정지
            audioSource.Stop();

            // 승리 사운드
            if (victorySound != null)
            {
                audioSource.PlayOneShot(victorySound);
            }

            OnVictory?.Invoke();
            Debug.Log("[HorrorGameManager] 승리! 탈출 성공!");

            // 승리 씬으로 이동
            StartCoroutine(LoadVictoryScene());
        }

        private IEnumerator RestartAfterDelay()
        {
            yield return new WaitForSeconds(gameOverDelay);

            if (!string.IsNullOrEmpty(gameOverSceneName))
            {
                SceneManager.LoadScene(gameOverSceneName);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        private IEnumerator LoadVictoryScene()
        {
            yield return new WaitForSeconds(victoryDelay);

            if (!string.IsNullOrEmpty(victorySceneName))
            {
                SceneManager.LoadScene(victorySceneName);
            }
        }

        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.Paused;
            Time.timeScale = 0;
            Debug.Log("[HorrorGameManager] 게임 일시정지");
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;

            currentState = GameState.Playing;
            Time.timeScale = 1;
            Debug.Log("[HorrorGameManager] 게임 재개");
        }

        /// <summary>
        /// 메인 메뉴로 이동
        /// </summary>
        public void GoToMainMenu(string menuSceneName = "MainMenu")
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(menuSceneName);
        }

        /// <summary>
        /// 게임 재시작
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
