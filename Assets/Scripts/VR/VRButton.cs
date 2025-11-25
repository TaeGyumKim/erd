using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// VR에서 누를 수 있는 버튼
    /// 물리적으로 손으로 눌러서 작동하는 버튼
    ///
    /// 사용법:
    /// 1. 버튼 오브젝트에 이 스크립트 추가
    /// 2. Collider를 Trigger로 설정
    /// 3. OnButtonPressed 이벤트에 원하는 동작 연결
    /// </summary>
    public class VRButton : XRSimpleInteractable
    {
        [Header("Button Settings")]
        [Tooltip("버튼이 눌리는 거리")]
        public float pressDepth = 0.02f;

        [Tooltip("버튼 복귀 속도")]
        public float returnSpeed = 5f;

        [Header("Visual Feedback")]
        [Tooltip("눌렸을 때 색상")]
        public Color pressedColor = Color.green;

        [Tooltip("기본 색상")]
        public Color normalColor = Color.white;

        [Header("Events")]
        [Tooltip("버튼이 눌렸을 때 실행될 이벤트")]
        public UnityEvent OnButtonPressed;

        [Tooltip("버튼에서 손을 뗐을 때 실행될 이벤트")]
        public UnityEvent OnButtonReleased;

        private Vector3 initialLocalPosition;
        private bool isPressed = false;
        private Renderer buttonRenderer;
        private Material buttonMaterial;

        protected override void Awake()
        {
            base.Awake();

            initialLocalPosition = transform.localPosition;
            buttonRenderer = GetComponent<Renderer>();

            if (buttonRenderer != null)
            {
                buttonMaterial = buttonRenderer.material;
                buttonMaterial.color = normalColor;
            }
        }

        private void Update()
        {
            // 버튼이 눌리지 않은 상태면 원래 위치로 복귀
            if (!isPressed)
            {
                transform.localPosition = Vector3.Lerp(
                    transform.localPosition,
                    initialLocalPosition,
                    Time.deltaTime * returnSpeed
                );
            }
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);
            PressButton();
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            ReleaseButton();
        }

        private void PressButton()
        {
            if (isPressed) return;

            isPressed = true;

            // 버튼 위치 변경
            Vector3 pressedPosition = initialLocalPosition;
            pressedPosition.y -= pressDepth;
            transform.localPosition = pressedPosition;

            // 색상 변경
            if (buttonMaterial != null)
            {
                buttonMaterial.color = pressedColor;
            }

            // 이벤트 실행
            OnButtonPressed?.Invoke();
            Debug.Log($"[VRButton] {gameObject.name} 눌림");
        }

        private void ReleaseButton()
        {
            if (!isPressed) return;

            isPressed = false;

            // 색상 복귀
            if (buttonMaterial != null)
            {
                buttonMaterial.color = normalColor;
            }

            // 이벤트 실행
            OnButtonReleased?.Invoke();
            Debug.Log($"[VRButton] {gameObject.name} 해제됨");
        }
    }
}
