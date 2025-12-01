using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

namespace HorrorGame.Editor
{
    /// <summary>
    /// VR 플레이어 자동 설정 도구
    /// XR Origin에 필요한 모든 컴포넌트를 자동으로 추가하고 설정합니다.
    /// </summary>
    public class VRPlayerSetupTool : EditorWindow
    {
        // 설정 값
        private float maxStamina = 100f;
        private float walkSpeed = 2f;
        private float sprintSpeed = 4f;
        private float crouchSpeed = 1f;

        private bool addFlashlight = true;
        private bool addQuest3Controllers = true;
        private bool addVRHUD = true;
        private bool addComfortSettings = true;
        private bool createFromScratch = false;

        private Vector2 scrollPos;

        [MenuItem("Horror Game/VR 플레이어 설정 도구", false, 110)]
        public static void ShowWindow()
        {
            var window = GetWindow<VRPlayerSetupTool>("VR 플레이어 설정");
            window.minSize = new Vector2(350, 550);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 헤더
            EditorGUILayout.Space(10);
            GUILayout.Label("VR 플레이어 자동 설정 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "XR Origin을 공포 게임용 VR 플레이어로 자동 설정합니다.\n" +
                "VRPlayer, PlayerInventory, Quest3Controller 등이 추가됩니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 플레이어 스탯
            EditorGUILayout.LabelField("플레이어 스탯", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            maxStamina = EditorGUILayout.FloatField("최대 스태미나", maxStamina);
            walkSpeed = EditorGUILayout.FloatField("걷기 속도", walkSpeed);
            sprintSpeed = EditorGUILayout.FloatField("달리기 속도", sprintSpeed);
            crouchSpeed = EditorGUILayout.FloatField("웅크리기 속도", crouchSpeed);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 컴포넌트 옵션
            EditorGUILayout.LabelField("추가 컴포넌트", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            addFlashlight = EditorGUILayout.Toggle("손전등 (VRFlashlight)", addFlashlight);
            addQuest3Controllers = EditorGUILayout.Toggle("Quest3 컨트롤러", addQuest3Controllers);
            addVRHUD = EditorGUILayout.Toggle("VR HUD", addVRHUD);
            addComfortSettings = EditorGUILayout.Toggle("VR 편의 설정", addComfortSettings);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 고급 옵션
            EditorGUILayout.LabelField("고급 옵션", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            createFromScratch = EditorGUILayout.Toggle("XR Origin 새로 생성", createFromScratch);
            EditorGUILayout.HelpBox(
                createFromScratch
                    ? "새로운 XR Origin을 처음부터 생성합니다."
                    : "기존 XR Origin을 찾거나 선택된 오브젝트를 사용합니다.",
                MessageType.None);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(20);

            // 버튼들
            EditorGUILayout.LabelField("실행", EditorStyles.boldLabel);

            // 자동 설정
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("VR 플레이어 자동 설정", GUILayout.Height(40)))
            {
                SetupVRPlayer();
            }

            EditorGUILayout.Space(5);

            // 선택된 오브젝트에 설정
            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            if (GUILayout.Button("선택된 오브젝트에 설정 적용", GUILayout.Height(35)))
            {
                SetupSelectedAsVRPlayer();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // 유틸리티
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("XR Origin 찾기"))
            {
                FindAndSelectXROrigin();
            }
            if (GUILayout.Button("Main Camera 찾기"))
            {
                FindAndSelectMainCamera();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("VR 프로젝트 설정 확인"))
            {
                CheckVRProjectSettings();
            }

            EditorGUILayout.Space(10);

            // 빠른 생성 버튼들
            EditorGUILayout.LabelField("빠른 추가", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 손전등"))
            {
                AddFlashlightToSelected();
            }
            if (GUILayout.Button("+ HUD"))
            {
                AddVRHUDToScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// VR 플레이어 자동 설정
        /// </summary>
        private void SetupVRPlayer()
        {
            GameObject xrOrigin = null;

            if (createFromScratch)
            {
                xrOrigin = CreateXROriginFromScratch();
            }
            else
            {
                // 기존 XR Origin 찾기
                xrOrigin = FindExistingXROrigin();

                if (xrOrigin == null)
                {
                    bool create = EditorUtility.DisplayDialog("XR Origin 없음",
                        "씬에 XR Origin이 없습니다.\n새로 생성하시겠습니까?",
                        "생성", "취소");

                    if (create)
                    {
                        xrOrigin = CreateXROriginFromScratch();
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (xrOrigin != null)
            {
                SetupVRPlayerComponents(xrOrigin);
                Selection.activeGameObject = xrOrigin;

                Debug.Log("[VRPlayerSetupTool] VR 플레이어가 성공적으로 설정되었습니다!");
                EditorUtility.DisplayDialog("완료", "VR 플레이어가 설정되었습니다!", "확인");
            }
        }

        /// <summary>
        /// 선택된 오브젝트를 VR 플레이어로 설정
        /// </summary>
        private void SetupSelectedAsVRPlayer()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("오류", "오브젝트를 선택해주세요.", "확인");
                return;
            }

            Undo.RecordObject(selected, "Setup as VR Player");
            SetupVRPlayerComponents(selected);

            Debug.Log($"[VRPlayerSetupTool] '{selected.name}'이(가) VR 플레이어로 설정되었습니다!");
            EditorUtility.DisplayDialog("완료", $"'{selected.name}'이(가) VR 플레이어로 설정되었습니다!", "확인");
        }

        /// <summary>
        /// 기존 XR Origin 찾기
        /// </summary>
        private GameObject FindExistingXROrigin()
        {
            // Unity.XR.CoreUtils.XROrigin 찾기
            var xrOriginComponent = Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOriginComponent != null)
            {
                return xrOriginComponent.gameObject;
            }

            // 이름으로 찾기
            var byName = GameObject.Find("XR Origin");
            if (byName != null) return byName;

            byName = GameObject.Find("XR Origin (XR Rig)");
            if (byName != null) return byName;

            byName = GameObject.Find("XR Rig");
            if (byName != null) return byName;

            return null;
        }

        /// <summary>
        /// XR Origin 새로 생성
        /// </summary>
        private GameObject CreateXROriginFromScratch()
        {
            // XR Origin 루트
            GameObject xrOrigin = new GameObject("XR Origin (VR Player)");
            Undo.RegisterCreatedObjectUndo(xrOrigin, "Create XR Origin");

            // XR Origin 컴포넌트 추가
            var xrOriginComponent = xrOrigin.AddComponent<Unity.XR.CoreUtils.XROrigin>();

            // Camera Offset
            GameObject cameraOffset = new GameObject("Camera Offset");
            cameraOffset.transform.SetParent(xrOrigin.transform);
            cameraOffset.transform.localPosition = Vector3.zero;

            // Main Camera
            GameObject mainCamera = new GameObject("Main Camera");
            mainCamera.transform.SetParent(cameraOffset.transform);
            mainCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
            mainCamera.tag = "MainCamera";

            Camera cam = mainCamera.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            mainCamera.AddComponent<AudioListener>();

            // TrackedPoseDriver 추가
            var trackedPose = mainCamera.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            trackedPose.positionAction = new UnityEngine.InputSystem.InputAction("Position",
                UnityEngine.InputSystem.InputActionType.Value);
            trackedPose.rotationAction = new UnityEngine.InputSystem.InputAction("Rotation",
                UnityEngine.InputSystem.InputActionType.Value);

            // XR Origin 설정
            xrOriginComponent.CameraFloorOffsetObject = cameraOffset;
            xrOriginComponent.Camera = cam;

            // Left Controller
            GameObject leftController = CreateController("Left Controller", cameraOffset.transform, true);

            // Right Controller
            GameObject rightController = CreateController("Right Controller", cameraOffset.transform, false);

            // 위치 설정
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                xrOrigin.transform.position = sceneView.pivot;
            }

            return xrOrigin;
        }

        /// <summary>
        /// 컨트롤러 생성
        /// </summary>
        private GameObject CreateController(string name, Transform parent, bool isLeft)
        {
            GameObject controller = new GameObject(name);
            controller.transform.SetParent(parent);
            controller.transform.localPosition = new Vector3(isLeft ? -0.2f : 0.2f, 1.4f, 0.3f);

            // XR Controller 컴포넌트
            var xrController = controller.AddComponent<ActionBasedController>();

            // XR Direct Interactor
            var interactor = controller.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();

            // Sphere Collider for interaction
            var collider = controller.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            collider.isTrigger = true;

            return controller;
        }

        /// <summary>
        /// VR 플레이어 컴포넌트 설정
        /// </summary>
        private void SetupVRPlayerComponents(GameObject target)
        {
            // VRPlayer 설정
            VRPlayer vrPlayer = target.GetComponent<VRPlayer>();
            if (vrPlayer == null)
            {
                vrPlayer = target.AddComponent<VRPlayer>();
            }
            vrPlayer.maxStamina = maxStamina;
            vrPlayer.currentStamina = maxStamina;
            vrPlayer.walkSpeed = walkSpeed;
            vrPlayer.sprintSpeed = sprintSpeed;
            vrPlayer.crouchSpeed = crouchSpeed;
            vrPlayer.staminaRegenRate = 10f;
            vrPlayer.sprintStaminaCost = 20f;
            vrPlayer.walkNoiseRadius = 3f;
            vrPlayer.sprintNoiseRadius = 8f;

            // PlayerInventory 설정
            PlayerInventory inventory = target.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                inventory = target.AddComponent<PlayerInventory>();
            }
            inventory.maxItems = 10;

            // CharacterController 설정 (움직임용)
            CharacterController charController = target.GetComponent<CharacterController>();
            if (charController == null)
            {
                charController = target.AddComponent<CharacterController>();
            }
            charController.height = 1.8f;
            charController.radius = 0.3f;
            charController.center = new Vector3(0, 0.9f, 0);

            // Quest3 Controllers 설정
            if (addQuest3Controllers)
            {
                SetupQuest3Controllers(target);
            }

            // 손전등 설정
            if (addFlashlight)
            {
                SetupFlashlight(target);
            }

            // VR Comfort Settings
            if (addComfortSettings)
            {
                VRComfortSettings comfort = target.GetComponent<VRComfortSettings>();
                if (comfort == null)
                {
                    comfort = target.AddComponent<VRComfortSettings>();
                }
            }

            // VR HUD
            if (addVRHUD)
            {
                SetupVRHUD(target);
            }

            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Quest3 컨트롤러 설정
        /// </summary>
        private void SetupQuest3Controllers(GameObject vrPlayer)
        {
            // 컨트롤러 찾기
            var controllers = vrPlayer.GetComponentsInChildren<ActionBasedController>();

            foreach (var controller in controllers)
            {
                Quest3Controller quest3Ctrl = controller.GetComponent<Quest3Controller>();
                if (quest3Ctrl == null)
                {
                    quest3Ctrl = controller.gameObject.AddComponent<Quest3Controller>();
                }

                // 왼손/오른손 자동 감지
                string nameLower = controller.gameObject.name.ToLower();
                if (nameLower.Contains("left"))
                {
                    quest3Ctrl.controllerNode = UnityEngine.XR.XRNode.LeftHand;
                }
                else if (nameLower.Contains("right"))
                {
                    quest3Ctrl.controllerNode = UnityEngine.XR.XRNode.RightHand;
                }
            }

            // 컨트롤러가 없으면 경고
            if (controllers.Length == 0)
            {
                Debug.LogWarning("[VRPlayerSetupTool] XR Controller가 없습니다. 수동으로 추가해주세요.");
            }
        }

        /// <summary>
        /// 손전등 설정
        /// </summary>
        private void SetupFlashlight(GameObject vrPlayer)
        {
            // 오른손 컨트롤러 찾기
            Transform rightHand = null;
            var controllers = vrPlayer.GetComponentsInChildren<ActionBasedController>();

            foreach (var ctrl in controllers)
            {
                if (ctrl.gameObject.name.ToLower().Contains("right"))
                {
                    rightHand = ctrl.transform;
                    break;
                }
            }

            if (rightHand == null)
            {
                // Camera Offset 아래에서 찾기
                rightHand = vrPlayer.transform.Find("Camera Offset/Right Controller");
            }

            if (rightHand == null)
            {
                Debug.LogWarning("[VRPlayerSetupTool] 오른손 컨트롤러를 찾을 수 없어 손전등을 추가하지 않았습니다.");
                return;
            }

            // 손전등 오브젝트 생성
            GameObject flashlightObj = rightHand.Find("Flashlight")?.gameObject;
            if (flashlightObj == null)
            {
                flashlightObj = new GameObject("Flashlight");
                flashlightObj.transform.SetParent(rightHand);
                flashlightObj.transform.localPosition = new Vector3(0, 0, 0.1f);
                flashlightObj.transform.localRotation = Quaternion.identity;
            }

            // Spot Light 추가
            Light spotLight = flashlightObj.GetComponent<Light>();
            if (spotLight == null)
            {
                spotLight = flashlightObj.AddComponent<Light>();
            }
            spotLight.type = LightType.Spot;
            spotLight.spotAngle = 50f;
            spotLight.range = 15f;
            spotLight.intensity = 2f;
            spotLight.color = new Color(1f, 0.95f, 0.8f); // 따뜻한 색

            // VRFlashlight 컴포넌트
            VRFlashlight vrFlashlight = flashlightObj.GetComponent<VRFlashlight>();
            if (vrFlashlight == null)
            {
                vrFlashlight = flashlightObj.AddComponent<VRFlashlight>();
            }

            Debug.Log("[VRPlayerSetupTool] 손전등이 추가되었습니다.");
        }

        /// <summary>
        /// VR HUD 설정
        /// </summary>
        private void SetupVRHUD(GameObject vrPlayer)
        {
            // 카메라 찾기
            Camera mainCam = vrPlayer.GetComponentInChildren<Camera>();
            if (mainCam == null)
            {
                Debug.LogWarning("[VRPlayerSetupTool] 카메라를 찾을 수 없어 HUD를 추가하지 않았습니다.");
                return;
            }

            // 기존 VRHUD 찾기
            VRHUD existingHUD = Object.FindFirstObjectByType<VRHUD>();
            if (existingHUD != null)
            {
                Debug.Log("[VRPlayerSetupTool] 기존 VRHUD가 있습니다.");
                return;
            }

            // HUD Canvas 생성
            GameObject hudCanvas = new GameObject("VR HUD Canvas");
            hudCanvas.transform.SetParent(mainCam.transform);
            hudCanvas.transform.localPosition = new Vector3(0, 0, 0.5f);
            hudCanvas.transform.localRotation = Quaternion.identity;
            hudCanvas.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = hudCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 300);

            // VRHUD 컴포넌트 추가
            VRHUD vrhud = hudCanvas.AddComponent<VRHUD>();

            Debug.Log("[VRPlayerSetupTool] VR HUD가 추가되었습니다.");
        }

        /// <summary>
        /// XR Origin 찾아서 선택
        /// </summary>
        private void FindAndSelectXROrigin()
        {
            var xrOrigin = FindExistingXROrigin();
            if (xrOrigin != null)
            {
                Selection.activeGameObject = xrOrigin;
                EditorGUIUtility.PingObject(xrOrigin);
            }
            else
            {
                EditorUtility.DisplayDialog("알림", "씬에 XR Origin이 없습니다.", "확인");
            }
        }

        /// <summary>
        /// Main Camera 찾아서 선택
        /// </summary>
        private void FindAndSelectMainCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Selection.activeGameObject = cam.gameObject;
                EditorGUIUtility.PingObject(cam.gameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("알림", "씬에 Main Camera가 없습니다.", "확인");
            }
        }

        /// <summary>
        /// VR 프로젝트 설정 확인
        /// </summary>
        private void CheckVRProjectSettings()
        {
            string report = "VR 프로젝트 설정 확인\n\n";

            // XR Plugin 확인
            #if UNITY_XR_OPENXR
            report += "✓ OpenXR Plugin 설치됨\n";
            #else
            report += "✗ OpenXR Plugin 미설치\n";
            #endif

            // Input System 확인
            #if ENABLE_INPUT_SYSTEM
            report += "✓ New Input System 활성화됨\n";
            #else
            report += "✗ New Input System 비활성화\n";
            #endif

            // XR Interaction Toolkit 확인
            var xrInteraction = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRBaseInteractable, Unity.XR.Interaction.Toolkit");
            if (xrInteraction != null)
            {
                report += "✓ XR Interaction Toolkit 설치됨\n";
            }
            else
            {
                report += "✗ XR Interaction Toolkit 미설치\n";
            }

            // XR Origin 확인
            var xrOrigin = FindExistingXROrigin();
            if (xrOrigin != null)
            {
                report += $"✓ XR Origin 존재: {xrOrigin.name}\n";
            }
            else
            {
                report += "✗ XR Origin 없음\n";
            }

            EditorUtility.DisplayDialog("VR 설정 확인", report, "확인");
        }

        /// <summary>
        /// 선택된 오브젝트에 손전등 추가
        /// </summary>
        private void AddFlashlightToSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("오류", "오브젝트를 선택해주세요.", "확인");
                return;
            }

            GameObject flashlight = new GameObject("Flashlight");
            flashlight.transform.SetParent(selected.transform);
            flashlight.transform.localPosition = Vector3.forward * 0.1f;

            Light light = flashlight.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 50f;
            light.range = 15f;
            light.intensity = 2f;

            flashlight.AddComponent<VRFlashlight>();

            Selection.activeGameObject = flashlight;
            Undo.RegisterCreatedObjectUndo(flashlight, "Add Flashlight");

            Debug.Log("[VRPlayerSetupTool] 손전등이 추가되었습니다.");
        }

        /// <summary>
        /// VR HUD를 씬에 추가
        /// </summary>
        private void AddVRHUDToScene()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                EditorUtility.DisplayDialog("오류", "Main Camera를 찾을 수 없습니다.", "확인");
                return;
            }

