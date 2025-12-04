using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using HorrorGame;

namespace HorrorGame.Editor
{
    /// <summary>
    /// PC에서 VR 없이 테스트할 수 있는 플레이어 생성 도구
    /// XR Interaction Toolkit을 활용하여 VR과 동일한 상호작용 지원
    /// </summary>
    public class PCPlayerSetupTool : EditorWindow
    {
        private bool addFlashlight = true;
        private bool addDualHandControllers = true;
        private bool addXRDeviceSimulator = false;

        [MenuItem("Horror Game/PC 테스트 플레이어 생성", false, 200)]
        public static void ShowWindow()
        {
            var window = GetWindow<PCPlayerSetupTool>("PC 테스트 플레이어");
            window.minSize = new Vector2(350, 450);
        }

        private void OnGUI()
        {
            GUILayout.Label("PC 테스트 플레이어 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "VR 헤드셋 없이 키보드/마우스로 테스트할 수 있는 플레이어를 생성합니다.\n\n" +
                "XR Interaction Toolkit을 활용하여 VR과 동일한 방식으로 오브젝트와 상호작용할 수 있습니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 옵션
            EditorGUILayout.LabelField("옵션", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            addFlashlight = EditorGUILayout.Toggle("손전등 추가", addFlashlight);
            addDualHandControllers = EditorGUILayout.Toggle("양손 컨트롤러 추가", addDualHandControllers);
            addXRDeviceSimulator = EditorGUILayout.Toggle("XR Device Simulator 추가", addXRDeviceSimulator);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(15);

            // 메인 버튼
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("PC 테스트 플레이어 생성", GUILayout.Height(40)))
            {
                CreatePCTestPlayer();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // 유틸리티 버튼
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);

            if (GUILayout.Button("XR Device Simulator 추가", GUILayout.Height(30)))
            {
                AddXRDeviceSimulator();
            }

            if (GUILayout.Button("XR Interaction Manager 확인/생성", GUILayout.Height(25)))
            {
                EnsureXRInteractionManager();
            }

            EditorGUILayout.Space(15);

            // 조작 방법
            EditorGUILayout.LabelField("조작 방법", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "WASD: 이동\n" +
                "Shift: 달리기\n" +
                "C: 웅크리기\n" +
                "마우스: 시점 회전\n" +
                "좌클릭: 상호작용 (활성 손)\n" +
                "Tab: 손 전환 (왼손 ↔ 오른손)\n" +
                "1: 왼손 레이 조정 모드\n" +
                "2: 오른손 레이 조정 모드\n" +
                "R: 레이 방향 리셋 (조정 모드 중)\n" +
                "E: 상호작용 (레거시)\n" +
                "F: 손전등 토글\n" +
                "Q: 숨기 해제\n" +
                "ESC: 커서 잠금 해제",
                MessageType.None);
        }

        private void CreatePCTestPlayer()
        {
            // 기존 플레이어 확인
            var existingPlayer = GameObject.Find("PCTestPlayer");
            if (existingPlayer != null)
            {
                if (!EditorUtility.DisplayDialog("확인",
                    "이미 PC 테스트 플레이어가 있습니다. 삭제하고 새로 생성하시겠습니까?",
                    "예", "아니오"))
                {
                    return;
                }
                DestroyImmediate(existingPlayer);
            }

            // XR Origin이 있으면 비활성화할지 물어봄
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                if (EditorUtility.DisplayDialog("XR Origin 감지",
                    "씬에 XR Origin이 있습니다. 비활성화하시겠습니까?\n" +
                    "(PC 테스트 시 충돌 방지)",
                    "비활성화", "유지"))
                {
                    xrOrigin.gameObject.SetActive(false);
                    Debug.Log("[PCPlayerSetup] XR Origin 비활성화됨");
                }
            }

            // XR Interaction Manager 확인/생성
            EnsureXRInteractionManager();

            // 루트 오브젝트 생성
            GameObject playerRoot = new GameObject("PCTestPlayer");
            playerRoot.tag = "Player";
            Undo.RegisterCreatedObjectUndo(playerRoot, "Create PC Test Player");

            // CharacterController 추가
            CharacterController controller = playerRoot.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0, 0.9f, 0);

            // 카메라 홀더 생성
            GameObject cameraHolder = new GameObject("CameraHolder");
            cameraHolder.transform.SetParent(playerRoot.transform);
            cameraHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

            // 메인 카메라 생성
            GameObject cameraObj = new GameObject("MainCamera");
            cameraObj.transform.SetParent(cameraHolder.transform);
            cameraObj.transform.localPosition = Vector3.zero;
            cameraObj.transform.localRotation = Quaternion.identity;
            cameraObj.tag = "MainCamera";

