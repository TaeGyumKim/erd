using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace HorrorGame.Editor
{
    /// <summary>
    /// "Run 'Till the End" 게임 씬 빌더
    /// 저택 맵을 빠르게 구성하는 에디터 도구
    /// </summary>
    public class RunTillEndSceneBuilder : EditorWindow
    {
        private Vector2 scrollPosition;

        // 프리팹 경로
        private const string HORROR_PACK_PATH = "Assets/Asset pack for horror game/Prefabs/";
        private const string HOSPITAL_PACK_PATH = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/";
        private const string DUNGEON_PACK_PATH = "Assets/StylizedHandPaintedDungeon(Free)/Prefabs/";
        private const string FANTASY_PACK_PATH = "Assets/Mega Fantasy Props Pack/Prefabs/";
        private const string CHARACTER_PATH = "Assets/Character/";

        [MenuItem("Horror Game/Run Till End Scene Builder", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<RunTillEndSceneBuilder>("Scene Builder");
            window.minSize = new Vector2(400, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Run 'Till the End - Scene Builder", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 새 씬 생성
            DrawNewSceneSection();

            GUILayout.Space(20);

            // 게임 시스템 설정
            DrawGameSystemsSection();

            GUILayout.Space(20);

            // 환경 오브젝트 배치
            DrawEnvironmentSection();

            GUILayout.Space(20);

            // 단서 아이템 배치
            DrawClueItemsSection();

            GUILayout.Space(20);

            // 캐릭터 배치
            DrawCharactersSection();

            GUILayout.Space(20);

            // 빠른 설정
            DrawQuickSetupSection();

            EditorGUILayout.EndScrollView();
        }

        #region 새 씬 생성

        private void DrawNewSceneSection()
        {
            GUILayout.Label("1. 씬 생성", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("새 게임 씬 생성", GUILayout.Height(30)))
            {
                CreateNewGameScene();
            }

            if (GUILayout.Button("현재 씬에 설정 추가", GUILayout.Height(30)))
            {
                SetupCurrentScene();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateNewGameScene()
        {
            // 새 씬 생성
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 기본 조명 설정
            SetupLighting();

            // 게임 시스템 추가
            SetupGameSystems();

            // 바닥 생성
            CreateFloor();

            // 씬 저장
            string path = EditorUtility.SaveFilePanelInProject(
                "씬 저장",
                "RunTillEnd_Main",
                "unity",
                "게임 씬을 저장할 위치를 선택하세요",
                "Assets/Scenes"
            );

            if (!string.IsNullOrEmpty(path))
            {
                EditorSceneManager.SaveScene(newScene, path);
                Debug.Log($"[SceneBuilder] 새 게임 씬 생성됨: {path}");
            }
        }

        private void SetupCurrentScene()
        {
            SetupLighting();
            SetupGameSystems();
            Debug.Log("[SceneBuilder] 현재 씬에 게임 시스템 추가됨");
        }

        private void SetupLighting()
        {
            // 어두운 분위기 조명
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.15f);

            // 기존 조명 수정
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.intensity = 0.3f;
                    light.color = new Color(0.7f, 0.7f, 0.8f);
                }
            }
        }

        #endregion

        #region 게임 시스템 설정

        private void DrawGameSystemsSection()
        {
            GUILayout.Label("2. 게임 시스템", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("게임 매니저", GUILayout.Height(25)))
            {
                CreateGameManager();
            }

            if (GUILayout.Button("스토리 매니저", GUILayout.Height(25)))
            {
                CreateStoryManager();
            }

            if (GUILayout.Button("VR 플레이어", GUILayout.Height(25)))
            {
                CreateVRPlayer();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("목표 시스템", GUILayout.Height(25)))
            {
                CreateObjectiveSystem();
            }

            if (GUILayout.Button("스토리 UI", GUILayout.Height(25)))
            {
                CreateStoryUI();
            }

            if (GUILayout.Button("전체 시스템", GUILayout.Height(25)))
            {
                SetupGameSystems();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SetupGameSystems()
        {
            // 게임 매니저
            CreateGameManager();

            // 스토리 매니저
            CreateStoryManager();

            // 목표 시스템
            CreateObjectiveSystem();

            // VR 플레이어 (없으면)
            if (FindObjectOfType<VRPlayer>() == null)
            {
                CreateVRPlayer();
            }

            Debug.Log("[SceneBuilder] 게임 시스템 설정 완료");
        }

        private void CreateGameManager()
        {
            if (FindObjectOfType<HorrorGameManager>() != null)
            {
                Debug.LogWarning("HorrorGameManager가 이미 존재합니다");
                return;
            }

            GameObject managerObj = new GameObject("--- GAME MANAGER ---");
            var manager = managerObj.AddComponent<HorrorGameManager>();
            manager.useTimeLimit = true;
            manager.timeLimit = 600f; // 10분
            manager.requiredKeysToEscape = 1;

            Undo.RegisterCreatedObjectUndo(managerObj, "Create Game Manager");
        }

        private void CreateStoryManager()
        {
            if (FindObjectOfType<StoryProgressManager>() != null)
            {
                Debug.LogWarning("StoryProgressManager가 이미 존재합니다");
                return;
            }

            GameObject storyObj = new GameObject("Story Manager");
            storyObj.AddComponent<StoryProgressManager>();

            // 게임 매니저의 자식으로
            var gameManager = FindObjectOfType<HorrorGameManager>();
            if (gameManager != null)
            {
                storyObj.transform.SetParent(gameManager.transform);
            }

            Undo.RegisterCreatedObjectUndo(storyObj, "Create Story Manager");
        }

        private void CreateObjectiveSystem()
        {
            if (FindObjectOfType<ObjectiveSystem>() != null)
            {
                Debug.LogWarning("ObjectiveSystem이 이미 존재합니다");
                return;
            }

            GameObject objSystem = new GameObject("Objective System");
            objSystem.AddComponent<ObjectiveSystem>();

            var gameManager = FindObjectOfType<HorrorGameManager>();
            if (gameManager != null)
            {
                objSystem.transform.SetParent(gameManager.transform);
            }

            Undo.RegisterCreatedObjectUndo(objSystem, "Create Objective System");
        }

        private void CreateVRPlayer()
        {
            // XR Origin 프리팹 찾기
            string[] xrOriginGuids = AssetDatabase.FindAssets("XR Origin t:Prefab");
            GameObject playerPrefab = null;

            foreach (string guid in xrOriginGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("XR Origin"))
                {
                    playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    break;
                }
            }

            if (playerPrefab != null)
            {
                GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.name = "VR Player";

                // VRPlayer 컴포넌트 추가
                if (player.GetComponent<VRPlayer>() == null)
                {
                    player.AddComponent<VRPlayer>();
                }

                // 인벤토리 추가
                if (player.GetComponent<PlayerInventory>() == null)
                {
                    player.AddComponent<PlayerInventory>();
                }

                player.transform.position = new Vector3(0, 0, 0);
                Undo.RegisterCreatedObjectUndo(player, "Create VR Player");
            }
            else
            {
                // XR Origin 없으면 빈 오브젝트로
                GameObject player = new GameObject("VR Player");
                player.AddComponent<VRPlayer>();
                player.AddComponent<PlayerInventory>();
                player.transform.position = new Vector3(0, 1.7f, 0);
                Undo.RegisterCreatedObjectUndo(player, "Create VR Player");
            }
        }

        private void CreateStoryUI()
        {
            if (FindObjectOfType<StoryUI>() != null)
            {
                Debug.LogWarning("StoryUI가 이미 존재합니다");
                return;
            }

            // UI Canvas 생성
            GameObject canvasObj = new GameObject("Story UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // StoryUI 컴포넌트 추가
            canvasObj.AddComponent<StoryUI>();

            // 플레이어 앞에 배치
            canvasObj.transform.position = new Vector3(0, 1.5f, 2f);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Story UI");
        }

        #endregion

        #region 환경 오브젝트

        private void DrawEnvironmentSection()
        {
            GUILayout.Label("3. 환경 오브젝트", EditorStyles.boldLabel);

            GUILayout.Label("Hospital Horror Pack:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("바닥")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Floor_01.prefab");
            if (GUILayout.Button("바닥2")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Floor_02.prefab");
            if (GUILayout.Button("벽")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Wall_01.prefab");
            if (GUILayout.Button("벽2")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Wall_02.prefab");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("천장")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Ceiling_01.prefab");
            if (GUILayout.Button("문")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Door_01_.prefab");
            if (GUILayout.Button("침대")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Bed_01.prefab");
            if (GUILayout.Button("램프")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Lamp.prefab");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("문방")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Module_DoorRoom_01.prefab");
            if (GUILayout.Button("문방2")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Module_DoorRoom_02.prefab");
            if (GUILayout.Button("기둥")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Column_01.prefab");
            if (GUILayout.Button("의료박스")) SpawnPrefab(HOSPITAL_PACK_PATH + "P_Med_box_01.prefab");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label("Horror Pack:", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("방")) SpawnPrefab(HORROR_PACK_PATH + "Room.prefab");
            if (GUILayout.Button("문")) SpawnPrefab(HORROR_PACK_PATH + "Door.prefab");
            if (GUILayout.Button("창문")) SpawnPrefab(HORROR_PACK_PATH + "Window.prefab");
            if (GUILayout.Button("침대")) SpawnPrefab(HORROR_PACK_PATH + "Bed.prefab");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("테이블")) SpawnPrefab(HORROR_PACK_PATH + "Wooden table.prefab");
            if (GUILayout.Button("의자")) SpawnPrefab(HORROR_PACK_PATH + "Wood chair.prefab");
            if (GUILayout.Button("커튼")) SpawnPrefab(HORROR_PACK_PATH + "Curtain.prefab");
            if (GUILayout.Button("휠체어")) SpawnPrefab(HORROR_PACK_PATH + "Wheelchair.prefab");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("열쇠(에셋)")) SpawnPrefab(HORROR_PACK_PATH + "Key.prefab");
            if (GUILayout.Button("형광등")) SpawnPrefab(HORROR_PACK_PATH + "Fluorescent lamp.prefab");
            if (GUILayout.Button("백열등")) SpawnPrefab(HORROR_PACK_PATH + "Incandescent lamp.prefab");
            if (GUILayout.Button("철의자")) SpawnPrefab(HORROR_PACK_PATH + "Iron chair.prefab");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("피침대")) SpawnPrefab(HORROR_PACK_PATH + "Bed with person with blood.prefab");
            if (GUILayout.Button("망가진침대")) SpawnPrefab(HORROR_PACK_PATH + "Broken bed.prefab");
            if (GUILayout.Button("더러운커튼")) SpawnPrefab(HORROR_PACK_PATH + "Dirty curtain.prefab");
            if (GUILayout.Button("피휠체어")) SpawnPrefab(HORROR_PACK_PATH + "Broken wheelchair with blood.prefab");
            EditorGUILayout.EndHorizontal();
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(10, 1, 10);

            // 어두운 머티리얼
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material darkMat = new Material(Shader.Find("Standard"));
                darkMat.color = new Color(0.1f, 0.1f, 0.1f);
                renderer.material = darkMat;
            }

            // NavMesh Static
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);

            Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
        }

        #endregion

        #region 단서 아이템

        private void DrawClueItemsSection()
        {
            GUILayout.Label("4. 단서 아이템", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("USB", GUILayout.Height(25)))
            {
                CreateClueItem(ClueItemType.USB);
            }
            if (GUILayout.Button("라이터", GUILayout.Height(25)))
            {
                CreateClueItem(ClueItemType.Lighter);
            }
            if (GUILayout.Button("보안카드", GUILayout.Height(25)))
            {
                CreateClueItem(ClueItemType.SecurityCard);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("배터리", GUILayout.Height(25)))
            {
                CreateClueItem(ClueItemType.Battery);
            }
            if (GUILayout.Button("기어", GUILayout.Height(25)))
            {
                CreateClueItem(ClueItemType.Gear);
            }
            if (GUILayout.Button("탈출 열쇠", GUILayout.Height(25)))
            {
                CreateClueItem(ClueItemType.FinalKey);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label("상호작용 오브젝트:", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("컴퓨터 단말기", GUILayout.Height(25)))
            {
                CreateInteractable("ComputerTerminal");
            }
            if (GUILayout.Button("벽 문양", GUILayout.Height(25)))
            {
                CreateInteractable("HiddenWallSymbol");
            }
            if (GUILayout.Button("카드 리더", GUILayout.Height(25)))
            {
                CreateInteractable("CardReader");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("탈출문", GUILayout.Height(25)))
            {
                CreateInteractable("ExitDoor");
            }
            if (GUILayout.Button("숨기 장소", GUILayout.Height(25)))
            {
                CreateHidingSpot();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CreateClueItem(ClueItemType type)
        {
            // 기본 메쉬로 임시 오브젝트 생성
            GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            switch (type)
            {
                case ClueItemType.USB:
                    item.name = "Clue_USB";
                    item.AddComponent<USBItem>();
                    break;
                case ClueItemType.Lighter:
                    item.name = "Clue_Lighter";
                    item.AddComponent<LighterItem>();
                    break;
                case ClueItemType.SecurityCard:
                    item.name = "Clue_SecurityCard";
                    item.transform.localScale = new Vector3(0.08f, 0.005f, 0.05f);
                    item.AddComponent<SecurityCardItem>();
                    break;
                case ClueItemType.Battery:
                    item.name = "Clue_Battery";
                    item.transform.localScale = new Vector3(0.02f, 0.05f, 0.02f);
                    item.AddComponent<ClueBatteryItem>();
                    break;
                case ClueItemType.Gear:
                    item.name = "Clue_Gear";
                    item.AddComponent<GearItem>();
                    break;
                case ClueItemType.FinalKey:
                    item.name = "FinalKey";
                    item.transform.localScale = new Vector3(0.1f, 0.02f, 0.03f);
                    item.AddComponent<FinalKeyItem>();
                    break;
            }

            // 발광 색상 설정
            var renderer = item.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = Color.yellow;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.yellow * 0.3f);
                renderer.material = mat;
            }

            // 씬 뷰 중앙에 배치
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                item.transform.position = sceneView.pivot;
            }

            Selection.activeGameObject = item;
            Undo.RegisterCreatedObjectUndo(item, $"Create {type}");
        }

        private void CreateInteractable(string type)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);

            switch (type)
            {
                case "ComputerTerminal":
                    obj.name = "ComputerTerminal";
                    obj.transform.localScale = new Vector3(0.5f, 0.4f, 0.3f);
                    obj.AddComponent<ComputerTerminal>();
                    break;
                case "HiddenWallSymbol":
                    obj.name = "HiddenWallSymbol";
                    obj.transform.localScale = new Vector3(1f, 1f, 0.01f);
                    obj.AddComponent<HiddenWallSymbol>();
                    break;
                case "CardReader":
                    obj.name = "CardReader";
                    obj.transform.localScale = new Vector3(0.1f, 0.15f, 0.05f);
                    obj.AddComponent<CardReader>();
                    break;
                case "ExitDoor":
                    obj.name = "ExitDoor";
                    obj.transform.localScale = new Vector3(1f, 2.5f, 0.1f);
                    obj.AddComponent<ExitDoor>();
                    break;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                obj.transform.position = sceneView.pivot;
            }

            Selection.activeGameObject = obj;
            Undo.RegisterCreatedObjectUndo(obj, $"Create {type}");
        }

        private void CreateHidingSpot()
        {
            // 숨기 장소 프리팹 찾기 또는 생성
            GameObject spot = new GameObject("HidingSpot");

            // 트리거 콜라이더 추가
            BoxCollider collider = spot.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.5f, 2f, 1.5f);

            spot.AddComponent<HidingSpot>();

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                spot.transform.position = sceneView.pivot;
            }

            Selection.activeGameObject = spot;
            Undo.RegisterCreatedObjectUndo(spot, "Create Hiding Spot");
        }

        #endregion

        #region 캐릭터

        private void DrawCharactersSection()
        {
            GUILayout.Label("5. 캐릭터", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("살인마 (KillerAI)", GUILayout.Height(30)))
            {
                CreateKiller();
            }

            if (GUILayout.Button("유령 (GhostAI)", GUILayout.Height(30)))
            {
                CreateGhost();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("순찰 지점 추가", GUILayout.Height(25)))
            {
                CreatePatrolPoint();
            }

            if (GUILayout.Button("힌트 지점 추가", GUILayout.Height(25)))
            {
                CreateHintPoint();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void CreateKiller()
        {
            // TG_Hero 캐릭터 프리팹 직접 로드
            GameObject killerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CHARACTER_PATH + "TG_Hero_Interactive_Prefab.prefab");

            GameObject killer;
            if (killerPrefab != null)
            {
                killer = (GameObject)PrefabUtility.InstantiatePrefab(killerPrefab);
                Debug.Log("[SceneBuilder] TG_Hero 캐릭터 프리팹 로드 성공");
            }
            else
            {
                // 임시 캡슐로 생성
                killer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Debug.LogWarning("[SceneBuilder] 캐릭터 프리팹을 찾을 수 없어 임시 오브젝트 생성");
            }

            killer.name = "Killer";

            // KillerAI 컴포넌트 추가
            if (killer.GetComponent<KillerAI>() == null)
            {
                killer.AddComponent<KillerAI>();
            }

            // NavMeshAgent 추가
            if (killer.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
            {
                var agent = killer.AddComponent<UnityEngine.AI.NavMeshAgent>();
                agent.speed = 3.5f;
                agent.angularSpeed = 120f;
                agent.stoppingDistance = 1.5f;
            }

            // 마스크 프리팹 추가
            GameObject maskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CHARACTER_PATH + "Mask_Prefab.prefab");
            if (maskPrefab != null)
            {
                GameObject mask = (GameObject)PrefabUtility.InstantiatePrefab(maskPrefab);
                mask.transform.SetParent(killer.transform);
                mask.transform.localPosition = new Vector3(0, 1.6f, 0.1f);
                mask.transform.localRotation = Quaternion.identity;
            }

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                killer.transform.position = sceneView.pivot;
            }

            // 처음엔 비활성화
            killer.SetActive(false);

            Selection.activeGameObject = killer;
            Undo.RegisterCreatedObjectUndo(killer, "Create Killer");
        }

        private void CreateGhost()
        {
            GameObject ghost = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            ghost.name = "Ghost";
            ghost.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            // 반투명 머티리얼
            var renderer = ghost.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                mat.color = new Color(0.5f, 0.5f, 1f, 0.3f);
                renderer.material = mat;
            }

            ghost.AddComponent<GhostAI>();
            ghost.AddComponent<UnityEngine.AI.NavMeshAgent>();

            // 콜라이더 제거 (유령은 통과)
            DestroyImmediate(ghost.GetComponent<Collider>());

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                ghost.transform.position = sceneView.pivot;
            }

            Selection.activeGameObject = ghost;
            Undo.RegisterCreatedObjectUndo(ghost, "Create Ghost");
        }

        private void CreatePatrolPoint()
        {
            GameObject point = new GameObject("PatrolPoint");
            point.transform.position = SceneView.lastActiveSceneView?.pivot ?? Vector3.zero;

            // 아이콘 표시를 위한 빈 게임오브젝트
            // 기즈모로 표시됨

            // 순찰 지점 부모 찾기 또는 생성
            GameObject patrolParent = GameObject.Find("PatrolPoints");
            if (patrolParent == null)
            {
                patrolParent = new GameObject("PatrolPoints");
            }

            point.transform.SetParent(patrolParent.transform);
            point.name = $"PatrolPoint_{patrolParent.transform.childCount}";

            Selection.activeGameObject = point;
            Undo.RegisterCreatedObjectUndo(point, "Create Patrol Point");
        }

        private void CreateHintPoint()
        {
            GameObject point = new GameObject("HintPoint");
            point.transform.position = SceneView.lastActiveSceneView?.pivot ?? Vector3.zero;

            // 힌트 지점 부모 찾기 또는 생성
            GameObject hintParent = GameObject.Find("HintPoints");
            if (hintParent == null)
            {
                hintParent = new GameObject("HintPoints");
            }

            point.transform.SetParent(hintParent.transform);
            point.name = $"HintPoint_{hintParent.transform.childCount}";

            Selection.activeGameObject = point;
            Undo.RegisterCreatedObjectUndo(point, "Create Hint Point");
        }

        #endregion

        #region 빠른 설정

        private void DrawQuickSetupSection()
        {
            GUILayout.Label("6. 빠른 설정", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            if (GUILayout.Button("전체 게임 자동 설정", GUILayout.Height(35)))
            {
                AutoSetupGame();
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("NavMesh 베이크"))
            {
                BakeNavMesh();
            }

            if (GUILayout.Button("씬 검증"))
            {
                ValidateScene();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void AutoSetupGame()
        {
            // 1. 게임 시스템 설정
            SetupGameSystems();

            // 2. 기본 환경 생성
            CreateFloor();

            // 3. 단서 아이템 배치
            CreateClueItem(ClueItemType.USB);
            CreateClueItem(ClueItemType.Lighter);
            CreateClueItem(ClueItemType.SecurityCard);
            CreateClueItem(ClueItemType.FinalKey);

            // 4. 상호작용 오브젝트
            CreateInteractable("ComputerTerminal");
            CreateInteractable("HiddenWallSymbol");
            CreateInteractable("CardReader");
            CreateInteractable("ExitDoor");

            // 5. 캐릭터
            CreateKiller();
            CreateGhost();

            // 6. 숨기 장소
            CreateHidingSpot();

            Debug.Log("[SceneBuilder] 전체 게임 자동 설정 완료!");
            EditorUtility.DisplayDialog("설정 완료", "게임 자동 설정이 완료되었습니다.\n\n오브젝트 위치를 조정하고 NavMesh를 베이크하세요.", "확인");
        }

        private void BakeNavMesh()
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[SceneBuilder] NavMesh 베이크 완료");
        }

        private void ValidateScene()
        {
            List<string> issues = new List<string>();

            // 게임 매니저 확인
            if (FindObjectOfType<HorrorGameManager>() == null)
                issues.Add("- HorrorGameManager가 없습니다");

            // 스토리 매니저 확인
            if (FindObjectOfType<StoryProgressManager>() == null)
                issues.Add("- StoryProgressManager가 없습니다");

            // VR 플레이어 확인
            if (FindObjectOfType<VRPlayer>() == null)
                issues.Add("- VRPlayer가 없습니다");

            // 킬러 확인
            if (FindObjectOfType<KillerAI>() == null)
                issues.Add("- KillerAI가 없습니다");

            // 탈출문 확인
            if (FindObjectOfType<ExitDoor>() == null)
                issues.Add("- ExitDoor가 없습니다");

            // 단서 아이템 확인
            if (FindObjectOfType<ClueItem>() == null)
                issues.Add("- 단서 아이템이 없습니다");

            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("검증 완료", "모든 필수 컴포넌트가 있습니다!", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("검증 결과", "다음 항목이 누락되었습니다:\n\n" + string.Join("\n", issues), "확인");
            }
        }

        #endregion

        #region 유틸리티

        private void SpawnPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    instance.transform.position = sceneView.pivot;
                }

                Selection.activeGameObject = instance;
                Undo.RegisterCreatedObjectUndo(instance, $"Spawn {prefab.name}");
            }
            else
            {
                Debug.LogWarning($"프리팹을 찾을 수 없습니다: {path}");
            }
        }

        #endregion
    }
}
