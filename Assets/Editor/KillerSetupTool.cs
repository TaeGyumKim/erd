using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
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
                "NavMeshAgent, KillerAI, KillerAnimator 컴포넌트가 추가됩니다.",
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

            // 선택
            Selection.activeGameObject = killer;
            Undo.RegisterCreatedObjectUndo(killer, "Create Killer");

            Debug.Log("[KillerSetupTool] 살인마가 성공적으로 생성되었습니다!");
            EditorUtility.DisplayDialog("완료", "살인마가 씬에 생성되었습니다!", "확인");
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

            Debug.Log($"[KillerSetupTool] '{selected.name}'이(가) 살인마로 설정되었습니다!");
            EditorUtility.DisplayDialog("완료", $"'{selected.name}'이(가) 살인마로 설정되었습니다!", "확인");
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

            EditorUtility.SetDirty(target);
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
        /// NavMesh 베이크
        /// </summary>
        private void BakeNavMesh()
        {
            #pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            #pragma warning restore CS0618
            Debug.Log("[KillerSetupTool] NavMesh 베이크가 완료되었습니다.");
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
