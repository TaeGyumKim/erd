using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using HorrorGame;

namespace HorrorGame.Editor
{
    /// <summary>
    /// EndingSequence (World Space UI) 자동 생성 도구
    /// </summary>
    public class EndingSequenceSetup : EditorWindow
    {
        [MenuItem("Horror Game/Create Ending Sequence (World Space)")]
        public static void CreateEndingSequenceUI()
        {
            // 기존 EndingSequence 제거
            EndingSequence existing = FindObjectOfType<EndingSequence>();
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }

            // 기존 EndingWorldSpaceCanvas 제거
            var existingCanvas = GameObject.Find("EndingWorldSpaceCanvas");
            if (existingCanvas != null)
            {
                DestroyImmediate(existingCanvas);
            }

            // EndingSequence 오브젝트 생성 (빈 오브젝트)
            GameObject endingSequenceObj = new GameObject("EndingSequence");
            EndingSequence endingSequence = endingSequenceObj.AddComponent<EndingSequence>();

            // World Space Canvas 생성
            GameObject canvasObj = new GameObject("EndingWorldSpaceCanvas");
            Canvas worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
            worldSpaceCanvas.renderMode = RenderMode.WorldSpace;

            // Canvas Scaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            // Graphic Raycaster
            canvasObj.AddComponent<GraphicRaycaster>();

            // RectTransform 설정
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(800, 600);
            canvasRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // 초기 위치 (플레이어 앞에 배치될 것이므로 원점에 둠)
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

            // 승리 텍스트 (화면 상단)
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

            // 크레딧 텍스트 (화면 중앙)
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

            // 스킵 안내 텍스트 (화면 하단)
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
            GameObject mainMenuButtonObj = CreateButton(buttonContainer.transform, "MainMenuButton", "메인 메뉴");
            Button mainMenuButton = mainMenuButtonObj.GetComponent<Button>();
            mainMenuButtonObj.SetActive(false);

            // 다시 시작 버튼
            GameObject restartButtonObj = CreateButton(buttonContainer.transform, "RestartButton", "다시 시작");
            Button restartButton = restartButtonObj.GetComponent<Button>();
            restartButtonObj.SetActive(false);

            // EndingSequence 컴포넌트에 참조 연결
            endingSequence.worldSpaceCanvas = worldSpaceCanvas;
            endingSequence.victoryText = victoryText;
            endingSequence.creditsText = creditsText;
            endingSequence.skipHintText = skipHintText;
            endingSequence.mainMenuButton = mainMenuButton;
            endingSequence.restartButton = restartButton;

            // 초기 상태 설정
            canvasObj.SetActive(false);

            // Selection
            Selection.activeGameObject = endingSequenceObj;

            Debug.Log("[EndingSequenceSetup] World Space EndingSequence UI 생성 완료!");
            EditorUtility.DisplayDialog("EndingSequence UI 생성 완료",
                "World Space 엔딩 시퀀스 UI가 생성되었습니다.\n\n" +
                "- 플레이어 앞에 3D 공간에 표시됨\n" +
                "- 탈출 성공 시 스토리 텍스트 순차 표시\n" +
                "- 킬러 AI 자동 중지\n" +
                "- 아무 키나 눌러 스킵 가능\n\n" +
                "EscapeZone에서 Victory() 호출 시 자동으로 재생됩니다.",
                "확인");
        }

        private static GameObject CreateButton(Transform parent, string name, string text)
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
            colors.selectedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
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
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;

            return buttonObj;
        }
    }
}
