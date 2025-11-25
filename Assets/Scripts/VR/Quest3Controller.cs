using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace VRGame
{
    /// <summary>
    /// Meta Quest 3 컨트롤러 전용 기능
    /// 진동(햅틱) 피드백, 버튼 입력 등을 처리
    ///
    /// 사용법:
    /// 1. XR Origin의 컨트롤러 오브젝트에 추가
    /// 2. controllerNode를 LeftHand 또는 RightHand로 설정
    /// </summary>
    public class Quest3Controller : MonoBehaviour
    {
        [Header("Controller Settings")]
        [Tooltip("왼손/오른손 선택")]
        public XRNode controllerNode = XRNode.RightHand;

        [Header("Haptic Settings")]
        [Tooltip("기본 진동 강도 (0~1)")]
        [Range(0f, 1f)]
        public float defaultHapticAmplitude = 0.5f;

        [Tooltip("기본 진동 지속시간 (초)")]
        public float defaultHapticDuration = 0.1f;

        private InputDevice controller;
        private bool isControllerValid = false;

        // 버튼 상태
        public bool IsTriggerPressed { get; private set; }
        public bool IsGripPressed { get; private set; }
        public bool IsPrimaryButtonPressed { get; private set; }  // A 또는 X
        public bool IsSecondaryButtonPressed { get; private set; } // B 또는 Y
        public bool IsThumbstickPressed { get; private set; }

        // 아날로그 값
        public float TriggerValue { get; private set; }
        public float GripValue { get; private set; }
        public Vector2 ThumbstickValue { get; private set; }

        private void Start()
        {
            InitializeController();
        }

        private void InitializeController()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(controllerNode, devices);

            if (devices.Count > 0)
            {
                controller = devices[0];
                isControllerValid = controller.isValid;
                Debug.Log($"[Quest3Controller] {controllerNode} 컨트롤러 연결됨");
            }
        }

        private void Update()
        {
            if (!isControllerValid || !controller.isValid)
            {
                InitializeController();
                return;
            }

            UpdateInputs();
        }

        private void UpdateInputs()
        {
            // 트리거
            controller.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerPressed);
            controller.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
            IsTriggerPressed = triggerPressed;
            TriggerValue = triggerValue;

            // 그립
            controller.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed);
            controller.TryGetFeatureValue(CommonUsages.grip, out float gripValue);
            IsGripPressed = gripPressed;
            GripValue = gripValue;

            // 버튼
            controller.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed);
            controller.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryPressed);
            IsPrimaryButtonPressed = primaryPressed;
            IsSecondaryButtonPressed = secondaryPressed;

            // 썸스틱
            controller.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool thumbstickPressed);
            controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstickValue);
            IsThumbstickPressed = thumbstickPressed;
            ThumbstickValue = thumbstickValue;
        }

        /// <summary>
        /// 컨트롤러 진동 (기본 설정)
        /// </summary>
        public void Vibrate()
        {
            Vibrate(defaultHapticAmplitude, defaultHapticDuration);
        }

        /// <summary>
        /// 컨트롤러 진동 (커스텀 설정)
        /// </summary>
        /// <param name="amplitude">진동 강도 (0~1)</param>
        /// <param name="duration">지속시간 (초)</param>
        public void Vibrate(float amplitude, float duration)
        {
            if (!isControllerValid) return;

            HapticCapabilities capabilities;
            if (controller.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                uint channel = 0;
                controller.SendHapticImpulse(channel, amplitude, duration);
            }
        }

        /// <summary>
        /// 짧은 진동 (클릭 피드백용)
        /// </summary>
        public void VibrateClick()
        {
            Vibrate(0.3f, 0.05f);
        }

        /// <summary>
        /// 강한 진동 (충돌/임팩트용)
        /// </summary>
        public void VibrateImpact()
        {
            Vibrate(0.8f, 0.15f);
        }

        /// <summary>
        /// 부드러운 진동 (호버 피드백용)
        /// </summary>
        public void VibrateHover()
        {
            Vibrate(0.1f, 0.02f);
        }
    }
}
