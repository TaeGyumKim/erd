using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 게임 초기 설정
    /// 게임 시작 시 필요한 시스템 초기화
    /// </summary>
    public class GameSetup : MonoBehaviour
    {
        [Header("Game Mode")]
        [Tooltip("간단 모드 (단서 조합 없이 아이템만 수집)")]
        public bool simpleMode = true;

        [Tooltip("튜토리얼 모드")]
        public bool tutorialMode = false;

        [Tooltip("디버그 모드")]
        public bool debugMode = false;

        [Header("Difficulty")]
        [Tooltip("제한 시간 (초), 0이면 무제한")]
        public float timeLimit = 600f;

        [Tooltip("살인마 등장 지연 시간")]
        public float killerDelay = 30f;

        [Tooltip("살인마 이동 속도 배율")]
        public float killerSpeedMultiplier = 1f;

        [Header("References")]
        public HorrorGameManager gameManager;
        public StoryProgressManager storyManager;
        public ObjectiveSystem objectiveSystem;
        public KillerAI killer;
        public GhostAI ghost;

        private void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// 게임 초기화
        /// </summary>
        public void InitializeGame()
        {
            // 레퍼런스 자동 찾기
            FindReferences();

            // 게임 매니저 설정
            SetupGameManager();

            // 스토리 매니저 설정
            SetupStoryManager();

            // 킬러 설정
            SetupKiller();

            // 유령 설정
            SetupGhost();

            // 목표 설정
            SetupObjectives();

            Debug.Log("[GameSetup] 게임 초기화 완료");
        }

        private void FindReferences()
        {
            if (gameManager == null)
                gameManager = FindObjectOfType<HorrorGameManager>();

            if (storyManager == null)
                storyManager = FindObjectOfType<StoryProgressManager>();

            if (objectiveSystem == null)
                objectiveSystem = FindObjectOfType<ObjectiveSystem>();

            if (killer == null)
                killer = FindObjectOfType<KillerAI>();

            if (ghost == null)
                ghost = FindObjectOfType<GhostAI>();
        }

        private void SetupGameManager()
        {
            if (gameManager == null) return;

            gameManager.useTimeLimit = timeLimit > 0;
            gameManager.timeLimit = timeLimit;
            gameManager.requiredKeysToEscape = 1; // 탈출 열쇠 1개

            if (debugMode)
            {
                gameManager.gameOverDelay = 1f;
            }
        }

        private void SetupStoryManager()
        {
            if (storyManager == null) return;

            storyManager.killerAppearDelay = killerDelay;

            if (simpleMode)
            {
                storyManager.SetSimpleMode();
            }
        }

        private void SetupKiller()
        {
            if (killer == null) return;

            // 킬러 비활성화 (스토리에서 활성화)
            killer.gameObject.SetActive(false);

            // 속도 조절
            var agent = killer.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                agent.speed *= killerSpeedMultiplier;
            }
        }

        private void SetupGhost()
        {
            if (ghost == null) return;

            // 유령도 처음엔 비활성화
            ghost.gameObject.SetActive(false);
        }

        private void SetupObjectives()
        {
            if (objectiveSystem == null) return;

            // 기본 목표 설정
            if (simpleMode)
            {
                objectiveSystem.AddObjective("find_items", "단서 수집", "USB, 라이터, 보안카드를 찾으세요", false, 3);
                objectiveSystem.AddObjective("find_key", "탈출 열쇠", "탈출 열쇠를 찾으세요");
                objectiveSystem.AddObjective("escape", "탈출", "출구로 탈출하세요");
            }
            else
            {
                objectiveSystem.AddObjective("find_usb", "USB 찾기", "컴퓨터에서 정보를 확인할 USB를 찾으세요");
                objectiveSystem.AddObjective("find_lighter", "라이터 찾기", "숨겨진 것을 밝힐 라이터를 찾으세요");
                objectiveSystem.AddObjective("find_card", "보안카드 찾기", "잠긴 문을 열 보안카드를 찾으세요");
                objectiveSystem.AddObjective("use_computer", "USB 사용", "컴퓨터에 USB를 꽂아 정보를 확인하세요");
                objectiveSystem.AddObjective("find_symbol", "벽 문양 찾기", "라이터로 숨겨진 문양을 찾으세요");
                objectiveSystem.AddObjective("find_key", "탈출 열쇠", "단서를 조합해 탈출 열쇠를 찾으세요");
                objectiveSystem.AddObjective("escape", "탈출", "출구로 탈출하세요");
            }
        }

        /// <summary>
        /// 디버그: 모든 단서 즉시 획득
        /// </summary>
        [ContextMenu("Debug: Collect All Clues")]
        public void DebugCollectAllClues()
        {
            if (storyManager != null)
            {
                storyManager.hasUSB = true;
                storyManager.hasLighter = true;
                storyManager.hasSecurityCard = true;
                storyManager.hasBattery = true;
                storyManager.hasGear = true;
                storyManager.usbMessageRead = true;
                storyManager.wallSymbolFound = true;
                storyManager.ghostMessageReceived = true;
                storyManager.canObtainFinalKey = true;

                Debug.Log("[Debug] 모든 단서 획득!");
            }
        }

        /// <summary>
        /// 디버그: 킬러 즉시 활성화
        /// </summary>
        [ContextMenu("Debug: Activate Killer")]
        public void DebugActivateKiller()
        {
            if (storyManager != null)
            {
                storyManager.ActivateKiller();
            }
            else if (killer != null)
            {
                killer.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 디버그: 승리
        /// </summary>
        [ContextMenu("Debug: Win Game")]
        public void DebugWinGame()
        {
            if (gameManager != null)
            {
                gameManager.Victory();
            }
        }
    }
}