            Camera camera = cameraObj.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 70f;

            // AudioListener 추가
            cameraObj.AddComponent<AudioListener>();

            // 손전등 추가
            if (addFlashlight)
            {
                SetupFlashlight(cameraObj);
            }

            // PC 컨트롤러 스크립트 추가
            // (양손 컨트롤러는 PCPlayerController에서 자동 생성됨)
            var pcController = playerRoot.AddComponent<PCPlayerController>();

            // 컴포넌트 설정은 PCPlayerController.Start()에서 자동 처리됨

            // XR Device Simulator 추가
            if (addXRDeviceSimulator)
            {
                AddXRDeviceSimulator();
            }

            // 위치 설정
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                playerRoot.transform.position = sceneView.pivot + Vector3.up * 0.1f;
            }

            Selection.activeGameObject = playerRoot;
            Debug.Log("[PCPlayerSetup] PC 테스트 플레이어 생성 완료");

            EditorUtility.DisplayDialog("완료",
                "PC 테스트 플레이어가 생성되었습니다.\n\n" +
                "Play 모드에서 WASD로 이동, 마우스로 시점 조작하세요.\n" +
                "E키 또는 좌클릭으로 오브젝트와 상호작용합니다.",
                "확인");
        }

        /// <summary>
        /// 손전등 설정
        /// </summary>
        private void SetupFlashlight(GameObject cameraObj)
        {
            GameObject flashlightObj = new GameObject("Flashlight");
            flashlightObj.transform.SetParent(cameraObj.transform);
            flashlightObj.transform.localPosition = new Vector3(0.2f, -0.1f, 0.3f);
            flashlightObj.transform.localRotation = Quaternion.identity;

            Light flashlight = flashlightObj.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.spotAngle = 45f;
            flashlight.innerSpotAngle = 25f;
            flashlight.range = 20f;
            flashlight.intensity = 2f;
            flashlight.color = new Color(1f, 0.95f, 0.85f);
            flashlight.shadows = LightShadows.Soft;
            flashlight.enabled = false; // 기본 꺼짐

            Debug.Log("[PCPlayerSetup] 손전등 추가됨");
        }

        /// <summary>
        /// XR Interaction Manager 확인/생성
        /// </summary>
        private void EnsureXRInteractionManager()
        {
            var existingManager = FindFirstObjectByType<XRInteractionManager>();
            if (existingManager != null)
            {
                Debug.Log("[PCPlayerSetup] XR Interaction Manager가 이미 존재합니다.");
                return;
            }

            GameObject managerObj = new GameObject("XRInteractionManager");
            managerObj.AddComponent<XRInteractionManager>();
            Undo.RegisterCreatedObjectUndo(managerObj, "Create XR Interaction Manager");
            Debug.Log("[PCPlayerSetup] XR Interaction Manager 생성됨");
        }

        /// <summary>
        /// XR Device Simulator 추가
        /// </summary>
        private void AddXRDeviceSimulator()
        {
            // XR Device Simulator 프리팹 찾기
            string[] guids = AssetDatabase.FindAssets("XR Device Simulator t:prefab");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("오류",
                    "XR Device Simulator 프리팹을 찾을 수 없습니다.\n\n" +
                    "Package Manager에서:\n" +
                    "1. XR Interaction Toolkit 선택\n" +
                    "2. Samples 탭 열기\n" +
                    "3. 'XR Device Simulator' Import 클릭",
                    "확인");
                return;
            }

            // 이미 존재하는지 확인
            var existingSimulator = FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
            if (existingSimulator != null)
            {
                EditorUtility.DisplayDialog("알림", "XR Device Simulator가 이미 씬에 있습니다.", "확인");
                Selection.activeGameObject = existingSimulator.gameObject;
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Add XR Device Simulator");
                Selection.activeGameObject = instance;
                Debug.Log("[PCPlayerSetup] XR Device Simulator 추가됨");
                EditorUtility.DisplayDialog("완료",
                    "XR Device Simulator가 추가되었습니다.\n\n" +
                    "Play 모드에서 VR 컨트롤러를 시뮬레이션할 수 있습니다.",
                    "확인");
            }
        }

        /// <summary>
        /// 빠른 PC 플레이어 생성 (메뉴)
        /// </summary>
        [MenuItem("Horror Game/씬에 PC 플레이어 빠른 생성", false, 201)]
        public static void QuickCreatePCPlayer()
        {
            var tool = CreateInstance<PCPlayerSetupTool>();
            tool.addFlashlight = true;
            tool.addDualHandControllers = true;
            tool.addXRDeviceSimulator = false;
            tool.CreatePCTestPlayer();
            DestroyImmediate(tool);
        }
    }
}
