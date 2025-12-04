using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace HorrorGame
{
    /// <summary>
    /// PC에서 키보드/마우스로 조작하는 플레이어 컨트롤러
    /// VR 없이 테스트할 때 사용
    /// XR Interaction Toolkit과 통합하여 VR과 동일한 상호작용 지원
    /// 양손 컨트롤러를 마우스로 시뮬레이션
    /// </summary>
    public class PCPlayerController : MonoBehaviour
    {
        public static PCPlayerController Instance { get; private set; }

        [Header("이동 설정")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float crouchSpeed = 1.5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("마우스 설정")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;

        [Header("스태미나")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float sprintStaminaCost = 20f;

        [Header("소음 시스템")]
        [SerializeField] private float walkNoiseRadius = 3f;
        [SerializeField] private float sprintNoiseRadius = 8f;
        [SerializeField] private float noiseInterval = 0.5f;

        [Header("양손 컨트롤러 시뮬레이션")]
        [Tooltip("왼손 컨트롤러 (Ray Interactor)")]
        [SerializeField] private Transform leftHand;
        [Tooltip("오른손 컨트롤러 (Ray Interactor)")]
        [SerializeField] private Transform rightHand;

        [Tooltip("왼손 Ray Interactor")]
        [SerializeField] private XRRayInteractor leftRayInteractor;
        [Tooltip("오른손 Ray Interactor")]
        [SerializeField] private XRRayInteractor rightRayInteractor;

        [Tooltip("현재 활성화된 손 (true=오른손, false=왼손)")]
        [SerializeField] private bool useRightHand = true;

        [Tooltip("손 위치 오프셋 (카메라 기준)")]
        [SerializeField] private Vector3 leftHandOffset = new Vector3(-0.3f, -0.2f, 0.5f);
        [SerializeField] private Vector3 rightHandOffset = new Vector3(0.3f, -0.2f, 0.5f);

        [Tooltip("상호작용 거리")]
        [SerializeField] private float interactionRange = 10f;

        [Header("레이 조정 모드")]
        [Tooltip("레이 조정 감도")]
        [SerializeField] private float rayAimSensitivity = 2f;
        [Tooltip("레이 조정 최대 각도")]
        [SerializeField] private float maxRayAngle = 60f;

        [Header("손전등")]
        [SerializeField] private Light flashlight;
        [SerializeField] private bool flashlightOn = false;

        [Header("References")]
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Camera playerCamera;

        // 컴포넌트
        private CharacterController controller;
        private float verticalVelocity;
        private float cameraPitch;
        private bool cursorLocked = true;

        // 스태미나
        private float currentStamina;
        public float CurrentStamina => currentStamina;
        public float StaminaPercent => currentStamina / maxStamina;

        // 상태
        public bool IsHiding { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsCrouching { get; private set; }

        // 소음
        private float lastNoiseTime;

        // 레이 시각화
        private LineRenderer leftHandLine;
        private LineRenderer rightHandLine;

        // 레이 조정 모드
        private enum RayAimMode { None, LeftHand, RightHand }
        private RayAimMode currentRayAimMode = RayAimMode.None;
        private float leftHandPitch = 0f;
        private float leftHandYaw = 0f;
        private float rightHandPitch = 0f;
        private float rightHandYaw = 0f;

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
        }

        private void Start()
        {
            controller = GetComponent<CharacterController>();

            // 카메라 홀더 찾기
            if (cameraHolder == null)
            {
                cameraHolder = transform.Find("CameraHolder");
                if (cameraHolder == null)
                {
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        cameraHolder = mainCam.transform.parent ?? mainCam.transform;
                    }
                }
            }

            // 카메라 찾기
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            // 양손 컨트롤러 설정
            SetupDualHandControllers();

            // 손전등 찾기
            if (flashlight == null)
            {
                flashlight = GetComponentInChildren<Light>();
            }

            currentStamina = maxStamina;
            LockCursor(true);

            Debug.Log("[PCPlayerController] 초기화 완료 - 양손 컨트롤러 시뮬레이션 활성화");
        }

        /// <summary>
        /// 양손 컨트롤러 설정
        /// </summary>
        private void SetupDualHandControllers()
        {
            if (playerCamera == null) return;

            // 왼손 컨트롤러 생성/찾기
            if (leftHand == null)
            {
                leftHand = transform.Find("LeftHand");
                if (leftHand == null)
                {
                    GameObject leftHandObj = new GameObject("LeftHand");
                    leftHandObj.transform.SetParent(playerCamera.transform);
                    leftHand = leftHandObj.transform;
                }
            }
            leftHand.localPosition = leftHandOffset;

            // 오른손 컨트롤러 생성/찾기
            if (rightHand == null)
            {
                rightHand = transform.Find("RightHand");
                if (rightHand == null)
                {
                    GameObject rightHandObj = new GameObject("RightHand");
                    rightHandObj.transform.SetParent(playerCamera.transform);
                    rightHand = rightHandObj.transform;
                }
            }
            rightHand.localPosition = rightHandOffset;

            // XR Ray Interactor 설정
            SetupHandRayInteractor(leftHand, ref leftRayInteractor, ref leftHandLine, new Color(1f, 0.3f, 0.3f, 0.8f));
            SetupHandRayInteractor(rightHand, ref rightRayInteractor, ref rightHandLine, new Color(0.3f, 1f, 0.3f, 0.8f));

            Debug.Log("[PCPlayerController] 양손 컨트롤러 설정 완료");
        }

        /// <summary>
        /// 손에 Ray Interactor 설정
        /// </summary>
        private void SetupHandRayInteractor(Transform hand, ref XRRayInteractor rayInteractor, ref LineRenderer lineRenderer, Color rayColor)
        {
            if (hand == null) return;

            // XR Ray Interactor
            if (rayInteractor == null)
            {
                rayInteractor = hand.GetComponent<XRRayInteractor>();
                if (rayInteractor == null)
                {
                    rayInteractor = hand.gameObject.AddComponent<XRRayInteractor>();
                }
            }
            rayInteractor.maxRaycastDistance = interactionRange;
            rayInteractor.enableUIInteraction = true;

            // Line Renderer
            lineRenderer = hand.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = hand.gameObject.AddComponent<LineRenderer>();
            }
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.positionCount = 2;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.2f);
            lineRenderer.enabled = true;
        }

        private void Update()
        {
            HandleCursorLock();

            if (cursorLocked)
            {
                // 레이 조정 모드일 때는 이동/시점 대신 레이 조정
                if (currentRayAimMode != RayAimMode.None)
                {
                    HandleRayAiming();
                }
                else
                {
                    HandleMovement();
                    HandleMouseLook();
                }
                HandleStamina();
                HandleNoise();
            }

            HandleInput();
            UpdateHandControllers();
        }

        #region Cursor Lock

        private void HandleCursorLock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(!cursorLocked);
            }

            if (Input.GetMouseButtonDown(0) && !cursorLocked)
            {
                LockCursor(true);
            }
        }

        private void LockCursor(bool locked)
        {
            cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        #endregion

        #region Movement

        private void HandleMovement()
        {
            if (controller == null) return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 move = transform.right * horizontal + transform.forward * vertical;

            // 이동 속도 결정
            float speed = walkSpeed;
            if (IsSprinting && currentStamina > 0)
            {
                speed = runSpeed;
            }
            else if (IsCrouching)
            {
                speed = crouchSpeed;
            }

            controller.Move(move * speed * Time.deltaTime);

            // 중력 적용
            if (controller.isGrounded && verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            if (!cursorLocked) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // 좌우 회전 (플레이어 전체)
            transform.Rotate(Vector3.up * mouseX);

            // 상하 회전 (카메라만)
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);

            if (cameraHolder != null)
            {
                cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
            }
        }

        #endregion

        #region Stamina

        private void HandleStamina()
        {
            if (IsSprinting && !IsHiding)
            {
                currentStamina -= sprintStaminaCost * Time.deltaTime;
                if (currentStamina <= 0)
                {
                    currentStamina = 0;
                    StopSprinting();
                }
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        #endregion

        #region Noise System

        private void HandleNoise()
        {
            if (IsHiding || IsCrouching) return;

            float horizontal = Mathf.Abs(Input.GetAxis("Horizontal"));
            float vertical = Mathf.Abs(Input.GetAxis("Vertical"));

            if (horizontal > 0.1f || vertical > 0.1f)
            {
                if (Time.time - lastNoiseTime >= noiseInterval)
                {
                    float noiseRadius = IsSprinting ? sprintNoiseRadius : walkNoiseRadius;
                    MakeNoise(noiseRadius);
                    lastNoiseTime = Time.time;
                }
            }
        }

        public void MakeNoise(float radius)
        {
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

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // Shift - 달리기
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                StartSprinting();
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                StopSprinting();
            }

            // C - 웅크리기
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleCrouch();
            }

            // F - 손전등
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleFlashlight();
            }

            // Tab - 손 전환
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SwitchHand();
            }

            // 1 - 왼손 레이 조정 모드 토글
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ToggleRayAimMode(RayAimMode.LeftHand);
            }

            // 2 - 오른손 레이 조정 모드 토글
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ToggleRayAimMode(RayAimMode.RightHand);
            }

            // R - 레이 방향 리셋 (카메라 정면으로)
            if (Input.GetKeyDown(KeyCode.R) && currentRayAimMode != RayAimMode.None)
            {
                ResetRayDirection();
            }

            // 좌클릭 - 상호작용 (현재 활성 손)
            if (Input.GetMouseButtonDown(0))
            {
                TryInteract();
            }

            // E - 상호작용 (레거시)
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteractLegacy();
            }

            // Q - 숨기 해제
            if (Input.GetKeyDown(KeyCode.Q) && IsHiding)
            {
                StopHiding();
            }
        }

        /// <summary>
        /// 레이 조정 모드 토글
        /// </summary>
        private void ToggleRayAimMode(RayAimMode mode)
        {
            if (currentRayAimMode == mode)
            {
                // 같은 키를 다시 누르면 해제
                currentRayAimMode = RayAimMode.None;
                Debug.Log("[PCPlayerController] 레이 조정 모드 해제");
            }
            else
            {
                currentRayAimMode = mode;
                string handName = mode == RayAimMode.LeftHand ? "왼손" : "오른손";
                Debug.Log($"[PCPlayerController] {handName} 레이 조정 모드 (마우스로 조준, 1/2로 전환, R로 리셋)");
            }
        }

        /// <summary>
        /// 레이 방향 리셋
        /// </summary>
        private void ResetRayDirection()
        {
            if (currentRayAimMode == RayAimMode.LeftHand)
            {
                leftHandPitch = 0f;
                leftHandYaw = 0f;
                Debug.Log("[PCPlayerController] 왼손 레이 리셋");
            }
            else if (currentRayAimMode == RayAimMode.RightHand)
            {
                rightHandPitch = 0f;
                rightHandYaw = 0f;
                Debug.Log("[PCPlayerController] 오른손 레이 리셋");
            }
        }

        /// <summary>
        /// 레이 조정 처리
        /// </summary>
        private void HandleRayAiming()
        {
            float mouseX = Input.GetAxis("Mouse X") * rayAimSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * rayAimSensitivity;

            if (currentRayAimMode == RayAimMode.LeftHand)
            {
                leftHandYaw += mouseX;
                leftHandPitch -= mouseY;
                leftHandYaw = Mathf.Clamp(leftHandYaw, -maxRayAngle, maxRayAngle);
                leftHandPitch = Mathf.Clamp(leftHandPitch, -maxRayAngle, maxRayAngle);
            }
            else if (currentRayAimMode == RayAimMode.RightHand)
            {
                rightHandYaw += mouseX;
                rightHandPitch -= mouseY;
                rightHandYaw = Mathf.Clamp(rightHandYaw, -maxRayAngle, maxRayAngle);
                rightHandPitch = Mathf.Clamp(rightHandPitch, -maxRayAngle, maxRayAngle);
            }
        }

        /// <summary>
        /// 손 전환 (왼손 ↔ 오른손)
        /// </summary>
        private void SwitchHand()
        {
            useRightHand = !useRightHand;
            Debug.Log($"[PCPlayerController] 활성 손: {(useRightHand ? "오른손" : "왼손")}");
        }

        #endregion

        #region Hand Controllers

        /// <summary>
        /// 양손 컨트롤러 업데이트
        /// </summary>
        private void UpdateHandControllers()
        {
            if (playerCamera == null) return;

            // 양손 위치 업데이트 (카메라 기준)
            if (leftHand != null)
            {
                leftHand.localPosition = leftHandOffset;
                UpdateHandRay(leftHand, leftHandLine, !useRightHand, true);
            }

            if (rightHand != null)
            {
                rightHand.localPosition = rightHandOffset;
                UpdateHandRay(rightHand, rightHandLine, useRightHand, false);
            }
        }

        /// <summary>
        /// 손 레이 업데이트
        /// </summary>
        private void UpdateHandRay(Transform hand, LineRenderer lineRenderer, bool isActive, bool isLeftHand)
        {
            if (hand == null || lineRenderer == null) return;

            // 레이 방향 계산
            Vector3 direction;

            if (isLeftHand)
            {
                // 왼손 레이 방향 (조정된 각도 적용)
                Quaternion rayRotation = Quaternion.Euler(leftHandPitch, leftHandYaw, 0);
                direction = playerCamera.transform.rotation * rayRotation * Vector3.forward;
            }
            else
            {
                // 오른손 레이 방향 (조정된 각도 적용)
                Quaternion rayRotation = Quaternion.Euler(rightHandPitch, rightHandYaw, 0);
                direction = playerCamera.transform.rotation * rayRotation * Vector3.forward;
            }

            // 손의 회전도 레이 방향에 맞춤
            hand.rotation = Quaternion.LookRotation(direction);

            Ray ray = new Ray(hand.position, direction);
            float rayLength = interactionRange;

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            {
                rayLength = hit.distance;
            }

            lineRenderer.SetPosition(0, hand.position);
            lineRenderer.SetPosition(1, hand.position + direction * rayLength);

            // 레이 조정 모드인 손은 더 밝게, 그렇지 않으면 활성 손이 밝게
            bool isBeingAimed = (isLeftHand && currentRayAimMode == RayAimMode.LeftHand) ||
                               (!isLeftHand && currentRayAimMode == RayAimMode.RightHand);

            Color baseColor = lineRenderer.startColor;
            if (isBeingAimed)
            {
                baseColor.a = 1f; // 조정 중인 손은 가장 밝게
            }
            else if (isActive)
            {
                baseColor.a = 0.8f;
            }
            else
            {
                baseColor.a = 0.3f;
            }
            lineRenderer.startColor = baseColor;
        }

        /// <summary>
        /// 현재 활성 손의 Ray Interactor 반환
        /// </summary>
        private XRRayInteractor GetActiveRayInteractor()
        {
            return useRightHand ? rightRayInteractor : leftRayInteractor;
        }

        /// <summary>
        /// 현재 활성 손의 Transform 반환
        /// </summary>
        private Transform GetActiveHand()
        {
            return useRightHand ? rightHand : leftHand;
        }

        #endregion

        #region XR Interaction

        private void TryInteract()
        {
            XRRayInteractor activeInteractor = GetActiveRayInteractor();

            // XR Interactor를 통한 상호작용
            if (activeInteractor != null && activeInteractor.TryGetCurrent3DRaycastHit(out RaycastHit xrHit))
            {
                var interactable = xrHit.collider.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable>();
                if (interactable != null)
                {
                    Debug.Log($"[PCPlayerController] XR 상호작용: {xrHit.collider.gameObject.name}");
                    return;
                }
            }

            // 레거시 상호작용
            TryInteractLegacy();
        }

        private void TryInteractLegacy()
        {
            if (playerCamera == null) return;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            {
                // InteractableObject
                var customInteractable = hit.collider.GetComponent<InteractableObject>();
                if (customInteractable != null)
                {
                    Debug.Log($"[PCPlayerController] 상호작용: {hit.collider.gameObject.name}");
                    customInteractable.Interact();
                    return;
                }

                // Door
                var door = hit.collider.GetComponent<Door>();
                if (door != null)
                {
                    Debug.Log($"[PCPlayerController] 문 상호작용: {hit.collider.gameObject.name}");
                    door.Interact();
                    return;
                }

                // HidingSpot
                var hidingSpot = hit.collider.GetComponent<HidingSpot>();
                if (hidingSpot != null)
                {
                    Debug.Log($"[PCPlayerController] 숨기 장소: {hit.collider.gameObject.name}");
                    StartHiding(hidingSpot.transform);
                    return;
                }
            }
        }

        #endregion

        #region Flashlight

        private void ToggleFlashlight()
        {
            if (flashlight != null)
            {
                flashlightOn = !flashlightOn;
                flashlight.enabled = flashlightOn;
                Debug.Log($"[PCPlayerController] 손전등: {(flashlightOn ? "ON" : "OFF")}");
            }
            else
            {
                var vrFlashlight = GetComponentInChildren<VRFlashlight>();
                if (vrFlashlight != null)
                {
                    vrFlashlight.Toggle();
                }
                else
                {
                    Debug.Log("[PCPlayerController] 손전등을 찾을 수 없습니다.");
                }
            }
        }

        #endregion

        #region State Management

        public void StartSprinting()
        {
            if (currentStamina > 0 && !IsHiding && !IsCrouching)
            {
                IsSprinting = true;
            }
        }

        public void StopSprinting()
        {
            IsSprinting = false;
        }

        public void ToggleCrouch()
        {
            IsCrouching = !IsCrouching;
            if (IsCrouching)
            {
                IsSprinting = false;
                if (cameraHolder != null)
                {
                    cameraHolder.localPosition = new Vector3(
                        cameraHolder.localPosition.x,
                        0.8f,
                        cameraHolder.localPosition.z
                    );
                }
            }
            else
            {
                if (cameraHolder != null)
                {
                    cameraHolder.localPosition = new Vector3(
                        cameraHolder.localPosition.x,
                        1.6f,
                        cameraHolder.localPosition.z
                    );
                }
            }
            Debug.Log($"[PCPlayerController] 웅크리기: {(IsCrouching ? "ON" : "OFF")}");
        }

        public void StartHiding(Transform hideSpot)
        {
            IsHiding = true;
            IsSprinting = false;

            transform.position = hideSpot.position;
            transform.rotation = hideSpot.rotation;

            Debug.Log("[PCPlayerController] 숨기 시작");
        }

        public void StopHiding()
        {
            IsHiding = false;
            Debug.Log("[PCPlayerController] 숨기 종료");
        }

        public void GetCaught()
        {
            Debug.Log("[PCPlayerController] 잡혔습니다!");
            enabled = false;
            LockCursor(false);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위
            Gizmos.color = Color.yellow;
            if (playerCamera != null)
            {
                Gizmos.DrawLine(playerCamera.transform.position,
                    playerCamera.transform.position + playerCamera.transform.forward * interactionRange);
            }

            // 소음 범위
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, walkNoiseRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, sprintNoiseRadius);

            // 양손 위치
            if (Application.isPlaying)
            {
                if (leftHand != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(leftHand.position, 0.05f);
                }
                if (rightHand != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(rightHand.position, 0.05f);
                }
            }
        }

        #endregion
    }
}
