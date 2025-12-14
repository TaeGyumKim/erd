using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        [Header("레이캐스트 설정")]
        [Tooltip("레이캐스트에서 무시할 레이어 (Player, Ignore Raycast 등)")]
        [SerializeField] private LayerMask ignoreLayers = 0;

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

        // 숨기 장소
        private HidingSpot currentHidingSpot;
        private BoxHidingSpot currentBoxHidingSpot;

        // 현재 들고 있는 아이템
        private PickupItem heldItem;
        public PickupItem HeldItem => heldItem;
        public bool IsHoldingItem => heldItem != null;

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
            SetupHandRayInteractor(leftHand, ref leftRayInteractor, ref leftHandLine, new Color(1f, 0.2f, 0.2f, 1f));  // 빨간색 (왼손)
            SetupHandRayInteractor(rightHand, ref rightRayInteractor, ref rightHandLine, new Color(0.2f, 1f, 0.2f, 1f));  // 초록색 (오른손)

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
            lineRenderer.startWidth = 0.015f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.positionCount = 2;

            // Unlit 셰이더로 항상 밝게 보이도록 설정
            Material rayMaterial = new Material(Shader.Find("Unlit/Color"));
            if (rayMaterial.shader == null)
            {
                rayMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            rayMaterial.color = rayColor;
            rayMaterial.hideFlags = HideFlags.HideAndDontSave; // 에디터 저장 시 경고 방지
            lineRenderer.material = rayMaterial;
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.5f);
            lineRenderer.enabled = true;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
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

            // 손전등으로 살인마 스턴 체크
            if (flashlightOn)
            {
                CheckFlashlightStun();
            }
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
            // CharacterController가 없거나 비활성화된 경우 (숨기 상태 등) 이동 처리 안함
            if (controller == null || !controller.enabled) return;

            // 숨어있으면 이동 안함
            if (IsHiding) return;

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
                if (currentBoxHidingSpot != null)
                {
                    // 상자에서 나오기
                    currentBoxHidingSpot.ExitHiding();
                }
                else if (currentHidingSpot != null)
                {
                    currentHidingSpot.ExitHidingPC();
                    currentHidingSpot = null;
                }
                else
                {
                    StopHiding();
                }
            }

            // G - 아이템 내려놓기
            if (Input.GetKeyDown(KeyCode.G) && IsHoldingItem)
            {
                DropHeldItem(false);
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

            // 레이캐스트 마스크 설정 (ignoreLayers를 제외한 모든 레이어)
            int layerMask = ~ignoreLayers.value;
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, layerMask))
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

            // UI Button 클릭 처리 (VRPasswordKeypad 등)
            if (TryClickUIButton())
            {
                return;
            }

            // 레거시 상호작용
            TryInteractLegacy();
        }

        /// <summary>
        /// UI Button 클릭 시도 (레이캐스트로 World Space Canvas의 버튼 클릭)
        /// </summary>
        private bool TryClickUIButton()
        {
            Transform activeHand = GetActiveHand();
            if (activeHand == null && playerCamera == null) return false;

            Vector3 rayOrigin;
            Vector3 rayDirection;

            if (activeHand != null)
            {
                rayOrigin = activeHand.position;
                rayDirection = activeHand.forward;
            }
            else
            {
                rayOrigin = playerCamera.transform.position;
                rayDirection = playerCamera.transform.forward;
            }

            Ray ray = new Ray(rayOrigin, rayDirection);

            // UI 레이어 또는 모든 레이어에서 Graphic Raycaster가 있는 Canvas 찾기
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange))
            {
                // Button 컴포넌트 찾기 (자신 또는 부모)
                Button button = hit.collider.GetComponent<Button>();
                if (button == null)
                {
                    button = hit.collider.GetComponentInParent<Button>();
                }

                if (button != null && button.interactable)
                {
                    Debug.Log($"[PCPlayerController] UI 버튼 클릭: {button.gameObject.name}");
                    button.onClick.Invoke();
                    return true;
                }

                // Canvas의 자식에서 Button 찾기 (BoxCollider가 Canvas에 있는 경우)
                Canvas canvas = hit.collider.GetComponent<Canvas>();
                if (canvas == null)
                {
                    canvas = hit.collider.GetComponentInParent<Canvas>();
                }

                if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
                {
                    // World Space Canvas에서 실제 히트 위치를 기준으로 버튼 찾기
                    Button[] buttons = canvas.GetComponentsInChildren<Button>();
                    foreach (var btn in buttons)
                    {
                        RectTransform rectTransform = btn.GetComponent<RectTransform>();
                        if (rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(
                            rectTransform,
                            Camera.main.WorldToScreenPoint(hit.point),
                            Camera.main))
                        {
                            if (btn.interactable)
                            {
                                Debug.Log($"[PCPlayerController] UI 버튼 클릭 (Canvas): {btn.gameObject.name}");
                                btn.onClick.Invoke();
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void TryInteractLegacy()
        {
            // 현재 활성 손의 레이 방향 사용
            Transform activeHand = GetActiveHand();
            if (activeHand == null && playerCamera == null) return;

            Vector3 rayOrigin;
            Vector3 rayDirection;

            if (activeHand != null)
            {
                rayOrigin = activeHand.position;
                rayDirection = activeHand.forward;
            }
            else
            {
                rayOrigin = playerCamera.transform.position;
                rayDirection = playerCamera.transform.forward;
            }

            // 레이캐스트 마스크 설정 (ignoreLayers를 제외한 모든 레이어)
            int layerMask = ~ignoreLayers.value;

            Ray ray = new Ray(rayOrigin, rayDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, layerMask))
            {
                // 디버그: 레이캐스트 히트 대상 로그
                Debug.Log($"[PCPlayerController] 레이캐스트 히트: {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");

                // Door (InteractableObject보다 먼저 체크 - 더 구체적인 타입)
                // 자식 오브젝트에서 히트할 수 있으므로 부모에서도 찾기
                var door = hit.collider.GetComponent<Door>();
                if (door == null)
                {
                    door = hit.collider.GetComponentInParent<Door>();
                }
                if (door != null)
                {
                    Debug.Log($"[PCPlayerController] 문 상호작용: {hit.collider.gameObject.name}, 잠김: {door.IsLocked}, 필요 키: {door.requiredKeyId}");

                    // 들고 있는 키가 있고 문이 잠겨있으면 키 사용 시도
                    if (IsHoldingItem && door.IsLocked && heldItem is KeyItem heldKey)
                    {
                        Debug.Log($"[PCPlayerController] 들고 있는 키: {heldKey.keyId}, 문이 필요한 키: {door.requiredKeyId}");

                        if (door.requiredKeyId == heldKey.keyId)
                        {
                            Debug.Log($"[PCPlayerController] 키 사용: {heldKey.keyId}");

                            // 키 파괴 처리
                            DestroyHeldKey(heldKey);

                            // 문 잠금 해제 후 열기 (삭제하지 않음)
                            Debug.Log($"[PCPlayerController] 키로 문 열기: {door.requiredKeyId}");
                            door.Unlock();
                            door.OpenDoor();
                            return;
                        }
                        else
                        {
                            Debug.Log($"[PCPlayerController] 키 불일치 - 들고 있는 키: {heldKey.keyId}, 필요한 키: {door.requiredKeyId}");
                        }
                    }

                    door.Interact();
                    return;
                }

                // KeyItem (PickupItem 상속, 우선 체크)
                var keyItem = hit.collider.GetComponent<KeyItem>();
                if (keyItem == null)
                {
                    keyItem = hit.collider.GetComponentInParent<KeyItem>();
                }
                if (keyItem != null)
                {
                    // 이미 아이템을 들고 있으면 교체
                    if (IsHoldingItem)
                    {
                        DropHeldItem(false);
                    }

                    Debug.Log($"[PCPlayerController] 키 집기: {keyItem.gameObject.name} (keyId: {keyItem.keyId})");
                    HoldItem(keyItem);
                    return;
                }

                // PickupItem (일반 아이템)
                var pickupItem = hit.collider.GetComponent<PickupItem>();
                if (pickupItem == null)
                {
                    pickupItem = hit.collider.GetComponentInParent<PickupItem>();
                }
                if (pickupItem != null)
                {
                    // 이미 아이템을 들고 있으면 교체, 아니면 집기
                    if (IsHoldingItem)
                    {
                        DropHeldItem(false); // 현재 아이템 내려놓기
                    }

                    Debug.Log($"[PCPlayerController] 아이템 집기: {hit.collider.gameObject.name}");
                    HoldItem(pickupItem);
                    return;
                }

                // InteractableObject (기본 상호작용)
                var customInteractable = hit.collider.GetComponent<InteractableObject>();
                if (customInteractable != null)
                {
                    Debug.Log($"[PCPlayerController] 상호작용: {hit.collider.gameObject.name}");
                    customInteractable.Interact();
                    return;
                }

                // HidingSpot
                var hidingSpot = hit.collider.GetComponent<HidingSpot>();
                if (hidingSpot != null)
                {
                    Debug.Log($"[PCPlayerController] 숨기 장소: {hit.collider.gameObject.name}");
                    if (IsHiding)
                    {
                        // 이미 숨어있으면 나오기
                        hidingSpot.ExitHidingPC();
                        currentHidingSpot = null;
                    }
                    else
                    {
                        // 숨기
                        hidingSpot.EnterHidingPC(this);
                        currentHidingSpot = hidingSpot;
                    }
                    return;
                }

                // BoxHidingSpot (상자에서 숨기)
                var boxHidingSpot = hit.collider.GetComponent<BoxHidingSpot>();
                if (boxHidingSpot == null)
                {
                    boxHidingSpot = hit.collider.GetComponentInParent<BoxHidingSpot>();
                }
                if (boxHidingSpot != null)
                {
                    Debug.Log($"[PCPlayerController] 상자 숨기 장소: {hit.collider.gameObject.name}");
                    boxHidingSpot.Interact(this);
                    return;
                }

                // SlidingDoor (탈출문)
                var slidingDoor = hit.collider.GetComponent<SlidingDoor>();
                if (slidingDoor != null)
                {
                    Debug.Log($"[PCPlayerController] 슬라이딩 문: {hit.collider.gameObject.name}");
                    slidingDoor.TryInteract();
                    return;
                }

                // ReadableNote (책/메모)
                var readableNote = hit.collider.GetComponent<ReadableNote>();
                if (readableNote != null)
                {
                    Debug.Log($"[PCPlayerController] 메모 읽기: {hit.collider.gameObject.name}");
                    readableNote.OpenNote();
                    return;
                }

                // PasswordChest (비밀번호 상자)
                var passwordChest = hit.collider.GetComponent<PasswordChest>();
                if (passwordChest != null)
                {
                    Debug.Log($"[PCPlayerController] 비밀번호 상자: {hit.collider.gameObject.name}");
                    if (passwordChest.isOpen)
                    {
                        passwordChest.TakeItem();
                    }
                    else if (!passwordChest.isLocked)
                    {
                        passwordChest.OpenChest();
                    }
                    else
                    {
                        // 비밀번호 입력 UI 표시
                        passwordChest.ShowPasswordUI();
                    }
                    return;
                }
            }
        }

        #endregion

        #region Flashlight

        [Header("손전등 스턴 설정")]
        [SerializeField] private float flashlightStunRange = 12f;
        [SerializeField] private float flashlightStunAngle = 45f;
        [SerializeField] private float flashlightStunTime = 0.15f; // 빛을 비춰야 하는 시간 (0.15초 - VR에서 빠르게 비추기 가능)
        [SerializeField] private bool showStunProgress = false; // 스턴 진행도 표시 (0.15초라 불필요)
        private float flashlightStunTimer = 0f;
        private KillerAI currentTargetKiller = null;

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

            // 손전등 끄면 스턴 타이머 리셋
            if (!flashlightOn)
            {
                flashlightStunTimer = 0f;
                currentTargetKiller = null;
            }
        }

        /// <summary>
        /// 손전등으로 살인마 스턴 체크
        /// 개선: 살인마 어느 방향에서든 비추면 스턴 가능 (0.5초로 단축)
        /// </summary>
        private void CheckFlashlightStun()
        {
            if (playerCamera == null) return;

            // 카메라 전방으로 레이캐스트
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, flashlightStunRange))
            {
                // 살인마인지 확인
                KillerAI killer = hit.collider.GetComponent<KillerAI>();
                if (killer == null)
                {
                    killer = hit.collider.GetComponentInParent<KillerAI>();
                }

                if (killer != null && !killer.IsStunned)
                {
                    // 같은 살인마에게 계속 비추고 있으면 타이머 증가
                    if (currentTargetKiller == killer)
                    {
                        flashlightStunTimer += Time.deltaTime;

                        // 스턴 진행도 로그 (디버그용)
                        if (showStunProgress && flashlightStunTimer > 0.1f)
                        {
                            float progress = (flashlightStunTimer / flashlightStunTime) * 100f;
                            if (progress < 100f)
                            {
                                Debug.Log($"[PCPlayerController] 손전등 스턴 진행: {progress:F0}%");
                            }
                        }

                        // 스턴 시간 도달
                        if (flashlightStunTimer >= flashlightStunTime)
                        {
                            killer.StunByFlashlight();
                            flashlightStunTimer = 0f;
                            currentTargetKiller = null;
                            Debug.Log("[PCPlayerController] 손전등으로 살인마 스턴 성공!");
                        }
                    }
                    else
                    {
                        // 새로운 타겟
                        currentTargetKiller = killer;
                        flashlightStunTimer = 0f;
                        Debug.Log($"[PCPlayerController] 손전등 타겟: {killer.gameObject.name}");
                    }
                    return;
                }
            }

            // 타겟에서 벗어남 - 타이머 천천히 감소 (완전 리셋 대신)
            if (flashlightStunTimer > 0f)
            {
                flashlightStunTimer -= Time.deltaTime * 0.5f; // 절반 속도로 감소
                if (flashlightStunTimer <= 0f)
                {
                    flashlightStunTimer = 0f;
                    currentTargetKiller = null;
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

        /// <summary>
        /// 상자에서 숨기 시작 (CharacterController는 BoxHidingSpot에서 관리)
        /// </summary>
        public void StartHidingInBox(BoxHidingSpot box)
        {
            IsHiding = true;
            IsSprinting = false;
            currentBoxHidingSpot = box;

            Debug.Log("[PCPlayerController] 상자에서 숨기 시작");
        }

        /// <summary>
        /// 상자에서 숨기 종료
        /// </summary>
        public void StopHidingFromBox()
        {
            IsHiding = false;
            currentBoxHidingSpot = null;

            Debug.Log("[PCPlayerController] 상자에서 숨기 종료");
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

        #region Item Holding

        /// <summary>
        /// 아이템 집기
        /// </summary>
        public void HoldItem(PickupItem item)
        {
            if (item == null) return;

            heldItem = item;

            // 아이템을 손 위치로 이동
            Transform activeHand = GetActiveHand();
            if (activeHand != null)
            {
                item.transform.SetParent(activeHand);
                item.transform.localPosition = Vector3.forward * 0.3f;
                item.transform.localRotation = Quaternion.identity;
                item.transform.localScale = Vector3.one;
            }

            // Rigidbody 비활성화
            var rb = item.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Collider 비활성화 (집은 상태에서 충돌 방지)
            var col = item.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;
            }

            Debug.Log($"[PCPlayerController] 아이템 집음: {item.name}");
        }

        /// <summary>
        /// 아이템 내려놓기
        /// </summary>
        public void DropHeldItem(bool destroy = false)
        {
            if (heldItem == null) return;

            var item = heldItem;
            heldItem = null;

            if (destroy)
            {
                Destroy(item.gameObject);
                Debug.Log($"[PCPlayerController] 아이템 사용됨: {item.name}");
            }
            else
            {
                // 부모 해제
                item.transform.SetParent(null);

                // 플레이어 앞에 내려놓기
                item.transform.position = transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;

                // Collider 활성화
                var col = item.GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = true;
                }

                // Rigidbody는 usePhysics 설정에 따라 결정
                var rb = item.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 내려놓을 때는 잠시 물리 활성화하여 떨어지게
                    rb.isKinematic = false;
                    rb.useGravity = true;
                }

                Debug.Log($"[PCPlayerController] 아이템 내려놓음: {item.name}");
            }
        }

        #endregion

        #region Key Destruction

        /// <summary>
        /// 들고 있는 키를 파괴
        /// </summary>
        private void DestroyHeldKey(KeyItem keyItem)
        {
            if (keyItem == null) return;

            string keyId = keyItem.keyId;
            GameObject keyObject = keyItem.gameObject;

            // heldItem을 먼저 null로 설정
            heldItem = null;

            // 부모에서 분리 (손에서 떼어냄)
            keyObject.transform.SetParent(null);

            // 즉시 파괴 (DestroyImmediate는 에디터에서만 사용, Destroy 사용)
            Destroy(keyObject);

            Debug.Log($"[PCPlayerController] 키 파괴됨: {keyId}");
        }

        #endregion

        #region Collision Detection

        /// <summary>
        /// CharacterController 충돌 감지 - 키를 들고 문에 부딪히면 문 열기
        /// </summary>
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // 키를 들고 있지 않으면 무시
            if (!IsHoldingItem || !(heldItem is KeyItem keyItem)) return;

            // Door 컴포넌트 찾기 (자신 또는 부모에서)
            var door = hit.collider.GetComponent<Door>();
            if (door == null)
            {
                door = hit.collider.GetComponentInParent<Door>();
            }

            if (door != null && door.IsLocked)
            {
                Debug.Log($"[PCPlayerController] 문 충돌 감지: {hit.collider.name}, 키: {keyItem.keyId}, 필요 키: {door.requiredKeyId}");

                if (door.requiredKeyId == keyItem.keyId)
                {
                    Debug.Log($"[PCPlayerController] 키로 문 열림 (충돌): {keyItem.keyId}");

                    // 키 파괴 처리
                    DestroyHeldKey(keyItem);

                    // 문 잠금 해제 후 열기 (삭제하지 않음)
                    door.Unlock();
                    door.OpenDoor();
                }
            }
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
