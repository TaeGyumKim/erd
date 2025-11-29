using UnityEngine;


namespace HorrorGame
{
    /// <summary>
    /// VR 없이 데스크톱에서 테스트하기 위한 간단한 시뮬레이터
    /// WASD로 이동, 마우스로 시점 회전
    /// </summary>
    public class DesktopXRSimulator : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float sprintMultiplier = 2f;
        [SerializeField] private float mouseSensitivity = 2f;

        [Header("참조")]
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftHandInteractor;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rightHandInteractor;

        private float rotationX = 0f;
        private float rotationY = 0f;
        private bool isSimulatorActive = false;

        private void Start()
        {
            // VR이 연결되어 있지 않으면 시뮬레이터 활성화
            if (!UnityEngine.XR.XRSettings.isDeviceActive)
            {
                EnableSimulator();
            }
        }

        private void EnableSimulator()
        {
            isSimulatorActive = true;

            // XR Origin 자동 찾기
            if (xrOrigin == null)
            {
                var origin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (origin != null)
                    xrOrigin = origin.transform;
            }

            // 카메라 자동 찾기
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
            }

            // Interactor 자동 찾기
            if (rightHandInteractor == null)
            {
                var interactors = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
                foreach (var interactor in interactors)
                {
                    if (interactor.name.Contains("Right"))
                        rightHandInteractor = interactor;
                    else if (interactor.name.Contains("Left"))
                        leftHandInteractor = interactor;
                }
            }

            // TrackedPoseDriver 비활성화 (데스크톱 모드에서) - 리플렉션 사용
            if (cameraTransform != null)
            {
                var poseDriverType = System.Type.GetType("UnityEngine.InputSystem.XR.TrackedPoseDriver, Unity.InputSystem");
                if (poseDriverType != null)
                {
                    var poseDriver = cameraTransform.GetComponent(poseDriverType) as Behaviour;
                    if (poseDriver != null)
                        poseDriver.enabled = false;
                }
            }

            // 초기 회전값 설정
            if (cameraTransform != null)
            {
                rotationX = cameraTransform.localEulerAngles.x;
                rotationY = xrOrigin != null ? xrOrigin.eulerAngles.y : 0f;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            Debug.Log("[DesktopXRSimulator] 데스크톱 모드 활성화 - WASD: 이동, 마우스: 시점, ESC: 마우스 해제, 클릭: 상호작용");
        }

        private void Update()
        {
            if (!isSimulatorActive) return;

            HandleMouseLock();
            HandleRotation();
            HandleMovement();
            HandleInteraction();
        }

        private void HandleMouseLock()
        {
            // ESC로 마우스 잠금 해제/설정
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }

            // 클릭으로 다시 잠금
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleRotation()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;
            if (cameraTransform == null || xrOrigin == null) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            // XR Origin의 Y 회전 (좌우)
            xrOrigin.rotation = Quaternion.Euler(0f, rotationY, 0f);

            // 카메라의 X 회전 (상하)
            cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }

        private void HandleMovement()
        {
            if (xrOrigin == null) return;

            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            float currentSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
                currentSpeed *= sprintMultiplier;

            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
            Vector3 move = xrOrigin.TransformDirection(direction) * currentSpeed * Time.deltaTime;

            xrOrigin.position += move;
        }

        private void HandleInteraction()
        {
            // 마우스 왼쪽 클릭 - 상호작용 (오른손 컨트롤러 시뮬레이션)
            if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.Locked)
            {
                if (rightHandInteractor != null)
                {
                    // Ray Interactor의 방향을 카메라 방향으로 설정
                    rightHandInteractor.transform.position = cameraTransform.position;
                    rightHandInteractor.transform.rotation = cameraTransform.rotation;
                }
            }

            // E키로 상호작용
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryInteract();
            }
        }

        private void TryInteract()
        {
            if (cameraTransform == null) return;

            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                // XRGrabInteractable 확인
                var grabInteractable = hit.collider.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                if (grabInteractable != null)
                {
                    Debug.Log($"[DesktopXRSimulator] 상호작용 가능: {hit.collider.name}");
                }

                // XRSimpleInteractable 확인
                var simpleInteractable = hit.collider.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                if (simpleInteractable != null)
                {
                    Debug.Log($"[DesktopXRSimulator] 상호작용 가능: {hit.collider.name}");
                }
            }
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnGUI()
        {
            if (!isSimulatorActive) return;

            // 화면 중앙에 크로스헤어 표시
            float size = 10f;
            float x = Screen.width / 2f;
            float y = Screen.height / 2f;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x - size / 2, y - 1, size, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1, y - size / 2, 2, size), Texture2D.whiteTexture);

            // 조작법 안내
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(10, 10, 300, 200),
                "[데스크톱 테스트 모드]\n" +
                "WASD - 이동\n" +
                "마우스 - 시점\n" +
                "Shift - 달리기\n" +
                "E - 상호작용\n" +
                "ESC - 마우스 해제", style);
        }
    }
}
