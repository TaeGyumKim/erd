using UnityEngine;
using UnityEngine.Events;

namespace HorrorGame
{
    /// <summary>
    /// VR 플레이어 메인 컨트롤러
    /// 스태미나, 상태 관리, 소리 발생 등을 처리
    /// </summary>
    public class VRPlayer : MonoBehaviour
    {
        public static VRPlayer Instance { get; private set; }

        [Header("Player Stats")]
        [Tooltip("최대 스태미나")]
        public float maxStamina = 100f;

        [Tooltip("현재 스태미나")]
        public float currentStamina;

        [Tooltip("스태미나 회복 속도 (초당)")]
        public float staminaRegenRate = 10f;

        [Tooltip("달리기 스태미나 소모 (초당)")]
        public float sprintStaminaCost = 20f;

        [Header("Movement")]
        [Tooltip("걷기 속도")]
        public float walkSpeed = 2f;

        [Tooltip("달리기 속도")]
        public float sprintSpeed = 4f;

        [Tooltip("웅크리기 속도")]
        public float crouchSpeed = 1f;

        [Header("Noise System")]
        [Tooltip("걷기 소음 반경")]
        public float walkNoiseRadius = 3f;

        [Tooltip("달리기 소음 반경")]
        public float sprintNoiseRadius = 8f;

        [Tooltip("소음 발생 간격")]
        public float noiseInterval = 0.5f;

        [Header("State")]
        public PlayerState currentState = PlayerState.Normal;
        public bool IsHiding { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }

        [Header("Events")]
        public UnityEvent OnPlayerCaught;
        public UnityEvent OnPlayerEscaped;
        public UnityEvent<float> OnStaminaChanged;
        public UnityEvent OnStaminaDepleted;

        public enum PlayerState
        {
            Normal,
            Hiding,
            Caught,
            Escaped
        }

        private float lastNoiseTime;
        private float currentSpeed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            currentStamina = maxStamina;
            currentSpeed = walkSpeed;
        }

        private void Update()
        {
            if (currentState == PlayerState.Caught || currentState == PlayerState.Escaped)
                return;

            UpdateStamina();
            UpdateNoise();
        }

        private void UpdateStamina()
        {
            if (IsSprinting && !IsHiding)
            {
                currentStamina -= sprintStaminaCost * Time.deltaTime;
                if (currentStamina <= 0)
                {
                    currentStamina = 0;
                    StopSprinting();
                    OnStaminaDepleted?.Invoke();
                }
            }
            else if (!IsSprinting)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }

            OnStaminaChanged?.Invoke(currentStamina / maxStamina);
        }

        private void UpdateNoise()
        {
            if (IsHiding || IsCrouching) return;

            if (Time.time - lastNoiseTime >= noiseInterval)
            {
                float noiseRadius = IsSprinting ? sprintNoiseRadius : walkNoiseRadius;
                MakeNoise(noiseRadius);
                lastNoiseTime = Time.time;
            }
        }

        /// <summary>
        /// 소음 발생 - 적 AI가 감지할 수 있음
        /// </summary>
        public void MakeNoise(float radius)
        {
            // 주변의 모든 Enemy에게 알림
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (var col in colliders)
            {
                var enemy = col.GetComponent<KillerAI>();
                if (enemy != null)
                {
                    enemy.HearNoise(transform.position);
                }
            }
        }

        /// <summary>
        /// 달리기 시작
        /// </summary>
        public void StartSprinting()
        {
            if (currentStamina > 0 && !IsHiding)
            {
                IsSprinting = true;
                currentSpeed = sprintSpeed;
            }
        }

        /// <summary>
        /// 달리기 중지
        /// </summary>
        public void StopSprinting()
        {
            IsSprinting = false;
            currentSpeed = IsCrouching ? crouchSpeed : walkSpeed;
        }

        /// <summary>
        /// 웅크리기 토글
        /// </summary>
        public void ToggleCrouch()
        {
            IsCrouching = !IsCrouching;
            currentSpeed = IsCrouching ? crouchSpeed : walkSpeed;
        }

        /// <summary>
        /// 숨기 시작
        /// </summary>
        public void StartHiding(Transform hideSpot)
        {
            if (currentState == PlayerState.Normal)
            {
                IsHiding = true;
                currentState = PlayerState.Hiding;
                IsSprinting = false;

                // 플레이어를 숨는 위치로 이동
                transform.position = hideSpot.position;
                transform.rotation = hideSpot.rotation;

                Debug.Log("[VRPlayer] 숨기 시작");
            }
        }

        /// <summary>
        /// 숨기 종료
        /// </summary>
        public void StopHiding()
        {
            if (currentState == PlayerState.Hiding)
            {
                IsHiding = false;
                currentState = PlayerState.Normal;
                Debug.Log("[VRPlayer] 숨기 종료");
            }
        }

        /// <summary>
        /// 플레이어가 잡힘 (게임 오버)
        /// </summary>
        public void GetCaught()
        {
            if (currentState == PlayerState.Caught) return;

            currentState = PlayerState.Caught;
            IsHiding = false;
            IsSprinting = false;

            Debug.Log("[VRPlayer] 플레이어가 잡혔습니다!");
            OnPlayerCaught?.Invoke();
        }

        /// <summary>
        /// 플레이어가 탈출 성공
        /// </summary>
        public void Escape()
        {
            if (currentState == PlayerState.Escaped) return;

            currentState = PlayerState.Escaped;
            Debug.Log("[VRPlayer] 탈출 성공!");
            OnPlayerEscaped?.Invoke();
        }

        /// <summary>
        /// 현재 이동 속도 반환
        /// </summary>
        public float GetCurrentSpeed()
        {
            return currentSpeed;
        }
    }
}
