using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 공포 게임 기본 설정 도구
    /// 게임에 필요한 모든 매니저와 시스템을 자동으로 설정합니다.
    /// </summary>
    public class GameSetupTool : EditorWindow
    {
        // 설정 옵션
        private bool setupGameManager = true;
        private bool setupStoryManager = true;
        private bool setupAudioManager = true;
        private bool setupObjectiveSystem = true;
        private bool setupCheckpointSystem = true;
        private bool setupSceneConnector = true;
        private bool setupVRHUD = true;
        private bool setupEnvironment = true;

        // 게임 설정
        private int requiredKeys = 3;
        private bool useTimeLimit = false;
        private float timeLimit = 600f;
        private float gameOverDelay = 3f;

        // 환경 설정
        private Color ambientColor = new Color(0.1f, 0.1f, 0.15f);
        private Color fogColor = new Color(0.05f, 0.05f, 0.08f);
        private float fogDensity = 0.02f;
        private bool enableFog = true;

        // 난이도 설정
        private GameDifficulty difficulty = GameDifficulty.Normal;

        private Vector2 scrollPos;

        public enum GameDifficulty
        {
            Easy,       // 쉬움
            Normal,     // 보통
            Hard,       // 어려움
            Nightmare   // 악몽
        }

        [MenuItem("Horror Game/게임 기본 설정 도구", false, 130)]
        public static void ShowWindow()
        {
            var window = GetWindow<GameSetupTool>("게임 기본 설정");
            window.minSize = new Vector2(400, 650);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 헤더
            EditorGUILayout.Space(10);
            GUILayout.Label("공포 게임 기본 설정 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "게임에 필요한 모든 매니저와 시스템을 자동으로 설정합니다.\n" +
                "씬에 필수 오브젝트들이 생성되고 연결됩니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 매니저 선택
            EditorGUILayout.LabelField("설치할 시스템", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            setupGameManager = EditorGUILayout.Toggle("게임 매니저", setupGameManager);
            setupStoryManager = EditorGUILayout.Toggle("스토리 진행 매니저", setupStoryManager);
            setupAudioManager = EditorGUILayout.Toggle("오디오 매니저", setupAudioManager);
            setupObjectiveSystem = EditorGUILayout.Toggle("목표 시스템", setupObjectiveSystem);
            setupCheckpointSystem = EditorGUILayout.Toggle("체크포인트 시스템", setupCheckpointSystem);
            setupSceneConnector = EditorGUILayout.Toggle("씬 자동 연결", setupSceneConnector);
            setupVRHUD = EditorGUILayout.Toggle("VR HUD", setupVRHUD);
            setupEnvironment = EditorGUILayout.Toggle("환경 설정 (조명/안개)", setupEnvironment);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 난이도 설정
            EditorGUILayout.LabelField("난이도 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            difficulty = (GameDifficulty)EditorGUILayout.EnumPopup("난이도", difficulty);
            ShowDifficultyInfo();

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 게임 설정
            EditorGUILayout.LabelField("게임 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            requiredKeys = EditorGUILayout.IntSlider("필요한 열쇠 수", requiredKeys, 1, 10);
            useTimeLimit = EditorGUILayout.Toggle("제한 시간 사용", useTimeLimit);

            if (useTimeLimit)
            {
                timeLimit = EditorGUILayout.FloatField("제한 시간 (초)", timeLimit);
                int minutes = Mathf.FloorToInt(timeLimit / 60);
                int seconds = Mathf.FloorToInt(timeLimit % 60);
                EditorGUILayout.HelpBox($"= {minutes}분 {seconds}초", MessageType.None);
            }

            gameOverDelay = EditorGUILayout.FloatField("게임오버 딜레이", gameOverDelay);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 환경 설정
            if (setupEnvironment)
            {
                EditorGUILayout.LabelField("환경 설정", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                ambientColor = EditorGUILayout.ColorField("앰비언트 색상", ambientColor);
                enableFog = EditorGUILayout.Toggle("안개 활성화", enableFog);

                if (enableFog)
                {
                    fogColor = EditorGUILayout.ColorField("안개 색상", fogColor);
                    fogDensity = EditorGUILayout.Slider("안개 밀도", fogDensity, 0.001f, 0.1f);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(20);

            // 버튼들
            EditorGUILayout.LabelField("실행", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("게임 기본 설정 적용", GUILayout.Height(45)))
            {
                ApplyGameSetup();
            }

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            if (GUILayout.Button("전체 게임 빠른 설정 (모든 시스템)", GUILayout.Height(35)))
            {
                QuickFullSetup();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // 유틸리티
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("태그/레이어 설정"))
            {
                SetupTagsAndLayers();
            }
            if (GUILayout.Button("프로젝트 설정 확인"))
            {
                CheckProjectSettings();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("게임 매니저 찾기"))
            {
                FindAndSelectManager<HorrorGameManager>("HorrorGameManager");
            }
            if (GUILayout.Button("모든 매니저 표시"))
            {
                ShowAllManagers();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("씬 검증 (필수 요소 확인)"))
            {
                ValidateScene();
            }

            EditorGUILayout.Space(10);

            // 프리셋
            EditorGUILayout.LabelField("빠른 프리셋", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("공포 분위기"))
            {
                ApplyHorrorPreset();
            }
            if (GUILayout.Button("던전 분위기"))
            {
                ApplyDungeonPreset();
            }
            if (GUILayout.Button("밝은 분위기"))
            {
                ApplyBrightPreset();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 난이도 정보 표시
        /// </summary>
        private void ShowDifficultyInfo()
        {
            string info = "";
            switch (difficulty)
            {
                case GameDifficulty.Easy:
                    info = "적 속도 -20%, 시야 -20%, 스태미나 +50%";
                    break;
                case GameDifficulty.Normal:
                    info = "기본 설정";
                    break;
                case GameDifficulty.Hard:
                    info = "적 속도 +20%, 시야 +20%, 스태미나 -20%";
                    break;
                case GameDifficulty.Nightmare:
                    info = "적 속도 +50%, 시야 +50%, 스태미나 -50%, 손전등 배터리 -50%";
                    break;
            }
            EditorGUILayout.HelpBox(info, MessageType.None);
        }

        /// <summary>
        /// 게임 설정 적용
        /// </summary>
        private void ApplyGameSetup()
        {
            // 게임 매니저 루트 생성
            GameObject gameManagersRoot = GameObject.Find("--- GAME MANAGERS ---");
            if (gameManagersRoot == null)
            {
                gameManagersRoot = new GameObject("--- GAME MANAGERS ---");
                Undo.RegisterCreatedObjectUndo(gameManagersRoot, "Create Game Managers");
            }

            if (setupGameManager) SetupHorrorGameManager(gameManagersRoot);
            if (setupStoryManager) SetupStoryProgressManager(gameManagersRoot);
            if (setupAudioManager) SetupAudioManager(gameManagersRoot);
            if (setupObjectiveSystem) SetupObjectiveSystem(gameManagersRoot);
            if (setupCheckpointSystem) SetupCheckpointSystem(gameManagersRoot);
            if (setupSceneConnector) SetupSceneAutoConnector(gameManagersRoot);
            if (setupVRHUD) SetupVRHUD();
            if (setupEnvironment) ApplyEnvironmentSettings();

            // 난이도 적용
            ApplyDifficultySettings();

            Selection.activeGameObject = gameManagersRoot;

            Debug.Log("[GameSetupTool] 게임 기본 설정이 완료되었습니다!");
            EditorUtility.DisplayDialog("완료", "게임 기본 설정이 완료되었습니다!", "확인");
        }

        /// <summary>
        /// 전체 빠른 설정
        /// </summary>
        private void QuickFullSetup()
        {
            setupGameManager = true;
            setupStoryManager = true;
            setupAudioManager = true;
            setupObjectiveSystem = true;
            setupCheckpointSystem = true;
            setupSceneConnector = true;
            setupVRHUD = true;
            setupEnvironment = true;

            ApplyGameSetup();
            SetupTagsAndLayers();
        }

        /// <summary>
        /// HorrorGameManager 설정
        /// </summary>
        private void SetupHorrorGameManager(GameObject parent)
        {
            HorrorGameManager existing = Object.FindFirstObjectByType<HorrorGameManager>();
            if (existing != null)
            {
                // 기존 매니저 업데이트
                existing.requiredKeysToEscape = requiredKeys;
                existing.useTimeLimit = useTimeLimit;
                existing.timeLimit = timeLimit;
                existing.gameOverDelay = gameOverDelay;
                EditorUtility.SetDirty(existing);
                Debug.Log("[GameSetupTool] 기존 HorrorGameManager 업데이트됨");
                return;
            }

            GameObject obj = new GameObject("HorrorGameManager");
            obj.transform.SetParent(parent.transform);

            var manager = obj.AddComponent<HorrorGameManager>();
            manager.requiredKeysToEscape = requiredKeys;
            manager.useTimeLimit = useTimeLimit;
            manager.timeLimit = timeLimit;
            manager.gameOverDelay = gameOverDelay;
            manager.victoryDelay = 5f;

            obj.AddComponent<AudioSource>();

            Debug.Log("[GameSetupTool] HorrorGameManager 생성됨");
        }

        /// <summary>
        /// StoryProgressManager 설정
        /// </summary>
        private void SetupStoryProgressManager(GameObject parent)
        {
            StoryProgressManager existing = Object.FindFirstObjectByType<StoryProgressManager>();
            if (existing != null)
            {
                Debug.Log("[GameSetupTool] 기존 StoryProgressManager 발견됨");
                return;
            }

            GameObject obj = new GameObject("StoryProgressManager");
            obj.transform.SetParent(parent.transform);

            var manager = obj.AddComponent<StoryProgressManager>();

            Debug.Log("[GameSetupTool] StoryProgressManager 생성됨");
        }

        /// <summary>
        /// AudioManager 설정
        /// </summary>
        private void SetupAudioManager(GameObject parent)
        {
            HorrorSoundManager existing = Object.FindFirstObjectByType<HorrorSoundManager>();
            if (existing != null)
            {
                Debug.Log("[GameSetupTool] 기존 HorrorSoundManager 발견됨");
                return;
            }

            GameObject obj = new GameObject("HorrorSoundManager");
            obj.transform.SetParent(parent.transform);

            var manager = obj.AddComponent<HorrorSoundManager>();

            // 오디오 소스들 추가
            for (int i = 0; i < 3; i++)
            {
                obj.AddComponent<AudioSource>();
            }

            Debug.Log("[GameSetupTool] HorrorSoundManager 생성됨");
        }

        /// <summary>
        /// ObjectiveSystem 설정
        /// </summary>
        private void SetupObjectiveSystem(GameObject parent)
        {
            ObjectiveSystem existing = Object.FindFirstObjectByType<ObjectiveSystem>();
            if (existing != null)
            {
                Debug.Log("[GameSetupTool] 기존 ObjectiveSystem 발견됨");
                return;
            }

            GameObject obj = new GameObject("ObjectiveSystem");
            obj.transform.SetParent(parent.transform);

            var system = obj.AddComponent<ObjectiveSystem>();

            Debug.Log("[GameSetupTool] ObjectiveSystem 생성됨");
        }

        /// <summary>
        /// CheckpointSystem 설정
        /// </summary>
        private void SetupCheckpointSystem(GameObject parent)
        {
            CheckpointSystem existing = Object.FindFirstObjectByType<CheckpointSystem>();
            if (existing != null)
            {
                Debug.Log("[GameSetupTool] 기존 CheckpointSystem 발견됨");
                return;
            }

            GameObject obj = new GameObject("CheckpointSystem");
            obj.transform.SetParent(parent.transform);

            var system = obj.AddComponent<CheckpointSystem>();

            Debug.Log("[GameSetupTool] CheckpointSystem 생성됨");
        }

        /// <summary>
        /// SceneAutoConnector 설정
        /// </summary>
        private void SetupSceneAutoConnector(GameObject parent)
        {
            SceneAutoConnector existing = Object.FindFirstObjectByType<SceneAutoConnector>();
            if (existing != null)
            {
                Debug.Log("[GameSetupTool] 기존 SceneAutoConnector 발견됨");
                return;
            }

            GameObject obj = new GameObject("SceneAutoConnector");
            obj.transform.SetParent(parent.transform);

            var connector = obj.AddComponent<SceneAutoConnector>();

            Debug.Log("[GameSetupTool] SceneAutoConnector 생성됨");
        }

        /// <summary>
        /// VR HUD 설정
        /// </summary>
        private void SetupVRHUD()
        {
            VRHUD existing = Object.FindFirstObjectByType<VRHUD>();
            if (existing != null)
            {
                Debug.Log("[GameSetupTool] 기존 VRHUD 발견됨");
                return;
            }

            // 메인 카메라 찾기
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Debug.LogWarning("[GameSetupTool] Main Camera를 찾을 수 없어 VRHUD를 생성하지 않았습니다.");
                return;
            }

            GameObject hudCanvas = new GameObject("VR HUD Canvas");
            hudCanvas.transform.SetParent(mainCam.transform);
            hudCanvas.transform.localPosition = new Vector3(0, 0, 0.5f);
            hudCanvas.transform.localRotation = Quaternion.identity;
            hudCanvas.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = hudCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 300);

            hudCanvas.AddComponent<VRHUD>();

            Debug.Log("[GameSetupTool] VRHUD 생성됨");
        }

        /// <summary>
        /// 환경 설정 적용
        /// </summary>
        private void ApplyEnvironmentSettings()
        {
            // 앰비언트 라이트
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;

            // 안개
            RenderSettings.fog = enableFog;
            if (enableFog)
            {
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogDensity = fogDensity;
            }

            // Skybox 어둡게
            RenderSettings.skybox = null; // 검은 배경

            Debug.Log("[GameSetupTool] 환경 설정 적용됨");
        }

        /// <summary>
        /// 난이도 설정 적용
        /// </summary>
        private void ApplyDifficultySettings()
        {
            // 난이도별 설정 값
            float speedMult = 1f;
            float visionMult = 1f;
            float difficultyKillerDelay = 30f;
            float difficultyTimeLimit = 600f;

            switch (difficulty)
            {
                case GameDifficulty.Easy:
                    speedMult = 0.8f;
                    visionMult = 0.8f;
                    difficultyKillerDelay = 60f;
                    difficultyTimeLimit = 900f;
                    break;
                case GameDifficulty.Normal:
                    speedMult = 1f;
                    visionMult = 1f;
                    difficultyKillerDelay = 30f;
                    difficultyTimeLimit = 600f;
                    break;
                case GameDifficulty.Hard:
                    speedMult = 1.2f;
                    visionMult = 1.2f;
                    difficultyKillerDelay = 20f;
                    difficultyTimeLimit = 480f;
                    break;
                case GameDifficulty.Nightmare:
                    speedMult = 1.5f;
                    visionMult = 1.5f;
                    difficultyKillerDelay = 10f;
                    difficultyTimeLimit = 300f;
                    break;
            }

            // VRPlayer 찾기
            VRPlayer player = Object.FindFirstObjectByType<VRPlayer>();
            if (player != null)
            {
                switch (difficulty)
                {
                    case GameDifficulty.Easy:
                        player.maxStamina = 150f;
                        break;
                    case GameDifficulty.Normal:
                        player.maxStamina = 100f;
                        break;
                    case GameDifficulty.Hard:
                        player.maxStamina = 80f;
                        break;
                    case GameDifficulty.Nightmare:
                        player.maxStamina = 50f;
                        break;
                }
                player.currentStamina = player.maxStamina;
                EditorUtility.SetDirty(player);
            }

            // KillerAI 찾기
            KillerAI killer = Object.FindFirstObjectByType<KillerAI>();
            if (killer != null)
            {
                killer.patrolSpeed = 2f * speedMult;
                killer.chaseSpeed = 4.5f * speedMult;
                killer.viewDistance = 15f * visionMult;
                EditorUtility.SetDirty(killer);
            }

            // GameSetup 컴포넌트에도 적용
            GameSetup gameSetup = Object.FindFirstObjectByType<GameSetup>();
            if (gameSetup != null)
            {
                gameSetup.killerSpeedMultiplier = speedMult;
                gameSetup.killerDelay = difficultyKillerDelay;
                gameSetup.timeLimit = difficultyTimeLimit;
                EditorUtility.SetDirty(gameSetup);
            }

            Debug.Log($"[GameSetupTool] 난이도 '{difficulty}' 적용됨");
        }

        /// <summary>
        /// 태그/레이어 설정
        /// </summary>
        private void SetupTagsAndLayers()
        {
            // Tags
            string[] requiredTags = { "Player", "Enemy", "Interactable", "HidingSpot", "Key", "Door", "ClueItem" };

            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            foreach (string tag in requiredTags)
            {
                bool found = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                }
            }

            tagManager.ApplyModifiedProperties();

            Debug.Log("[GameSetupTool] 태그 설정 완료");
            EditorUtility.DisplayDialog("완료", "태그가 설정되었습니다.\n\n추가된 태그:\n" + string.Join(", ", requiredTags), "확인");
        }

        /// <summary>
        /// 프로젝트 설정 확인
        /// </summary>
        private void CheckProjectSettings()
        {
            string report = "프로젝트 설정 확인\n\n";

            // VR 설정
            #if UNITY_XR_OPENXR
            report += "✓ OpenXR 활성화됨\n";
            #else
            report += "✗ OpenXR 비활성화\n";
            #endif

            // Input System
            #if ENABLE_INPUT_SYSTEM
            report += "✓ New Input System 활성화됨\n";
            #else
            report += "✗ New Input System 비활성화\n";
            #endif

            // 렌더링
            var currentRP = GraphicsSettings.currentRenderPipeline;
            if (currentRP != null)
            {
                report += $"✓ Render Pipeline: {currentRP.name}\n";
            }
            else
            {
                report += "△ Built-in Render Pipeline 사용 중\n";
            }

            // 품질 설정
            report += $"✓ 현재 품질 레벨: {QualitySettings.names[QualitySettings.GetQualityLevel()]}\n";

            // 씬 내 필수 요소
            report += "\n씬 내 요소:\n";
            report += Object.FindFirstObjectByType<HorrorGameManager>() != null ? "✓ HorrorGameManager\n" : "✗ HorrorGameManager\n";
            report += Object.FindFirstObjectByType<VRPlayer>() != null ? "✓ VRPlayer\n" : "✗ VRPlayer\n";
            report += Object.FindFirstObjectByType<KillerAI>() != null ? "✓ KillerAI\n" : "✗ KillerAI\n";
            report += Camera.main != null ? "✓ Main Camera\n" : "✗ Main Camera\n";

            EditorUtility.DisplayDialog("프로젝트 설정", report, "확인");
        }

        /// <summary>
        /// 씬 검증
        /// </summary>
        private void ValidateScene()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            List<string> success = new List<string>();

            // 필수 요소 확인
            if (Object.FindFirstObjectByType<HorrorGameManager>() == null)
                errors.Add("HorrorGameManager 없음");
            else
                success.Add("HorrorGameManager");

            if (Object.FindFirstObjectByType<VRPlayer>() == null)
                warnings.Add("VRPlayer 없음");
            else
                success.Add("VRPlayer");

            if (Object.FindFirstObjectByType<KillerAI>() == null)
                warnings.Add("KillerAI (살인마) 없음");
            else
                success.Add("KillerAI");

            if (Camera.main == null)
                errors.Add("Main Camera 없음");
            else
                success.Add("Main Camera");

            // 문 확인
            var doors = Object.FindObjectsByType<Door>(FindObjectsSortMode.None);
            if (doors.Length == 0)
                warnings.Add("Door 없음");
            else
                success.Add($"Door ({doors.Length}개)");

            // NavMesh 확인
            var navMeshSurface = Object.FindFirstObjectByType<UnityEngine.AI.NavMeshAgent>();
            if (navMeshSurface == null)
                warnings.Add("NavMeshAgent 없음 (AI 이동 불가)");

            // 결과 표시
            string report = "=== 씬 검증 결과 ===\n\n";

            if (success.Count > 0)
            {
                report += "✓ 정상:\n";
                foreach (var s in success) report += $"  • {s}\n";
            }

            if (warnings.Count > 0)
            {
                report += "\n△ 경고:\n";
                foreach (var w in warnings) report += $"  • {w}\n";
            }

            if (errors.Count > 0)
            {
                report += "\n✗ 오류:\n";
                foreach (var e in errors) report += $"  • {e}\n";
            }

            if (errors.Count == 0 && warnings.Count == 0)
            {
                report += "\n모든 필수 요소가 준비되었습니다!";
            }

            EditorUtility.DisplayDialog("씬 검증", report, "확인");
        }

        /// <summary>
        /// 매니저 찾기
        /// </summary>
        private void FindAndSelectManager<T>(string typeName) where T : Object
        {
            T manager = Object.FindFirstObjectByType<T>();
            if (manager != null)
            {
                if (manager is Component comp)
                {
                    Selection.activeGameObject = comp.gameObject;
                    EditorGUIUtility.PingObject(comp.gameObject);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("알림", $"{typeName}을(를) 찾을 수 없습니다.", "확인");
            }
        }

        /// <summary>
        /// 모든 매니저 표시
        /// </summary>
        private void ShowAllManagers()
        {
            string info = "현재 씬의 매니저:\n\n";

            var gameManager = Object.FindFirstObjectByType<HorrorGameManager>();
            info += gameManager != null ? $"✓ HorrorGameManager: {gameManager.gameObject.name}\n" : "✗ HorrorGameManager\n";

            var storyManager = Object.FindFirstObjectByType<StoryProgressManager>();
            info += storyManager != null ? $"✓ StoryProgressManager: {storyManager.gameObject.name}\n" : "✗ StoryProgressManager\n";

            var audioManager = Object.FindFirstObjectByType<HorrorSoundManager>();
            info += audioManager != null ? $"✓ HorrorSoundManager: {audioManager.gameObject.name}\n" : "✗ HorrorSoundManager\n";

            var objective = Object.FindFirstObjectByType<ObjectiveSystem>();
            info += objective != null ? $"✓ ObjectiveSystem: {objective.gameObject.name}\n" : "✗ ObjectiveSystem\n";

            var checkpoint = Object.FindFirstObjectByType<CheckpointSystem>();
            info += checkpoint != null ? $"✓ CheckpointSystem: {checkpoint.gameObject.name}\n" : "✗ CheckpointSystem\n";

            var vrhud = Object.FindFirstObjectByType<VRHUD>();
            info += vrhud != null ? $"✓ VRHUD: {vrhud.gameObject.name}\n" : "✗ VRHUD\n";

            EditorUtility.DisplayDialog("매니저 목록", info, "확인");
        }

        /// <summary>
        /// 공포 분위기 프리셋
        /// </summary>
        private void ApplyHorrorPreset()
        {
            ambientColor = new Color(0.05f, 0.05f, 0.08f);
            fogColor = new Color(0.02f, 0.02f, 0.03f);
            fogDensity = 0.03f;
            enableFog = true;
            ApplyEnvironmentSettings();
            Debug.Log("[GameSetupTool] 공포 분위기 프리셋 적용됨");
        }

        /// <summary>
        /// 던전 분위기 프리셋
        /// </summary>
        private void ApplyDungeonPreset()
        {
            ambientColor = new Color(0.08f, 0.06f, 0.04f);
            fogColor = new Color(0.05f, 0.03f, 0.02f);
            fogDensity = 0.025f;
            enableFog = true;
            ApplyEnvironmentSettings();
            Debug.Log("[GameSetupTool] 던전 분위기 프리셋 적용됨");
        }

        /// <summary>
        /// 밝은 분위기 프리셋
        /// </summary>
        private void ApplyBrightPreset()
        {
            ambientColor = new Color(0.3f, 0.3f, 0.35f);
            fogColor = new Color(0.5f, 0.5f, 0.5f);
            fogDensity = 0.005f;
            enableFog = false;
            ApplyEnvironmentSettings();
            Debug.Log("[GameSetupTool] 밝은 분위기 프리셋 적용됨");
        }

        /// <summary>
        /// 빠른 게임 설정
        /// </summary>
        [MenuItem("Horror Game/게임 빠른 설정 (전체)", false, 131)]
        public static void QuickGameSetup()
        {
            var tool = CreateInstance<GameSetupTool>();
            tool.QuickFullSetup();
            DestroyImmediate(tool);
        }
    }
}
