using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HorrorGame
{
    /// <summary>
    /// VR HUD (손목 또는 월드 스페이스)
    /// 스태미나, 배터리, 열쇠 등 표시
    ///
    /// 사용법:
    /// 1. 왼쪽 손목에 Canvas 추가 (World Space)
    /// 2. 이 스크립트 추가
    /// 3. UI 요소들 연결
    /// </summary>
    public class VRHUD : MonoBehaviour
    {
        public static VRHUD Instance { get; private set; }

        [Header("References")]
        [Tooltip("플레이어 카메라 (HUD가 따라다님)")]
        public Transform playerCamera;

        [Tooltip("HUD가 손목에 부착")]
        public bool attachToWrist = true;

        [Tooltip("왼쪽 손 Transform")]
        public Transform leftHand;

        [Header("Stamina UI")]
        [Tooltip("스태미나 바")]
        public Image staminaBar;

        [Tooltip("스태미나 배경")]
        public Image staminaBackground;

        [Tooltip("스태미나 텍스트")]
        public TextMeshProUGUI staminaText;

        [Header("Battery UI")]
        [Tooltip("배터리 바")]
        public Image batteryBar;

        [Tooltip("배터리 아이콘")]
        public Image batteryIcon;

        [Tooltip("배터리 텍스트")]
        public TextMeshProUGUI batteryText;

        [Header("Key UI")]
        [Tooltip("열쇠 아이콘")]
        public Image keyIcon;

        [Tooltip("열쇠 카운트 텍스트")]
        public TextMeshProUGUI keyCountText;

        [Header("Status UI")]
        [Tooltip("상태 텍스트 (숨는 중 등)")]
        public TextMeshProUGUI statusText;

        [Tooltip("경고 텍스트")]
        public TextMeshProUGUI warningText;

        [Header("Timer UI (Optional)")]
        [Tooltip("타이머 텍스트")]
        public TextMeshProUGUI timerText;

        [Header("Settings")]
        [Tooltip("HUD 가시성")]
        public bool isVisible = true;

        [Tooltip("낮은 값 경고 색상")]
        public Color warningColor = Color.red;

        [Tooltip("정상 색상")]
        public Color normalColor = Color.white;

        [Tooltip("가득 찬 색상")]
        public Color fullColor = Color.green;

        [Tooltip("배터리 낮음 경고 기준")]
        [Range(0f, 0.5f)]
        public float lowBatteryThreshold = 0.2f;

        [Tooltip("스태미나 낮음 경고 기준")]
        [Range(0f, 0.5f)]
        public float lowStaminaThreshold = 0.3f;

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private float warningTimer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 경고 텍스트 초기화
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            // 플레이어 카메라 자동 찾기
            if (playerCamera == null && VRPlayer.Instance != null)
            {
                playerCamera = Camera.main?.transform ?? VRPlayer.Instance.transform;
            }

            // 초기 상태 업데이트
            UpdateAllUI();
        }

        private void Update()
        {
            UpdatePosition();
            UpdateAllUI();
            UpdateWarnings();
        }

        private void UpdatePosition()
        {
            if (attachToWrist && leftHand != null)
            {
                // 손목에 부착
                transform.position = leftHand.position;
                transform.rotation = leftHand.rotation;
            }
            else if (playerCamera != null)
            {
                // 카메라 앞에 고정
                transform.position = playerCamera.position + playerCamera.forward * 0.5f;
                transform.LookAt(playerCamera);
                transform.Rotate(0, 180, 0);
            }
        }

        private void UpdateAllUI()
        {
            UpdateStaminaUI();
            UpdateBatteryUI();
            UpdateKeyUI();
            UpdateStatusUI();
            UpdateTimerUI();
        }

        private void UpdateStaminaUI()
        {
            if (VRPlayer.Instance == null) return;

            float staminaPercent = VRPlayer.Instance.currentStamina / VRPlayer.Instance.maxStamina;

            if (staminaBar != null)
            {
                staminaBar.fillAmount = staminaPercent;

                // 색상 변경
                if (staminaPercent <= lowStaminaThreshold)
                {
                    staminaBar.color = warningColor;
                }
                else if (staminaPercent >= 0.9f)
                {
                    staminaBar.color = fullColor;
                }
                else
                {
                    staminaBar.color = normalColor;
                }
            }

            if (staminaText != null)
            {
                staminaText.text = $"{staminaPercent * 100:F0}%";
            }
        }

        private void UpdateBatteryUI()
        {
            // 손전등 찾기
            VRFlashlight flashlight = null;
            if (VRPlayer.Instance != null)
            {
                flashlight = VRPlayer.Instance.GetComponentInChildren<VRFlashlight>();
            }

            if (flashlight == null) return;

            float batteryPercent = flashlight.BatteryPercent;

            if (batteryBar != null)
            {
                batteryBar.fillAmount = batteryPercent;

                // 색상 변경
                if (batteryPercent <= lowBatteryThreshold)
                {
                    batteryBar.color = warningColor;
                }
                else if (batteryPercent >= 0.9f)
                {
                    batteryBar.color = fullColor;
                }
                else
                {
                    batteryBar.color = normalColor;
                }
            }

            if (batteryText != null)
            {
                batteryText.text = $"{batteryPercent * 100:F0}%";
            }

            if (batteryIcon != null)
            {
                batteryIcon.color = batteryPercent <= lowBatteryThreshold ? warningColor : normalColor;
            }
        }

        private void UpdateKeyUI()
        {
            if (PlayerInventory.Instance == null) return;

            int keyCount = PlayerInventory.Instance.GetKeyCount();
            int requiredKeys = HorrorGameManager.Instance != null ?
                HorrorGameManager.Instance.requiredKeysToEscape : 0;

            if (keyCountText != null)
            {
                keyCountText.text = $"{keyCount}/{requiredKeys}";

                // 모든 열쇠 수집 시 색상 변경
                if (keyCount >= requiredKeys && requiredKeys > 0)
                {
                    keyCountText.color = fullColor;
                }
                else
                {
                    keyCountText.color = normalColor;
                }
            }

            if (keyIcon != null)
            {
                keyIcon.color = keyCount >= requiredKeys ? fullColor : normalColor;
            }
        }

        private void UpdateStatusUI()
        {
            if (statusText == null || VRPlayer.Instance == null) return;

            // 상태 우선순위: Caught > Hiding > Sprinting > Normal
            if (VRPlayer.Instance.currentState == VRPlayer.PlayerState.Caught)
            {
                statusText.text = "잡혔다!";
                statusText.color = Color.red;
            }
            else if (VRPlayer.Instance.currentState == VRPlayer.PlayerState.Hiding)
            {
                statusText.text = "숨는 중...";
                statusText.color = Color.cyan;
            }
            else if (VRPlayer.Instance.IsSprinting)
            {
                statusText.text = "달리는 중";
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.text = "";
            }
        }

        private void UpdateTimerUI()
        {
            if (timerText == null || HorrorGameManager.Instance == null) return;

            if (HorrorGameManager.Instance.useTimeLimit)
            {
                float time = HorrorGameManager.Instance.remainingTime;
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);

                timerText.text = $"{minutes:00}:{seconds:00}";

                // 시간 부족 경고
                if (time <= 60)
                {
                    timerText.color = warningColor;
                }
                else
                {
                    timerText.color = normalColor;
                }
            }
            else
            {
                timerText.gameObject.SetActive(false);
            }
        }

        private void UpdateWarnings()
        {
            if (warningText == null) return;

            // 경고 타이머
            if (warningTimer > 0)
            {
                warningTimer -= Time.deltaTime;
                if (warningTimer <= 0)
                {
                    warningText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 경고 메시지 표시
        /// </summary>
        public void ShowWarning(string message, float duration = 3f)
        {
            if (warningText == null) return;

            warningText.text = message;
            warningText.gameObject.SetActive(true);
            warningTimer = duration;
        }

        /// <summary>
        /// HUD 표시/숨기기
        /// </summary>
        public void SetVisible(bool visible)
        {
            isVisible = visible;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1 : 0;
            }
        }

        /// <summary>
        /// HUD 토글
        /// </summary>
        public void ToggleVisibility()
        {
            SetVisible(!isVisible);
        }

        /// <summary>
        /// 상태 메시지 표시 (체크포인트, 목표 등)
        /// </summary>
        public void ShowStatus(string message, float duration = 2f)
        {
            ShowWarning(message, duration);
        }
    }
}
