using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using HorrorGame;

namespace HorrorGame.Editor
{
    /// <summary>
    /// VR 프로젝트 초기 설정을 도와주는 에디터 도구
    /// Meta Quest 3 + PC (Quest Link) 전용
    /// </summary>
    public class VRProjectSetup : EditorWindow
    {
        [MenuItem("VR Game/프로젝트 설정 도우미", false, 0)]
        public static void ShowWindow()
        {
            GetWindow<VRProjectSetup>("VR 프로젝트 설정");
        }

        [MenuItem("VR Game/Quest 3 빠른 설정", false, 1)]
        public static void QuickSetupQuest3()
        {
            EditorUtility.DisplayDialog(
                "Meta Quest 3 설정 가이드",
                "Quest 3 + PC (Quest Link) 설정 순서:\n\n" +
                "1. Edit > Project Settings > XR Plug-in Management\n" +
                "   - Windows 탭: OpenXR 체크\n\n" +
                "2. XR Plug-in Management > OpenXR 클릭\n" +
                "   - Interaction Profiles에서 + 클릭\n" +
                "   - 'Oculus Touch Controller Profile' 추가\n\n" +
                "3. OpenXR > Features 에서:\n" +
                "   - Meta Quest Support 체크\n" +
                "   - Hand Tracking Subsystem 체크 (선택)\n\n" +
                "4. PC에서 Meta Quest 앱 실행\n" +
                "5. Quest 3에서 Quest Link 연결\n" +
                "6. Unity Play 모드 실행!",
                "확인"
            );

            // XR Plugin Management 설정 창 열기
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
        }

        [MenuItem("VR Game/새 VR 씬 생성", false, 2)]
        public static void CreateNewVRScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            EditorUtility.DisplayDialog(
                "VR 씬 생성됨",
                "새 VR 씬이 생성되었습니다.\n\n" +
                "다음 단계:\n" +
                "1. GameObject > XR > XR Origin (VR) 추가\n" +
                "2. 바닥 Plane 추가 (3D Object > Plane)\n" +
                "3. 바닥에 Teleportation Area 컴포넌트 추가\n\n" +
                "또는 'VR Game > 기본 VR 오브젝트 생성' 메뉴를 사용하세요.",
                "확인"
            );
        }

