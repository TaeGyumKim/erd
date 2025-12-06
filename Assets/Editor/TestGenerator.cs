using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using HorrorGame;
using System.Collections.Generic;

/// <summary>
/// 테스트용 맵 자동 생성기
/// 실제 에셋을 활용하여 5개 방 + 복도 구조 생성
/// 플레이어, 살인마, 유령 모두 배치
/// </summary>
public class TestGenerator : EditorWindow
{
    private Vector2 scrollPosition;

    // 설정
    private float roomSize = 6f;
    private float corridorWidth = 3f;
    private float wallHeight = 3f;

    // 에셋 캐시
    private static Dictionary<string, GameObject> assetCache = new Dictionary<string, GameObject>();

    #region Asset Paths

    private static class Assets
    {
        // Hospital Horror Pack
        public const string HospitalWall = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Wall_01.prefab";
        public const string HospitalFloor = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Floor_01.prefab";
        public const string HospitalCeiling = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Ceiling_01.prefab";
        public const string HospitalDoor = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Door_01_.prefab";
        public const string HospitalBed = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Bed_01.prefab";
        public const string HospitalLamp = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Lamp.prefab";

        // Horror Game Pack
        public const string HorrorDoor = "Assets/Asset pack for horror game/Prefabs/Door.prefab";
        public const string HorrorBed = "Assets/Asset pack for horror game/Prefabs/Bed.prefab";
        public const string HorrorTable = "Assets/Asset pack for horror game/Prefabs/Wooden table.prefab";
        public const string HorrorChair = "Assets/Asset pack for horror game/Prefabs/Wood chair.prefab";
        public const string HorrorKey = "Assets/Asset pack for horror game/Prefabs/Key.prefab";
        public const string HorrorWheelchair = "Assets/Asset pack for horror game/Prefabs/Wheelchair.prefab";
        public const string HorrorWindow = "Assets/Asset pack for horror game/Prefabs/Window.prefab";
        public const string HorrorLamp = "Assets/Asset pack for horror game/Prefabs/Fluorescent lamp.prefab";
        public const string HorrorRoom = "Assets/Asset pack for horror game/Prefabs/Room.prefab";

        // PSX Crates and Barrels
        public const string Chest = "Assets/McSteeg/PSX Style Crates and Barrels Pack/Prefabs/Chest_1.prefab";
        public const string Barrel = "Assets/McSteeg/PSX Style Crates and Barrels Pack/Prefabs/Barrel_1.prefab";
        public const string Crate = "Assets/McSteeg/PSX Style Crates and Barrels Pack/Prefabs/Crate_1.prefab";

        // Character
        public const string Character = "Assets/Character/TG_Hero_Interactive_Prefab.prefab";
        public const string Mask = "Assets/Common/Mask/Mask_Prefab.prefab";
    }

    #endregion

    [MenuItem("Horror Game/Test Generator %#t")]
    public static void ShowWindow()
    {
        var window = GetWindow<TestGenerator>("Test Generator");
        window.minSize = new Vector2(400, 550);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawSettings();
        DrawGenerateButtons();
        DrawIndividualRooms();
        DrawUtilities();
        DrawCleanup();

        EditorGUILayout.EndScrollView();
    }

