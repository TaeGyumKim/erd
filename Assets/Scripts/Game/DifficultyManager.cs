using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 난이도 관리자
    /// 씬 시작 시 저장된 난이도에 따라 KillerAI 속도 조절
    ///
    /// 난이도별 설정 (보통 기준):
    /// - 쉬움: 속도 0.7배
    /// - 보통: 속도 1.0배 (기본)
    /// - 어려움: 속도 1.3배
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public static DifficultyManager Instance { get; private set; }

        [Header("Difficulty Settings")]
        [Tooltip("쉬움 난이도 속도 배율")]
        public float easySpeedMultiplier = 0.7f;

        [Tooltip("보통 난이도 속도 배율")]
        public float normalSpeedMultiplier = 1.0f;

        [Tooltip("어려움 난이도 속도 배율")]
        public float hardSpeedMultiplier = 1.3f;

        [Header("Detection Settings")]
        [Tooltip("쉬움: 시야 거리 배율")]
        public float easyViewDistanceMultiplier = 0.8f;

        [Tooltip("어려움: 시야 거리 배율")]
        public float hardViewDistanceMultiplier = 1.2f;

        [Header("References")]
        [Tooltip("KillerAI (자동 검색됨)")]
        public KillerAI killer;

        /// <summary>
        /// 현재 난이도
        /// </summary>
        public MainMenuUI.GameDifficulty CurrentDifficulty { get; private set; }

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
        }

        private void Start()
        {
            // 저장된 난이도 로드
            CurrentDifficulty = MainMenuUI.GetSelectedDifficulty();
            Debug.Log($"[DifficultyManager] 난이도 로드됨: {CurrentDifficulty}");

            // KillerAI 찾기
            if (killer == null)
            {
                killer = FindObjectOfType<KillerAI>();
            }

            // 난이도 적용
            ApplyDifficulty();
        }

        /// <summary>
        /// 난이도 적용
        /// </summary>
        public void ApplyDifficulty()
        {
            if (killer == null)
            {
                Debug.LogWarning("[DifficultyManager] KillerAI를 찾을 수 없습니다.");
                return;
            }

            float speedMultiplier = GetSpeedMultiplier();
            float viewMultiplier = GetViewDistanceMultiplier();

            // 속도 적용
            killer.patrolSpeed *= speedMultiplier;
            killer.chaseSpeed *= speedMultiplier;
            killer.searchSpeed *= speedMultiplier;

            // 시야 거리 적용
            killer.viewDistance *= viewMultiplier;

            Debug.Log($"[DifficultyManager] 난이도 적용 완료:");
            Debug.Log($"  - 난이도: {CurrentDifficulty}");
            Debug.Log($"  - 속도 배율: {speedMultiplier}x");
            Debug.Log($"  - 순찰 속도: {killer.patrolSpeed}");
            Debug.Log($"  - 추적 속도: {killer.chaseSpeed}");
            Debug.Log($"  - 수색 속도: {killer.searchSpeed}");
            Debug.Log($"  - 시야 거리: {killer.viewDistance}");
        }

        /// <summary>
        /// 현재 난이도의 속도 배율 반환
        /// </summary>
        public float GetSpeedMultiplier()
        {
            switch (CurrentDifficulty)
            {
                case MainMenuUI.GameDifficulty.Easy:
                    return easySpeedMultiplier;
                case MainMenuUI.GameDifficulty.Normal:
                    return normalSpeedMultiplier;
                case MainMenuUI.GameDifficulty.Hard:
                    return hardSpeedMultiplier;
                default:
                    return normalSpeedMultiplier;
            }
        }

        /// <summary>
        /// 현재 난이도의 시야 거리 배율 반환
        /// </summary>
        public float GetViewDistanceMultiplier()
        {
            switch (CurrentDifficulty)
            {
                case MainMenuUI.GameDifficulty.Easy:
                    return easyViewDistanceMultiplier;
                case MainMenuUI.GameDifficulty.Normal:
                    return 1.0f;
                case MainMenuUI.GameDifficulty.Hard:
                    return hardViewDistanceMultiplier;
                default:
                    return 1.0f;
            }
        }

        /// <summary>
        /// 난이도 이름 반환 (UI 표시용)
        /// </summary>
        public string GetDifficultyName()
        {
            switch (CurrentDifficulty)
            {
                case MainMenuUI.GameDifficulty.Easy:
                    return "쉬움";
                case MainMenuUI.GameDifficulty.Normal:
                    return "보통";
                case MainMenuUI.GameDifficulty.Hard:
                    return "어려움";
                default:
                    return "보통";
            }
        }

        /// <summary>
        /// 런타임에 난이도 변경 (테스트용)
        /// </summary>
        public void SetDifficulty(MainMenuUI.GameDifficulty newDifficulty)
        {
            // 기존 배율 제거를 위해 원래 값으로 복원
            if (killer != null)
            {
                float currentMultiplier = GetSpeedMultiplier();
                killer.patrolSpeed /= currentMultiplier;
                killer.chaseSpeed /= currentMultiplier;
                killer.searchSpeed /= currentMultiplier;

                float currentViewMultiplier = GetViewDistanceMultiplier();
                killer.viewDistance /= currentViewMultiplier;
            }

            CurrentDifficulty = newDifficulty;
            PlayerPrefs.SetInt("GameDifficulty", (int)newDifficulty);
            PlayerPrefs.Save();

            ApplyDifficulty();
        }

        /// <summary>
        /// 난이도 하향 (게임오버 시 호출)
        /// 어려움 → 보통, 보통 → 쉬움
        /// </summary>
        /// <returns>하향된 경우 true, 이미 쉬움이면 false</returns>
        public bool ReduceDifficulty()
        {
            MainMenuUI.GameDifficulty previousDifficulty = CurrentDifficulty;

            switch (CurrentDifficulty)
            {
                case MainMenuUI.GameDifficulty.Hard:
                    CurrentDifficulty = MainMenuUI.GameDifficulty.Normal;
                    break;
                case MainMenuUI.GameDifficulty.Normal:
                    CurrentDifficulty = MainMenuUI.GameDifficulty.Easy;
                    break;
                case MainMenuUI.GameDifficulty.Easy:
                    // 이미 쉬움이면 하향 불가
                    return false;
            }

            // PlayerPrefs에 저장
            PlayerPrefs.SetInt("GameDifficulty", (int)CurrentDifficulty);
            PlayerPrefs.Save();

            Debug.Log($"[DifficultyManager] 난이도 하향: {GetDifficultyName(previousDifficulty)} → {GetDifficultyName()}");
            return true;
        }

        /// <summary>
        /// 난이도 하향 가능 여부
        /// </summary>
        public bool CanReduceDifficulty()
        {
            return CurrentDifficulty != MainMenuUI.GameDifficulty.Easy;
        }

        /// <summary>
        /// 특정 난이도 이름 반환
        /// </summary>
        public string GetDifficultyName(MainMenuUI.GameDifficulty difficulty)
        {
            switch (difficulty)
            {
                case MainMenuUI.GameDifficulty.Easy:
                    return "쉬움";
                case MainMenuUI.GameDifficulty.Normal:
                    return "보통";
                case MainMenuUI.GameDifficulty.Hard:
                    return "어려움";
                default:
                    return "보통";
            }
        }

        /// <summary>
        /// 난이도 하향 후 재시작 (GameOverUI에서 호출)
        /// </summary>
        public void ReduceDifficultyAndRestart()
        {
            ReduceDifficulty();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
