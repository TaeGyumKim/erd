using UnityEngine;
using UnityEditor;
using HorrorGame;
using System.Collections.Generic;

/// <summary>
/// Room Progress 시스템 설정 에디터 도구
/// 기획서 기반 게임 시스템 - 에셋 자동 배치 지원
/// </summary>
public class RoomProgressSetupTool : EditorWindow
{
    private Vector2 scrollPosition;

    // 에셋 경로
    private static class AssetPaths
    {
        // 프리팹
        public const string KeyPrefab = "Assets/Asset pack for horror game/Prefabs/Key.prefab";
        public const string DoorPrefab = "Assets/Asset pack for horror game/Prefabs/Door.prefab";
        public const string BedPrefab = "Assets/Asset pack for horror game/Prefabs/Bed.prefab";
        public const string WheelchairPrefab = "Assets/Asset pack for horror game/Prefabs/Wheelchair.prefab";
        public const string WindowPrefab = "Assets/Asset pack for horror game/Prefabs/Window.prefab";
        public const string TablePrefab = "Assets/Asset pack for horror game/Prefabs/Wooden table.prefab";

        // 상자/배럴
        public const string ChestPrefab = "Assets/McSteeg/PSX Style Crates and Barrels Pack/Prefabs/Chest_1.prefab";
        public const string BarrelPrefab = "Assets/McSteeg/PSX Style Crates and Barrels Pack/Prefabs/Barrel_1.prefab";

        // 책 (FBX)
        public const string BookFBX = "Assets/Mega Fantasy Props Pack/FBX/book.fbx";

        // 캐릭터
        public const string KillerPrefab = "Assets/Character/TG_Hero_Interactive_Prefab.prefab";
        public const string MaskPrefab = "Assets/Common/Mask/Mask_Prefab.prefab";

        // 병원 에셋
        public const string HospitalDoor = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Door_01_.prefab";
        public const string HospitalBed = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Bed_01.prefab";
        public const string HospitalLamp = "Assets/Dnk_Dev/HospitalHorrorPack/Prefab/P_Lamp.prefab";
    }

    [MenuItem("Horror Game/Room Progress Setup")]
    public static void ShowWindow()
    {
        var window = GetWindow<RoomProgressSetupTool>("Room Progress Setup");
        window.minSize = new Vector2(450, 700);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Room Progress System Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "기획서 기반 9단계 게임 진행 시스템\n" +
            "에셋을 사용하여 자동으로 오브젝트를 생성합니다.",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 0. 전체 자동 생성
        DrawSection("0. 빠른 설정 (전체 자동 생성)", () =>
        {
            EditorGUILayout.HelpBox("모든 필수 오브젝트를 한 번에 생성합니다.", MessageType.None);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("전체 게임 시스템 자동 생성", GUILayout.Height(40)))
            {
                CreateAllGameSystems();
            }
            GUI.backgroundColor = Color.white;
        });

