using UnityEngine;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;
using UnityEngine.AI;

namespace HorrorGame.Editor
{
    /// <summary>
    /// VR Locomotion 자동 설정 도구
    /// </summary>
    public static class VRLocomotionSetup
    {
        [MenuItem("Tools/VR Setup/Setup Locomotion System")]
        public static void SetupLocomotionSystem()
        {
            // XR Origin 찾기
            var xrOrigin = Object.FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("[VRLocomotionSetup] XR Origin을 찾을 수 없습니다!");
                return;
            }

            // Locomotion System 찾기
            var locomotionSystem = Object.FindObjectOfType<LocomotionSystem>();
            if (locomotionSystem == null)
            {
                Debug.LogError("[VRLocomotionSetup] Locomotion System을 찾을 수 없습니다!");
                return;
            }

            // LocomotionSystem에 XR Origin 연결
            locomotionSystem.xrOrigin = xrOrigin;
            EditorUtility.SetDirty(locomotionSystem);
            Debug.Log("[VRLocomotionSetup] LocomotionSystem에 XR Origin 연결됨");

            // ContinuousMoveProvider 설정
            var moveProviders = Object.FindObjectsOfType<ActionBasedContinuousMoveProvider>();
            foreach (var provider in moveProviders)
            {
                provider.system = locomotionSystem;
                EditorUtility.SetDirty(provider);
                Debug.Log($"[VRLocomotionSetup] {provider.name}의 ContinuousMoveProvider에 LocomotionSystem 연결됨");
            }

            // SnapTurnProvider 설정
            var turnProviders = Object.FindObjectsOfType<ActionBasedSnapTurnProvider>();
            foreach (var provider in turnProviders)
            {
                provider.system = locomotionSystem;
                EditorUtility.SetDirty(provider);
                Debug.Log($"[VRLocomotionSetup] {provider.name}의 SnapTurnProvider에 LocomotionSystem 연결됨");
            }

