using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace HorrorGame
{
    /// <summary>
    /// VR 비밀번호 키패드 UI 생성 도구
    /// World Space Canvas로 VR에서 사용 가능한 키패드 생성
    /// </summary>
    public class PasswordKeypadSetup : EditorWindow
    {
        [MenuItem("Horror Game/VR 비밀번호 키패드 생성")]
        public static void CreateKeypadDirectly()
        {
            CreateVRKeypad();
        }

        [MenuItem("Horror Game/VR 비밀번호 키패드 설정 창")]
        public static void ShowWindow()
        {
            GetWindow<PasswordKeypadSetup>("VR 비밀번호 키패드");
        }

        private void OnGUI()
        {
            GUILayout.Label("VR 비밀번호 키패드 생성", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "VR용 World Space Canvas 비밀번호 키패드를 생성합니다.\n" +
                "- XR Ray Interactor로 버튼 클릭 가능\n" +
                "- 플레이어 앞에 자동 위치\n" +
                "- 레이아웃: 3열 (7-8-9, 4-5-6, 1-2-3, ⌫-0-Enter)",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("VR 키패드 생성", GUILayout.Height(40)))
            {
                CreateVRKeypad();
            }
        }

        /// <summary>
        /// VR 키패드 생성
        /// </summary>
        private static void CreateVRKeypad()
        {
            // 기존 키패드 확인
            var existing = FindObjectOfType<VRPasswordKeypad>();
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("경고", "이미 VRPasswordKeypad가 씬에 있습니다. 새로 생성하시겠습니까?", "예", "아니오"))
                {
                    Selection.activeGameObject = existing.gameObject;
                    return;
                }
                DestroyImmediate(existing.gameObject);
            }

            // 루트 오브젝트 생성
            var keypadRoot = new GameObject("VRPasswordKeypad");
            Undo.RegisterCreatedObjectUndo(keypadRoot, "Create VR Password Keypad");

            var vrKeypad = keypadRoot.AddComponent<VRPasswordKeypad>();
            var audioSource = keypadRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 5f;

            // World Space Canvas 생성
            var canvasGO = new GameObject("KeypadCanvas");
            canvasGO.transform.SetParent(keypadRoot.transform, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 10;

            var raycaster = canvasGO.AddComponent<GraphicRaycaster>();

            // TrackedDeviceGraphicRaycaster 추가 (XR UI용)
            var trackedRaycaster = canvasGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();

            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(400, 500);
            canvasRect.localScale = Vector3.one * 0.002f; // World Space 스케일 조정

            vrKeypad.keypadCanvas = canvas;

            // 키패드 패널 생성
            var keypadPanel = CreatePanel(canvasGO.transform, "KeypadPanel", new Color(0.1f, 0.1f, 0.15f, 0.98f));
            var panelRect = keypadPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            vrKeypad.keypadPanel = keypadPanel;

            // 제목 텍스트
            var titleText = CreateText(keypadPanel.transform, "TitleText", "비밀번호를 입력하세요", 28);
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.sizeDelta = new Vector2(0, 50);
            titleRect.anchoredPosition = new Vector2(0, -10);
            vrKeypad.titleText = titleText.GetComponent<TextMeshProUGUI>();

            // 닫기 버튼 (우측 상단)
            var closeBtn = CreateButton(keypadPanel.transform, "CloseButton", "X", new Vector2(50, 50));
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 1);
            closeBtnRect.anchorMax = new Vector2(1, 1);
            closeBtnRect.pivot = new Vector2(1, 1);
            closeBtnRect.anchoredPosition = new Vector2(-10, -10);
            closeBtn.GetComponent<Image>().color = new Color(0.7f, 0.2f, 0.2f, 1f);
            vrKeypad.closeButton = closeBtn.GetComponent<Button>();

            // 비밀번호 디스플레이
            var displayPanel = CreatePanel(keypadPanel.transform, "DisplayPanel", new Color(0.05f, 0.05f, 0.08f, 1f));
            var displayRect = displayPanel.GetComponent<RectTransform>();
            displayRect.anchorMin = new Vector2(0.5f, 1);
            displayRect.anchorMax = new Vector2(0.5f, 1);
            displayRect.pivot = new Vector2(0.5f, 1);
            displayRect.sizeDelta = new Vector2(350, 60);
            displayRect.anchoredPosition = new Vector2(0, -70);

            var displayText = CreateText(displayPanel.transform, "DisplayText", "○ ○ ○ ○", 36);
            var displayTextRect = displayText.GetComponent<RectTransform>();
            displayTextRect.anchorMin = Vector2.zero;
            displayTextRect.anchorMax = Vector2.one;
            displayTextRect.offsetMin = Vector2.zero;
            displayTextRect.offsetMax = Vector2.zero;
            vrKeypad.passwordDisplayText = displayText.GetComponent<TextMeshProUGUI>();

            // 숫자 버튼 그리드
            var gridPanel = new GameObject("NumberGrid");
            gridPanel.transform.SetParent(keypadPanel.transform, false);
            var gridRect = gridPanel.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.sizeDelta = new Vector2(350, 280);
            gridRect.anchoredPosition = new Vector2(0, -30);

            var gridLayout = gridPanel.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(100, 60);
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            // 버튼 배열 초기화
            vrKeypad.numberButtons = new Button[10];

            // 숫자 버튼 생성 (7-8-9, 4-5-6, 1-2-3 순서)
            int[] buttonOrder = { 7, 8, 9, 4, 5, 6, 1, 2, 3 };

            foreach (int num in buttonOrder)
            {
                var btn = CreateButton(gridPanel.transform, $"Button_{num}", num.ToString(), new Vector2(100, 60));
                btn.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);
                vrKeypad.numberButtons[num] = btn.GetComponent<Button>();
            }

            // 마지막 줄: ⌫, 0, Enter
            var backspaceBtn = CreateButton(gridPanel.transform, "BackspaceButton", "⌫", new Vector2(100, 60));
            backspaceBtn.GetComponent<Image>().color = new Color(0.5f, 0.3f, 0.3f, 1f);
            vrKeypad.backspaceButton = backspaceBtn.GetComponent<Button>();

            var zeroBtn = CreateButton(gridPanel.transform, "Button_0", "0", new Vector2(100, 60));
            zeroBtn.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);
            vrKeypad.numberButtons[0] = zeroBtn.GetComponent<Button>();

            var enterBtn = CreateButton(gridPanel.transform, "EnterButton", "Enter", new Vector2(100, 60));
            enterBtn.GetComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f, 1f);
            var enterText = enterBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (enterText != null) enterText.fontSize = 22;
            vrKeypad.enterButton = enterBtn.GetComponent<Button>();

            // Clear 버튼 (하단)
            var clearBtn = CreateButton(keypadPanel.transform, "ClearButton", "Clear", new Vector2(350, 45));
            var clearRect = clearBtn.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0.5f, 0);
            clearRect.anchorMax = new Vector2(0.5f, 0);
            clearRect.pivot = new Vector2(0.5f, 0);
            clearRect.anchoredPosition = new Vector2(0, 15);
            clearBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.3f, 1f);
            vrKeypad.clearButton = clearBtn.GetComponent<Button>();

            // 초기 위치 설정
            keypadRoot.transform.position = new Vector3(0, 1.5f, 2f);

            Selection.activeGameObject = keypadRoot;
            EditorUtility.SetDirty(keypadRoot);

            Debug.Log("[PasswordKeypadSetup] VR 비밀번호 키패드 생성 완료!");
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            panel.AddComponent<RectTransform>();
            var image = panel.AddComponent<Image>();
            image.color = color;

            return panel;
        }

        private static GameObject CreateButton(Transform parent, string name, string text, Vector2 size)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            var rect = btnGO.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            var image = btnGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = image;

            // 버튼 색상 설정
            var colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.45f, 0.45f, 0.5f, 1f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.25f, 1f);
            colors.selectedColor = new Color(0.35f, 0.35f, 0.4f, 1f);
            button.colors = colors;

            // 텍스트
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnGO;
        }

        private static GameObject CreateText(Transform parent, string name, string text, int fontSize)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);

            textGO.AddComponent<RectTransform>();

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return textGO;
        }
    }
}