        [MenuItem("VR Game/기본 VR 오브젝트 생성/바닥 (Teleport 가능)", false, 10)]
        public static void CreateTeleportFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2, 1, 2);

            Debug.Log("[VR Setup] 바닥이 생성되었습니다. Teleportation Area 컴포넌트를 추가하세요.");
            Selection.activeGameObject = floor;
        }

        [MenuItem("VR Game/기본 VR 오브젝트 생성/잡을 수 있는 큐브", false, 11)]
        public static void CreateGrabbableCube()
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "GrabbableCube";
            cube.transform.position = new Vector3(0, 1, 0.5f);
            cube.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            var rb = cube.AddComponent<Rigidbody>();
            rb.useGravity = true;

            cube.AddComponent<GrabbableObject>();

            Selection.activeGameObject = cube;
            Debug.Log("[VR Setup] 잡을 수 있는 큐브가 생성되었습니다.");
        }

        [MenuItem("VR Game/기본 VR 오브젝트 생성/VR 버튼", false, 12)]
        public static void CreateVRButton()
        {
            GameObject buttonBase = new GameObject("VR Button");
            buttonBase.transform.position = new Vector3(0, 1, 0.3f);

            GameObject buttonFace = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            buttonFace.name = "Button Face";
            buttonFace.transform.SetParent(buttonBase.transform);
            buttonFace.transform.localPosition = Vector3.zero;
            buttonFace.transform.localScale = new Vector3(0.1f, 0.02f, 0.1f);
            buttonFace.transform.localRotation = Quaternion.identity;

            var collider = buttonFace.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            buttonFace.AddComponent<VRButton>();

            Selection.activeGameObject = buttonBase;
            Debug.Log("[VR Setup] VR 버튼이 생성되었습니다.");
        }

        [MenuItem("VR Game/기본 VR 오브젝트 생성/Quest Link 매니저", false, 20)]
        public static void CreateQuestLinkManager()
        {
            // 이미 있는지 확인
            var existing = Object.FindObjectOfType<QuestLinkManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[VR Setup] Quest Link Manager가 이미 존재합니다.");
                return;
            }

            GameObject manager = new GameObject("Quest Link Manager");
            manager.AddComponent<QuestLinkManager>();

            Selection.activeGameObject = manager;
            Debug.Log("[VR Setup] Quest Link Manager가 생성되었습니다.");
        }

        private Vector2 scrollPosition;

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 헤더
            GUILayout.Label("Meta Quest 3 + PC 설정 가이드", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Quest Link를 사용하여 PC에 연결된 Quest 3로 VR 개발",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Quest Link 연결 전 준비
            DrawSection("0. Quest Link 연결 전 준비 (PC)", @"
1. PC에 Meta Quest 앱 설치
   - https://www.meta.com/quest/setup/

2. Meta Quest 앱 실행 및 로그인

3. Quest 3를 USB-C 케이블로 PC에 연결
   (또는 Air Link 사용)", "Meta Quest 앱 다운로드 페이지", "https://www.meta.com/quest/setup/");

            // 1. XR Plugin Management
            DrawSection("1. XR Plugin Management 설정", @"
Edit > Project Settings > XR Plug-in Management

[Windows 탭]
✅ OpenXR 체크

[OpenXR 하위 메뉴]
1. Interaction Profiles 에서 + 클릭
2. 'Oculus Touch Controller Profile' 선택

[OpenXR > Features]
✅ Meta Quest Support
✅ Hand Tracking Subsystem (핸드트래킹 사용 시)");

            if (GUILayout.Button("XR Plug-in Management 열기"))
            {
                SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
            }

            EditorGUILayout.Space(5);

            // 2. Input System
            DrawSection("2. Input System 설정", @"
Edit > Project Settings > Player

[Other Settings 섹션]
- Active Input Handling: Both

* 변경 후 Unity 재시작 필요!");

            if (GUILayout.Button("Player Settings 열기"))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }

            EditorGUILayout.Space(5);

            // 3. XR Interaction Toolkit Samples
            DrawSection("3. 필수 샘플 설치", @"
Window > Package Manager

[XR Interaction Toolkit 선택]
Samples 탭에서:
✅ Starter Assets (필수!)
✅ XR Device Simulator (헤드셋 없이 테스트용)

[XR Hands 선택] (핸드트래킹 사용 시)
Samples 탭에서:
✅ HandVisualizer");

            if (GUILayout.Button("Package Manager 열기"))
            {
                UnityEditor.PackageManager.UI.Window.Open("com.unity.xr.interaction.toolkit");
            }

            EditorGUILayout.Space(5);

            // 4. 씬 설정
            DrawSection("4. VR 씬 구성", @"
씬에 필요한 오브젝트:

1. XR Origin (VR)
   GameObject > XR > XR Origin (VR)

2. 바닥
   GameObject > 3D Object > Plane
   + Add Component > Teleportation Area

3. (선택) Quest Link Manager
   VR Game > 기본 VR 오브젝트 생성 > Quest Link 매니저");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("새 VR 씬 생성"))
            {
                CreateNewVRScene();
            }
            if (GUILayout.Button("Quest Link 매니저 추가"))
            {
                CreateQuestLinkManager();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 5. 테스트
            DrawSection("5. Quest Link로 테스트", @"
1. PC에서 Meta Quest 앱 실행 확인
2. Quest 3 착용
3. Quest 3에서 Quick Settings > Quest Link 선택
4. PC와 연결 확인
5. Unity에서 Play 모드 실행 (Ctrl+P)
6. Quest 3 화면에서 VR 확인!

[문제 해결]
- VR이 안 보이면: Quest Link 연결 상태 확인
- 컨트롤러 안 되면: OpenXR 프로필 확인
- 렉이 심하면: USB 3.0 케이블 사용");

            EditorGUILayout.Space(10);

            // Quest 3 컨트롤러 매핑
            GUILayout.Label("Quest 3 컨트롤러 버튼 매핑", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "왼손 컨트롤러:\n" +
                "- X 버튼: Primary Button\n" +
                "- Y 버튼: Secondary Button\n" +
                "- 트리거: Select/Activate\n" +
                "- 그립: Grab\n" +
                "- 썸스틱: 이동\n\n" +
                "오른손 컨트롤러:\n" +
                "- A 버튼: Primary Button\n" +
                "- B 버튼: Secondary Button\n" +
                "- 트리거: Select/Activate\n" +
                "- 그립: Grab\n" +
                "- 썸스틱: 회전",
                MessageType.None
            );

            EditorGUILayout.EndScrollView();
        }

        private void DrawSection(string title, string content, string linkText = null, string linkUrl = null)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(content.Trim(), MessageType.None);

            if (!string.IsNullOrEmpty(linkText) && !string.IsNullOrEmpty(linkUrl))
            {
                if (GUILayout.Button(linkText))
                {
                    Application.OpenURL(linkUrl);
                }
            }

            EditorGUILayout.Space(3);
        }
    }
}