        // 1. 핵심 매니저
        DrawSection("1. 핵심 매니저", () =>
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("RoomProgressManager", GUILayout.Height(25)))
                CreateRoomProgressManager();
            if (GUILayout.Button("GamePopupUI", GUILayout.Height(25)))
                CreateGamePopupUI();
            EditorGUILayout.EndHorizontal();
        });

        // 2. 인터랙션 오브젝트 (에셋 사용)
        DrawSection("2. 인터랙션 오브젝트 (에셋 자동 배치)", () =>
        {
            if (GUILayout.Button("책 생성 (Room3용) - book.fbx", GUILayout.Height(25)))
                CreateBookWithAsset();

            if (GUILayout.Button("비밀번호 상자 생성 (Room4용) - Chest_1", GUILayout.Height(25)))
                CreateChestWithAsset();

            if (GUILayout.Button("탈출문 생성 (Room5용) - Door", GUILayout.Height(25)))
                CreateSlidingDoorWithAsset();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("선택된 오브젝트에 컴포넌트 추가:", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ InteractableBook"))
                AddInteractableBook();
            if (GUILayout.Button("+ PasswordChest"))
                AddPasswordChest();
            if (GUILayout.Button("+ SlidingDoor"))
                AddSlidingDoor();
            EditorGUILayout.EndHorizontal();
        });

        // 3. 열쇠 아이템 (에셋 사용)
        DrawSection("3. 열쇠 아이템 (에셋 자동 배치)", () =>
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fork Key\n(Room1)", GUILayout.Height(35)))
                CreateKeyWithAsset("fork_key_room1", "fork key - Room1", "Door - Room1");
            if (GUILayout.Button("Key\n(Room2)", GUILayout.Height(35)))
                CreateKeyWithAsset("key_room2", "key - Room2", "Door - Room3");
            if (GUILayout.Button("Escape Key\n(상자용)", GUILayout.Height(35)))
                CreateKeyWithAsset("escape_key", "탈출 열쇠", "Panel_Wood - Room5");
            EditorGUILayout.EndHorizontal();
        });

        // 4. Room Trigger
        DrawSection("4. Room Trigger (영역 감지)", () =>
        {
            if (GUILayout.Button("모든 Room Trigger 생성", GUILayout.Height(25)))
                CreateAllRoomTriggers();

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Room1")) CreateRoomTrigger("room1", "Room 1");
            if (GUILayout.Button("Room2")) CreateRoomTrigger("room2", "Room 2");
            if (GUILayout.Button("Room3")) CreateRoomTrigger("room3", "Room 3");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Room4")) CreateRoomTrigger("room4", "Room 4");
            if (GUILayout.Button("Room5")) CreateRoomTrigger("room5", "Room 5");
            if (GUILayout.Button("Hiding")) CreateRoomTrigger("hiding_spot", "숨기 장소");
            EditorGUILayout.EndHorizontal();
        });

        // 5. 캐릭터
        DrawSection("5. 캐릭터 (에셋 자동 배치)", () =>
        {
            if (GUILayout.Button("살인마 (Killer) 생성 - 캐릭터 + 마스크", GUILayout.Height(30)))
                CreateKillerWithAsset();

            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ghost1\n(창문)"))
                CreateGhostWithCharacter(GhostController.GhostType.Window, "ghost 1");
            if (GUILayout.Button("Ghost2\n(러너)"))
                CreateGhostWithCharacter(GhostController.GhostType.Runner, "ghost 2");
            if (GUILayout.Button("Ghost3\n(문열기)"))
                CreateGhostWithCharacter(GhostController.GhostType.DoorOpener, "ghost 3");
            EditorGUILayout.EndHorizontal();
        });

        // 6. 문 (Door)
        DrawSection("6. 문 생성 (에셋 사용)", () =>
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Door - Room1"))
                CreateDoorWithAsset("Door - Room1", "fork_key_room1");
            if (GUILayout.Button("Door - Room3"))
                CreateDoorWithAsset("Door - Room3", "key_room2");
            if (GUILayout.Button("Door - Room4"))
                CreateDoorWithAsset("Door - Room4", ""); // 잠금 없음
            EditorGUILayout.EndHorizontal();
        });

        // 7. 조명 (Lighting - Room3)
        DrawSection("7. 조명 (Room3용)", () =>
        {
            if (GUILayout.Button("Room3 조명 세트 생성 (Light 3개)", GUILayout.Height(25)))
                CreateRoom3Lighting();
        });

        // 8. 검증 및 자동 연결
        DrawSection("8. 검증 및 자동 연결", () =>
        {
            if (GUILayout.Button("자동으로 참조 연결", GUILayout.Height(25)))
                AutoConnectReferences();

            if (GUILayout.Button("시스템 검증", GUILayout.Height(25)))
                ValidateSetup();
        });

        EditorGUILayout.Space(10);

        // 기획서 기준 오브젝트 이름
        EditorGUILayout.LabelField("기획서 기준 오브젝트 이름", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "fork key - Room1: 포크 열쇠\n" +
            "Door - Room1: Room1 문\n" +
            "key - Room2: Room3용 열쇠\n" +
            "Door - Room3: Room3 문\n" +
            "book - Room3: 스토리 책\n" +
            "Lighting - Room3: 라이트 3개\n" +
            "ghost 1/2/3: 유령\n" +
            "Door - Room4: Room4 문 (잠금 없음)\n" +
            "chest - Room4: 비밀번호 상자\n" +
            "Panel_Wood - Room5: 탈출문",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void DrawSection(string title, System.Action content)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        content?.Invoke();
        EditorGUILayout.EndVertical();
    }

    #region 전체 자동 생성

    private void CreateAllGameSystems()
    {
        if (!EditorUtility.DisplayDialog("전체 생성",
            "모든 게임 시스템 오브젝트를 생성합니다.\n\n" +
            "생성될 오브젝트:\n" +
            "- RoomProgressManager\n" +
            "- GamePopupUI\n" +
            "- 문 3개 (Room1, Room3, Room4)\n" +
            "- 열쇠 3개\n" +
            "- 책 (Room3)\n" +
            "- 상자 (Room4)\n" +
            "- 탈출문 (Room5)\n" +
            "- 살인마\n" +
            "- 유령 3개\n" +
            "- Room Trigger 6개\n" +
            "- Room3 조명\n\n" +
            "계속하시겠습니까?",
            "생성", "취소"))
        {
            return;
        }

        // 부모 오브젝트 생성
        var gameSystemsParent = new GameObject("=== Game Systems ===");
        var interactablesParent = new GameObject("=== Interactables ===");
        var charactersParent = new GameObject("=== Characters ===");
        var triggersParent = new GameObject("=== Room Triggers ===");

        // 1. 매니저
        CreateRoomProgressManager();
        CreateGamePopupUI();

        // 2. 문
        var door1 = CreateDoorWithAsset("Door - Room1", "fork_key_room1");
        var door3 = CreateDoorWithAsset("Door - Room3", "key_room2");
        var door4 = CreateDoorWithAsset("Door - Room4", "");

        if (door1) door1.transform.SetParent(interactablesParent.transform);
        if (door3) door3.transform.SetParent(interactablesParent.transform);
        if (door4) door4.transform.SetParent(interactablesParent.transform);

        // 3. 열쇠
        var key1 = CreateKeyWithAsset("fork_key_room1", "fork key - Room1", "Door - Room1");
        var key2 = CreateKeyWithAsset("key_room2", "key - Room2", "Door - Room3");
        var key3 = CreateKeyWithAsset("escape_key", "탈출 열쇠", "Panel_Wood - Room5");

        if (key1) key1.transform.SetParent(interactablesParent.transform);
        if (key2) key2.transform.SetParent(interactablesParent.transform);
        if (key3) key3.transform.SetParent(interactablesParent.transform);

        // 4. 책
        var book = CreateBookWithAsset();
        if (book) book.transform.SetParent(interactablesParent.transform);

        // 5. 상자
        var chest = CreateChestWithAsset();
        if (chest) chest.transform.SetParent(interactablesParent.transform);

        // 6. 탈출문
        var exitDoor = CreateSlidingDoorWithAsset();
        if (exitDoor) exitDoor.transform.SetParent(interactablesParent.transform);

        // 7. 살인마
        var killer = CreateKillerWithAsset();
        if (killer) killer.transform.SetParent(charactersParent.transform);

        // 8. 유령
        var ghost1 = CreateGhostWithCharacter(GhostController.GhostType.Window, "ghost 1");
        var ghost2 = CreateGhostWithCharacter(GhostController.GhostType.Runner, "ghost 2");
        var ghost3 = CreateGhostWithCharacter(GhostController.GhostType.DoorOpener, "ghost 3");

        if (ghost1) ghost1.transform.SetParent(charactersParent.transform);
        if (ghost2) ghost2.transform.SetParent(charactersParent.transform);
        if (ghost3) ghost3.transform.SetParent(charactersParent.transform);

        // 9. Room Trigger
        CreateAllRoomTriggersUnderParent(triggersParent.transform);

        // 10. Room3 조명
        var lighting = CreateRoom3Lighting();
        if (lighting) lighting.transform.SetParent(interactablesParent.transform);

        // 11. 자동 연결
        AutoConnectReferences();

        // 위치 정리
        ArrangeObjectsForPreview(interactablesParent, charactersParent, triggersParent);

        EditorUtility.DisplayDialog("완료",
            "모든 게임 시스템이 생성되었습니다!\n\n" +
            "각 오브젝트를 맵의 적절한 위치로 이동시켜주세요.",
            "확인");

        Debug.Log("[RoomProgressSetup] 전체 게임 시스템 생성 완료");
    }

    private void ArrangeObjectsForPreview(GameObject interactables, GameObject characters, GameObject triggers)
    {
        // 미리보기용 위치 정리 (나중에 맵에 배치)
        float spacing = 3f;
        int index = 0;

        foreach (Transform child in interactables.transform)
        {
            child.position = new Vector3(index * spacing, 0, 0);
            index++;
        }

        index = 0;
        foreach (Transform child in characters.transform)
        {
            child.position = new Vector3(index * spacing, 0, 10);
            index++;
        }

        index = 0;
        foreach (Transform child in triggers.transform)
        {
            child.position = new Vector3(index * spacing, 0, 20);
            index++;
        }
    }

    #endregion

    #region 에셋 기반 생성

    private GameObject CreateBookWithAsset()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.BookFBX);
        if (prefab == null)
        {
            Debug.LogWarning($"[RoomProgressSetup] 책 에셋을 찾을 수 없습니다: {AssetPaths.BookFBX}");
            // 빈 오브젝트로 생성
            var emptyBook = new GameObject("book - Room3");
            var book = emptyBook.AddComponent<InteractableBook>();
            book.bookTitle = "오래된 일기";
            book.bookContent = "이곳에서 끔찍한 일이 일어났다...\n\n비밀번호: 1234";
            book.passwordHint = "1234";
            emptyBook.AddComponent<BoxCollider>().isTrigger = true;
            Undo.RegisterCreatedObjectUndo(emptyBook, "Create Book");
            Selection.activeGameObject = emptyBook;
            return emptyBook;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "book - Room3";

        // 스크립트 추가
        var interactableBook = instance.AddComponent<InteractableBook>();
        interactableBook.bookTitle = "오래된 일기";
        interactableBook.bookContent = "이곳에서 끔찍한 일이 일어났다...\n\n누군가가 여기서 실험을 했다.\n상자의 비밀번호는... 1234";
        interactableBook.passwordHint = "1234";

        // Collider 확인
        if (instance.GetComponent<Collider>() == null)
        {
            var col = instance.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        if (Selection.activeGameObject != null)
        {
            instance.transform.position = Selection.activeGameObject.transform.position;
        }

        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Create Book");

        Debug.Log("[RoomProgressSetup] book - Room3 생성됨");
        return instance;
    }

    private GameObject CreateChestWithAsset()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.ChestPrefab);
        if (prefab == null)
        {
            Debug.LogWarning($"[RoomProgressSetup] 상자 에셋을 찾을 수 없습니다: {AssetPaths.ChestPrefab}");
            var emptyChest = new GameObject("chest - Room4");
            var chest = emptyChest.AddComponent<PasswordChest>();
            chest.correctPassword = "1234";
            emptyChest.AddComponent<BoxCollider>().isTrigger = true;
            Undo.RegisterCreatedObjectUndo(emptyChest, "Create Chest");
            Selection.activeGameObject = emptyChest;
            return emptyChest;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "chest - Room4";

        // 스크립트 추가
        var passwordChest = instance.AddComponent<PasswordChest>();
        passwordChest.correctPassword = "1234";
        passwordChest.passwordLength = 4;

        // 뚜껑 찾기
        var lid = instance.transform.Find("Lid") ?? instance.transform.Find("lid") ?? instance.transform.Find("Top");
        if (lid != null)
        {
            passwordChest.lid = lid;
        }

        // 탈출 열쇠 생성해서 상자 안에 넣기
        var escapeKey = CreateKeyWithAsset("escape_key", "탈출 열쇠 (상자)", "Panel_Wood - Room5");
        if (escapeKey != null)
        {
            escapeKey.transform.SetParent(instance.transform);
            escapeKey.transform.localPosition = Vector3.up * 0.2f;
            escapeKey.SetActive(false);
            passwordChest.containedItem = escapeKey;
        }

        // Collider 확인
        if (instance.GetComponent<Collider>() == null)
        {
            var col = instance.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        if (Selection.activeGameObject != null)
        {
            instance.transform.position = Selection.activeGameObject.transform.position;
        }

        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Create Chest");

        Debug.Log("[RoomProgressSetup] chest - Room4 생성됨");
        return instance;
    }

    private GameObject CreateSlidingDoorWithAsset()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.DoorPrefab);
        if (prefab == null)
        {
            // Hospital Door 시도
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.HospitalDoor);
        }

        GameObject instance;
        if (prefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            instance = new GameObject("Panel_Wood - Room5");
            // 기본 큐브 메시 추가
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(instance.transform);
            cube.transform.localScale = new Vector3(2f, 3f, 0.2f);
        }

        instance.name = "Panel_Wood - Room5";

        // 스크립트 추가
        var slidingDoor = instance.AddComponent<SlidingDoor>();
        slidingDoor.requiredItemId = "escape_key";
        slidingDoor.slideDirection = Vector3.up;
        slidingDoor.openDistance = 3f;
        slidingDoor.slowOpenDuration = 5f;

        if (Selection.activeGameObject != null)
        {
            instance.transform.position = Selection.activeGameObject.transform.position;
        }

        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, "Create Sliding Door");

        Debug.Log("[RoomProgressSetup] Panel_Wood - Room5 생성됨");
        return instance;
    }

    private GameObject CreateKeyWithAsset(string keyId, string displayName, string targetDoor)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.KeyPrefab);

        GameObject instance;
        if (prefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            instance = new GameObject(displayName);
            // 기본 캡슐로 대체
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(instance.transform);
            capsule.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
            DestroyImmediate(capsule.GetComponent<Collider>());
        }

        instance.name = displayName;

        // KeyItem 추가
        var keyItem = instance.AddComponent<KeyItem>();
        keyItem.keyId = keyId;
        keyItem.keyName = displayName;

        // Collider 확인
        if (instance.GetComponent<Collider>() == null)
        {
            var col = instance.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(0.2f, 0.1f, 0.1f);
        }

        if (Selection.activeGameObject != null)
        {
            instance.transform.position = Selection.activeGameObject.transform.position + Vector3.up * 0.5f;
        }

        Undo.RegisterCreatedObjectUndo(instance, $"Create Key {keyId}");

        Debug.Log($"[RoomProgressSetup] {displayName} 생성됨 (대상: {targetDoor})");
        return instance;
    }

    private GameObject CreateDoorWithAsset(string doorName, string requiredKeyId)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.DoorPrefab);
        if (prefab == null)
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.HospitalDoor);
        }

        GameObject instance;
        if (prefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            instance = new GameObject(doorName);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(instance.transform);
            cube.transform.localScale = new Vector3(1f, 2f, 0.1f);
        }

        instance.name = doorName;

        // Door 스크립트 추가
        var door = instance.AddComponent<Door>();
        if (!string.IsNullOrEmpty(requiredKeyId))
        {
            door.isLocked = true;
            door.requiredKeyId = requiredKeyId;
        }
        else
        {
            door.isLocked = false;
        }

        if (Selection.activeGameObject != null)
        {
            instance.transform.position = Selection.activeGameObject.transform.position;
        }

        Selection.activeGameObject = instance;
        Undo.RegisterCreatedObjectUndo(instance, $"Create Door {doorName}");

        Debug.Log($"[RoomProgressSetup] {doorName} 생성됨" +
            (string.IsNullOrEmpty(requiredKeyId) ? "" : $" (필요 열쇠: {requiredKeyId})"));
        return instance;
    }

    private GameObject CreateKillerWithAsset()
    {
        var characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.KillerPrefab);
        var maskPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.MaskPrefab);

        GameObject killer;
        if (characterPrefab != null)
        {
            killer = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
        }
        else
        {
            killer = new GameObject("Killer");
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(killer.transform);
            capsule.transform.localPosition = Vector3.up;
        }

        killer.name = "Killer";

        // 마스크 추가
        if (maskPrefab != null)
        {
            var mask = (GameObject)PrefabUtility.InstantiatePrefab(maskPrefab);
            mask.transform.SetParent(killer.transform);
            mask.transform.localPosition = new Vector3(0, 1.7f, 0.1f);
            mask.name = "Mask";
        }

        // KillerAI 추가
        var killerAI = killer.GetComponent<KillerAI>();
        if (killerAI == null)
        {
            killerAI = killer.AddComponent<KillerAI>();
        }

        // NavMeshAgent 추가
        var agent = killer.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null)
        {
            agent = killer.AddComponent<UnityEngine.AI.NavMeshAgent>();
        }

        // 기타 컴포넌트
        if (killer.GetComponent<KillerAnimator>() == null)
            killer.AddComponent<KillerAnimator>();
        if (killer.GetComponent<KillerFootstep>() == null)
            killer.AddComponent<KillerFootstep>();
        if (killer.GetComponent<KillerCatchSequence>() == null)
            killer.AddComponent<KillerCatchSequence>();

        killer.SetActive(false); // 초기 비활성화

        if (Selection.activeGameObject != null)
        {
            killer.transform.position = Selection.activeGameObject.transform.position;
        }

        Selection.activeGameObject = killer;
        Undo.RegisterCreatedObjectUndo(killer, "Create Killer");

        Debug.Log("[RoomProgressSetup] Killer 생성됨");
        return killer;
    }

    private GameObject CreateGhostWithCharacter(GhostController.GhostType type, string name)
    {
        var characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetPaths.KillerPrefab);

        GameObject ghost;
        if (characterPrefab != null)
        {
            ghost = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
        }
        else
        {
            ghost = new GameObject(name);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.SetParent(ghost.transform);
            capsule.transform.localPosition = Vector3.up;
            // 반투명 효과
            var renderer = capsule.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                mat.SetFloat("_Mode", 3); // Transparent
                renderer.material = mat;
            }
        }

        ghost.name = name;

        // GhostController 추가
        var ghostController = ghost.AddComponent<GhostController>();
        ghostController.ghostType = type;

        // AudioSource
        if (ghost.GetComponent<AudioSource>() == null)
        {
            ghost.AddComponent<AudioSource>();
        }

        // 시작/목표 위치 생성
        var startPos = new GameObject($"{name}_StartPos");
        startPos.transform.SetParent(ghost.transform);
        startPos.transform.localPosition = Vector3.zero;
        ghostController.startPosition = startPos.transform;

        var targetPos = new GameObject($"{name}_TargetPos");
        targetPos.transform.SetParent(ghost.transform);
        targetPos.transform.localPosition = Vector3.forward * 5f;
        ghostController.targetPosition = targetPos.transform;

        ghost.SetActive(false); // 초기 비활성화

        if (Selection.activeGameObject != null)
        {
            ghost.transform.position = Selection.activeGameObject.transform.position;
        }

        Selection.activeGameObject = ghost;
        Undo.RegisterCreatedObjectUndo(ghost, $"Create Ghost {name}");

        Debug.Log($"[RoomProgressSetup] {name} (타입: {type}) 생성됨");
        return ghost;
    }

    private GameObject CreateRoom3Lighting()
    {
        var parent = new GameObject("Lighting - Room3");

        for (int i = 0; i < 3; i++)
        {
            var lightGo = new GameObject($"Light_{i}");
            lightGo.transform.SetParent(parent.transform);
            lightGo.transform.localPosition = new Vector3((i - 1) * 2f, 2.5f, 0);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.9f, 0.7f); // 따뜻한 색
            light.intensity = 1.5f;
            light.range = 5f;
            light.enabled = false; // 초기 꺼짐
        }

        if (Selection.activeGameObject != null)
        {
            parent.transform.position = Selection.activeGameObject.transform.position;
        }

        Selection.activeGameObject = parent;
        Undo.RegisterCreatedObjectUndo(parent, "Create Room3 Lighting");

        Debug.Log("[RoomProgressSetup] Lighting - Room3 생성됨 (Light 3개, 초기 꺼짐)");
        return parent;
    }

    #endregion

    #region Room Trigger

    private void CreateAllRoomTriggers()
    {
        var parent = new GameObject("=== Room Triggers ===");
        CreateAllRoomTriggersUnderParent(parent.transform);
    }

    private void CreateAllRoomTriggersUnderParent(Transform parent)
    {
        string[] roomIds = { "room1", "room2", "room3", "room4", "room5", "hiding_spot" };
        string[] roomNames = { "Room 1", "Room 2", "Room 3", "Room 4", "Room 5", "숨기 장소" };

        for (int i = 0; i < roomIds.Length; i++)
        {
            var trigger = CreateRoomTriggerObject(roomIds[i], roomNames[i]);
            if (trigger != null && parent != null)
            {
                trigger.transform.SetParent(parent);
                trigger.transform.localPosition = new Vector3(i * 6f, 0, 0);
            }
        }

        Debug.Log("[RoomProgressSetup] 모든 Room Trigger 생성됨");
    }

    private void CreateRoomTrigger(string roomId, string roomName)
    {
        var trigger = CreateRoomTriggerObject(roomId, roomName);
        if (trigger != null && Selection.activeGameObject != null)
        {
            trigger.transform.position = Selection.activeGameObject.transform.position;
        }
        Selection.activeGameObject = trigger;
    }

    private GameObject CreateRoomTriggerObject(string roomId, string roomName)
    {
        var go = new GameObject($"RoomTrigger_{roomId}");
        var trigger = go.AddComponent<RoomTrigger>();
        trigger.roomId = roomId;
        trigger.roomName = roomName;

        var collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(5f, 3f, 5f);

        Undo.RegisterCreatedObjectUndo(go, $"Create RoomTrigger {roomId}");
        return go;
    }

    #endregion

    #region 기존 메서드 (컴포넌트 추가)

    private void CreateRoomProgressManager()
    {
        var existing = FindFirstObjectByType<RoomProgressManager>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[RoomProgressSetup] RoomProgressManager가 이미 존재합니다.");
            return;
        }

        var go = new GameObject("RoomProgressManager");
        go.AddComponent<RoomProgressManager>();
        go.AddComponent<AudioSource>();

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create RoomProgressManager");

        Debug.Log("[RoomProgressSetup] RoomProgressManager 생성됨");
    }

    private void CreateGamePopupUI()
    {
        var existing = FindFirstObjectByType<GamePopupUI>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[RoomProgressSetup] GamePopupUI가 이미 존재합니다.");
            return;
        }

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        var go = new GameObject("GamePopupUI");
        go.transform.SetParent(canvas.transform);
        var popupUI = go.AddComponent<GamePopupUI>();
        go.AddComponent<AudioSource>();

        var panel = new GameObject("PopupPanel");
        panel.transform.SetParent(go.transform);
        var rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        panel.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.8f);
        panel.AddComponent<CanvasGroup>();
        panel.SetActive(false);

        popupUI.popupPanel = panel;

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create GamePopupUI");

        Debug.Log("[RoomProgressSetup] GamePopupUI 생성됨");
    }

    private void AddInteractableBook()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("알림", "오브젝트를 선택해주세요.", "확인");
            return;
        }

        if (selected.GetComponent<InteractableBook>() == null)
        {
            Undo.AddComponent<InteractableBook>(selected);
        }

        if (selected.GetComponent<Collider>() == null)
        {
            var col = Undo.AddComponent<BoxCollider>(selected);
            col.isTrigger = true;
        }

        Debug.Log($"[RoomProgressSetup] {selected.name}에 InteractableBook 추가됨");
    }

    private void AddPasswordChest()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("알림", "오브젝트를 선택해주세요.", "확인");
            return;
        }

        if (selected.GetComponent<PasswordChest>() == null)
        {
            Undo.AddComponent<PasswordChest>(selected);
        }

        if (selected.GetComponent<Collider>() == null)
        {
            var col = Undo.AddComponent<BoxCollider>(selected);
            col.isTrigger = true;
        }

        Debug.Log($"[RoomProgressSetup] {selected.name}에 PasswordChest 추가됨");
    }

    private void AddSlidingDoor()
    {
        var selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("알림", "오브젝트를 선택해주세요.", "확인");
            return;
        }

        if (selected.GetComponent<SlidingDoor>() == null)
        {
            Undo.AddComponent<SlidingDoor>(selected);
        }

        Debug.Log($"[RoomProgressSetup] {selected.name}에 SlidingDoor 추가됨");
    }

    #endregion

    #region 자동 연결 및 검증

    private void AutoConnectReferences()
    {
        var rpm = FindFirstObjectByType<RoomProgressManager>();
        if (rpm == null)
        {
            Debug.LogError("[자동연결] RoomProgressManager가 없습니다!");
            return;
        }

        int connected = 0;

        // 문 연결
        var doorRoom1 = GameObject.Find("Door - Room1");
        if (doorRoom1 != null)
        {
            rpm.doorRoom1 = doorRoom1.GetComponent<Door>();
            connected++;
        }

        var doorRoom3 = GameObject.Find("Door - Room3");
        if (doorRoom3 != null)
        {
            rpm.doorRoom3 = doorRoom3.GetComponent<Door>();
            connected++;
        }

        var doorRoom4 = GameObject.Find("Door - Room4");
        if (doorRoom4 != null)
        {
            rpm.doorRoom4 = doorRoom4.GetComponent<Door>();
            connected++;
        }

        // 키 아이템
        var forkKey = GameObject.Find("fork key - Room1");
        if (forkKey != null)
        {
            rpm.forkKeyRoom1 = forkKey;
            connected++;
        }

        var keyRoom2 = GameObject.Find("key - Room2");
        if (keyRoom2 != null)
        {
            rpm.keyRoom2 = keyRoom2;
            connected++;
        }

        // 책
        var book = GameObject.Find("book - Room3");
        if (book != null)
        {
            rpm.bookRoom3 = book;
            connected++;
        }

        // 상자
        var chest = GameObject.Find("chest - Room4");
        if (chest != null)
        {
            rpm.chestRoom4 = chest;
            connected++;
        }

        // 탈출문
        var exitDoor = GameObject.Find("Panel_Wood - Room5");
        if (exitDoor != null)
        {
            rpm.exitDoorRoom5 = exitDoor;
            connected++;
        }

        // 살인마
        var killer = FindFirstObjectByType<KillerAI>();
        if (killer != null)
        {
            rpm.killer = killer;
            connected++;
        }

        // 유령
        var ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        foreach (var ghost in ghosts)
        {
            if (ghost.name.Contains("1")) rpm.ghost1 = ghost;
            else if (ghost.name.Contains("2")) rpm.ghost2 = ghost;
            else if (ghost.name.Contains("3")) rpm.ghost3 = ghost;
            connected++;
        }

        // Room3 조명
        var lightingRoom3 = GameObject.Find("Lighting - Room3");
        if (lightingRoom3 != null)
        {
            var lights = lightingRoom3.GetComponentsInChildren<Light>();
            rpm.room3Lights = lights;
            connected++;
        }

        // 숨기 장소
        var hidingSpot = FindFirstObjectByType<HidingSpot>();
        if (hidingSpot != null)
        {
            rpm.hidingSpotRoom2 = hidingSpot;
            connected++;
        }

        EditorUtility.SetDirty(rpm);

        Debug.Log($"[자동연결] {connected}개의 참조가 연결되었습니다.");
        EditorUtility.DisplayDialog("자동 연결 완료", $"{connected}개의 참조가 연결되었습니다.", "확인");
    }

    private void ValidateSetup()
    {
        int errors = 0;
        int warnings = 0;

        var rpm = FindFirstObjectByType<RoomProgressManager>();
        if (rpm == null)
        {
            Debug.LogError("[검증] RoomProgressManager가 없습니다!");
            errors++;
        }
        else
        {
            if (rpm.killer == null) { Debug.LogWarning("[검증] killer 연결 안됨"); warnings++; }
            if (rpm.doorRoom1 == null) { Debug.LogWarning("[검증] doorRoom1 연결 안됨"); warnings++; }
            if (rpm.doorRoom3 == null) { Debug.LogWarning("[검증] doorRoom3 연결 안됨"); warnings++; }
            if (rpm.bookRoom3 == null) { Debug.LogWarning("[검증] bookRoom3 연결 안됨"); warnings++; }
            if (rpm.chestRoom4 == null) { Debug.LogWarning("[검증] chestRoom4 연결 안됨"); warnings++; }
            if (rpm.exitDoorRoom5 == null) { Debug.LogWarning("[검증] exitDoorRoom5 연결 안됨"); warnings++; }
            if (rpm.room3Lights == null || rpm.room3Lights.Length == 0) { Debug.LogWarning("[검증] room3Lights 연결 안됨"); warnings++; }
        }

        var triggers = FindObjectsByType<RoomTrigger>(FindObjectsSortMode.None);
        Debug.Log($"[검증] RoomTrigger {triggers.Length}개 발견");

        var ghosts = FindObjectsByType<GhostController>(FindObjectsSortMode.None);
        Debug.Log($"[검증] GhostController {ghosts.Length}개 발견");

        if (errors == 0 && warnings == 0)
        {
            EditorUtility.DisplayDialog("검증 완료", "모든 설정이 올바릅니다!", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("검증 완료",
                $"에러: {errors}개\n경고: {warnings}개\n\nConsole에서 자세한 내용을 확인하세요.",
                "확인");
        }
    }

    #endregion
}
