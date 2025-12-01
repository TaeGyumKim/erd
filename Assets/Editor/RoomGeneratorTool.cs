using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 밀실 랜덤 맵 생성 도구
    /// 프로젝트 내 에셋을 사용하여 공포 게임용 밀실을 자동 생성합니다.
    /// </summary>
    public class RoomGeneratorTool : EditorWindow
    {
        // 에셋 팩 경로
        private const string HOSPITAL_PACK_PATH = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab";
        private const string HORROR_PACK_PATH = "Assets/Asset pack for horror game/Prefabs";
        private const string DUNGEON_PACK_PATH = "Assets/StylizedHandPaintedDungeon(Free)/Prefabs";

        // 방 설정
        private Vector2Int roomSize = new Vector2Int(5, 5);
        private float cellSize = 4f;  // 모듈 하나의 크기
        private float wallHeight = 3f;
        private int doorCount = 1;

        // 소품 설정
        private int minProps = 5;
        private int maxProps = 15;
        private bool addLighting = true;
        private int lightCount = 2;
        private float lightIntensity = 1f;

        // 스타일 선택
        private RoomStyle roomStyle = RoomStyle.Hospital;
        private bool randomizeProps = true;

        // 시드
        private bool useRandomSeed = true;
        private int seed = 12345;

        private Vector2 scrollPos;

        // 캐시된 프리팹 목록
        private Dictionary<string, List<GameObject>> cachedPrefabs = new Dictionary<string, List<GameObject>>();

        public enum RoomStyle
        {
            Hospital,   // 병원 스타일
            Dungeon,    // 던전 스타일
            Mixed       // 혼합
        }

        [MenuItem("Horror Game/밀실 맵 생성 도구", false, 120)]
        public static void ShowWindow()
        {
            var window = GetWindow<RoomGeneratorTool>("밀실 맵 생성");
            window.minSize = new Vector2(380, 600);
        }

        private void OnEnable()
        {
            LoadPrefabCache();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 헤더
            EditorGUILayout.Space(10);
            GUILayout.Label("밀실 랜덤 맵 생성 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "프로젝트 내 에셋을 사용하여 밀실을 자동 생성합니다.\n" +
                "벽, 바닥, 문, 가구가 랜덤하게 배치됩니다.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // 방 크기 설정
            EditorGUILayout.LabelField("방 크기 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            roomSize = EditorGUILayout.Vector2IntField("방 크기 (칸)", roomSize);
            roomSize.x = Mathf.Clamp(roomSize.x, 2, 20);
            roomSize.y = Mathf.Clamp(roomSize.y, 2, 20);

            cellSize = EditorGUILayout.FloatField("칸 크기 (m)", cellSize);
            wallHeight = EditorGUILayout.FloatField("벽 높이 (m)", wallHeight);
            doorCount = EditorGUILayout.IntSlider("문 개수", doorCount, 1, 4);

            float totalWidth = roomSize.x * cellSize;
            float totalDepth = roomSize.y * cellSize;
            EditorGUILayout.HelpBox($"총 크기: {totalWidth}m x {totalDepth}m", MessageType.None);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 스타일 설정
            EditorGUILayout.LabelField("스타일 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            roomStyle = (RoomStyle)EditorGUILayout.EnumPopup("방 스타일", roomStyle);

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 소품 설정
            EditorGUILayout.LabelField("소품 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            randomizeProps = EditorGUILayout.Toggle("소품 랜덤 배치", randomizeProps);

            if (randomizeProps)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("소품 개수", GUILayout.Width(80));
                minProps = EditorGUILayout.IntField(minProps, GUILayout.Width(50));
                EditorGUILayout.LabelField("~", GUILayout.Width(20));
                maxProps = EditorGUILayout.IntField(maxProps, GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();

                minProps = Mathf.Clamp(minProps, 0, 50);
                maxProps = Mathf.Clamp(maxProps, minProps, 50);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 조명 설정
            EditorGUILayout.LabelField("조명 설정", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            addLighting = EditorGUILayout.Toggle("조명 추가", addLighting);

            if (addLighting)
            {
                lightCount = EditorGUILayout.IntSlider("조명 개수", lightCount, 1, 10);
                lightIntensity = EditorGUILayout.Slider("조명 강도", lightIntensity, 0.1f, 3f);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 시드 설정
            EditorGUILayout.LabelField("랜덤 시드", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            useRandomSeed = EditorGUILayout.Toggle("랜덤 시드 사용", useRandomSeed);

            if (!useRandomSeed)
            {
                seed = EditorGUILayout.IntField("시드 값", seed);
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(20);

            // 생성 버튼
            EditorGUILayout.LabelField("생성", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("밀실 생성", GUILayout.Height(45)))
            {
                GenerateRoom();
            }

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.9f, 0.6f, 0.3f);
            if (GUILayout.Button("프리뷰 (벽만)", GUILayout.Height(30)))
            {
                GenerateRoom(previewOnly: true);
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // 유틸리티
            EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("프리팹 캐시 새로고침"))
            {
                LoadPrefabCache();
                EditorUtility.DisplayDialog("완료", "프리팹 캐시가 새로고침되었습니다.", "확인");
            }
            if (GUILayout.Button("NavMesh 베이크"))
            {
                BakeNavMesh();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("선택된 방 삭제"))
            {
                DeleteSelectedRoom();
            }

            EditorGUILayout.Space(10);

            // 에셋 정보
            EditorGUILayout.LabelField("로드된 에셋", EditorStyles.boldLabel);
            ShowAssetInfo();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 프리팹 캐시 로드
        /// </summary>
        private void LoadPrefabCache()
        {
            cachedPrefabs.Clear();

            // 병원 에셋
            cachedPrefabs["hospital_walls"] = LoadPrefabsFromPath(HOSPITAL_PACK_PATH, "Wall");
            cachedPrefabs["hospital_floors"] = LoadPrefabsFromPath(HOSPITAL_PACK_PATH, "Floor");
            cachedPrefabs["hospital_ceilings"] = LoadPrefabsFromPath(HOSPITAL_PACK_PATH, "Ceiling");
            cachedPrefabs["hospital_doors"] = LoadPrefabsFromPath(HOSPITAL_PACK_PATH, "Door");
            cachedPrefabs["hospital_props"] = LoadPrefabsFromPath(HOSPITAL_PACK_PATH, "Bed", "Lamp", "Med");

            // 공포 에셋
            cachedPrefabs["horror_props"] = LoadPrefabsFromPath(HORROR_PACK_PATH,
                "Bed", "chair", "Chair", "Wheelchair", "table", "Table", "Curtain", "lamp", "Lamp");
            cachedPrefabs["horror_doors"] = LoadPrefabsFromPath(HORROR_PACK_PATH, "Door");
            cachedPrefabs["horror_windows"] = LoadPrefabsFromPath(HORROR_PACK_PATH, "Window", "window");

            // 던전 에셋
            cachedPrefabs["dungeon_walls"] = LoadPrefabsFromPath(DUNGEON_PACK_PATH, "Wall");
            cachedPrefabs["dungeon_floors"] = LoadPrefabsFromPath(DUNGEON_PACK_PATH, "Floor");
            cachedPrefabs["dungeon_doors"] = LoadPrefabsFromPath(DUNGEON_PACK_PATH, "Door");
            cachedPrefabs["dungeon_props"] = LoadPrefabsFromPath(DUNGEON_PACK_PATH, "Lamp", "Pillar");
        }

        /// <summary>
        /// 경로에서 프리팹 로드
        /// </summary>
        private List<GameObject> LoadPrefabsFromPath(string path, params string[] filters)
        {
            var result = new List<GameObject>();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                // 필터가 없으면 모두 추가
                if (filters.Length == 0)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefab != null) result.Add(prefab);
                }
                else
                {
                    // 필터에 맞는 것만 추가
                    foreach (string filter in filters)
                    {
                        if (fileName.ToLower().Contains(filter.ToLower()))
                        {
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            if (prefab != null) result.Add(prefab);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 에셋 정보 표시
        /// </summary>
        private void ShowAssetInfo()
        {
            EditorGUI.indentLevel++;

            int totalCount = 0;
            foreach (var kvp in cachedPrefabs)
            {
                totalCount += kvp.Value.Count;
            }

            EditorGUILayout.LabelField($"총 {totalCount}개의 프리팹 로드됨");

            if (cachedPrefabs.ContainsKey("hospital_walls"))
                EditorGUILayout.LabelField($"  병원 벽: {cachedPrefabs["hospital_walls"].Count}개");
            if (cachedPrefabs.ContainsKey("horror_props"))
                EditorGUILayout.LabelField($"  공포 소품: {cachedPrefabs["horror_props"].Count}개");
            if (cachedPrefabs.ContainsKey("dungeon_walls"))
                EditorGUILayout.LabelField($"  던전 벽: {cachedPrefabs["dungeon_walls"].Count}개");

            EditorGUI.indentLevel--;
        }

        // 공통 타일 크기 (바닥, 벽, 천장이 모두 동일한 크기 사용)
        private float calculatedTileSize = 3f;
        private int calculatedTilesX = 7;
        private int calculatedTilesZ = 7;

        /// <summary>
        /// 방 생성
        /// </summary>
        private void GenerateRoom(bool previewOnly = false)
        {
            // 시드 설정
            if (useRandomSeed)
            {
                seed = Random.Range(0, int.MaxValue);
            }
            Random.InitState(seed);

            // 타일 크기 계산 (벽 프리팹 기준으로 통일)
            GameObject wallPrefab = GetPrefab("walls");
            calculatedTileSize = 3f;
            if (wallPrefab != null)
            {
                calculatedTileSize = GetPrefabWidth(wallPrefab);
                if (calculatedTileSize < 0.5f) calculatedTileSize = 3f;
            }

            // 타일 수 계산
            float requestedWidth = roomSize.x * cellSize;
            float requestedDepth = roomSize.y * cellSize;
            calculatedTilesX = Mathf.CeilToInt(requestedWidth / calculatedTileSize);
            calculatedTilesZ = Mathf.CeilToInt(requestedDepth / calculatedTileSize);

            Debug.Log($"[RoomGenerator] 타일 크기: {calculatedTileSize}m, 타일 수: {calculatedTilesX}x{calculatedTilesZ}");

            // 루트 오브젝트 생성
            GameObject roomRoot = new GameObject($"Room_{seed}");
            Undo.RegisterCreatedObjectUndo(roomRoot, "Generate Room");

            // 씬 뷰 중앙에 배치
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                roomRoot.transform.position = sceneView.pivot;
            }

            // 구조물 생성 (모두 동일한 타일 크기 사용)
            GenerateFloor(roomRoot);
            GenerateWalls(roomRoot);
            GenerateCeiling(roomRoot);
            GenerateDoors(roomRoot);

            if (!previewOnly)
            {
                // 소품 배치
                if (randomizeProps)
                {
                    GenerateProps(roomRoot);
                }

                // 조명 배치
                if (addLighting)
                {
                    GenerateLighting(roomRoot);
                }
            }

            // Static 설정
            SetStaticRecursive(roomRoot, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);

            Selection.activeGameObject = roomRoot;

            string message = previewOnly ? "프리뷰가 생성되었습니다." : "밀실이 생성되었습니다!";
            Debug.Log($"[RoomGenerator] {message} (시드: {seed})");

            if (!previewOnly)
            {
                EditorUtility.DisplayDialog("완료", $"{message}\n시드: {seed}", "확인");
            }
        }

        /// <summary>
        /// 바닥 생성 - calculatedTileSize 사용
        /// 바닥 프리팹 P_Floor_01: 피벗이 한쪽 모서리에 있고, 메시는 +X, -Z 방향으로 확장
        /// Min: (0, 0, -3), Max: (3, 0, 0) → 피벗에서 X는 +방향, Z는 -방향으로 타일이 있음
        /// </summary>
        private void GenerateFloor(GameObject parent)
        {
            GameObject floorParent = new GameObject("Floor");
            floorParent.transform.SetParent(parent.transform);

            GameObject floorPrefab = GetPrefab("floors");
            float tileSize = calculatedTileSize;
            float totalWidth = calculatedTilesX * tileSize;
            float totalDepth = calculatedTilesZ * tileSize;

            if (floorPrefab != null)
            {
                // 바닥 프리팹 바운드 분석
                Bounds floorBounds = GetPrefabBounds(floorPrefab);
                Debug.Log($"[RoomGenerator] 바닥 프리팹 - Min: {floorBounds.min}, Max: {floorBounds.max}");

                // P_Floor_01은 피벗이 (0,0,0)에 있고 메시가 +X, -Z 방향으로 확장됨
                // 따라서 타일을 (0,0,0)에 놓으면 실제 바닥은 X: 0~3, Z: -3~0 범위를 덮음
                // 바닥이 X: 0~totalWidth, Z: 0~totalDepth를 덮으려면:
                // 각 타일 위치는 (x * tileSize, 0, (z+1) * tileSize) 가 되어야 함

                for (int x = 0; x < calculatedTilesX; x++)
                {
                    for (int z = 0; z < calculatedTilesZ; z++)
                    {
                        // 바닥 프리팹이 -Z 방향으로 확장되므로, Z 위치를 +tileSize 해줌
                        Vector3 pos = new Vector3(
                            x * tileSize,
                            0,
                            (z + 1) * tileSize
                        );
                        GameObject floor = (GameObject)PrefabUtility.InstantiatePrefab(floorPrefab);
                        floor.transform.SetParent(floorParent.transform);
                        floor.transform.localPosition = pos;
                        floor.name = $"Floor_{x}_{z}";
                    }
                }
            }
            else
            {
                // 기본 평면 생성
                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "FloorPlane";
                floor.transform.SetParent(floorParent.transform);
                floor.transform.localPosition = new Vector3(totalWidth / 2f, 0, totalDepth / 2f);
                floor.transform.localScale = new Vector3(totalWidth / 10f, 1, totalDepth / 10f);

                var renderer = floor.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.2f, 0.2f, 0.2f);
                    renderer.material = mat;
                }
            }
        }

        /// <summary>
        /// 벽 생성 - 4면을 완전히 둘러싸는 닫힌 사각형
        /// 벽 프리팹은 +Z 방향을 바라봄 (테스트 결과 확인됨)
        /// </summary>
        private void GenerateWalls(GameObject parent)
        {
            GameObject wallParent = new GameObject("Walls");
            wallParent.transform.SetParent(parent.transform);

            // 벽 프리팹 미리 가져오기 (일관성을 위해)
            GameObject wallPrefab = GetPrefab("walls");
            if (wallPrefab == null) return;

            // 프리팹 바운드 분석
            Bounds bounds = GetPrefabBounds(wallPrefab);
            bool meshExtendsNegativeX = bounds.min.x < -0.1f;

            float tileSize = calculatedTileSize;
            int tilesX = calculatedTilesX;
            int tilesZ = calculatedTilesZ;

            float totalWidth = tilesX * tileSize;
            float totalDepth = tilesZ * tileSize;

            Debug.Log($"[RoomGenerator] 벽 프리팹 타입: {(meshExtendsNegativeX ? "P_Wall_02 (-X 확장)" : "P_Wall_01 (+X 확장)")}");

            // 벽 프리팹이 +Z 방향을 바라본다고 가정 (스크린샷에서 확인됨)
            // - 남쪽(Z=0): 방 안쪽(-Z→+Z)을 바라봐야 함 → 0도
            // - 북쪽(Z=totalDepth): 방 안쪽(+Z→-Z)을 바라봐야 함 → 180도
            // - 서쪽(X=0): 방 안쪽(-X→+X)을 바라봐야 함 → 90도
            // - 동쪽(X=totalWidth): 방 안쪽(+X→-X)을 바라봐야 함 → -90도

            // 남쪽 벽 (Z = 0)
            for (int i = 0; i < tilesX; i++)
            {
                float x = meshExtendsNegativeX ? (i + 1) * tileSize : i * tileSize;
                Vector3 pos = new Vector3(x, 0, 0);
                CreateSingleWall(wallParent, wallPrefab, pos, 0, "South", tileSize);
            }

            // 북쪽 벽 (Z = totalDepth)
            for (int i = 0; i < tilesX; i++)
            {
                float x = meshExtendsNegativeX ? i * tileSize : (i + 1) * tileSize;
                Vector3 pos = new Vector3(x, 0, totalDepth);
                CreateSingleWall(wallParent, wallPrefab, pos, 180, "North", tileSize);
            }

            // 서쪽 벽 (X = 0)
            for (int i = 0; i < tilesZ; i++)
            {
                float z = meshExtendsNegativeX ? i * tileSize : (i + 1) * tileSize;
                Vector3 pos = new Vector3(0, 0, z);
                CreateSingleWall(wallParent, wallPrefab, pos, 90, "West", tileSize);
            }

            // 동쪽 벽 (X = totalWidth)
            for (int i = 0; i < tilesZ; i++)
            {
                float z = meshExtendsNegativeX ? (i + 1) * tileSize : i * tileSize;
                Vector3 pos = new Vector3(totalWidth, 0, z);
                CreateSingleWall(wallParent, wallPrefab, pos, -90, "East", tileSize);
            }
        }

        /// <summary>
        /// 프리팹의 X축 폭 계산
        /// </summary>
        private float GetPrefabWidth(GameObject prefab)
        {
            Bounds bounds = GetPrefabBounds(prefab);
            // X축 크기 반환 (벽의 폭)
            return bounds.size.x;
        }

        /// <summary>
        /// 프리팹의 전체 바운드 계산 (로컬 좌표계 기준)
        /// </summary>
        private Bounds GetPrefabBounds(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one * 3f);

            // 임시로 프리팹 인스턴스화하여 정확한 로컬 바운드 계산
            GameObject tempInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            tempInstance.transform.position = Vector3.zero;
            tempInstance.transform.rotation = Quaternion.identity;

            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (var r in tempInstance.GetComponentsInChildren<Renderer>())
            {
                if (first)
                {
                    bounds = r.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            Debug.Log($"[RoomGenerator] 프리팹 '{prefab.name}' 바운드 - Center: {bounds.center}, Size: {bounds.size}, Min: {bounds.min}, Max: {bounds.max}");

            // 임시 인스턴스 제거
            Object.DestroyImmediate(tempInstance);

            return bounds;
        }

        /// <summary>
        /// 단일 벽 생성
        /// </summary>
        private void CreateSingleWall(GameObject parent, GameObject prefab, Vector3 position, float rotationY, string side, float segmentWidth)
        {
            GameObject wall;

            if (prefab != null)
            {
                wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                wall.transform.SetParent(parent.transform);
                wall.transform.localPosition = position;
                wall.transform.localRotation = Quaternion.Euler(0, rotationY, 0);
            }
            else
            {
                // 기본 큐브로 벽 생성
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.SetParent(parent.transform);
                wall.transform.localScale = new Vector3(segmentWidth, wallHeight, 0.2f);
                wall.transform.localPosition = new Vector3(position.x, wallHeight / 2f, position.z);
                wall.transform.localRotation = Quaternion.Euler(0, rotationY, 0);

                var renderer = wall.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.3f, 0.3f, 0.35f);
                    renderer.material = mat;
                }
            }

            wall.name = $"Wall_{side}_{parent.transform.childCount}";
        }

        /// <summary>
        /// 프리팹 높이 추정
        /// </summary>
        private float GetPrefabHeight(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 1f;

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            return Mathf.Max(bounds.size.y, 0.1f);
        }

        /// <summary>
        /// 천장 생성 - 바닥과 동일한 구조
        /// 천장 프리팹 P_Ceiling_01: 바닥과 동일하게 피벗에서 +X, -Z 방향으로 확장
        /// </summary>
        private void GenerateCeiling(GameObject parent)
        {
            GameObject ceilingParent = new GameObject("Ceiling");
            ceilingParent.transform.SetParent(parent.transform);

            GameObject ceilingPrefab = GetPrefab("ceilings");

            float tileSize = calculatedTileSize;
            int tilesX = calculatedTilesX;
            int tilesZ = calculatedTilesZ;
            float totalWidth = tilesX * tileSize;
            float totalDepth = tilesZ * tileSize;

            if (ceilingPrefab != null)
            {
                Bounds ceilingBounds = GetPrefabBounds(ceilingPrefab);
                Debug.Log($"[RoomGenerator] 천장 프리팹 - Min: {ceilingBounds.min}, Max: {ceilingBounds.max}");

                // 천장 프리팹도 바닥과 동일하게 +X, -Z 방향으로 확장됨
                for (int x = 0; x < tilesX; x++)
                {
                    for (int z = 0; z < tilesZ; z++)
                    {
                        Vector3 pos = new Vector3(
                            x * tileSize,
                            wallHeight,
                            (z + 1) * tileSize
                        );
                        GameObject ceiling = (GameObject)PrefabUtility.InstantiatePrefab(ceilingPrefab);
                        ceiling.transform.SetParent(ceilingParent.transform);
                        ceiling.transform.localPosition = pos;
                        ceiling.name = $"Ceiling_{x}_{z}";
                    }
                }
            }
            else
            {

                GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ceiling.name = "CeilingPlane";
                ceiling.transform.SetParent(ceilingParent.transform);
                ceiling.transform.localPosition = new Vector3(
                    totalWidth / 2f,
                    wallHeight,
                    totalDepth / 2f
                );
                ceiling.transform.localRotation = Quaternion.Euler(180, 0, 0);
                ceiling.transform.localScale = new Vector3(
                    totalWidth / 10f,
                    1,
                    totalDepth / 10f
                );

                var renderer = ceiling.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.25f, 0.25f, 0.25f);
                    renderer.material = mat;
                }
            }
        }

        /// <summary>
        /// 문 생성
        /// </summary>
        private void GenerateDoors(GameObject parent)
        {
            GameObject doorParent = new GameObject("Doors");
            doorParent.transform.SetParent(parent.transform);

            GameObject doorPrefab = GetPrefab("doors");
            if (doorPrefab == null) return;

            // 벽 방향 (0: 앞, 1: 뒤, 2: 왼, 3: 오른)
            List<int> availableWalls = new List<int> { 0, 1, 2, 3 };

            for (int i = 0; i < doorCount && availableWalls.Count > 0; i++)
            {
                int wallIndex = availableWalls[Random.Range(0, availableWalls.Count)];
                availableWalls.Remove(wallIndex);

                Vector3 pos = Vector3.zero;
                float rotation = 0;

                switch (wallIndex)
                {
                    case 0: // 앞
                        pos = new Vector3(Random.Range(1, roomSize.x - 1) * cellSize, 0, 0);
                        rotation = 0;
                        break;
                    case 1: // 뒤
                        pos = new Vector3(Random.Range(1, roomSize.x - 1) * cellSize, 0, roomSize.y * cellSize);
                        rotation = 180;
                        break;
                    case 2: // 왼
                        pos = new Vector3(0, 0, Random.Range(1, roomSize.y - 1) * cellSize);
                        rotation = 90;
                        break;
                    case 3: // 오른
                        pos = new Vector3(roomSize.x * cellSize, 0, Random.Range(1, roomSize.y - 1) * cellSize);
                        rotation = -90;
                        break;
                }

                GameObject door = (GameObject)PrefabUtility.InstantiatePrefab(doorPrefab);
                door.name = $"Door_{i}";
                door.transform.SetParent(doorParent.transform);
                door.transform.localPosition = pos;
                door.transform.localRotation = Quaternion.Euler(0, rotation, 0);

                // Door 스크립트 추가
                if (door.GetComponent<Door>() == null)
                {
                    var doorScript = door.AddComponent<Door>();
                    doorScript.isLocked = (i == 0); // 첫 번째 문은 잠김
                    if (doorScript.isLocked)
                    {
                        doorScript.requiredKeyId = "exit_key";
                    }
                }
            }
        }

        // 배치된 소품 정보 (위치, 반경)
        private struct PlacedProp
        {
            public Vector3 position;
            public float radius;

            public PlacedProp(Vector3 pos, float r)
            {
                position = pos;
                radius = r;
            }
        }

        /// <summary>
        /// 소품 생성
        /// </summary>
        private void GenerateProps(GameObject parent)
        {
            GameObject propParent = new GameObject("Props");
            propParent.transform.SetParent(parent.transform);

            List<GameObject> propPrefabs = GetPropPrefabs();
            if (propPrefabs.Count == 0)
            {
                Debug.LogWarning("[RoomGenerator] 소품 프리팹이 없습니다.");
                return;
            }

            int propCount = Random.Range(minProps, maxProps + 1);
            float margin = cellSize * 0.8f; // 벽에서 떨어진 거리 (증가)
            float totalWidth = roomSize.x * cellSize;
            float totalDepth = roomSize.y * cellSize;

            List<PlacedProp> placedProps = new List<PlacedProp>();

            for (int i = 0; i < propCount; i++)
            {
                // 먼저 프리팹 선택 (크기를 미리 알아야 함)
                GameObject prefab = propPrefabs[Random.Range(0, propPrefabs.Count)];
                float propRadius = GetPrefabRadius(prefab);

                // 랜덤 위치 (벽에서 떨어진 곳)
                Vector3 pos = Vector3.zero;
                int attempts = 0;
                bool validPos = false;

                while (!validPos && attempts < 100)
                {
                    pos = new Vector3(
                        Random.Range(margin, totalWidth - margin),
                        0,
                        Random.Range(margin, totalDepth - margin)
                    );

                    // 다른 소품과 겹치지 않는지 확인
                    validPos = true;
                    foreach (var placed in placedProps)
                    {
                        float minDistance = propRadius + placed.radius + 0.3f; // 0.3m 여유 공간
                        if (Vector3.Distance(pos, placed.position) < minDistance)
                        {
                            validPos = false;
                            break;
                        }
                    }
                    attempts++;
                }

                if (!validPos)
                {
                    // 공간이 부족하면 더 이상 배치하지 않음
                    Debug.Log($"[RoomGenerator] 소품 {i + 1}/{propCount}: 배치 공간 부족");
                    continue;
                }

                placedProps.Add(new PlacedProp(pos, propRadius));

                GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                prop.transform.SetParent(propParent.transform);
                prop.transform.localPosition = pos;
                prop.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }

            Debug.Log($"[RoomGenerator] 총 {placedProps.Count}개의 소품 배치 완료");
        }

        /// <summary>
        /// 프리팹의 대략적인 반경 계산
        /// </summary>
        private float GetPrefabRadius(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 0.5f;

            Bounds bounds = new Bounds(prefab.transform.position, Vector3.zero);
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            // XZ 평면에서의 반경 (대각선의 절반)
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            return Mathf.Max(radius, 0.3f); // 최소 0.3m
        }

        /// <summary>
        /// 조명 생성
        /// </summary>
        private void GenerateLighting(GameObject parent)
        {
            GameObject lightParent = new GameObject("Lighting");
            lightParent.transform.SetParent(parent.transform);

            // 앰비언트 라이트 (약간 어둡게)
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.12f);

            float totalWidth = roomSize.x * cellSize;
            float totalDepth = roomSize.y * cellSize;

            for (int i = 0; i < lightCount; i++)
            {
                // 랜덤 또는 균등 배치
                Vector3 pos;
                if (lightCount <= 2)
                {
                    // 중앙 배치
                    float offset = (i == 0) ? -0.25f : 0.25f;
                    pos = new Vector3(
                        totalWidth / 2f + totalWidth * offset,
                        wallHeight - 0.3f,
                        totalDepth / 2f
                    );
                }
                else
                {
                    // 랜덤 배치
                    pos = new Vector3(
                        Random.Range(cellSize, totalWidth - cellSize),
                        wallHeight - 0.3f,
                        Random.Range(cellSize, totalDepth - cellSize)
                    );
                }

                GameObject lightObj = new GameObject($"Light_{i}");
                lightObj.transform.SetParent(lightParent.transform);
                lightObj.transform.localPosition = pos;

                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = cellSize * 2f;
                light.intensity = lightIntensity;
                light.color = new Color(1f, 0.9f, 0.8f); // 따뜻한 색

                // 깜빡임 효과 (선택적)
                if (Random.value > 0.7f)
                {
                    var flicker = lightObj.AddComponent<LightFlicker>();
                }
            }
        }

        /// <summary>
        /// 스타일에 맞는 프리팹 가져오기
        /// </summary>
        private GameObject GetPrefab(string type)
        {
            string key = "";

            switch (roomStyle)
            {
                case RoomStyle.Hospital:
                    key = $"hospital_{type}";
                    break;
                case RoomStyle.Dungeon:
                    key = $"dungeon_{type}";
                    break;
                case RoomStyle.Mixed:
                    // 랜덤 선택
                    key = Random.value > 0.5f ? $"hospital_{type}" : $"dungeon_{type}";
                    break;
            }

            if (cachedPrefabs.ContainsKey(key) && cachedPrefabs[key].Count > 0)
            {
                return cachedPrefabs[key][Random.Range(0, cachedPrefabs[key].Count)];
            }

            // 대체 키 시도
            string altKey = $"hospital_{type}";
            if (cachedPrefabs.ContainsKey(altKey) && cachedPrefabs[altKey].Count > 0)
            {
                return cachedPrefabs[altKey][Random.Range(0, cachedPrefabs[altKey].Count)];
            }

            return null;
        }

        /// <summary>
        /// 소품 프리팹 목록 가져오기
        /// </summary>
        private List<GameObject> GetPropPrefabs()
        {
            var result = new List<GameObject>();

            switch (roomStyle)
            {
                case RoomStyle.Hospital:
                    if (cachedPrefabs.ContainsKey("hospital_props"))
                        result.AddRange(cachedPrefabs["hospital_props"]);
                    if (cachedPrefabs.ContainsKey("horror_props"))
                        result.AddRange(cachedPrefabs["horror_props"]);
                    break;

                case RoomStyle.Dungeon:
                    if (cachedPrefabs.ContainsKey("dungeon_props"))
                        result.AddRange(cachedPrefabs["dungeon_props"]);
                    break;

                case RoomStyle.Mixed:
                    foreach (var kvp in cachedPrefabs)
                    {
                        if (kvp.Key.Contains("props"))
                        {
                            result.AddRange(kvp.Value);
                        }
                    }
                    break;
            }

            return result;
        }

        /// <summary>
        /// 프리팹 크기 추정
        /// </summary>
        private float GetPrefabSize(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 1f;

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            return Mathf.Max(bounds.size.x, bounds.size.z);
        }

        /// <summary>
        /// Static 플래그 재귀 설정
        /// </summary>
        private void SetStaticRecursive(GameObject obj, StaticEditorFlags flags)
        {
            GameObjectUtility.SetStaticEditorFlags(obj, flags);
            foreach (Transform child in obj.transform)
            {
                SetStaticRecursive(child.gameObject, flags);
            }
        }

        /// <summary>
        /// NavMesh 베이크
        /// </summary>
        private void BakeNavMesh()
        {
            #pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            #pragma warning restore CS0618
            Debug.Log("[RoomGenerator] NavMesh 베이크 완료");
        }

        /// <summary>
        /// 선택된 방 삭제
        /// </summary>
        private void DeleteSelectedRoom()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null && selected.name.StartsWith("Room_"))
            {
                Undo.DestroyObjectImmediate(selected);
                Debug.Log("[RoomGenerator] 방이 삭제되었습니다.");
            }
            else
            {
                EditorUtility.DisplayDialog("알림", "Room_으로 시작하는 오브젝트를 선택해주세요.", "확인");
            }
        }

        /// <summary>
        /// 빠른 밀실 생성
        /// </summary>
        [MenuItem("Horror Game/밀실 빠른 생성", false, 121)]
        public static void QuickGenerateRoom()
        {
            var tool = CreateInstance<RoomGeneratorTool>();
            tool.LoadPrefabCache();
            tool.GenerateRoom();
            DestroyImmediate(tool);
        }
    }
}