    #region GUI Drawing

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Test Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "기획서 기반 5개 방 + 복도 구조 테스트 맵 생성\n" +
            "플레이어, 살인마, 유령 자동 배치\n\n" +
            "구조: Room1(시작) → Room2(복도) → Room3(책방)\n" +
            "                    ↓\n" +
            "              Room4(상자) → Room5(탈출)",
            MessageType.Info);
    }

    private void DrawSettings()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("맵 설정", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            roomSize = EditorGUILayout.Slider("방 크기", roomSize, 4f, 10f);
            corridorWidth = EditorGUILayout.Slider("복도 너비", corridorWidth, 2f, 5f);
            wallHeight = EditorGUILayout.Slider("벽 높이", wallHeight, 2.5f, 4f);
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawGenerateButtons()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("맵 생성", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("전체 테스트 맵 생성\n(맵 + 캐릭터 + 게임 시스템)", GUILayout.Height(50)))
            {
                GenerateFullTestMap();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("맵만 생성", GUILayout.Height(30)))
            {
                GenerateMapOnly();
            }
            if (GUILayout.Button("캐릭터만 생성", GUILayout.Height(30)))
            {
                GenerateCharactersOnly();
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawIndividualRooms()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("개별 방 생성", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Room1\n시작방", GUILayout.Height(40)))
                GenerateSingleRoom(1);
            if (GUILayout.Button("Room2\n복도", GUILayout.Height(40)))
                GenerateSingleRoom(2);
            if (GUILayout.Button("Room3\n책방", GUILayout.Height(40)))
                GenerateSingleRoom(3);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Room4\n상자방", GUILayout.Height(40)))
                GenerateSingleRoom(4);
            if (GUILayout.Button("Room5\n탈출구", GUILayout.Height(40)))
                GenerateSingleRoom(5);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawUtilities()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("유틸리티", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("NavMesh 베이크", GUILayout.Height(30)))
            {
                BakeNavMesh();
            }
            if (GUILayout.Button("참조 자동 연결", GUILayout.Height(30)))
            {
                AutoConnectReferences();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("에셋 캐시 초기화", GUILayout.Height(25)))
            {
                assetCache.Clear();
                Debug.Log("[TestGenerator] 에셋 캐시 초기화됨");
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawCleanup()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("정리", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("테스트 맵 삭제", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("확인", "테스트 맵을 삭제하시겠습니까?", "삭제", "취소"))
                {
                    DeleteTestMap();
                }
            }
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndVertical();
    }

    #endregion

    #region Main Generation

    private void GenerateFullTestMap()
    {
        DeleteTestMap();

        var root = new GameObject("=== TEST MAP ===");
        Undo.RegisterCreatedObjectUndo(root, "Generate Test Map");

        // 구조 생성
        var rooms = CreateChild(root, "Rooms");
        var doors = CreateChild(root, "Doors");
        var items = CreateChild(root, "Items");
        var characters = CreateChild(root, "Characters");
        var triggers = CreateChild(root, "Triggers");
        var lighting = CreateChild(root, "Lighting");
        var managers = CreateChild(root, "Managers");

        // 방 생성
        GenerateRoom1(rooms.transform);
        GenerateRoom2(rooms.transform);
        GenerateRoom3(rooms.transform);
        GenerateRoom4(rooms.transform);
        GenerateRoom5(rooms.transform);

        // 문 생성
        GenerateDoors(doors.transform);

        // 아이템 생성
        GenerateItems(items.transform);

        // 캐릭터 생성 (플레이어, 살인마, 유령)
        GeneratePlayer(characters.transform);
        GenerateKiller(characters.transform);
        GenerateGhosts(characters.transform);

        // 트리거 생성
        GenerateTriggers(triggers.transform);

        // 조명 생성
        GenerateLighting(lighting.transform);

        // 게임 매니저 생성
        GenerateManagers(managers.transform);

        // NavMesh 베이크
        BakeNavMesh();

        // 참조 연결
        AutoConnectReferences();

        Selection.activeGameObject = root;
        SceneView.lastActiveSceneView?.FrameSelected();

        ShowCompletionDialog();
    }

    private void GenerateMapOnly()
    {
        DeleteTestMap();

        var root = new GameObject("=== TEST MAP ===");
        Undo.RegisterCreatedObjectUndo(root, "Generate Map Only");

        var rooms = CreateChild(root, "Rooms");
        var doors = CreateChild(root, "Doors");
        var items = CreateChild(root, "Items");
        var lighting = CreateChild(root, "Lighting");

        GenerateRoom1(rooms.transform);
        GenerateRoom2(rooms.transform);
        GenerateRoom3(rooms.transform);
        GenerateRoom4(rooms.transform);
        GenerateRoom5(rooms.transform);
        GenerateDoors(doors.transform);
        GenerateItems(items.transform);
        GenerateLighting(lighting.transform);

        BakeNavMesh();

        Selection.activeGameObject = root;
        Debug.Log("[TestGenerator] 맵 생성 완료");
    }

    private void GenerateCharactersOnly()
    {
        var root = GameObject.Find("=== TEST MAP ===");
        if (root == null)
        {
            root = new GameObject("=== TEST MAP ===");
            Undo.RegisterCreatedObjectUndo(root, "Generate Characters");
        }

        var characters = root.transform.Find("Characters")?.gameObject;
        if (characters == null)
        {
            characters = CreateChild(root, "Characters");
        }

        // 기존 캐릭터 삭제
        while (characters.transform.childCount > 0)
        {
            DestroyImmediate(characters.transform.GetChild(0).gameObject);
        }

        GeneratePlayer(characters.transform);
        GenerateKiller(characters.transform);
        GenerateGhosts(characters.transform);

        Debug.Log("[TestGenerator] 캐릭터 생성 완료");
    }

    private void GenerateSingleRoom(int roomNumber)
    {
        var parent = new GameObject($"Room{roomNumber}_Generated");
        Undo.RegisterCreatedObjectUndo(parent, $"Generate Room {roomNumber}");

        switch (roomNumber)
        {
            case 1: GenerateRoom1(parent.transform); break;
            case 2: GenerateRoom2(parent.transform); break;
            case 3: GenerateRoom3(parent.transform); break;
            case 4: GenerateRoom4(parent.transform); break;
            case 5: GenerateRoom5(parent.transform); break;
        }

        Selection.activeGameObject = parent;
    }

    #endregion

    #region Room Generation

    private GameObject GenerateRoom1(Transform parent)
    {
        var room = CreateRoomStructure("Room1 - 시작방", Vector3.zero, roomSize, roomSize, parent);

        // 침대 배치
        var bed = InstantiatePrefab(Assets.HorrorBed, room.transform,
            new Vector3(-roomSize / 4, 0, 0), Quaternion.Euler(0, 90, 0));
        if (bed == null)
        {
            bed = CreateFallbackPrimitive("Bed", PrimitiveType.Cube, room.transform,
                new Vector3(-roomSize / 4, 0.3f, 0), new Vector3(2f, 0.6f, 1f), new Color(0.4f, 0.3f, 0.2f));
        }

        // 포크 열쇠 스폰 위치
        CreateSpawnPoint("ForkKey_Spawn", room.transform, new Vector3(roomSize / 4, 0.8f, 0));

        // 문 위치 마커
        CreateSpawnPoint("Door_To_Room2", room.transform, new Vector3(roomSize / 2, 0, 0));

        return room;
    }

    private GameObject GenerateRoom2(Transform parent)
    {
        float corridorLength = roomSize * 2;
        var room = CreateRoomStructure("Room2 - 복도",
            new Vector3(roomSize + corridorWidth, 0, 0),
            corridorLength, corridorWidth, parent);

        // 배럴 배치
        var barrel = InstantiatePrefab(Assets.Barrel, room.transform,
            new Vector3(corridorLength / 4, 0, 0), Quaternion.identity);
        if (barrel == null)
        {
            barrel = CreateFallbackPrimitive("Barrel", PrimitiveType.Cylinder, room.transform,
                new Vector3(corridorLength / 4, 0.5f, 0), new Vector3(0.6f, 0.5f, 0.6f), new Color(0.5f, 0.3f, 0.1f));
        }

        // 열쇠 스폰
        CreateSpawnPoint("Key_Spawn", room.transform, new Vector3(corridorLength / 4, 1.1f, 0));

        // 숨기 장소 (옷장 대용 - 상자)
        var hidingSpot = CreateFallbackPrimitive("HidingSpot_Wardrobe", PrimitiveType.Cube, room.transform,
            new Vector3(-corridorLength / 4, 1f, corridorWidth / 2 - 0.5f),
            new Vector3(1.2f, 2f, 0.8f), new Color(0.3f, 0.2f, 0.1f));

        // HidingSpot 컴포넌트 추가
        var hidingComponent = hidingSpot.AddComponent<HidingSpot>();
        var hidePos = new GameObject("HidePosition");
        hidePos.transform.SetParent(hidingSpot.transform);
        hidePos.transform.localPosition = new Vector3(0, 0, -0.5f);
        hidingComponent.hidePosition = hidePos.transform;

        return room;
    }

    private GameObject GenerateRoom3(Transform parent)
    {
        var room = CreateRoomStructure("Room3 - 책방",
            new Vector3(roomSize + corridorWidth + roomSize * 2, 0, corridorWidth),
            roomSize, roomSize, parent);

        // 테이블 배치
        var table = InstantiatePrefab(Assets.HorrorTable, room.transform,
            new Vector3(0, 0, 0), Quaternion.identity);
        if (table == null)
        {
            table = CreateFallbackPrimitive("Table", PrimitiveType.Cube, room.transform,
                new Vector3(0, 0.4f, 0), new Vector3(1.5f, 0.8f, 1f), new Color(0.4f, 0.25f, 0.1f));
        }

        // 책 스폰
        CreateSpawnPoint("Book_Spawn", room.transform, new Vector3(0, 0.85f, 0));

        // 창문
        var window = InstantiatePrefab(Assets.HorrorWindow, room.transform,
            new Vector3(0, wallHeight / 2, roomSize / 2 - 0.1f), Quaternion.identity);
        if (window == null)
        {
            window = CreateFallbackPrimitive("Window", PrimitiveType.Cube, room.transform,
                new Vector3(0, wallHeight / 2, roomSize / 2 - 0.1f),
                new Vector3(1.5f, 1.2f, 0.1f), new Color(0.5f, 0.7f, 0.9f, 0.5f));
        }

        // 살인마 창문 위치
        CreateSpawnPoint("Killer_WindowPosition", room.transform, new Vector3(0, 0, roomSize / 2 + 1f));

        // 휠체어
        var wheelchair = InstantiatePrefab(Assets.HorrorWheelchair, room.transform,
            new Vector3(roomSize / 3, 0, roomSize / 3), Quaternion.Euler(0, -45, 0));

        return room;
    }

    private GameObject GenerateRoom4(Transform parent)
    {
        var room = CreateRoomStructure("Room4 - 상자방",
            new Vector3(roomSize + corridorWidth + roomSize, 0, -roomSize / 2 - corridorWidth / 2),
            roomSize, roomSize, parent);

        // 상자 스폰 위치
        CreateSpawnPoint("Chest_Spawn", room.transform, new Vector3(0, 0.3f, roomSize / 3));

        // 선반
        var shelf = CreateFallbackPrimitive("Shelf", PrimitiveType.Cube, room.transform,
            new Vector3(-roomSize / 3, 1f, 0), new Vector3(0.4f, 2f, 1.5f), new Color(0.35f, 0.2f, 0.1f));

        // 크레이트 배치
        var crate = InstantiatePrefab(Assets.Crate, room.transform,
            new Vector3(roomSize / 3, 0, -roomSize / 4), Quaternion.Euler(0, 15, 0));

        return room;
    }

    private GameObject GenerateRoom5(Transform parent)
    {
        var room = CreateRoomStructure("Room5 - 탈출구",
            new Vector3(roomSize + corridorWidth + roomSize * 2, 0, -roomSize / 2 - corridorWidth / 2),
            roomSize / 2, roomSize, parent);

        // 탈출문 스폰
        CreateSpawnPoint("ExitDoor_Spawn", room.transform, new Vector3(roomSize / 4, 0, 0));

        // 탈출 빛
        var exitLight = new GameObject("ExitLight");
        exitLight.transform.SetParent(room.transform);
        exitLight.transform.localPosition = new Vector3(roomSize / 4, wallHeight - 0.5f, 0);
        var light = exitLight.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.95f, 0.8f);
        light.intensity = 2f;
        light.range = 8f;

        return room;
    }

    #endregion

    #region Room Structure Helper

    private GameObject CreateRoomStructure(string name, Vector3 position, float width, float depth, Transform parent)
    {
        var room = new GameObject(name);
        room.transform.SetParent(parent);
        room.transform.position = position;

        // 바닥 - 프리팹 시도, 실패시 기본 프리미티브
        var floor = InstantiatePrefab(Assets.HospitalFloor, room.transform, Vector3.zero, Quaternion.identity);
        if (floor != null)
        {
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(width / 4f, 1, depth / 4f);
            floor.isStatic = true;
            SetNavigationStatic(floor);
        }
        else
        {
            floor = CreateFallbackPrimitive("Floor", PrimitiveType.Cube, room.transform,
                new Vector3(0, -0.05f, 0), new Vector3(width, 0.1f, depth), new Color(0.3f, 0.3f, 0.3f));
            floor.isStatic = true;
            SetNavigationStatic(floor);
        }

        // 천장
        var ceiling = InstantiatePrefab(Assets.HospitalCeiling, room.transform,
            new Vector3(0, wallHeight, 0), Quaternion.identity);
        if (ceiling != null)
        {
            ceiling.name = "Ceiling";
            ceiling.transform.localScale = new Vector3(width / 4f, 1, depth / 4f);
            ceiling.isStatic = true;
        }
        else
        {
            ceiling = CreateFallbackPrimitive("Ceiling", PrimitiveType.Cube, room.transform,
                new Vector3(0, wallHeight, 0), new Vector3(width, 0.1f, depth), new Color(0.4f, 0.4f, 0.4f));
            ceiling.isStatic = true;
        }

        // 벽 생성
        CreateWall("Wall_Front", room.transform, new Vector3(0, wallHeight / 2, -depth / 2),
            new Vector3(width, wallHeight, 0.2f));
        CreateWall("Wall_Back", room.transform, new Vector3(0, wallHeight / 2, depth / 2),
            new Vector3(width, wallHeight, 0.2f));
        CreateWall("Wall_Left", room.transform, new Vector3(-width / 2, wallHeight / 2, 0),
            new Vector3(0.2f, wallHeight, depth));
        CreateWall("Wall_Right", room.transform, new Vector3(width / 2, wallHeight / 2, 0),
            new Vector3(0.2f, wallHeight, depth));

        // 방 조명
        var roomLight = new GameObject("RoomLight");
        roomLight.transform.SetParent(room.transform);
        roomLight.transform.localPosition = new Vector3(0, wallHeight - 0.5f, 0);
        var lightComp = roomLight.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = new Color(1f, 0.9f, 0.7f);
        lightComp.intensity = 1f;
        lightComp.range = Mathf.Max(width, depth) * 1.2f;

        return room;
    }

    private GameObject CreateWall(string name, Transform parent, Vector3 localPos, Vector3 scale)
    {
        var wall = InstantiatePrefab(Assets.HospitalWall, parent, localPos, Quaternion.identity);
        if (wall != null)
        {
            wall.name = name;
            wall.transform.localScale = new Vector3(scale.x / 4f, scale.y / 3f, 1);
            wall.isStatic = true;
            SetNavigationStatic(wall);
        }
        else
        {
            wall = CreateFallbackPrimitive(name, PrimitiveType.Cube, parent, localPos, scale, new Color(0.5f, 0.45f, 0.4f));
            wall.isStatic = true;
            SetNavigationStatic(wall);
        }
        return wall;
    }

    #endregion

    #region Doors Generation

    private void GenerateDoors(Transform parent)
    {
        // Room1 → Room2
        CreateDoor("Door_Room1", new Vector3(roomSize / 2, 0, 0),
            Quaternion.Euler(0, 90, 0), "fork_key_room1", parent);

        // Room2 → Room3
        CreateDoor("Door_Room3",
            new Vector3(roomSize + corridorWidth + roomSize * 1.5f, 0, corridorWidth / 2),
            Quaternion.identity, "key_room2", parent);

        // Room2 → Room4 (잠금 없음)
        CreateDoor("Door_Room4",
            new Vector3(roomSize + corridorWidth + roomSize, 0, -corridorWidth / 2),
            Quaternion.identity, "", parent);

        // Room4 → Room5 (잠금 없음)
        CreateDoor("Door_Room4to5",
            new Vector3(roomSize + corridorWidth + roomSize * 1.5f, 0, -roomSize / 2 - corridorWidth / 2),
            Quaternion.Euler(0, 90, 0), "", parent);
    }

    private GameObject CreateDoor(string name, Vector3 position, Quaternion rotation, string keyId, Transform parent)
    {
        var door = InstantiatePrefab(Assets.HorrorDoor, parent, position, rotation);
        if (door == null)
        {
            door = CreateFallbackPrimitive(name, PrimitiveType.Cube, parent, position,
                new Vector3(0.1f, 2.2f, 1f), new Color(0.4f, 0.25f, 0.15f));
            door.transform.rotation = rotation;
        }

        door.name = name;
        var doorComp = door.GetComponent<Door>();
        if (doorComp == null)
        {
            doorComp = door.AddComponent<Door>();
        }

        if (!string.IsNullOrEmpty(keyId))
        {
            doorComp.isLocked = true;
            doorComp.requiredKeyId = keyId;
        }

        return door;
    }

    #endregion

    #region Items Generation

    private void GenerateItems(Transform parent)
    {
        // 포크 열쇠 (Room1)
        CreateKeyItem("ForkKey_Room1", new Vector3(roomSize / 4, 0.8f, 0),
            "fork_key_room1", "포크 열쇠", parent);

        // 열쇠 (Room2)
        CreateKeyItem("Key_Room2",
            new Vector3(roomSize + corridorWidth + roomSize / 2, 1.1f, 0),
            "key_room2", "Room3 열쇠", parent);

        // 책 (Room3)
        CreateBook("Book_Room3",
            new Vector3(roomSize + corridorWidth + roomSize * 2, 0.85f, corridorWidth),
            parent);

        // 상자 (Room4)
        CreateChest("Chest_Room4",
            new Vector3(roomSize + corridorWidth + roomSize, 0.3f, -roomSize / 2 - corridorWidth / 2 + roomSize / 3),
            parent);

        // 탈출문 (Room5)
        CreateExitDoor("ExitDoor_Room5",
            new Vector3(roomSize + corridorWidth + roomSize * 2.25f, 0, -roomSize / 2 - corridorWidth / 2),
            parent);
    }

    private GameObject CreateKeyItem(string name, Vector3 position, string keyId, string keyName, Transform parent)
    {
        var key = InstantiatePrefab(Assets.HorrorKey, parent, position, Quaternion.identity);
        if (key == null)
        {
            key = CreateFallbackPrimitive(name, PrimitiveType.Capsule, parent, position,
                new Vector3(0.1f, 0.05f, 0.1f), Color.yellow);
        }

        key.name = name;
        key.GetComponent<Collider>().isTrigger = true;

        var keyItem = key.GetComponent<KeyItem>();
        if (keyItem == null)
        {
            keyItem = key.AddComponent<KeyItem>();
        }
        keyItem.keyId = keyId;
        keyItem.keyName = keyName;

        return key;
    }

    private GameObject CreateBook(string name, Vector3 position, Transform parent)
    {
        var book = CreateFallbackPrimitive(name, PrimitiveType.Cube, parent, position,
            new Vector3(0.3f, 0.05f, 0.2f), new Color(0.5f, 0.2f, 0.1f));
        book.GetComponent<Collider>().isTrigger = true;

        var bookComp = book.AddComponent<InteractableBook>();
        bookComp.bookTitle = "오래된 일기";
        bookComp.bookContent = "이곳에서 끔찍한 일이 일어났다...\n\n누군가가 여기서 실험을 했다.\n\n상자의 비밀번호는... 1234";
        bookComp.passwordHint = "1234";

        return book;
    }

    private GameObject CreateChest(string name, Vector3 position, Transform parent)
    {
        var chest = InstantiatePrefab(Assets.Chest, parent, position, Quaternion.identity);
        if (chest == null)
        {
            chest = CreateFallbackPrimitive(name, PrimitiveType.Cube, parent, position,
                new Vector3(0.8f, 0.5f, 0.5f), new Color(0.4f, 0.3f, 0.2f));
        }

        chest.name = name;

        // Collider 트리거 설정
        var col = chest.GetComponent<Collider>();
        if (col == null) col = chest.AddComponent<BoxCollider>();
        col.isTrigger = true;

        // PasswordChest 컴포넌트
        var chestComp = chest.AddComponent<PasswordChest>();
        chestComp.correctPassword = "1234";
        chestComp.passwordLength = 4;

        // 뚜껑
        var lid = CreateFallbackPrimitive("Lid", PrimitiveType.Cube, chest.transform,
            new Vector3(0, 0.3f, 0.2f), new Vector3(0.78f, 0.1f, 0.48f), new Color(0.45f, 0.35f, 0.25f));
        chestComp.lid = lid.transform;

        // 탈출 열쇠
        var escapeKey = CreateFallbackPrimitive("EscapeKey", PrimitiveType.Capsule, chest.transform,
            new Vector3(0, 0.1f, 0), new Vector3(0.1f, 0.05f, 0.1f), Color.yellow);
        escapeKey.GetComponent<Collider>().isTrigger = true;
        var keyComp = escapeKey.AddComponent<KeyItem>();
        keyComp.keyId = "escape_key";
        keyComp.keyName = "탈출 열쇠";
        escapeKey.SetActive(false);
        chestComp.containedItem = escapeKey;

        return chest;
    }

    private GameObject CreateExitDoor(string name, Vector3 position, Transform parent)
    {
        var exitDoor = CreateFallbackPrimitive(name, PrimitiveType.Cube, parent, position,
            new Vector3(2f, 3f, 0.2f), new Color(0.3f, 0.25f, 0.2f));

        var slidingDoor = exitDoor.AddComponent<SlidingDoor>();
        slidingDoor.requiredItemId = "escape_key";
        slidingDoor.slideDirection = Vector3.up;
        slidingDoor.openDistance = 3f;
        slidingDoor.slowOpenDuration = 5f;

        return exitDoor;
    }

    #endregion

    #region Characters Generation

    private void GeneratePlayer(Transform parent)
    {
        Vector3 spawnPos = new Vector3(0, 0.5f, 0);

        // 기존 플레이어 확인
        var existingPC = Object.FindFirstObjectByType<PCPlayerController>();
        if (existingPC != null)
        {
            existingPC.transform.position = spawnPos;
            return;
        }

        var existingVR = Object.FindFirstObjectByType<VRPlayer>();
        if (existingVR != null)
        {
            existingVR.transform.position = spawnPos;
            return;
        }

        // PC 플레이어 생성
        var player = new GameObject("PC Player");
        player.transform.SetParent(parent);
        player.transform.position = spawnPos;
        player.tag = "Player";

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.3f;
        controller.center = new Vector3(0, 0.9f, 0);

        player.AddComponent<PCPlayerController>();

        // 카메라
        var cameraHolder = new GameObject("CameraHolder");
        cameraHolder.transform.SetParent(player.transform);
        cameraHolder.transform.localPosition = new Vector3(0, 1.6f, 0);

        var camObj = new GameObject("PlayerCamera");
        camObj.transform.SetParent(cameraHolder.transform);
        camObj.transform.localPosition = Vector3.zero;
        var cam = camObj.AddComponent<Camera>();
        cam.tag = "MainCamera";
        camObj.AddComponent<AudioListener>();

        // 손전등
        var flashlight = new GameObject("Flashlight");
        flashlight.transform.SetParent(camObj.transform);
        flashlight.transform.localPosition = new Vector3(0.2f, -0.1f, 0.3f);
        var light = flashlight.AddComponent<Light>();
        light.type = LightType.Spot;
        light.range = 15f;
        light.spotAngle = 50f;
        light.intensity = 2f;
        light.enabled = false;

        Debug.Log("[TestGenerator] PC 플레이어 생성됨");
    }

    private void GenerateKiller(Transform parent)
    {
        Vector3 spawnPos = new Vector3(roomSize + corridorWidth + roomSize * 2, 1f, roomSize + 2f);

        // 캐릭터 프리팹 시도
        var killer = InstantiatePrefab(Assets.Character, parent, spawnPos, Quaternion.identity);
        if (killer == null)
        {
            killer = CreateFallbackPrimitive("Killer", PrimitiveType.Capsule, parent,
                spawnPos, new Vector3(0.8f, 1f, 0.8f), Color.red);
        }

        killer.name = "Killer";

        // 마스크 추가
        var mask = InstantiatePrefab(Assets.Mask, killer.transform, new Vector3(0, 1.5f, 0.2f), Quaternion.identity);
        if (mask != null)
        {
            mask.name = "KillerMask";
            mask.transform.localScale = Vector3.one * 0.01f;
        }

        // KillerAI 컴포넌트 (NavMeshAgent 자동 추가됨)
        var killerAI = killer.AddComponent<KillerAI>();

        // NavMeshAgent 설정
        var agent = killer.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = 2f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 1f;
        }

        // 추가 컴포넌트
        killer.AddComponent<KillerFootstep>();
        killer.AddComponent<KillerAnimator>();
        killer.AddComponent<KillerCatchSequence>();

        // 초기 비활성화
        killer.SetActive(false);

        Debug.Log("[TestGenerator] 살인마 생성됨");
    }

    private void GenerateGhosts(Transform parent)
    {
        // 유령 1 (창문)
        CreateGhost("Ghost_Window",
            new Vector3(roomSize + corridorWidth + roomSize * 2, 1f, roomSize + 1f),
            GhostController.GhostType.Window, parent);

        // 유령 2 (러너)
        CreateGhost("Ghost_Runner",
            new Vector3(roomSize + corridorWidth, 1f, 0),
            GhostController.GhostType.Runner, parent);

        // 유령 3 (문 열기)
        CreateGhost("Ghost_DoorOpener",
            new Vector3(roomSize + corridorWidth + roomSize - 1f, 1f, -roomSize / 2 - corridorWidth / 2),
            GhostController.GhostType.DoorOpener, parent);

        Debug.Log("[TestGenerator] 유령 3개 생성됨");
    }

    private GameObject CreateGhost(string name, Vector3 position, GhostController.GhostType type, Transform parent)
    {
        var ghost = CreateFallbackPrimitive(name, PrimitiveType.Capsule, parent,
            position, new Vector3(0.6f, 1f, 0.6f), Color.white);

        // 투명 머티리얼 설정
        var renderer = ghost.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.name = $"GhostMaterial_{name}";
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.color = new Color(0.5f, 0.5f, 1f, 0.5f);
        renderer.sharedMaterial = mat;

        var ghostController = ghost.AddComponent<GhostController>();
        ghostController.ghostType = type;
        ghostController.ghostVisual = ghost;
        ghost.SetActive(false);

        return ghost;
    }

    #endregion

    #region Triggers & Lighting

    private void GenerateTriggers(Transform parent)
    {
        CreateRoomTrigger("room1", "Room 1", Vector3.zero, new Vector3(roomSize, 3f, roomSize), parent);
        CreateRoomTrigger("room2", "Room 2",
            new Vector3(roomSize + corridorWidth + roomSize / 2, 0, 0),
            new Vector3(roomSize * 2, 3f, corridorWidth + 1f), parent);
        CreateRoomTrigger("room3", "Room 3",
            new Vector3(roomSize + corridorWidth + roomSize * 2, 0, corridorWidth),
            new Vector3(roomSize, 3f, roomSize), parent);
        CreateRoomTrigger("room4", "Room 4",
            new Vector3(roomSize + corridorWidth + roomSize, 0, -roomSize / 2 - corridorWidth / 2),
            new Vector3(roomSize, 3f, roomSize), parent);
        CreateRoomTrigger("room5", "Room 5",
            new Vector3(roomSize + corridorWidth + roomSize * 2, 0, -roomSize / 2 - corridorWidth / 2),
            new Vector3(roomSize / 2, 3f, roomSize), parent);
    }

    private void CreateRoomTrigger(string roomId, string roomName, Vector3 position, Vector3 size, Transform parent)
    {
        var triggerObj = new GameObject($"RoomTrigger_{roomId}");
        triggerObj.transform.SetParent(parent);
        triggerObj.transform.position = position;

        var collider = triggerObj.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;

        var trigger = triggerObj.AddComponent<RoomTrigger>();
        trigger.roomId = roomId;
        trigger.roomName = roomName;
    }

    private void GenerateLighting(Transform parent)
    {
        // Room3 조명
        var room3Lighting = new GameObject("Lighting_Room3");
        room3Lighting.transform.SetParent(parent);
        room3Lighting.transform.position = new Vector3(roomSize + corridorWidth + roomSize * 2, wallHeight - 0.5f, corridorWidth);

        for (int i = 0; i < 3; i++)
        {
            var lightObj = new GameObject($"Light_{i}");
            lightObj.transform.SetParent(room3Lighting.transform);
            lightObj.transform.localPosition = new Vector3((i - 1) * 2f, 0, 0);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.9f, 0.7f);
            light.intensity = 1.5f;
            light.range = 5f;
            light.enabled = false;
        }

        // 전역 조명
        var ambient = new GameObject("AmbientLight");
        ambient.transform.SetParent(parent);
        ambient.transform.position = new Vector3(roomSize, wallHeight * 2, 0);

        var ambientLight = ambient.AddComponent<Light>();
        ambientLight.type = LightType.Directional;
        ambientLight.color = new Color(0.2f, 0.2f, 0.3f);
        ambientLight.intensity = 0.3f;
        ambient.transform.rotation = Quaternion.Euler(50, -30, 0);
    }

    #endregion

    #region Managers

    private void GenerateManagers(Transform parent)
    {
        // RoomProgressManager
        if (Object.FindFirstObjectByType<RoomProgressManager>() == null)
        {
            var rpm = new GameObject("RoomProgressManager");
            rpm.transform.SetParent(parent);
            rpm.AddComponent<RoomProgressManager>();
            rpm.AddComponent<AudioSource>();
        }

        // HorrorGameManager
        if (Object.FindFirstObjectByType<HorrorGameManager>() == null)
        {
            var hgm = new GameObject("HorrorGameManager");
            hgm.transform.SetParent(parent);
            hgm.AddComponent<HorrorGameManager>();
        }

        // PlayerInventory
        if (Object.FindFirstObjectByType<PlayerInventory>() == null)
        {
            var inv = new GameObject("PlayerInventory");
            inv.transform.SetParent(parent);
            inv.AddComponent<PlayerInventory>();
        }

        // GamePopupUI
        if (Object.FindFirstObjectByType<GamePopupUI>() == null)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                canvasObj.transform.SetParent(parent);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            var popup = new GameObject("GamePopupUI");
            popup.transform.SetParent(canvas.transform);
            var popupComp = popup.AddComponent<GamePopupUI>();
            popup.AddComponent<AudioSource>();

            var panel = new GameObject("PopupPanel");
            panel.transform.SetParent(popup.transform);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            panel.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.8f);
            panel.AddComponent<CanvasGroup>();
            panel.SetActive(false);
            popupComp.popupPanel = panel;
        }

        Debug.Log("[TestGenerator] 게임 매니저 생성됨");
    }

    #endregion

    #region Utilities

    private void BakeNavMesh()
    {
        var floors = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in floors)
        {
            if (obj.name.Contains("Floor"))
            {
                obj.isStatic = true;
                SetNavigationStatic(obj);
            }
        }

        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        Debug.Log("[TestGenerator] NavMesh 베이크 완료");
    }

    private void AutoConnectReferences()
    {
        var rpm = Object.FindFirstObjectByType<RoomProgressManager>();
        if (rpm == null)
        {
            Debug.LogWarning("[TestGenerator] RoomProgressManager를 찾을 수 없습니다");
            return;
        }

        int connected = 0;

        // 문
        var door1 = GameObject.Find("Door_Room1");
        if (door1 != null) { rpm.doorRoom1 = door1.GetComponent<Door>(); connected++; }

        var door3 = GameObject.Find("Door_Room3");
        if (door3 != null) { rpm.doorRoom3 = door3.GetComponent<Door>(); connected++; }

        var door4 = GameObject.Find("Door_Room4");
        if (door4 != null) { rpm.doorRoom4 = door4.GetComponent<Door>(); connected++; }

        // 아이템
        var forkKey = GameObject.Find("ForkKey_Room1");
        if (forkKey != null) { rpm.forkKeyRoom1 = forkKey; connected++; }

        var key2 = GameObject.Find("Key_Room2");
        if (key2 != null) { rpm.keyRoom2 = key2; connected++; }

        var book = GameObject.Find("Book_Room3");
        if (book != null) { rpm.bookRoom3 = book; connected++; }

        var chest = GameObject.Find("Chest_Room4");
        if (chest != null) { rpm.chestRoom4 = chest; connected++; }

        var exitDoor = GameObject.Find("ExitDoor_Room5");
        if (exitDoor != null) { rpm.exitDoorRoom5 = exitDoor; connected++; }

        // 캐릭터
        var killer = Object.FindFirstObjectByType<KillerAI>();
        if (killer != null) { rpm.killer = killer; connected++; }

        var ghosts = Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        foreach (var ghost in ghosts)
        {
            if (ghost.name.Contains("Window")) { rpm.ghost1 = ghost; connected++; }
            else if (ghost.name.Contains("Runner")) { rpm.ghost2 = ghost; connected++; }
            else if (ghost.name.Contains("DoorOpener")) { rpm.ghost3 = ghost; connected++; }
        }

        // 조명
        var lighting = GameObject.Find("Lighting_Room3");
        if (lighting != null)
        {
            rpm.room3Lights = lighting.GetComponentsInChildren<Light>();
            connected++;
        }

        // 숨기 장소
        var hidingSpot = Object.FindFirstObjectByType<HidingSpot>();
        if (hidingSpot != null) { rpm.hidingSpotRoom2 = hidingSpot; connected++; }

        // 방 Transform
        var room1 = GameObject.Find("Room1 - 시작방");
        if (room1 != null) rpm.room1 = room1.transform;
        var room2 = GameObject.Find("Room2 - 복도");
        if (room2 != null) rpm.room2 = room2.transform;
        var room3 = GameObject.Find("Room3 - 책방");
        if (room3 != null) rpm.room3 = room3.transform;
        var room4 = GameObject.Find("Room4 - 상자방");
        if (room4 != null) rpm.room4 = room4.transform;
        var room5 = GameObject.Find("Room5 - 탈출구");
        if (room5 != null) rpm.room5 = room5.transform;

        var killerWindowPos = GameObject.Find("Killer_WindowPosition");
        if (killerWindowPos != null) rpm.killerWindowPosition = killerWindowPos.transform;

        EditorUtility.SetDirty(rpm);
        Debug.Log($"[TestGenerator] {connected}개 참조 연결 완료");
    }

    private void DeleteTestMap()
    {
        var testMap = GameObject.Find("=== TEST MAP ===");
        if (testMap != null)
        {
            Undo.DestroyObjectImmediate(testMap);
        }

        string[] toDelete = { "Room1_Generated", "Room2_Generated", "Room3_Generated",
            "Room4_Generated", "Room5_Generated", "PC Player" };

        foreach (var name in toDelete)
        {
            var obj = GameObject.Find(name);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        Debug.Log("[TestGenerator] 테스트 맵 삭제됨");
    }

    private void ShowCompletionDialog()
    {
        EditorUtility.DisplayDialog("완료",
            "테스트 맵이 생성되었습니다!\n\n" +
            "Play 버튼을 눌러 테스트하세요.\n\n" +
            "조작법:\n" +
            "- WASD: 이동\n" +
            "- Shift: 달리기\n" +
            "- E/클릭: 상호작용\n" +
            "- F: 손전등\n" +
            "- C: 웅크리기",
            "확인");
    }

    #endregion

    #region Helper Methods

    private GameObject CreateChild(GameObject parent, string name)
    {
        var child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }

    private GameObject CreateSpawnPoint(string name, Transform parent, Vector3 localPos)
    {
        var point = new GameObject(name);
        point.transform.SetParent(parent);
        point.transform.localPosition = localPos;
        return point;
    }

    private GameObject InstantiatePrefab(string path, Transform parent, Vector3 localPos, Quaternion rotation)
    {
        GameObject prefab = null;

        if (!assetCache.TryGetValue(path, out prefab))
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                assetCache[path] = prefab;
            }
        }

        if (prefab == null)
        {
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = rotation;
        return instance;
    }

    private GameObject CreateFallbackPrimitive(string name, PrimitiveType type, Transform parent,
        Vector3 localPos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial.color = color;
        return obj;
    }

    private void SetNavigationStatic(GameObject obj)
    {
        #pragma warning disable CS0618
        GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.NavigationStatic);
        #pragma warning restore CS0618
    }

    #endregion
}
