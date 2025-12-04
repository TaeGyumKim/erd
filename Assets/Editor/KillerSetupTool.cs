using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.IO;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 살인마(Killer) 프리팹 자동 설정 도구
    /// TG_Hero_Interactive_Prefab에 필요한 모든 컴포넌트를 자동으로 추가하고 설정합니다.
    /// </summary>
    public class KillerSetupTool : EditorWindow
    {
        // 프리팹 경로
        private const string HERO_PREFAB_PATH = "Assets/Character/TG_Hero_Interactive_Prefab.prefab";
        private const string KILLER_CONTROLLER_PATH = "Assets/Character/KillerAnimatorController.controller";

        // 설정 값
        private float patrolSpeed = 2f;
        private float chaseSpeed = 4.5f;
        private float searchSpeed = 3f;
        private float viewDistance = 15f;
        private float viewAngle = 90f;
        private float hearingRange = 10f;
        private float catchDistance = 1.5f;

        private bool createPatrolPoints = true;
        private int patrolPointCount = 4;
        private float patrolRadius = 10f;

        private Vector2 scrollPos;

        [MenuItem("Horror Game/살인마 설정 도구", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<KillerSetupTool>("살인마 설정");
            window.minSize = new Vector2(350, 500);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 헤더
            EditorGUILayout.Space(10);
            GUILayout.Label("살인마 자동 설정 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "TG_Hero_Interactive_Prefab을 살인마로 자동 설정합니다.\n" +
                "NavMeshAgent, KillerAI, KillerAnimator, KillerFootstep, KillerCatchSequence 컴포넌트가 추가됩니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // AI 설정
            EditorGUILayout.LabelField("AI 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            patrolSpeed = EditorGUILayout.FloatField("순찰 속도", patrolSpeed);
            chaseSpeed = EditorGUILayout.FloatField("추적 속도", chaseSpeed);
            searchSpeed = EditorGUILayout.FloatField("수색 속도", searchSpeed);

            EditorGUILayout.Space(5);

            viewDistance = EditorGUILayout.FloatField("시야 거리", viewDistance);
            viewAngle = EditorGUILayout.Slider("시야 각도", viewAngle, 30f, 180f);
            hearingRange = EditorGUILayout.FloatField("청각 범위", hearingRange);
            catchDistance = EditorGUILayout.FloatField("잡기 거리", catchDistance);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 순찰 지점 설정
            EditorGUILayout.LabelField("순찰 지점 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            createPatrolPoints = EditorGUILayout.Toggle("순찰 지점 자동 생성", createPatrolPoints);

            if (createPatrolPoints)
            {
                patrolPointCount = EditorGUILayout.IntSlider("순찰 지점 개수", patrolPointCount, 2, 10);
                patrolRadius = EditorGUILayout.FloatField("순찰 반경", patrolRadius);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(20);

            // 버튼들
            EditorGUILayout.LabelField("실행", EditorStyles.boldLabel);

            // 씬에 새로 생성
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("씬에 살인마 생성", GUILayout.Height(40)))
            {
                CreateKillerInScene();
            }

            EditorGUILayout.Space(5);

            // 선택된 오브젝트에 설정 적용
            GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            if (GUILayout.Button("선택된 오브젝트에 설정 적용", GUILayout.Height(35)))
            {
                SetupSelectedAsKiller();
            }

            EditorGUILayout.Space(5);

            // 프리팹 직접 수정
            GUI.backgroundColor = new Color(0.9f, 0.7f, 0.4f);
            if (GUILayout.Button("프리팹 원본 수정 (주의)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("프리팹 수정 확인",
                    "TG_Hero_Interactive_Prefab 원본을 수정합니다.\n계속하시겠습니까?",
                    "수정", "취소"))
                {
                    ModifyPrefabDirectly();
                }
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // 유틸리티
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);

            if (GUILayout.Button("NavMesh 베이크"))
            {
                BakeNavMesh();
            }

            if (GUILayout.Button("KillerAnimatorController 선택"))
            {
                SelectAnimatorController();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 씬에 살인마 생성
        /// </summary>
        private void CreateKillerInScene()
        {
            // 프리팹 로드
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HERO_PREFAB_PATH);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("오류",
                    $"프리팹을 찾을 수 없습니다:\n{HERO_PREFAB_PATH}", "확인");
                return;
            }

            // 씬에 인스턴스 생성
            GameObject killer = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            killer.name = "Killer";

            // 씬 뷰 중앙에 배치
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                killer.transform.position = sceneView.pivot;
            }

            // 컴포넌트 설정
            SetupKillerComponents(killer);

            // 순찰 지점 생성
            if (createPatrolPoints)
            {
                CreatePatrolPoints(killer);
            }

            // NavMesh 자동 베이크
            EnsureNavMeshExists();

            // 선택
            Selection.activeGameObject = killer;
            Undo.RegisterCreatedObjectUndo(killer, "Create Killer");

            Debug.Log("[KillerSetupTool] 살인마가 성공적으로 생성되었습니다!");
            EditorUtility.DisplayDialog("완료", "살인마가 씬에 생성되었습니다!\nNavMesh도 자동으로 베이크되었습니다.", "확인");
        }

        /// <summary>
        /// 선택된 오브젝트를 살인마로 설정
        /// </summary>
        private void SetupSelectedAsKiller()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("오류", "오브젝트를 선택해주세요.", "확인");
                return;
            }

            Undo.RecordObject(selected, "Setup as Killer");
            SetupKillerComponents(selected);

            if (createPatrolPoints)
            {
                CreatePatrolPoints(selected);
            }

            // NavMesh 자동 베이크
            EnsureNavMeshExists();

            Debug.Log($"[KillerSetupTool] '{selected.name}'이(가) 살인마로 설정되었습니다!");
            EditorUtility.DisplayDialog("완료", $"'{selected.name}'이(가) 살인마로 설정되었습니다!\nNavMesh도 자동으로 베이크되었습니다.", "확인");
        }

        /// <summary>
        /// 프리팹 원본 직접 수정
        /// </summary>
        private void ModifyPrefabDirectly()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HERO_PREFAB_PATH);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("오류",
                    $"프리팹을 찾을 수 없습니다:\n{HERO_PREFAB_PATH}", "확인");
                return;
            }

            // 프리팹 편집 모드
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                SetupKillerComponents(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                Debug.Log("[KillerSetupTool] 프리팹이 성공적으로 수정되었습니다!");
                EditorUtility.DisplayDialog("완료", "프리팹이 수정되었습니다!", "확인");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        /// <summary>
        /// 살인마 컴포넌트 설정
        /// </summary>
        private void SetupKillerComponents(GameObject target)
        {
            // NavMeshAgent 설정
            NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = target.AddComponent<NavMeshAgent>();
            }
            agent.speed = patrolSpeed;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            agent.radius = 0.4f;
            agent.height = 1.8f;

            // KillerAI 설정
            KillerAI killerAI = target.GetComponent<KillerAI>();
            if (killerAI == null)
            {
                killerAI = target.AddComponent<KillerAI>();
            }
            killerAI.patrolSpeed = patrolSpeed;
            killerAI.chaseSpeed = chaseSpeed;
            killerAI.searchSpeed = searchSpeed;
            killerAI.viewDistance = viewDistance;
            killerAI.viewAngle = viewAngle;
            killerAI.hearingRange = hearingRange;
            killerAI.catchDistance = catchDistance;
            killerAI.patrolWaitTime = 2f;
            killerAI.chaseTimeout = 8f;
            killerAI.searchTime = 10f;
            killerAI.searchRadius = 5f;

            // HorrorGameManager 자동 생성 확인
            EnsureGameManagerExists();

            // KillerAnimator 설정
            KillerAnimator killerAnimator = target.GetComponent<KillerAnimator>();
            if (killerAnimator == null)
            {
                killerAnimator = target.AddComponent<KillerAnimator>();
            }
            killerAnimator.speedSmoothTime = 0.1f;
            killerAnimator.runSpeedThreshold = 3f;
            killerAnimator.turnSmoothTime = 0.1f;
            killerAnimator.patrolAnimSpeed = 0.8f;
            killerAnimator.searchAnimSpeed = 1f;
            killerAnimator.chaseAnimSpeed = 1.2f;
            killerAnimator.investigateAnimSpeed = 0.9f;

            // Animator Controller 설정
            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                animator = target.GetComponentInChildren<Animator>();
            }

            if (animator != null)
            {
                RuntimeAnimatorController controller =
                    AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KILLER_CONTROLLER_PATH);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    Debug.Log("[KillerSetupTool] Animator Controller가 설정되었습니다.");
                }
                else
                {
                    Debug.LogWarning($"[KillerSetupTool] Animator Controller를 찾을 수 없습니다: {KILLER_CONTROLLER_PATH}");
                }
            }

            // 참조 연결
            killerAnimator.animator = animator;
            killerAnimator.killerAI = killerAI;
            killerAnimator.agent = agent;

            // KillerFootstep 설정 (발소리 시스템)
            KillerFootstep killerFootstep = target.GetComponent<KillerFootstep>();
            if (killerFootstep == null)
            {
                killerFootstep = target.AddComponent<KillerFootstep>();
            }
            killerFootstep.killerAI = killerAI;
            killerFootstep.agent = agent;
            killerFootstep.walkStepInterval = 0.5f;
            killerFootstep.runStepInterval = 0.28f;
            killerFootstep.walkVolume = 0.4f;
            killerFootstep.runVolume = 0.7f;
            killerFootstep.runSpeedThreshold = 3f;

            // 발소리 에셋 자동 로드
            LoadFootstepClips(killerFootstep);

            // KillerCatchSequence 설정 (잡기 연출)
            KillerCatchSequence catchSequence = target.GetComponent<KillerCatchSequence>();
            if (catchSequence == null)
            {
                catchSequence = target.AddComponent<KillerCatchSequence>();
            }
            catchSequence.killerAI = killerAI;
            catchSequence.killerAnimator = killerAnimator;
            catchSequence.enableShake = true;
            catchSequence.shakeIntensity = 0.15f;
            catchSequence.shakeDuration = 2f;
            catchSequence.shakeCount = 4;

            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// 발소리 에셋 자동 로드 (One Shots 사용 - Loop 대신 개별 발소리)
        /// </summary>
        private void LoadFootstepClips(KillerFootstep footstep)
        {
            const string FOOTSTEP_PATH = "Assets/Footstep(Concrete & Wood)";

            // One Shots 폴더에서 발소리 로드 (Loop 대신 개별 발소리 사용)
            string oneShotsPath = $"{FOOTSTEP_PATH}/Footstep  One Shots/concrete";
            var oneShotClips = LoadAudioClipsFromFolder(oneShotsPath);

            if (oneShotClips != null && oneShotClips.Length > 0)
            {
                // 같은 클립을 걷기/뛰기 모두에 사용 (볼륨과 간격으로 구분)
                footstep.walkFootsteps = oneShotClips;
                footstep.runFootsteps = oneShotClips;
                Debug.Log($"[KillerSetupTool] 발소리 One Shots {oneShotClips.Length}개 로드됨");
            }
            else
            {
                Debug.LogWarning($"[KillerSetupTool] One Shots 발소리를 찾을 수 없습니다: {oneShotsPath}");
            }
        }

        /// <summary>
        /// 폴더에서 AudioClip 배열 로드
        /// </summary>
        private AudioClip[] LoadAudioClipsFromFolder(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
            if (guids.Length == 0) return null;

            var clips = new System.Collections.Generic.List<AudioClip>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            clips.Sort((a, b) => a.name.CompareTo(b.name));
            return clips.ToArray();
        }

        /// <summary>
        /// 순찰 지점 자동 생성
        /// </summary>
        private void CreatePatrolPoints(GameObject killer)
        {
            // 순찰 지점 부모 오브젝트
            GameObject patrolParent = new GameObject("PatrolPoints");
            patrolParent.transform.position = killer.transform.position;
            Undo.RegisterCreatedObjectUndo(patrolParent, "Create Patrol Points");

            Transform[] patrolPoints = new Transform[patrolPointCount];

            for (int i = 0; i < patrolPointCount; i++)
            {
                float angle = (360f / patrolPointCount) * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * patrolRadius,
                    0f,
                    Mathf.Sin(angle) * patrolRadius
                );

                GameObject point = new GameObject($"PatrolPoint_{i + 1}");
                point.transform.parent = patrolParent.transform;
                point.transform.position = killer.transform.position + offset;

                // 기즈모용 아이콘 설정
                var iconContent = EditorGUIUtility.IconContent("sv_icon_dot6_pix16_gizmo");
                if (iconContent != null && iconContent.image != null)
                {
                    // Unity 2021.2+ 에서만 작동
                    #if UNITY_2021_2_OR_NEWER
                    EditorGUIUtility.SetIconForObject(point, (Texture2D)iconContent.image);
                    #endif
                }

                patrolPoints[i] = point.transform;
            }

            // KillerAI에 순찰 지점 연결
            KillerAI killerAI = killer.GetComponent<KillerAI>();
            if (killerAI != null)
            {
                killerAI.patrolPoints = patrolPoints;
                EditorUtility.SetDirty(killerAI);
            }

            Debug.Log($"[KillerSetupTool] {patrolPointCount}개의 순찰 지점이 생성되었습니다.");
        }

        /// <summary>
        /// NavMesh 베이크 (NavMeshSurface 사용)
        /// </summary>
        private void BakeNavMesh()
        {
            // 기존 NavMeshSurface 찾기
            NavMeshSurface surface = FindObjectOfType<NavMeshSurface>();

            if (surface == null)
            {
                // NavMeshSurface가 없으면 바닥에 추가
                surface = CreateNavMeshSurface();
            }

            if (surface != null)
            {
                surface.BuildNavMesh();
                EditorUtility.SetDirty(surface);
                Debug.Log("[KillerSetupTool] NavMesh 베이크가 완료되었습니다.");
            }
            else
            {
                Debug.LogWarning("[KillerSetupTool] NavMeshSurface를 생성할 수 없습니다. 바닥 오브젝트가 있는지 확인하세요.");
            }
        }

        /// <summary>
        /// NavMeshSurface 자동 생성
        /// </summary>
        private NavMeshSurface CreateNavMeshSurface()
        {
            // 바닥 오브젝트 찾기 (여러 이름 시도)
            string[] floorNames = { "Floor", "Ground", "Plane", "Terrain", "NavMeshFloor" };
            GameObject floorObj = null;

            foreach (string name in floorNames)
            {
                floorObj = GameObject.Find(name);
                if (floorObj != null) break;
            }

            // 바닥을 못 찾으면 MeshRenderer가 있는 가장 큰 오브젝트 찾기
            if (floorObj == null)
            {
                MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
                float maxSize = 0f;
                foreach (var renderer in renderers)
                {
                    float size = renderer.bounds.size.x * renderer.bounds.size.z;
                    if (size > maxSize && renderer.bounds.size.y < 1f) // 납작한 오브젝트
                    {
                        maxSize = size;
                        floorObj = renderer.gameObject;
                    }
                }
            }

            if (floorObj != null)
            {
                NavMeshSurface surface = floorObj.GetComponent<NavMeshSurface>();
                if (surface == null)
                {
                    surface = floorObj.AddComponent<NavMeshSurface>();
                    Undo.RegisterCreatedObjectUndo(surface, "Add NavMeshSurface");
                }

                // 설정
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;

                Debug.Log($"[KillerSetupTool] NavMeshSurface가 '{floorObj.name}'에 추가되었습니다.");
                return surface;
            }

            return null;
        }

        /// <summary>
        /// NavMesh 자동 설정 및 베이크
        /// </summary>
        private void EnsureNavMeshExists()
        {
            NavMeshSurface surface = FindObjectOfType<NavMeshSurface>();

            if (surface == null)
            {
                surface = CreateNavMeshSurface();
            }

            if (surface != null)
            {
                // NavMesh 데이터가 없으면 베이크
                if (surface.navMeshData == null)
                {
                    surface.BuildNavMesh();
                    EditorUtility.SetDirty(surface);
                    Debug.Log("[KillerSetupTool] NavMesh가 자동으로 베이크되었습니다.");
                }
            }
        }

        /// <summary>
        /// Animator Controller 선택
        /// </summary>
        private void SelectAnimatorController()
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(KILLER_CONTROLLER_PATH);
            if (controller != null)
            {
                Selection.activeObject = controller;
                EditorGUIUtility.PingObject(controller);
            }
            else
            {
                EditorUtility.DisplayDialog("오류",
                    $"Controller를 찾을 수 없습니다:\n{KILLER_CONTROLLER_PATH}", "확인");
            }
        }

        /// <summary>
        /// HorrorGameManager가 씬에 없으면 자동 생성
        /// </summary>
        private void EnsureGameManagerExists()
        {
            var existingManager = FindObjectOfType<HorrorGameManager>();
            if (existingManager == null)
            {
                GameObject managerObj = new GameObject("HorrorGameManager");
                managerObj.AddComponent<HorrorGameManager>();
                Undo.RegisterCreatedObjectUndo(managerObj, "Create Horror Game Manager");
                Debug.Log("[KillerSetupTool] HorrorGameManager가 자동 생성되었습니다.");
            }
            else
            {
                Debug.Log("[KillerSetupTool] HorrorGameManager가 이미 존재합니다.");
            }
        }

        /// <summary>
        /// 메뉴에서 빠르게 살인마 생성
        /// </summary>
        [MenuItem("Horror Game/씬에 살인마 빠른 생성", false, 101)]
        public static void QuickCreateKiller()
        {
            var tool = CreateInstance<KillerSetupTool>();
            tool.CreateKillerInScene();
            DestroyImmediate(tool);
        }

        /// <summary>
        /// 선택된 오브젝트를 살인마로 빠르게 설정
        /// </summary>
        [MenuItem("Horror Game/선택 오브젝트 → 살인마로 설정", false, 102)]
        public static void QuickSetupSelected()
        {
            var tool = CreateInstance<KillerSetupTool>();
            tool.SetupSelectedAsKiller();
            DestroyImmediate(tool);
        }

        [MenuItem("Horror Game/선택 오브젝트 → 살인마로 설정", true)]
        public static bool QuickSetupSelectedValidate()
        {
            return Selection.activeGameObject != null;
        }
    }
}
