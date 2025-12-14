using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using HorrorGame;

namespace HorrorGame.Editor
{
    /// <summary>
    /// GamePopupUI 자동 설정 도구
    /// </summary>
    public class GamePopupUISetup : EditorWindow
    {
        [MenuItem("Horror Game/Setup GamePopupUI References")]
        public static void SetupGamePopupUI()
        {
            // GamePopupUI 찾기
            GamePopupUI popupUI = FindFirstObjectByType<GamePopupUI>();
            if (popupUI == null)
            {
                Debug.LogError("[GamePopupUISetup] GamePopupUI를 찾을 수 없습니다!");
                return;
            }

            // PopupPanel 찾기
            Transform popupPanel = popupUI.transform.Find("PopupPanel");
            if (popupPanel == null)
            {
                Debug.LogError("[GamePopupUISetup] PopupPanel을 찾을 수 없습니다!");
                return;
            }

            // 참조 설정
            popupUI.popupCanvas = popupUI.GetComponent<Canvas>();
            popupUI.popupPanel = popupPanel.gameObject;

            // TitleText 찾기
            Transform titleTextTransform = popupPanel.Find("TitleText");
            if (titleTextTransform != null)
            {
                popupUI.titleText = titleTextTransform.GetComponent<TextMeshProUGUI>();
            }

            // ContentText 찾기
            Transform contentTextTransform = popupPanel.Find("ContentText");
            if (contentTextTransform != null)
            {
                popupUI.contentText = contentTextTransform.GetComponent<TextMeshProUGUI>();
            }

            // CanvasGroup 추가 (페이드용)
            CanvasGroup canvasGroup = popupPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = popupPanel.gameObject.AddComponent<CanvasGroup>();
            }

            // PopupPanel 비활성화 (시작 시 숨김)
            popupPanel.gameObject.SetActive(false);

            // 변경사항 저장
            EditorUtility.SetDirty(popupUI);

            Debug.Log("[GamePopupUISetup] GamePopupUI 참조 설정 완료!");
            Debug.Log($"  - popupCanvas: {(popupUI.popupCanvas != null ? "설정됨" : "없음")}");
            Debug.Log($"  - popupPanel: {(popupUI.popupPanel != null ? "설정됨" : "없음")}");
            Debug.Log($"  - titleText: {(popupUI.titleText != null ? "설정됨" : "없음")}");
            Debug.Log($"  - contentText: {(popupUI.contentText != null ? "설정됨" : "없음")}");
        }

        [MenuItem("Horror Game/Create GamePopupUI")]
        public static void CreateGamePopupUI()
        {
            // 기존 GamePopupUI 제거
            GamePopupUI existingUI = FindFirstObjectByType<GamePopupUI>();
            if (existingUI != null)
            {
                DestroyImmediate(existingUI.gameObject);
            }

            // Canvas 생성
            GameObject canvasObj = new GameObject("GamePopupUI");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // 최상단에 표시

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
            GamePopupUI popupUI = canvasObj.AddComponent<GamePopupUI>();

            // PopupPanel 생성
            GameObject panel = new GameObject("PopupPanel");
            panel.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.85f);

            panel.AddComponent<CanvasGroup>();

            // TitleText 생성
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(panel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.7f);
            titleRect.anchorMax = new Vector2(0.9f, 0.85f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "제목";
            titleText.fontSize = 48;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // ContentText 생성
            GameObject contentObj = new GameObject("ContentText");
            contentObj.transform.SetParent(panel.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.2f);
            contentRect.anchorMax = new Vector2(0.9f, 0.65f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            TextMeshProUGUI contentText = contentObj.AddComponent<TextMeshProUGUI>();
            contentText.text = "내용";
            contentText.fontSize = 36;
            contentText.alignment = TextAlignmentOptions.Center;
            contentText.color = Color.white;
            contentText.richText = true;

            // 참조 설정
            popupUI.popupCanvas = canvas;
            popupUI.popupPanel = panel;
            popupUI.titleText = titleText;
            popupUI.contentText = contentText;

            // PopupPanel 비활성화
            panel.SetActive(false);

            // Selection 설정
            Selection.activeGameObject = canvasObj;

            Debug.Log("[GamePopupUISetup] GamePopupUI 생성 완료!");
        }
    }
}