            // TeleportationProvider 설정
            var teleportProviders = Object.FindObjectsOfType<TeleportationProvider>();
            foreach (var provider in teleportProviders)
            {
                provider.system = locomotionSystem;
                EditorUtility.SetDirty(provider);
                Debug.Log($"[VRLocomotionSetup] {provider.name}의 TeleportationProvider에 LocomotionSystem 연결됨");
            }

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] Locomotion System 설정 완료!");
        }

        [MenuItem("Tools/VR Setup/Add Desktop Simulator")]
        public static void AddDesktopSimulator()
        {
            // 모든 어셈블리에서 타입 검색
            System.Type simulatorType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                simulatorType = assembly.GetType("HorrorGame.DesktopXRSimulator");
                if (simulatorType != null) break;
            }

            if (simulatorType == null)
            {
                Debug.LogError("[VRLocomotionSetup] DesktopXRSimulator 스크립트를 찾을 수 없습니다! 어셈블리 목록:");
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.FullName.Contains("Assembly-CSharp"))
                        Debug.Log($"  - {assembly.FullName}");
                }
                return;
            }

            // 기존 Desktop XR Simulator 찾기
            var existing = Object.FindObjectOfType(simulatorType);
            if (existing != null)
            {
                Debug.Log("[VRLocomotionSetup] Desktop XR Simulator가 이미 존재합니다.");
                return;
            }

            // 새 게임오브젝트 생성
            var simulatorGO = new GameObject("Desktop XR Simulator");
            simulatorGO.AddComponent(simulatorType);

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] Desktop XR Simulator 추가됨!");
        }

        [MenuItem("Tools/VR Setup/Cleanup Scene")]
        public static void CleanupScene()
        {
            // 중복 Main Camera 제거 (XR Origin 외부에 있는 것)
            var cameras = Object.FindObjectsOfType<Camera>();
            var xrOrigin = Object.FindObjectOfType<XROrigin>();

            foreach (var cam in cameras)
            {
                // XR Origin의 자식이 아닌 Main Camera 삭제
                if (cam.CompareTag("MainCamera") && xrOrigin != null)
                {
                    if (!cam.transform.IsChildOf(xrOrigin.transform))
                    {
                        Object.DestroyImmediate(cam.gameObject);
                        Debug.Log("[VRLocomotionSetup] 중복 Main Camera 제거됨");
                    }
                }
            }

            // 빈 XR Interaction Manager 제거
            var interactionManagers = Object.FindObjectsOfType<XRInteractionManager>();
            foreach (var manager in interactionManagers)
            {
                // 컴포넌트가 Transform만 있는 경우 삭제
                var components = manager.GetComponents<Component>();
                if (components.Length <= 2) // Transform + XRInteractionManager
                {
                    // XRInteractionManager가 실제로 있는지 확인
                    if (manager.GetComponent<XRInteractionManager>() == null)
                    {
                        Object.DestroyImmediate(manager.gameObject);
                        Debug.Log("[VRLocomotionSetup] 빈 Interaction Manager 제거됨");
                    }
                }
            }

            // 중복 LocomotionSystem 제거
            var locomotionSystems = Object.FindObjectsOfType<LocomotionSystem>();
            if (locomotionSystems.Length > 1)
            {
                for (int i = 1; i < locomotionSystems.Length; i++)
                {
                    Object.DestroyImmediate(locomotionSystems[i].gameObject);
                    Debug.Log("[VRLocomotionSetup] 중복 Locomotion System 제거됨");
                }
            }

            // XRDeviceSimulator 제거 (우리 DesktopXRSimulator를 사용하므로)
            var xrDeviceSimulatorType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator, Unity.XR.Interaction.Toolkit");
            if (xrDeviceSimulatorType != null)
            {
                var simulators = Object.FindObjectsOfType(xrDeviceSimulatorType);
                foreach (var sim in simulators)
                {
                    Object.DestroyImmediate((sim as Component).gameObject);
                    Debug.Log("[VRLocomotionSetup] XRDeviceSimulator 제거됨 (DesktopXRSimulator 사용)");
                }
            }

            // 누락된 스크립트가 있는 게임오브젝트 정리
            CleanupMissingScripts();

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] 씬 정리 완료!");
        }

        private static void CleanupMissingScripts()
        {
            var allObjects = Object.FindObjectsOfType<GameObject>();
            int removedCount = 0;

            foreach (var go in allObjects)
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (count > 0)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    removedCount += count;
                    Debug.Log($"[VRLocomotionSetup] {go.name}에서 누락된 스크립트 {count}개 제거됨");
                }
            }

            if (removedCount > 0)
            {
                Debug.Log($"[VRLocomotionSetup] 총 {removedCount}개의 누락된 스크립트 제거됨");
            }
        }

        [MenuItem("Tools/VR Setup/Setup Killer AI")]
        public static void SetupKillerAI()
        {
            // KillerAI 타입 찾기
            System.Type killerType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                killerType = assembly.GetType("HorrorGame.KillerAI");
                if (killerType != null) break;
            }

            if (killerType == null)
            {
                Debug.LogError("[VRLocomotionSetup] KillerAI 스크립트를 찾을 수 없습니다!");
                return;
            }

            // 기존 Killer 오브젝트 찾기 또는 생성
            var existingKiller = Object.FindObjectOfType(killerType);
            GameObject killerGO;

            if (existingKiller != null)
            {
                killerGO = (existingKiller as Component).gameObject;
                Debug.Log("[VRLocomotionSetup] 기존 Killer 오브젝트 사용");
            }
            else
            {
                // EnemySpawnPoint 위치 찾기
                var spawnPoint = GameObject.Find("EnemySpawnPoint");
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.transform.position : new Vector3(20, 0, 20);

                killerGO = new GameObject("Killer");
                killerGO.transform.position = spawnPos;
            }

            // NavMeshAgent 추가
            var agent = killerGO.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = killerGO.AddComponent<NavMeshAgent>();
                agent.speed = 2f;
                agent.angularSpeed = 360f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 1f;
                agent.radius = 0.5f;
                agent.height = 2f;
            }

            // KillerAI 추가
            var killerAI = killerGO.GetComponent(killerType);
            if (killerAI == null)
            {
                killerAI = killerGO.AddComponent(killerType);
            }

            // 비주얼 캡슐 추가 (없으면)
            var visual = killerGO.transform.Find("Visual");
            if (visual == null)
            {
                var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Visual";
                capsule.transform.SetParent(killerGO.transform);
                capsule.transform.localPosition = Vector3.up;
                capsule.transform.localScale = new Vector3(1f, 1f, 1f);

                // Collider 제거 (NavMeshAgent와 충돌 방지)
                Object.DestroyImmediate(capsule.GetComponent<Collider>());

                // 빨간색으로 변경
                var renderer = capsule.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.8f, 0.1f, 0.1f); // 어두운 빨간색
                renderer.material = mat;
            }

            // 순찰 지점 생성
            CreatePatrolPoints();

            // 순찰 지점 연결
            AssignPatrolPointsToKiller(killerGO, killerType);

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] 살인마 AI 설정 완료!");
        }

        private static void CreatePatrolPoints()
        {
            // PatrolPoints 부모 찾기 또는 생성
            var parent = GameObject.Find("PatrolPoints");
            if (parent == null)
            {
                parent = new GameObject("PatrolPoints");
            }

            // 이미 순찰 지점이 있으면 스킵
            if (parent.transform.childCount >= 4)
            {
                Debug.Log("[VRLocomotionSetup] 순찰 지점이 이미 존재합니다.");
                return;
            }

            // 4개의 순찰 지점 생성 (방의 네 모서리 근처)
            Vector3[] positions = new Vector3[]
            {
                new Vector3(-15, 0, -15),  // 남서
                new Vector3(15, 0, -15),   // 남동
                new Vector3(15, 0, 15),    // 북동
                new Vector3(-15, 0, 15)    // 북서
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var point = new GameObject($"PatrolPoint_{i + 1}");
                point.transform.SetParent(parent.transform);
                point.transform.position = positions[i];

                // PatrolPointGizmo 추가 (있으면)
                System.Type gizmoType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    gizmoType = assembly.GetType("HorrorGame.Editor.PatrolPointGizmo");
                    if (gizmoType != null) break;
                }
                if (gizmoType != null)
                {
                    point.AddComponent(gizmoType);
                }
            }

            Debug.Log("[VRLocomotionSetup] 순찰 지점 4개 생성됨");
        }

        private static void AssignPatrolPointsToKiller(GameObject killerGO, System.Type killerType)
        {
            var parent = GameObject.Find("PatrolPoints");
            if (parent == null) return;

            var killerAI = killerGO.GetComponent(killerType);
            if (killerAI == null) return;

            // patrolPoints 필드에 할당
            var field = killerType.GetField("patrolPoints");
            if (field != null)
            {
                var points = new Transform[parent.transform.childCount];
                for (int i = 0; i < parent.transform.childCount; i++)
                {
                    points[i] = parent.transform.GetChild(i);
                }
                field.SetValue(killerAI, points);
                EditorUtility.SetDirty(killerGO);
                Debug.Log($"[VRLocomotionSetup] {points.Length}개의 순찰 지점이 살인마에 연결됨");
            }
        }

        [MenuItem("Tools/VR Setup/Setup VRPlayer")]
        public static void SetupVRPlayer()
        {
            // XR Origin 찾기
            var xrOrigin = Object.FindObjectOfType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("[VRLocomotionSetup] XR Origin을 찾을 수 없습니다!");
                return;
            }

            // VRPlayer 타입 찾기
            System.Type vrPlayerType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                vrPlayerType = assembly.GetType("HorrorGame.VRPlayer");
                if (vrPlayerType != null) break;
            }

            if (vrPlayerType == null)
            {
                Debug.LogError("[VRLocomotionSetup] VRPlayer 스크립트를 찾을 수 없습니다!");
                return;
            }

            // VRPlayer 추가
            var vrPlayer = xrOrigin.GetComponent(vrPlayerType);
            if (vrPlayer == null)
            {
                vrPlayer = xrOrigin.gameObject.AddComponent(vrPlayerType);
                Debug.Log("[VRLocomotionSetup] VRPlayer 컴포넌트 추가됨");
            }
            else
            {
                Debug.Log("[VRLocomotionSetup] VRPlayer가 이미 존재합니다.");
            }

            // PlayerInventory 타입 찾기 및 추가
            System.Type inventoryType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                inventoryType = assembly.GetType("HorrorGame.PlayerInventory");
                if (inventoryType != null) break;
            }

            if (inventoryType != null)
            {
                var inventory = xrOrigin.GetComponent(inventoryType);
                if (inventory == null)
                {
                    xrOrigin.gameObject.AddComponent(inventoryType);
                    Debug.Log("[VRLocomotionSetup] PlayerInventory 컴포넌트 추가됨");
                }
            }

            // XR Origin에 Player 레이어 설정
            xrOrigin.gameObject.layer = LayerMask.NameToLayer("Default");

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] VRPlayer 설정 완료!");
        }

        [MenuItem("Tools/VR Setup/Fix Killer AI Detection")]
        public static void FixKillerAIDetection()
        {
            // KillerAI 찾기
            System.Type killerType = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                killerType = assembly.GetType("HorrorGame.KillerAI");
                if (killerType != null) break;
            }

            if (killerType == null)
            {
                Debug.LogError("[VRLocomotionSetup] KillerAI 스크립트를 찾을 수 없습니다!");
                return;
            }

            var killers = Object.FindObjectsOfType(killerType);
            foreach (var killer in killers)
            {
                var killerComp = killer as Component;

                // viewDistance 설정
                var viewDistField = killerType.GetField("viewDistance");
                if (viewDistField != null)
                {
                    viewDistField.SetValue(killer, 20f);
                }

                // viewAngle 설정
                var viewAngleField = killerType.GetField("viewAngle");
                if (viewAngleField != null)
                {
                    viewAngleField.SetValue(killer, 120f);
                }

                // hearingRange 설정
                var hearingField = killerType.GetField("hearingRange");
                if (hearingField != null)
                {
                    hearingField.SetValue(killer, 15f);
                }

                EditorUtility.SetDirty(killerComp);
                Debug.Log($"[VRLocomotionSetup] {killerComp.name}의 감지 범위 설정됨 (시야: 20m, 각도: 120도, 청각: 15m)");
            }

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] Killer AI 감지 설정 완료!");
        }

        [MenuItem("Tools/VR Setup/Remove Duplicates")]
        public static void RemoveDuplicates()
        {
            // 중복 체크할 오브젝트 이름들
            string[] checkNames = { "Killer", "Desktop XR Simulator", "PatrolPoints", "Floor", "Key_Exit", "ExitDoor", "Wardrobe_HidingSpot" };

            foreach (var name in checkNames)
            {
                var objects = new System.Collections.Generic.List<GameObject>();
                foreach (var go in Object.FindObjectsOfType<GameObject>())
                {
                    if (go.name == name)
                    {
                        objects.Add(go);
                    }
                }

                if (objects.Count > 1)
                {
                    Debug.Log($"[VRLocomotionSetup] '{name}' 중복 발견: {objects.Count}개");
                    // 첫 번째 것만 남기고 나머지 삭제
                    for (int i = 1; i < objects.Count; i++)
                    {
                        Object.DestroyImmediate(objects[i]);
                        Debug.Log($"[VRLocomotionSetup] 중복 '{name}' 제거됨");
                    }
                }
            }

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] 중복 제거 완료!");
        }

        [MenuItem("Tools/VR Setup/Bake NavMesh")]
        public static void BakeNavMesh()
        {
            // Floor를 Navigation Static으로 설정
            var floor = GameObject.Find("Floor");
            if (floor != null)
            {
#pragma warning disable CS0618
                GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
#pragma warning restore CS0618
                Debug.Log("[VRLocomotionSetup] Floor를 Navigation Static으로 설정");
            }

            // 벽들을 Navigation Static으로 설정
            string[] walls = { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };
            foreach (var wallName in walls)
            {
                var wall = GameObject.Find(wallName);
                if (wall != null)
                {
#pragma warning disable CS0618
                    GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.NavigationStatic);
#pragma warning restore CS0618
                }
            }

            // NavMesh 베이크
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();

            // 씬 저장
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[VRLocomotionSetup] NavMesh 베이크 완료!");
        }
    }
}
