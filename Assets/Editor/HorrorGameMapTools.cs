using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.AI;
using System.Collections.Generic;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 공포 게임 맵 제작 도구
    /// 디자이너를 위한 NavMesh 설정 및 게임 오브젝트 배치 도구
    /// </summary>
    public class HorrorGameMapTools : EditorWindow
    {
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabNames = { "NavMesh", "적 AI", "상호작용", "이벤트", "빠른 설정" };

        // NavMesh 설정
        private float agentRadius = 0.5f;
        private float agentHeight = 2f;
        private float maxSlope = 45f;
        private float stepHeight = 0.4f;

        // 순찰 지점
        private List<Transform> patrolPoints = new List<Transform>();
        private GameObject selectedKiller;

        [MenuItem("Horror Game/맵 제작 도구", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<HorrorGameMapTools>("맵 제작 도구");
            window.minSize = new Vector2(400, 500);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 탭 선택
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space(10);

            switch (selectedTab)
            {
                case 0:
                    DrawNavMeshTab();
                    break;
                case 1:
                    DrawEnemyTab();
                    break;
                case 2:
                    DrawInteractionTab();
                    break;
                case 3:
                    DrawEventTab();
                    break;
                case 4:
                    DrawQuickSetupTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        #region NavMesh Tab
        private void DrawNavMeshTab()
        {
            GUILayout.Label("NavMesh 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "NavMesh는 AI가 이동할 수 있는 영역을 정의합니다.\n" +
                "맵을 완성한 후 NavMesh를 Bake해야 살인마 AI가 제대로 움직입니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Agent 설정
            GUILayout.Label("Agent 설정 (살인마 크기)", EditorStyles.boldLabel);
            agentRadius = EditorGUILayout.FloatField("반경", agentRadius);
            agentHeight = EditorGUILayout.FloatField("높이", agentHeight);
            maxSlope = EditorGUILayout.Slider("최대 경사", maxSlope, 0, 60);
            stepHeight = EditorGUILayout.FloatField("계단 높이", stepHeight);

            EditorGUILayout.Space(5);

            // Static 설정 안내
            EditorGUILayout.HelpBox(
                "NavMesh에 포함할 오브젝트:\n" +
                "1. 바닥, 벽 등 이동 가능한 지형\n" +
                "2. 문, 가구 등 장애물\n\n" +
                "해당 오브젝트를 선택하고 Inspector에서\n" +
                "Static > Navigation Static을 체크하세요.",
                MessageType.None);

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택 오브젝트 Navigation Static 설정"))
            {
                SetNavigationStatic(true);
            }
            if (GUILayout.Button("해제"))
            {
                SetNavigationStatic(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Bake 버튼
            GUILayout.Label("NavMesh Bake", EditorStyles.boldLabel);

            if (GUILayout.Button("Navigation 창 열기", GUILayout.Height(30)))
            {
                EditorApplication.ExecuteMenuItem("Window/AI/Navigation");
            }

            EditorGUILayout.HelpBox(
                "NavMesh Bake 순서:\n" +
                "1. Navigation 창에서 'Bake' 탭 선택\n" +
                "2. Agent 설정 확인 (위 값 참고)\n" +
                "3. 'Bake' 버튼 클릭\n" +
                "4. 파란색 영역이 표시되면 성공!",
                MessageType.None);

            EditorGUILayout.Space(10);

            // NavMesh Area 설명
            GUILayout.Label("NavMesh Area (고급)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "특정 영역을 다르게 설정할 수 있습니다:\n" +
                "- Walkable: 기본 이동 가능\n" +
                "- Not Walkable: 이동 불가\n" +
                "- Jump: 점프 필요 (설정 시)\n\n" +
                "문을 'Not Walkable'로 설정하면\n" +
                "열리기 전까지 AI가 통과하지 못합니다.",
                MessageType.None);
        }

        private void SetNavigationStatic(bool isStatic)
        {
            foreach (var go in Selection.gameObjects)
            {
                GameObjectUtility.SetStaticEditorFlags(go,
                    isStatic
                        ? GameObjectUtility.GetStaticEditorFlags(go) | StaticEditorFlags.NavigationStatic
                        : GameObjectUtility.GetStaticEditorFlags(go) & ~StaticEditorFlags.NavigationStatic);
                EditorUtility.SetDirty(go);
            }
            Debug.Log($"[맵 도구] {Selection.gameObjects.Length}개 오브젝트의 Navigation Static을 {(isStatic ? "설정" : "해제")}했습니다.");
        }
        #endregion

        #region Enemy Tab
        private void DrawEnemyTab()
        {
            GUILayout.Label("살인마 AI 설정", EditorStyles.boldLabel);

            // 살인마 생성
            if (GUILayout.Button("살인마 생성", GUILayout.Height(30)))
            {
                CreateKiller();
            }

            EditorGUILayout.Space(10);

            // 순찰 지점
            GUILayout.Label("순찰 지점 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "살인마가 돌아다닐 경로를 설정합니다.\n" +
                "순찰 지점을 여러 개 배치하면 AI가 순서대로 이동합니다.",
                MessageType.Info);

            selectedKiller = EditorGUILayout.ObjectField("대상 살인마", selectedKiller, typeof(GameObject), true) as GameObject;

            if (GUILayout.Button("순찰 지점 생성"))
            {
                CreatePatrolPoint();
            }

            if (selectedKiller != null)
            {
                var killer = selectedKiller.GetComponent<KillerAI>();
                if (killer != null && killer.patrolPoints != null)
                {
                    EditorGUILayout.LabelField($"현재 순찰 지점: {killer.patrolPoints.Length}개");
                }

                if (GUILayout.Button("씬의 PatrolPoint를 살인마에 연결"))
                {
                    AssignPatrolPoints();
                }
            }

            EditorGUILayout.Space(10);

            // AI 설정 가이드
            GUILayout.Label("AI 설정 가이드", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "KillerAI 컴포넌트 주요 설정:\n\n" +
                "• View Distance: 시야 거리 (기본 15m)\n" +
                "• View Angle: 시야 각도 (기본 90도)\n" +
                "• Hearing Range: 소리 감지 거리 (기본 10m)\n" +
                "• Chase Speed: 추적 속도 (기본 4.5)\n" +
                "• Catch Distance: 잡기 거리 (기본 1.5m)\n\n" +
                "난이도 조절:\n" +
                "쉬움: 시야/청각 범위 줄이기, 속도 낮추기\n" +
                "어려움: 범위 넓히기, 속도 높이기",
                MessageType.None);
        }

        private void CreateKiller()
        {
            // 기존 살인마 확인
            var existing = Object.FindObjectOfType<KillerAI>();
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog("살인마 추가",
                    "이미 씬에 살인마가 있습니다. 추가로 생성하시겠습니까?",
                    "생성", "취소"))
                {
                    return;
                }
            }

            GameObject killer = new GameObject("Killer");
            killer.transform.position = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;

            // NavMeshAgent 추가
            var agent = killer.AddComponent<NavMeshAgent>();
            agent.speed = 4.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 1f;

            // KillerAI 추가
            killer.AddComponent<KillerAI>();

            // 임시 비주얼
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Visual";
            capsule.transform.SetParent(killer.transform);
            capsule.transform.localPosition = Vector3.up;
            capsule.GetComponent<Collider>().enabled = false;

            // 색상 변경
            var renderer = capsule.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.red;

            Selection.activeGameObject = killer;
            selectedKiller = killer;

            Debug.Log("[맵 도구] 살인마가 생성되었습니다. 모델을 교체하고 애니메이션을 추가하세요.");
        }

        private void CreatePatrolPoint()
        {
            // PatrolPoints 부모 찾기 또는 생성
            var parent = GameObject.Find("PatrolPoints");
            if (parent == null)
            {
                parent = new GameObject("PatrolPoints");
            }

            int count = parent.transform.childCount + 1;
            var point = new GameObject($"PatrolPoint_{count}");
            point.transform.SetParent(parent.transform);
            point.transform.position = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;

            // 아이콘을 위한 컴포넌트
            var icon = point.AddComponent<PatrolPointGizmo>();

            Selection.activeGameObject = point;
            Debug.Log($"[맵 도구] 순찰 지점 {count}이 생성되었습니다.");
        }

        private void AssignPatrolPoints()
        {
            if (selectedKiller == null) return;

            var killer = selectedKiller.GetComponent<KillerAI>();
            if (killer == null) return;

            var parent = GameObject.Find("PatrolPoints");
            if (parent == null)
            {
                Debug.LogWarning("[맵 도구] PatrolPoints 오브젝트가 없습니다.");
                return;
            }

            var points = new List<Transform>();
            foreach (Transform child in parent.transform)
            {
                points.Add(child);
            }

            killer.patrolPoints = points.ToArray();
            EditorUtility.SetDirty(killer);

            Debug.Log($"[맵 도구] {points.Count}개의 순찰 지점이 연결되었습니다.");
        }
        #endregion

        #region Interaction Tab
        private void DrawInteractionTab()
        {
            GUILayout.Label("상호작용 오브젝트", EditorStyles.boldLabel);

            // 문
            GUILayout.Label("문", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("일반 문"))
            {
                CreateDoor(false);
            }
            if (GUILayout.Button("잠긴 문"))
            {
                CreateDoor(true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 열쇠
            GUILayout.Label("열쇠", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("열쇠 생성"))
            {
                CreateKey();
            }

            EditorGUILayout.Space(5);

            // 숨는 장소
            GUILayout.Label("숨는 장소", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("옷장"))
            {
                CreateHidingSpot("Wardrobe");
            }
            if (GUILayout.Button("침대 밑"))
            {
                CreateHidingSpot("UnderBed");
            }
            if (GUILayout.Button("사물함"))
            {
                CreateHidingSpot("Locker");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 탈출구
            GUILayout.Label("탈출구", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("탈출구 생성"))
            {
                CreateEscapeZone();
            }

            EditorGUILayout.Space(5);

            // 메모
            GUILayout.Label("메모/문서", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("읽을 수 있는 메모 생성"))
            {
                CreateReadableNote();
            }

            EditorGUILayout.Space(10);

            // 설정 가이드
            EditorGUILayout.HelpBox(
                "문과 열쇠 연결 방법:\n" +
                "1. 잠긴 문의 'Required Key Id' 설정 (예: key_01)\n" +
                "2. 열쇠의 'Key Id'를 동일하게 설정\n" +
                "3. 플레이어가 열쇠를 먼저 획득해야 문이 열림",
                MessageType.Info);
        }

        private void CreateDoor(bool locked)
        {
            var door = new GameObject(locked ? "LockedDoor" : "Door");
            door.transform.position = GetSceneViewPosition();

            // 문 비주얼
            var doorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorVisual.name = "DoorMesh";
            doorVisual.transform.SetParent(door.transform);
            doorVisual.transform.localPosition = new Vector3(0.5f, 1f, 0);
            doorVisual.transform.localScale = new Vector3(1f, 2f, 0.1f);

            var doorComp = door.AddComponent<Door>();
            doorComp.isLocked = locked;
            if (locked)
            {
                doorComp.requiredKeyId = "key_" + Random.Range(1, 100).ToString("00");
            }

            Selection.activeGameObject = door;
            Debug.Log($"[맵 도구] {(locked ? "잠긴 문" : "문")}이 생성되었습니다.");
        }

        private void CreateKey()
        {
            var key = new GameObject("Key");
            key.transform.position = GetSceneViewPosition();

            var keyVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            keyVisual.name = "KeyMesh";
            keyVisual.transform.SetParent(key.transform);
            keyVisual.transform.localPosition = Vector3.zero;
            keyVisual.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);

            var keyComp = key.AddComponent<KeyItem>();
            keyComp.keyId = "key_" + Random.Range(1, 100).ToString("00");

            Selection.activeGameObject = key;
            Debug.Log($"[맵 도구] 열쇠가 생성되었습니다. Key ID: {keyComp.keyId}");
        }

        private void CreateHidingSpot(string type)
        {
            var hiding = new GameObject($"HidingSpot_{type}");
            hiding.transform.position = GetSceneViewPosition();

            var collider = hiding.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1f, 2f, 1f);

            hiding.AddComponent<HidingSpot>();

            Selection.activeGameObject = hiding;
            Debug.Log($"[맵 도구] 숨는 장소 ({type})가 생성되었습니다. 가구 모델을 추가하세요.");
        }

        private void CreateEscapeZone()
        {
            var escape = new GameObject("EscapeZone");
            escape.transform.position = GetSceneViewPosition();

            var collider = escape.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(3f, 3f, 1f);

            escape.AddComponent<EscapeZone>();

            Selection.activeGameObject = escape;
            Debug.Log("[맵 도구] 탈출구가 생성되었습니다.");
        }

        private void CreateReadableNote()
        {
            var note = new GameObject("ReadableNote");
            note.transform.position = GetSceneViewPosition();

            var noteVisual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            noteVisual.name = "NoteMesh";
            noteVisual.transform.SetParent(note.transform);
            noteVisual.transform.localPosition = Vector3.zero;
            noteVisual.transform.localScale = new Vector3(0.2f, 0.3f, 1f);

            note.AddComponent<ReadableNote>();

            Selection.activeGameObject = note;
            Debug.Log("[맵 도구] 읽을 수 있는 메모가 생성되었습니다.");
        }
        #endregion

        #region Event Tab
        private void DrawEventTab()
        {
            GUILayout.Label("이벤트 시스템", EditorStyles.boldLabel);

            // 체크포인트
            GUILayout.Label("체크포인트", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("체크포인트 생성"))
            {
                CreateCheckpoint();
            }

            EditorGUILayout.Space(5);

            // 랜덤 이벤트
            GUILayout.Label("랜덤 이벤트", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("랜덤 이벤트 트리거 생성"))
            {
                CreateRandomEventTrigger();
            }

            EditorGUILayout.Space(5);

            // 점프스케어
            GUILayout.Label("점프스케어", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("점프스케어 트리거 생성"))
            {
                CreateJumpScare();
            }

            EditorGUILayout.Space(5);

            // 조명 깜빡임
            GUILayout.Label("조명 효과", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("깜빡이는 조명 생성"))
            {
                CreateFlickeringLight();
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "이벤트 트리거 사용법:\n\n" +
                "1. 트리거 영역 크기 조절\n" +
                "2. 발생 확률 설정 (0~1)\n" +
                "3. 쿨다운 시간 설정\n" +
                "4. Events에서 실행할 동작 연결\n\n" +
                "예: 특정 구역 진입 시 조명 깜빡임",
                MessageType.Info);
        }

        private void CreateCheckpoint()
        {
            var checkpoint = new GameObject("Checkpoint");
            checkpoint.transform.position = GetSceneViewPosition();

            var collider = checkpoint.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(3f, 3f, 3f);

            var cp = checkpoint.AddComponent<Checkpoint>();
            cp.checkpointId = "checkpoint_" + Random.Range(1, 100).ToString("00");

            Selection.activeGameObject = checkpoint;
            Debug.Log($"[맵 도구] 체크포인트가 생성되었습니다. ID: {cp.checkpointId}");
        }

        private void CreateRandomEventTrigger()
        {
            var trigger = new GameObject("RandomEventTrigger");
            trigger.transform.position = GetSceneViewPosition();

            var collider = trigger.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(5f, 3f, 5f);

            trigger.AddComponent<RandomEventTrigger>();

            Selection.activeGameObject = trigger;
            Debug.Log("[맵 도구] 랜덤 이벤트 트리거가 생성되었습니다.");
        }

        private void CreateJumpScare()
        {
            var jumpscare = new GameObject("JumpScare");
            jumpscare.transform.position = GetSceneViewPosition();

            var collider = jumpscare.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2f, 2f, 2f);

            jumpscare.AddComponent<JumpScare>();

            Selection.activeGameObject = jumpscare;
            Debug.Log("[맵 도구] 점프스케어 트리거가 생성되었습니다.");
        }

        private void CreateFlickeringLight()
        {
            var lightObj = new GameObject("FlickeringLight");
            lightObj.transform.position = GetSceneViewPosition() + Vector3.up * 2;

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 1.5f;

            var flicker = lightObj.AddComponent<LightFlicker>();
            flicker.flickerType = LightFlicker.FlickerType.Random;

            Selection.activeGameObject = lightObj;
            Debug.Log("[맵 도구] 깜빡이는 조명이 생성되었습니다.");
        }
        #endregion

        #region Quick Setup Tab
        private void DrawQuickSetupTab()
        {
            GUILayout.Label("빠른 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "게임에 필요한 핵심 시스템을 한 번에 설정합니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 게임 매니저
            GUILayout.Label("필수 시스템", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("게임 매니저 생성", GUILayout.Height(30)))
            {
                CreateGameManager();
            }

            EditorGUILayout.Space(5);

            // 플레이어 설정
            if (GUILayout.Button("VR 플레이어 컴포넌트 추가", GUILayout.Height(30)))
            {
                SetupVRPlayer();
            }

            EditorGUILayout.Space(10);

            // 체크 시스템
            GUILayout.Label("필수 요소 체크", EditorStyles.miniBoldLabel);

            bool hasGameManager = Object.FindObjectOfType<HorrorGameManager>() != null;
            bool hasPlayer = Object.FindObjectOfType<VRPlayer>() != null;
            bool hasKiller = Object.FindObjectOfType<KillerAI>() != null;
            bool hasEscape = Object.FindObjectOfType<EscapeZone>() != null;
            bool hasKeys = Object.FindObjectOfType<KeyItem>() != null;

            DrawCheckItem("게임 매니저", hasGameManager);
            DrawCheckItem("VR 플레이어", hasPlayer);
            DrawCheckItem("살인마 AI", hasKiller);
            DrawCheckItem("탈출구", hasEscape);
            DrawCheckItem("열쇠", hasKeys);

            EditorGUILayout.Space(10);

            // NavMesh 체크
            var navMesh = NavMesh.CalculateTriangulation();
            bool hasNavMesh = navMesh.vertices.Length > 0;
            DrawCheckItem("NavMesh", hasNavMesh);

            if (!hasNavMesh)
            {
                EditorGUILayout.HelpBox(
                    "NavMesh가 없습니다!\n" +
                    "살인마 AI가 움직이려면 NavMesh가 필요합니다.\n" +
                    "'NavMesh' 탭에서 설정하세요.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // 전체 검증
            if (GUILayout.Button("게임 설정 검증", GUILayout.Height(30)))
            {
                ValidateGameSetup();
            }
        }

        private void DrawCheckItem(string name, bool isOk)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(isOk ? "✓" : "✗", GUILayout.Width(20));
            GUILayout.Label(name);
            GUILayout.Label(isOk ? "완료" : "필요", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        private void CreateGameManager()
        {
            var existing = Object.FindObjectOfType<HorrorGameManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[맵 도구] 게임 매니저가 이미 존재합니다.");
                return;
            }

            var manager = new GameObject("GameManager");
            manager.AddComponent<HorrorGameManager>();
            manager.AddComponent<ObjectiveSystem>();
            manager.AddComponent<CheckpointSystem>();

            Selection.activeGameObject = manager;
            Debug.Log("[맵 도구] 게임 매니저가 생성되었습니다.");
        }

        private void SetupVRPlayer()
        {
            // XR Origin 찾기
            var xrOrigin = Object.FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                EditorUtility.DisplayDialog("오류",
                    "XR Origin이 없습니다.\n" +
                    "GameObject > XR > XR Origin (VR)을 먼저 추가하세요.",
                    "확인");
                return;
            }

            // VRPlayer 추가
            var vrPlayer = xrOrigin.GetComponent<VRPlayer>();
            if (vrPlayer == null)
            {
                vrPlayer = xrOrigin.gameObject.AddComponent<VRPlayer>();
            }

            // PlayerInventory 추가
            var inventory = xrOrigin.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                inventory = xrOrigin.gameObject.AddComponent<PlayerInventory>();
            }

            // FootstepSystem 추가
            var footstep = xrOrigin.GetComponent<FootstepSystem>();
            if (footstep == null)
            {
                footstep = xrOrigin.gameObject.AddComponent<FootstepSystem>();
            }

            // BreathingSystem 추가
            var breathing = xrOrigin.GetComponent<BreathingSystem>();
            if (breathing == null)
            {
                breathing = xrOrigin.gameObject.AddComponent<BreathingSystem>();
            }

            Selection.activeGameObject = xrOrigin.gameObject;
            Debug.Log("[맵 도구] VR 플레이어 컴포넌트가 추가되었습니다.");
        }

        private void ValidateGameSetup()
        {
            var issues = new List<string>();

            // 필수 요소 체크
            if (Object.FindObjectOfType<HorrorGameManager>() == null)
                issues.Add("- 게임 매니저가 없습니다.");

            if (Object.FindObjectOfType<VRPlayer>() == null)
                issues.Add("- VR 플레이어가 없습니다.");

            if (Object.FindObjectOfType<KillerAI>() == null)
                issues.Add("- 살인마 AI가 없습니다.");

            if (Object.FindObjectOfType<EscapeZone>() == null)
                issues.Add("- 탈출구가 없습니다.");

            // 열쇠/문 매칭 체크
            var keys = Object.FindObjectsOfType<KeyItem>();
            var doors = Object.FindObjectsOfType<Door>();

            foreach (var door in doors)
            {
                if (door.isLocked && !string.IsNullOrEmpty(door.requiredKeyId))
                {
                    bool hasMatchingKey = false;
                    foreach (var key in keys)
                    {
                        if (key.keyId == door.requiredKeyId)
                        {
                            hasMatchingKey = true;
                            break;
                        }
                    }
                    if (!hasMatchingKey)
                    {
                        issues.Add($"- 문 '{door.name}'에 맞는 열쇠가 없습니다. (ID: {door.requiredKeyId})");
                    }
                }
            }

            // NavMesh 체크
            var navMesh = NavMesh.CalculateTriangulation();
            if (navMesh.vertices.Length == 0)
                issues.Add("- NavMesh가 Bake되지 않았습니다.");

            // 순찰 지점 체크
            var killer = Object.FindObjectOfType<KillerAI>();
            if (killer != null && (killer.patrolPoints == null || killer.patrolPoints.Length == 0))
                issues.Add("- 살인마의 순찰 지점이 설정되지 않았습니다.");

            // 결과 표시
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("검증 완료",
                    "모든 게임 설정이 올바릅니다!\n게임을 테스트할 준비가 되었습니다.",
                    "확인");
            }
            else
            {
                string message = "다음 문제를 해결하세요:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("문제 발견", message, "확인");
            }
        }
        #endregion

        private Vector3 GetSceneViewPosition()
        {
            if (SceneView.lastActiveSceneView != null)
            {
                return SceneView.lastActiveSceneView.pivot;
            }
            return Vector3.zero;
        }
    }

    /// <summary>
    /// 순찰 지점 시각화를 위한 Gizmo 컴포넌트
    /// </summary>
    public class PatrolPointGizmo : MonoBehaviour
    {
        public Color gizmoColor = Color.yellow;
        public float gizmoSize = 0.5f;

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, gizmoSize);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.position, gizmoSize);
        }
    }
}
