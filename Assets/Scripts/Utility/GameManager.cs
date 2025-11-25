using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRGame
{
    /// <summary>
    /// 게임 전체를 관리하는 싱글톤 매니저
    /// 씬 전환, 게임 상태 관리 등
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [Tooltip("현재 게임 상태")]
        public GameState currentState = GameState.Playing;

        [Header("Settings")]
        [Tooltip("게임 시작 시 커서 숨김")]
        public bool hideCursorOnStart = true;

        public enum GameState
        {
            MainMenu,
            Playing,
            Paused,
            GameOver
        }

        private void Awake()
        {
            // 싱글톤 패턴
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (hideCursorOnStart)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        /// <summary>
        /// 게임 일시정지
        /// </summary>
        public void PauseGame()
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("[GameManager] 게임 일시정지");
        }

        /// <summary>
        /// 게임 재개
        /// </summary>
        public void ResumeGame()
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            Debug.Log("[GameManager] 게임 재개");
        }

        /// <summary>
        /// 씬 로드
        /// </summary>
        public void LoadScene(string sceneName)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// 씬 로드 (인덱스)
        /// </summary>
        public void LoadScene(int sceneIndex)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneIndex);
        }

        /// <summary>
        /// 현재 씬 재시작
        /// </summary>
        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// 게임 종료
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] 게임 종료");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
