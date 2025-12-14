using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using HorrorGame;

namespace HorrorGame.Editor
{
    /// <summary>
    /// GameOverUI 자동 생성 도구
    /// </summary>
    public class GameOverUISetup : EditorWindow
    {
        [MenuItem("Horror Game/Create Game Over UI")]
        public static void CreateGameOverUI()
        {
            // Canvas 찾기 또는 생성
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // 최상단에 표시
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // 기존 GameOverUI 제거
            GameOverUI existingUI = FindObjectOfType<GameOverUI>();
            if (existingUI != null)
            {
                DestroyImmediate(existingUI.gameObject);
            }

            // GameOverUI 루트 오브젝트 생성
            GameObject gameOverRoot = new GameObject("GameOverUI");
            gameOverRoot.transform.SetParent(canvas.transform, false);
            GameOverUI gameOverUI = gameOverRoot.AddComponent<GameOverUI>();

            // 게임오버 패널 (전체 화면 검정 배경)
            GameObject panel = new GameObject("GameOverPanel");
            panel.transform.SetParent(gameOverRoot.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.9f);

            // YOU DIED 텍스트
            GameObject deathTextObj = new GameObject("DeathText");
            deathTextObj.transform.SetParent(panel.transform, false);
            RectTransform deathTextRect = deathTextObj.AddComponent<RectTransform>();
            deathTextRect.anchorMin = new Vector2(0.5f, 0.6f);
            deathTextRect.anchorMax = new Vector2(0.5f, 0.6f);
            deathTextRect.sizeDelta = new Vector2(800, 150);
            deathTextRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI deathText = deathTextObj.AddComponent<TextMeshProUGUI>();
            deathText.text = "YOU DIED";
            deathText.fontSize = 100;
            deathText.fontStyle = FontStyles.Bold;
            deathText.color = new Color(0.8f, 0.1f, 0.1f, 1f); // 진한 빨간색
            deathText.alignment = TextAlignmentOptions.Center;

            // 난이도 변경 텍스트 (YOU DIED 아래에 표시)
            GameObject difficultyTextObj = new GameObject("DifficultyChangeText");
            difficultyTextObj.transform.SetParent(panel.transform, false);
            RectTransform difficultyTextRect = difficultyTextObj.AddComponent<RectTransform>();
            difficultyTextRect.anchorMin = new Vector2(0.5f, 0.48f);
            difficultyTextRect.anchorMax = new Vector2(0.5f, 0.48f);
            difficultyTextRect.sizeDelta = new Vector2(600, 80);
            difficultyTextRect.anchoredPosition = Vector2.zero;

            TextMeshProUGUI difficultyText = difficultyTextObj.AddComponent<TextMeshProUGUI>();
            difficultyText.text = "난이도가 하향되었습니다\n어려움 → 보통";
            difficultyText.fontSize = 32;
            difficultyText.fontStyle = FontStyles.Normal;
            difficultyText.color = new Color(1f, 0.8f, 0.2f, 1f); // 노란색
            difficultyText.alignment = TextAlignmentOptions.Center;
            difficultyTextObj.SetActive(false); // 초기에는 숨김

            // 버튼 컨테이너
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(panel.transform, false);
            RectTransform buttonContainerRect = buttonContainer.AddComponent<RectTransform>();
            buttonContainerRect.anchorMin = new Vector2(0.5f, 0.3f);
            buttonContainerRect.anchorMax = new Vector2(0.5f, 0.3f);
            buttonContainerRect.sizeDelta = new Vector2(400, 150);
            buttonContainerRect.anchoredPosition = Vector2.zero;

            CanvasGroup buttonCanvasGroup = buttonContainer.AddComponent<CanvasGroup>();
            buttonCanvasGroup.alpha = 0f;

            // 재시작 버튼
            GameObject restartButtonObj = CreateButton(buttonContainer.transform, "RestartButton", "RESTART", new Vector2(0, 40));
            Button restartButton = restartButtonObj.GetComponent<Button>();

            // 메인메뉴 버튼
            GameObject mainMenuButtonObj = CreateButton(buttonContainer.transform, "MainMenuButton", "MAIN MENU", new Vector2(0, -40));
            Button mainMenuButton = mainMenuButtonObj.GetComponent<Button>();

            // GameOverUI 컴포넌트에 참조 연결
            gameOverUI.gameOverPanel = panel;
            gameOverUI.deathText = deathText;
            gameOverUI.difficultyChangeText = difficultyText;
            gameOverUI.restartButton = restartButton;
            gameOverUI.mainMenuButton = mainMenuButton;
            gameOverUI.buttonContainer = buttonCanvasGroup;

            // 초기 상태 설정
            panel.SetActive(false);

            // Selection
            Selection.activeGameObject = gameOverRoot;

            Debug.Log("[GameOverUISetup] GameOverUI 생성 완료!");
            EditorUtility.DisplayDialog("GameOverUI 생성 완료",
                "YOU DIED 스타일의 게임오버 UI가 생성되었습니다.\n\n" +
                "- 살인마에게 잡히면 자동으로 표시됩니다.\n" +
                "- 재시작 버튼: 현재 씬 재로드\n" +
                "- 메인메뉴 버튼: MainMenu 씬으로 이동",
                "확인");
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 position)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(250, 60);
            buttonRect.anchoredPosition = position;

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            colors.highlightedColor = new Color(0.4f, 0.1f, 0.1f, 0.9f);
            colors.pressedColor = new Color(0.6f, 0.1f, 0.1f, 1f);
            colors.selectedColor = new Color(0.3f, 0.1f, 0.1f, 0.9f);
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
            buttonText.fontSize = 28;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            return buttonObj;
        }
    }
}