            GameObject hudCanvas = new GameObject("VR HUD Canvas");
            hudCanvas.transform.SetParent(mainCam.transform);
            hudCanvas.transform.localPosition = new Vector3(0, 0, 0.5f);
            hudCanvas.transform.localScale = Vector3.one * 0.001f;

            Canvas canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            hudCanvas.AddComponent<VRHUD>();

            Selection.activeGameObject = hudCanvas;
            Undo.RegisterCreatedObjectUndo(hudCanvas, "Add VR HUD");

            Debug.Log("[VRPlayerSetupTool] VR HUD가 추가되었습니다.");
        }

        /// <summary>
        /// 메뉴에서 빠르게 VR 플레이어 설정
        /// </summary>
        [MenuItem("Horror Game/VR 플레이어 빠른 설정", false, 111)]
        public static void QuickSetupVRPlayer()
        {
            var tool = CreateInstance<VRPlayerSetupTool>();
            tool.SetupVRPlayer();
            DestroyImmediate(tool);
        }

        /// <summary>
        /// 선택된 오브젝트를 VR 플레이어로 빠르게 설정
        /// </summary>
        [MenuItem("Horror Game/선택 오브젝트 → VR 플레이어로 설정", false, 112)]
        public static void QuickSetupSelectedAsVRPlayer()
        {
            var tool = CreateInstance<VRPlayerSetupTool>();
            tool.SetupSelectedAsVRPlayer();
            DestroyImmediate(tool);
        }

        [MenuItem("Horror Game/선택 오브젝트 → VR 플레이어로 설정", true)]
        public static bool QuickSetupSelectedAsVRPlayerValidate()
        {
            return Selection.activeGameObject != null;
        }
    }
}
