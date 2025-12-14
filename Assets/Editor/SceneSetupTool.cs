using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
using System.Linq;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 씬 설정 자동화 도구
    /// MainMenu 씬과 Game 씬에 필요한 UI 및 시스템 오브젝트 자동 생성
    /// </summary>
    public class SceneSetupTool : EditorWindow
    {
        [MenuItem("Horror Game/Scene Setup Tool", false, 100)]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupTool>("Scene Setup Tool");
        }

        private void OnGUI()
        {
            GUILayout.Label("씬 설정 자동화 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox("현재 씬에 필요한 오브젝트들을 자동으로 생성합니다.", MessageType.Info);
            EditorGUILayout.Space(10);

            // MainMenu 씬 설정
            GUILayout.Label("MainMenu 씬 설정", EditorStyles.boldLabel);
            if (GUILayout.Button("MainMenu UI 전체 생성", GUILayout.Height(30)))
            {
                SetupMainMenuScene();
            }
            EditorGUILayout.Space(5);

            // Game 씬 설정
            GUILayout.Label("Game 씬 설정", EditorStyles.boldLabel);
            if (GUILayout.Button("Intro/Ending 시퀀스 추가", GUILayout.Height(30)))
            {
                SetupGameSequences();
            }
            EditorGUILayout.Space(5);

            // 유령 시스템 폐기됨
            EditorGUILayout.Space(10);

            // 개별 설정
            GUILayout.Label("개별 설정", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Canvas 생성"))
            {
                CreateUICanvas();
            }
            if (GUILayout.Button("EventSystem 생성"))
            {
                CreateEventSystem();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Build Settings
            GUILayout.Label("Build Settings", EditorStyles.boldLabel);
            if (GUILayout.Button("씬 Build Settings에 추가", GUILayout.Height(25)))
            {
                AddScenesToBuildSettings();
            }
        }

        #region MainMenu Scene Setup

        private void SetupMainMenuScene()
        {
            // Canvas 생성
            GameObject canvas = CreateUICanvas();

            // MainMenuManager 생성
            GameObject menuManager = new GameObject("MainMenuManager");
            menuManager.AddComponent<MainMenuUI>();
            menuManager.AddComponent<AudioSource>();
            Undo.RegisterCreatedObjectUndo(menuManager, "Create MainMenuManager");

            // MainMenu Panel
            GameObject mainPanel = CreatePanel(canvas.transform, "MainMenuPanel");

            // 타이틀
            CreateTextMeshPro(mainPanel.transform, "TitleText", "Run 'Till the End",
                new Vector2(0, 200), new Vector2(800, 100), 72, TextAlignmentOptions.Center);

            CreateTextMeshPro(mainPanel.transform, "SubtitleText", "끝까지 달려라",
                new Vector2(0, 120), new Vector2(600, 50), 36, TextAlignmentOptions.Center);

            // 버튼들
            GameObject startBtn = CreateButton(mainPanel.transform, "StartButton", "게임 시작",
                new Vector2(0, 0), new Vector2(300, 60));

            GameObject continueBtn = CreateButton(mainPanel.transform, "ContinueButton", "계속하기",
                new Vector2(0, -80), new Vector2(300, 60));

            GameObject settingsBtn = CreateButton(mainPanel.transform, "SettingsButton", "설정",
                new Vector2(0, -160), new Vector2(300, 60));

            GameObject quitBtn = CreateButton(mainPanel.transform, "QuitButton", "종료",
                new Vector2(0, -240), new Vector2(300, 60));

            // Settings Panel
            GameObject settingsPanel = CreatePanel(canvas.transform, "SettingsPanel");
            settingsPanel.SetActive(false);

            CreateTextMeshPro(settingsPanel.transform, "SettingsTitle", "설정",
                new Vector2(0, 200), new Vector2(400, 80), 48, TextAlignmentOptions.Center);

            // 볼륨 슬라이더
            CreateSlider(settingsPanel.transform, "MasterVolumeSlider", "마스터 볼륨", new Vector2(0, 80));
            CreateSlider(settingsPanel.transform, "BGMVolumeSlider", "배경음악", new Vector2(0, 0));
            CreateSlider(settingsPanel.transform, "SFXVolumeSlider", "효과음", new Vector2(0, -80));

            GameObject settingsBackBtn = CreateButton(settingsPanel.transform, "SettingsBackButton", "뒤로",
                new Vector2(0, -200), new Vector2(200, 50));

            // Difficulty Panel
            GameObject difficultyPanel = CreatePanel(canvas.transform, "DifficultyPanel");
            difficultyPanel.SetActive(false);

            CreateTextMeshPro(difficultyPanel.transform, "DifficultyTitle", "난이도 선택",
                new Vector2(0, 150), new Vector2(400, 80), 48, TextAlignmentOptions.Center);

            CreateButton(difficultyPanel.transform, "EasyButton", "쉬움",
                new Vector2(0, 50), new Vector2(250, 50));
            CreateButton(difficultyPanel.transform, "NormalButton", "보통",
                new Vector2(0, -20), new Vector2(250, 50));
            CreateButton(difficultyPanel.transform, "HardButton", "어려움",
                new Vector2(0, -90), new Vector2(250, 50));
            CreateButton(difficultyPanel.transform, "BackButton", "뒤로",
                new Vector2(0, -180), new Vector2(200, 50));

            // Fade Image
            GameObject fadeImage = new GameObject("FadeImage");
            fadeImage.transform.SetParent(canvas.transform, false);
            Image fade = fadeImage.AddComponent<Image>();
            fade.color = Color.black;
            RectTransform fadeRect = fadeImage.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.sizeDelta = Vector2.zero;
            fadeImage.SetActive(false);

            // MainMenuUI 컴포넌트 연결
            MainMenuUI menuUI = menuManager.GetComponent<MainMenuUI>();
            menuUI.mainMenuPanel = mainPanel;
            menuUI.settingsPanel = settingsPanel;
            menuUI.difficultyPanel = difficultyPanel;
            menuUI.fadeImage = fade;
            menuUI.startButton = startBtn.GetComponent<Button>();
            menuUI.continueButton = continueBtn.GetComponent<Button>();
            menuUI.settingsButton = settingsBtn.GetComponent<Button>();
            menuUI.quitButton = quitBtn.GetComponent<Button>();
            menuUI.settingsBackButton = settingsBackBtn.GetComponent<Button>();
            menuUI.titleText = mainPanel.transform.Find("TitleText").GetComponent<TextMeshProUGUI>();

            // EventSystem 확인
            CreateEventSystem();

            EditorUtility.SetDirty(menuManager);
            Debug.Log("[SceneSetupTool] MainMenu 씬 설정 완료!");
        }

        #endregion

        #region Game Scene Setup

        private void SetupGameSequences()
        {
            // IntroSequence 생성
            if (FindObjectOfType<IntroSequence>() == null)
            {
                GameObject introObj = new GameObject("IntroSequence");
                IntroSequence intro = introObj.AddComponent<IntroSequence>();
                introObj.AddComponent<AudioSource>();
                Undo.RegisterCreatedObjectUndo(introObj, "Create IntroSequence");

                // Intro Canvas
                GameObject introCanvas = CreateUICanvas("IntroCanvas");
                intro.introCanvas = introCanvas.GetComponent<Canvas>();

                // Fade Image
                GameObject fadeImg = new GameObject("FadeImage");
                fadeImg.transform.SetParent(introCanvas.transform, false);
                Image fade = fadeImg.AddComponent<Image>();
                fade.color = Color.black;
                RectTransform fadeRect = fadeImg.GetComponent<RectTransform>();
                fadeRect.anchorMin = Vector2.zero;
                fadeRect.anchorMax = Vector2.one;
                fadeRect.sizeDelta = Vector2.zero;
                intro.fadeImage = fade;

                // Intro Text
                GameObject introText = CreateTextMeshPro(introCanvas.transform, "IntroText", "",
                    new Vector2(0, 0), new Vector2(800, 200), 36, TextAlignmentOptions.Center);
                intro.introText = introText.GetComponent<TextMeshProUGUI>();

                // YES Button
                GameObject yesBtn = CreateButton(introCanvas.transform, "YesButton", "YES",
                    new Vector2(0, -150), new Vector2(200, 60));
                yesBtn.SetActive(false);
                intro.yesButton = yesBtn.GetComponent<Button>();
                intro.yesButtonText = yesBtn.GetComponentInChildren<TextMeshProUGUI>();

                Debug.Log("[SceneSetupTool] IntroSequence 생성 완료!");
            }
            else
            {
                Debug.Log("[SceneSetupTool] IntroSequence가 이미 존재합니다.");
            }

            // EndingSequence 생성 (World Space UI)
            if (FindObjectOfType<EndingSequence>() == null)
            {
                GameObject endingObj = new GameObject("EndingSequence");
                EndingSequence ending = endingObj.AddComponent<EndingSequence>();
                endingObj.AddComponent<AudioSource>();
                Undo.RegisterCreatedObjectUndo(endingObj, "Create EndingSequence");

                // World Space Canvas 생성
                GameObject canvasObj = new GameObject("EndingWorldSpaceCanvas");
                Canvas worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
                worldSpaceCanvas.renderMode = RenderMode.WorldSpace;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10f;

                canvasObj.AddComponent<GraphicRaycaster>();

                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(800, 600);
                canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                canvasObj.transform.position = Vector3.zero;

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
                GameObject victoryTextObj = new GameObject("VictoryText");
                victoryTextObj.transform.SetParent(canvasObj.transform, false);
                RectTransform victoryTextRect = victoryTextObj.AddComponent<RectTransform>();
                victoryTextRect.anchorMin = new Vector2(0.5f, 0.6f);
                victoryTextRect.anchorMax = new Vector2(0.5f, 0.6f);
                victoryTextRect.sizeDelta = new Vector2(700, 200);
                victoryTextRect.anchoredPosition = Vector2.zero;
                TextMeshProUGUI victoryText = victoryTextObj.AddComponent<TextMeshProUGUI>();
                victoryText.text = "";
                victoryText.fontSize = 40;
                victoryText.color = Color.white;
                victoryText.alignment = TextAlignmentOptions.Center;

                // 크레딧 텍스트
                GameObject creditsTextObj = new GameObject("CreditsText");
                creditsTextObj.transform.SetParent(canvasObj.transform, false);
                RectTransform creditsTextRect = creditsTextObj.AddComponent<RectTransform>();
                creditsTextRect.anchorMin = new Vector2(0.5f, 0.5f);
                creditsTextRect.anchorMax = new Vector2(0.5f, 0.5f);
                creditsTextRect.sizeDelta = new Vector2(700, 300);
                creditsTextRect.anchoredPosition = Vector2.zero;
                TextMeshProUGUI creditsText = creditsTextObj.AddComponent<TextMeshProUGUI>();
                creditsText.text = "";
                creditsText.fontSize = 32;
                creditsText.color = Color.white;
                creditsText.alignment = TextAlignmentOptions.Center;
                creditsTextObj.SetActive(false);

                // 스킵 안내 텍스트
                GameObject skipHintObj = new GameObject("SkipHintText");
                skipHintObj.transform.SetParent(canvasObj.transform, false);
                RectTransform skipHintRect = skipHintObj.AddComponent<RectTransform>();
                skipHintRect.anchorMin = new Vector2(0.5f, 0.1f);
                skipHintRect.anchorMax = new Vector2(0.5f, 0.1f);
                skipHintRect.sizeDelta = new Vector2(400, 50);
                skipHintRect.anchoredPosition = Vector2.zero;
                TextMeshProUGUI skipHintText = skipHintObj.AddComponent<TextMeshProUGUI>();
                skipHintText.text = "아무 키나 눌러서 스킵";
                skipHintText.fontSize = 20;
                skipHintText.fontStyle = FontStyles.Italic;
                skipHintText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                skipHintText.alignment = TextAlignmentOptions.Center;
                skipHintObj.SetActive(false);

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
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                // 메인 메뉴 버튼
                GameObject mainMenuBtn = CreateButton(buttonContainer.transform, "MainMenuButton", "메인 메뉴",
                    Vector2.zero, new Vector2(180, 50));
                mainMenuBtn.SetActive(false);

                // 다시 시작 버튼
                GameObject restartBtn = CreateButton(buttonContainer.transform, "RestartButton", "다시 시작",
                    Vector2.zero, new Vector2(180, 50));
                restartBtn.SetActive(false);

                // EndingSequence에 참조 연결
                ending.worldSpaceCanvas = worldSpaceCanvas;
                ending.victoryText = victoryText;
                ending.creditsText = creditsText;
                ending.skipHintText = skipHintText;
                ending.mainMenuButton = mainMenuBtn.GetComponent<Button>();
                ending.restartButton = restartBtn.GetComponent<Button>();

                // 초기 상태: 캔버스 비활성화
                canvasObj.SetActive(false);

                Debug.Log("[SceneSetupTool] EndingSequence (World Space UI) 생성 완료!");
            }
            else
            {
                Debug.Log("[SceneSetupTool] EndingSequence가 이미 존재합니다.");
            }

            // HorrorGameManager 연결 확인
            HorrorGameManager gameManager = FindObjectOfType<HorrorGameManager>();
            if (gameManager != null)
            {
                gameManager.introSequence = FindObjectOfType<IntroSequence>();
                gameManager.endingSequence = FindObjectOfType<EndingSequence>();
                EditorUtility.SetDirty(gameManager);
                Debug.Log("[SceneSetupTool] HorrorGameManager에 시퀀스 연결 완료!");
            }

            CreateEventSystem();
        }

        // 유령 시스템 폐기됨 - SetupGhostAI 메서드 제거됨

        #endregion

        #region UI Helper Methods

        private GameObject CreateUICanvas(string name = "Canvas")
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.GetComponent<Canvas>() != null)
            {
                return existing;
            }

            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            return canvasObj;
        }

        private void CreateEventSystem()
        {
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            return panel;
        }

        private GameObject CreateButton(Transform parent, string name, string text, Vector2 position, Vector2 size)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f);
            colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
            btn.colors = colors;

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            return btnObj;
        }

        private GameObject CreateTextMeshPro(Transform parent, string name, string text,
            Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;

            return textObj;
        }

        private void CreateSlider(Transform parent, string name, string label, Vector2 position)
        {
            // Label
            CreateTextMeshPro(parent, name + "Label", label,
                position + new Vector2(-150, 0), new Vector2(150, 30), 20, TextAlignmentOptions.Right);

            // Slider
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position + new Vector2(50, 0);
            rect.sizeDelta = new Vector2(200, 20);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.3f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = new Vector2(-10, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.8f, 0.8f, 0.8f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = bgImg;
        }

        #endregion

        #region Menu Items (MCP 호출 가능)

        [MenuItem("Horror Game/Setup/Add EscapeZone to Rail_A (2)", false, 199)]
        public static void AddEscapeZoneToRail()
        {
            // Rail_A (2) 찾기
            GameObject railObj = GameObject.Find("Rail_A (2)");
            if (railObj == null)
            {
                Debug.LogError("[SceneSetupTool] Rail_A (2)를 찾을 수 없습니다!");
                return;
            }

            // EscapeZone 추가
            EscapeZone escapeZone = railObj.GetComponent<EscapeZone>();
            if (escapeZone == null)
            {
                escapeZone = railObj.AddComponent<EscapeZone>();
                Undo.RegisterCreatedObjectUndo(escapeZone, "Add EscapeZone");
            }

            escapeZone.requiredKeys = 0;
            escapeZone.autoEscape = true;
            escapeZone.skipKeyCheck = true; // 열쇠 확인 없이 무조건 승리

            // BoxCollider 확인/추가
            BoxCollider boxCollider = railObj.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = railObj.AddComponent<BoxCollider>();
            }
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(3f, 3f, 3f);

            EditorUtility.SetDirty(railObj);
            Debug.Log("[SceneSetupTool] Rail_A (2)에 EscapeZone 추가 완료!");
        }

        [MenuItem("Horror Game/Setup/Create MainMenu UI", false, 200)]
        public static void SetupMainMenuUI()
        {
            var tool = CreateInstance<SceneSetupTool>();
            tool.SetupMainMenuScene();
            DestroyImmediate(tool);
        }

        [MenuItem("Horror Game/Setup/Add Intro Ending Sequences", false, 201)]
        public static void SetupSequences()
        {
            var tool = CreateInstance<SceneSetupTool>();
            tool.SetupGameSequences();
            DestroyImmediate(tool);
        }

        // 유령 시스템 폐기됨 - SetupGhost 메뉴 아이템 제거됨

        [MenuItem("Horror Game/Setup/Configure Build Settings", false, 210)]
        public static void SetupBuildSettings()
        {
            var tool = CreateInstance<SceneSetupTool>();
            tool.AddScenesToBuildSettings();
            DestroyImmediate(tool);
        }

        [MenuItem("Horror Game/Setup/Setup Sound System", false, 220)]
        public static void SetupSoundSystem()
        {
            // HorrorSoundManager 찾기 또는 생성
            var soundManager = FindObjectOfType<HorrorSoundManager>();
            if (soundManager == null)
            {
                GameObject managerObj = new GameObject("HorrorSoundManager");
                soundManager = managerObj.AddComponent<HorrorSoundManager>();
                soundManager.footstepInterval = 0.5f;
                soundManager.runFootstepInterval = 0.3f;
                soundManager.whisperIntervalRange = new Vector2(30f, 90f);
                Undo.RegisterCreatedObjectUndo(managerObj, "Create HorrorSoundManager");
                Debug.Log("[SceneSetupTool] HorrorSoundManager 생성됨");
            }

            // AudioAutoLoader 추가
            var autoLoader = soundManager.GetComponent<AudioAutoLoader>();
            if (autoLoader == null)
            {
                autoLoader = soundManager.gameObject.AddComponent<AudioAutoLoader>();
                autoLoader.autoLoadOnStart = true;
                autoLoader.loadFootsteps = true;
                Debug.Log("[SceneSetupTool] AudioAutoLoader 추가됨");
            }

            // 발소리 할당
            AssignFootstepSoundsInternal(soundManager);

            EditorUtility.SetDirty(soundManager.gameObject);
            Selection.activeGameObject = soundManager.gameObject;
            Debug.Log("[SceneSetupTool] 사운드 시스템 설정 완료!");
        }

        private static void AssignFootstepSoundsInternal(HorrorSoundManager manager)
        {
            string footstepRoot = "Assets/Footstep(Concrete & Wood)";
            if (!System.IO.Directory.Exists(footstepRoot))
            {
                Debug.LogWarning("[SceneSetupTool] Footstep 에셋 폴더가 없습니다: " + footstepRoot);
                return;
            }

            // 콘크리트 발소리
            var concreteClips = LoadAudioClipsFromPath($"{footstepRoot}/Footstep  One Shots/concrete");
            if (concreteClips.Length > 0 && (manager.footstepSounds == null || manager.footstepSounds.Length == 0))
            {
                manager.footstepSounds = concreteClips.Take(10).ToArray();
                Debug.Log($"[SceneSetupTool] 걷기 발소리 {manager.footstepSounds.Length}개 할당");
            }

            // 달리기 발소리
            var runClips = LoadAudioClipsFromPath($"{footstepRoot}/Footstep Loops/concrete/running");
            if (runClips.Length > 0 && (manager.runFootstepSounds == null || manager.runFootstepSounds.Length == 0))
            {
                manager.runFootstepSounds = runClips;
                Debug.Log($"[SceneSetupTool] 달리기 발소리 {manager.runFootstepSounds.Length}개 할당");
            }

            // 웅크리기 발소리
            var slowClips = LoadAudioClipsFromPath($"{footstepRoot}/Footstep Loops/concrete/slowsteps");
            if (slowClips.Length > 0 && (manager.crouchFootstepSounds == null || manager.crouchFootstepSounds.Length == 0))
            {
                manager.crouchFootstepSounds = slowClips;
                Debug.Log($"[SceneSetupTool] 웅크리기 발소리 {manager.crouchFootstepSounds.Length}개 할당");
            }

            // KillerFootstep에도 할당
            var killerFootsteps = FindObjectsOfType<KillerFootstep>();
            foreach (var kf in killerFootsteps)
            {
                if (kf.walkFootsteps == null || kf.walkFootsteps.Length == 0)
                {
                    kf.walkFootsteps = concreteClips.Take(5).ToArray();
                }
                if (kf.runFootsteps == null || kf.runFootsteps.Length == 0)
                {
                    kf.runFootsteps = runClips;
                }
                EditorUtility.SetDirty(kf);
                Debug.Log($"[SceneSetupTool] {kf.gameObject.name}에 발소리 할당");
            }
        }

        /// <summary>
        /// 다운로드된 에셋 팩에서 사운드 할당
        /// Horror Starter Pack, Free Pack, Free UI Click Sound Effects Pack
        /// </summary>
        [MenuItem("Horror Game/Setup/Assign Downloaded Sound Assets", false, 221)]
        public static void AssignDownloadedSoundAssets()
        {
            var soundManager = FindObjectOfType<HorrorSoundManager>();
            if (soundManager == null)
            {
                Debug.LogError("[SceneSetupTool] HorrorSoundManager를 찾을 수 없습니다. 먼저 Setup Sound System을 실행하세요.");
                return;
            }

            int assignedCount = 0;

            // === Horror Starter Pack (배경음악) ===
            string horrorPackPath = "Assets/Horror Starter Pack";
            if (System.IO.Directory.Exists(horrorPackPath))
            {
                var horrorClips = LoadAudioClipsFromPath(horrorPackPath);

                // 평상시 배경음 - 잔잔한 분위기
                var normalClip = horrorClips.FirstOrDefault(c => c.name.Contains("lost") || c.name.Contains("tenent"));
                if (normalClip != null && soundManager.normalAmbient == null)
                {
                    soundManager.normalAmbient = normalClip;
                    Debug.Log($"[SceneSetupTool] normalAmbient 할당: {normalClip.name}");
                    assignedCount++;
                }

                // 긴장 배경음 - 긴장감 있는 분위기
                var tensionClip = horrorClips.FirstOrDefault(c => c.name.Contains("conjuring") || c.name.Contains("theroom"));
                if (tensionClip != null && soundManager.tensionAmbient == null)
                {
                    soundManager.tensionAmbient = tensionClip;
                    Debug.Log($"[SceneSetupTool] tensionAmbient 할당: {tensionClip.name}");
                    assignedCount++;
                }

                // 추격 배경음 - 빠르고 긴박한 분위기
                var chaseClip = horrorClips.FirstOrDefault(c => c.name.Contains("horroraction") || c.name.Contains("re8") || c.name.Contains("getout"));
                if (chaseClip != null && soundManager.chaseAmbient == null)
                {
                    soundManager.chaseAmbient = chaseClip;
                    Debug.Log($"[SceneSetupTool] chaseAmbient 할당: {chaseClip.name}");
                    assignedCount++;
                }

                Debug.Log($"[SceneSetupTool] Horror Starter Pack에서 {horrorClips.Length}개 클립 발견");
            }
            else
            {
                Debug.LogWarning("[SceneSetupTool] Horror Starter Pack 폴더가 없습니다: " + horrorPackPath);
            }

            // === Free Pack (공포 효과음) ===
            string freePackPath = "Assets/Free Pack";
            if (System.IO.Directory.Exists(freePackPath))
            {
                var freeClips = LoadAudioClipsFromPath(freePackPath);

                // Ghost 소리 → whisperSounds
                var ghostClips = freeClips.Where(c => c.name.Contains("Ghost")).ToArray();
                if (ghostClips.Length > 0 && (soundManager.whisperSounds == null || soundManager.whisperSounds.Length == 0))
                {
                    soundManager.whisperSounds = ghostClips;
                    Debug.Log($"[SceneSetupTool] whisperSounds 할당: {ghostClips.Length}개 (Ghost Manifestation)");
                    assignedCount += ghostClips.Length;
                }

                // Secret door → creekyDoorSounds
                var doorClips = freeClips.Where(c => c.name.Contains("door") || c.name.Contains("Door")).ToArray();
                if (doorClips.Length > 0 && (soundManager.creekyDoorSounds == null || soundManager.creekyDoorSounds.Length == 0))
                {
                    soundManager.creekyDoorSounds = doorClips;
                    Debug.Log($"[SceneSetupTool] creekyDoorSounds 할당: {doorClips.Length}개");
                    assignedCount += doorClips.Length;
                }

                // Explosion → jumpScareSound
                var explosionClip = freeClips.FirstOrDefault(c => c.name.Contains("Explosion 1"));
                if (explosionClip != null && soundManager.jumpScareSound == null)
                {
                    soundManager.jumpScareSound = explosionClip;
                    Debug.Log($"[SceneSetupTool] jumpScareSound 할당: {explosionClip.name}");
                    assignedCount++;
                }

                // Magic Spell → itemPickupSound (신비로운 효과)
                var magicClip = freeClips.FirstOrDefault(c => c.name.Contains("Magic Spell_Coins"));
                if (magicClip != null && soundManager.itemPickupSound == null)
                {
                    soundManager.itemPickupSound = magicClip;
                    Debug.Log($"[SceneSetupTool] itemPickupSound 할당: {magicClip.name}");
                    assignedCount++;
                }

                // Thunder → warningSound
                var thunderClip = freeClips.FirstOrDefault(c => c.name.Contains("Thunder"));
                if (thunderClip != null && soundManager.warningSound == null)
                {
                    soundManager.warningSound = thunderClip;
                    Debug.Log($"[SceneSetupTool] warningSound 할당: {thunderClip.name}");
                    assignedCount++;
                }

                // Earthquake → heartbeatSound (저주파 효과)
                var earthquakeClip = freeClips.FirstOrDefault(c => c.name.Contains("Earthquake"));
                if (earthquakeClip != null && soundManager.heartbeatSound == null)
                {
                    soundManager.heartbeatSound = earthquakeClip;
                    Debug.Log($"[SceneSetupTool] heartbeatSound 할당: {earthquakeClip.name}");
                    assignedCount++;
                }

                // Cavern Atmosphere → breathingSounds (분위기 추가)
                var cavernClips = freeClips.Where(c => c.name.Contains("Cavern") || c.name.Contains("Ice")).ToArray();
                if (cavernClips.Length > 0 && (soundManager.breathingSounds == null || soundManager.breathingSounds.Length == 0))
                {
                    soundManager.breathingSounds = cavernClips;
                    Debug.Log($"[SceneSetupTool] breathingSounds 할당: {cavernClips.Length}개 (Cavern/Ice 분위기)");
                    assignedCount += cavernClips.Length;
                }

                Debug.Log($"[SceneSetupTool] Free Pack에서 {freeClips.Length}개 클립 발견");
            }
            else
            {
                Debug.LogWarning("[SceneSetupTool] Free Pack 폴더가 없습니다: " + freePackPath);
            }

            // === Free UI Click Sound Effects Pack (UI 사운드) ===
            string uiPackPath = "Assets/Free UI Click Sound Effects Pack/AUDIO";
            if (System.IO.Directory.Exists(uiPackPath))
            {
                // Button 폴더에서 UI 클릭 사운드
                var buttonClips = LoadAudioClipsFromPath($"{uiPackPath}/Button");

                // objectiveCompleteSound - 성취감 있는 버튼 소리
                var completeClip = buttonClips.FirstOrDefault(c => c.name.Contains("Enter_Thick"));
                if (completeClip != null && soundManager.objectiveCompleteSound == null)
                {
                    soundManager.objectiveCompleteSound = completeClip;
                    Debug.Log($"[SceneSetupTool] objectiveCompleteSound 할당: {completeClip.name}");
                    assignedCount++;
                }

                // timerTickSound - 가벼운 클릭 소리
                var tickClip = buttonClips.FirstOrDefault(c => c.name.Contains("Thin_Generic"));
                if (tickClip != null && soundManager.timerTickSound == null)
                {
                    soundManager.timerTickSound = tickClip;
                    Debug.Log($"[SceneSetupTool] timerTickSound 할당: {tickClip.name}");
                    assignedCount++;
                }

                // Pop 폴더에서 아이템 획득 대체 사운드
                var popClips = LoadAudioClipsFromPath($"{uiPackPath}/Pop");
                if (soundManager.itemPickupSound == null && popClips.Length > 0)
                {
                    var popClip = popClips.FirstOrDefault(c => c.name.Contains("Thick_Generic"));
                    if (popClip != null)
                    {
                        soundManager.itemPickupSound = popClip;
                        Debug.Log($"[SceneSetupTool] itemPickupSound 할당 (Pop): {popClip.name}");
                        assignedCount++;
                    }
                }

                Debug.Log($"[SceneSetupTool] UI Click Sound Effects Pack에서 Button: {buttonClips.Length}개, Pop: {popClips.Length}개 클립 발견");
            }
            else
            {
                Debug.LogWarning("[SceneSetupTool] Free UI Click Sound Effects Pack 폴더가 없습니다: " + uiPackPath);
            }

            EditorUtility.SetDirty(soundManager);
            Debug.Log($"[SceneSetupTool] === 총 {assignedCount}개 사운드 할당 완료! ===");

            // 할당 결과 요약
            Debug.Log("[SceneSetupTool] === 사운드 할당 현황 ===");
            Debug.Log($"  normalAmbient: {(soundManager.normalAmbient != null ? soundManager.normalAmbient.name : "없음")}");
            Debug.Log($"  tensionAmbient: {(soundManager.tensionAmbient != null ? soundManager.tensionAmbient.name : "없음")}");
            Debug.Log($"  chaseAmbient: {(soundManager.chaseAmbient != null ? soundManager.chaseAmbient.name : "없음")}");
            Debug.Log($"  whisperSounds: {soundManager.whisperSounds?.Length ?? 0}개");
            Debug.Log($"  creekyDoorSounds: {soundManager.creekyDoorSounds?.Length ?? 0}개");
            Debug.Log($"  breathingSounds: {soundManager.breathingSounds?.Length ?? 0}개");
            Debug.Log($"  heartbeatSound: {(soundManager.heartbeatSound != null ? soundManager.heartbeatSound.name : "없음")}");
            Debug.Log($"  jumpScareSound: {(soundManager.jumpScareSound != null ? soundManager.jumpScareSound.name : "없음")}");
            Debug.Log($"  itemPickupSound: {(soundManager.itemPickupSound != null ? soundManager.itemPickupSound.name : "없음")}");
            Debug.Log($"  objectiveCompleteSound: {(soundManager.objectiveCompleteSound != null ? soundManager.objectiveCompleteSound.name : "없음")}");
            Debug.Log($"  warningSound: {(soundManager.warningSound != null ? soundManager.warningSound.name : "없음")}");
            Debug.Log($"  timerTickSound: {(soundManager.timerTickSound != null ? soundManager.timerTickSound.name : "없음")}");

            // 씬의 모든 Door에 사운드 할당
            AssignDoorSounds();
        }

        /// <summary>
        /// 씬의 모든 Door에 사운드 할당
        /// </summary>
        [MenuItem("Horror Game/Setup/Assign Door Sounds", false, 222)]
        public static void AssignDoorSounds()
        {
            // Wooden 사운드 로드 (문 두드리는 소리)
            string woodenPath = "Assets/Free UI Click Sound Effects Pack/AUDIO/Wooden";
            var woodenClips = LoadAudioClipsFromPath(woodenPath);

            // Secret door 사운드 (문 열림/닫힘)
            string freePackPath = "Assets/Free Pack";
            var freeClips = LoadAudioClipsFromPath(freePackPath);
            var secretDoorClip = freeClips.FirstOrDefault(c => c.name.Contains("Secret door"));

            // Heavy Object Impact (잠금 해제 소리)
            var impactClip = freeClips.FirstOrDefault(c => c.name.Contains("Heavy Object Impact"));

            // 문 두드리는 소리 선택 (여러 개 중 하나)
            AudioClip knockClip = null;
            if (woodenClips.Length > 0)
            {
                // Wooden_Thin이 가장 노크 소리답다
                knockClip = woodenClips.FirstOrDefault(c => c.name.Contains("Wooden_Thin_1"));
                if (knockClip == null)
                {
                    knockClip = woodenClips[0];
                }
            }

            // 씬의 모든 Door 찾기
            var doors = FindObjectsOfType<Door>();
            int assignedCount = 0;

            foreach (var door in doors)
            {
                bool modified = false;

                // 문 두드리는 소리
                if (door.knockSound == null && knockClip != null)
                {
                    door.knockSound = knockClip;
                    modified = true;
                }

                // 문 열림 소리
                if (door.openSound == null && secretDoorClip != null)
                {
                    door.openSound = secretDoorClip;
                    modified = true;
                }

                // 문 닫힘 소리 (같은 소리 사용)
                if (door.closeSound == null && secretDoorClip != null)
                {
                    door.closeSound = secretDoorClip;
                    modified = true;
                }

                // 잠금 해제 소리
                if (door.unlockSound == null && impactClip != null)
                {
                    door.unlockSound = impactClip;
                    modified = true;
                }

                // 잠금 소리 (knockSound와 동일하게)
                if (door.lockedSound == null && knockClip != null)
                {
                    door.lockedSound = knockClip;
                    modified = true;
                }

                if (modified)
                {
                    EditorUtility.SetDirty(door);
                    assignedCount++;
                    Debug.Log($"[SceneSetupTool] {door.gameObject.name}에 문 사운드 할당");
                }
            }

            Debug.Log($"[SceneSetupTool] === {assignedCount}개 Door에 사운드 할당 완료 ===");
            Debug.Log($"  knockSound: {(knockClip != null ? knockClip.name : "없음")}");
            Debug.Log($"  openSound: {(secretDoorClip != null ? secretDoorClip.name : "없음")}");
            Debug.Log($"  unlockSound: {(impactClip != null ? impactClip.name : "없음")}");
        }

        private static AudioClip[] LoadAudioClipsFromPath(string path)
        {
            if (!System.IO.Directory.Exists(path)) return new AudioClip[0];

            var clips = new System.Collections.Generic.List<AudioClip>();
            string[] extensions = new[] { "*.wav", "*.mp3", "*.ogg", "*.aif" };

            foreach (var ext in extensions)
            {
                string[] files = System.IO.Directory.GetFiles(path, ext);
                foreach (string file in files)
                {
                    string assetPath = file.Replace("\\", "/");
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip != null)
                    {
                        clips.Add(clip);
                    }
                }
            }

            return clips.ToArray();
        }

        #endregion

        #region Build Settings

        private void AddScenesToBuildSettings()
        {
            string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            string sampleScenePath = "Assets/Scenes/SampleScene.unity";

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            bool changed = false;

            // MainMenu가 첫 번째가 아니면 추가/이동
            bool hasMainMenu = false;
            int mainMenuIndex = -1;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == mainMenuPath)
                {
                    hasMainMenu = true;
                    mainMenuIndex = i;
                    break;
                }
            }

            if (!hasMainMenu && System.IO.File.Exists(mainMenuPath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(mainMenuPath, true));
                changed = true;
                Debug.Log("[SceneSetupTool] MainMenu 씬을 Build Settings에 추가했습니다.");
            }
            else if (mainMenuIndex > 0)
            {
                var mainMenuScene = scenes[mainMenuIndex];
                scenes.RemoveAt(mainMenuIndex);
                scenes.Insert(0, mainMenuScene);
                changed = true;
                Debug.Log("[SceneSetupTool] MainMenu 씬을 첫 번째로 이동했습니다.");
            }

            // SampleScene 확인
            bool hasSampleScene = false;
            foreach (var scene in scenes)
            {
                if (scene.path == sampleScenePath)
                {
                    hasSampleScene = true;
                    break;
                }
            }

            if (!hasSampleScene && System.IO.File.Exists(sampleScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(sampleScenePath, true));
                changed = true;
                Debug.Log("[SceneSetupTool] SampleScene을 Build Settings에 추가했습니다.");
            }

            if (changed)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[SceneSetupTool] Build Settings 업데이트 완료!");
            }
            else
            {
                Debug.Log("[SceneSetupTool] Build Settings가 이미 올바르게 설정되어 있습니다.");
            }
        }

        #endregion
    }
}
