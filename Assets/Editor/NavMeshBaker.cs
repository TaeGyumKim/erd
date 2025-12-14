using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

namespace HorrorGame.Editor
{
    /// <summary>
    /// NavMesh 자동 베이크 도구
    /// </summary>
    public static class NavMeshBaker
    {
        [MenuItem("Horror Game/NavMesh/Bake NavMesh Surface")]
        public static void BakeNavMeshSurface()
        {
            var surface = Object.FindObjectOfType<NavMeshSurface>();
            if (surface == null)
            {
                Debug.LogError("[NavMeshBaker] NavMeshSurface가 씬에 없습니다.");
                return;
            }

            // 모든 Room 오브젝트를 Navigation Static으로 설정
            SetRoomsNavigationStatic();

            // NavMesh 베이크
            surface.BuildNavMesh();

            EditorUtility.SetDirty(surface);
            Debug.Log("[NavMeshBaker] NavMesh 베이크 완료!");
        }

        [MenuItem("Horror Game/NavMesh/Set All Rooms Navigation Static")]
        public static void SetRoomsNavigationStatic()
        {
            // Room으로 시작하는 모든 오브젝트 찾기
            var allObjects = Object.FindObjectsOfType<GameObject>();
            int count = 0;

            foreach (var obj in allObjects)
            {
                if (obj.name.StartsWith("Room") ||
                    obj.name.Contains("Floor") ||
                    obj.name.Contains("Wall") ||
                    obj.name.Contains("Ceiling"))
                {
                    // 모든 자식 포함하여 Navigation Static 설정
                    SetNavigationStaticRecursive(obj);
                    count++;
                }
            }

            Debug.Log($"[NavMeshBaker] {count}개 오브젝트에 Navigation Static 설정 완료");
        }

        private static void SetNavigationStaticRecursive(GameObject obj)
        {
            // MeshRenderer가 있는 경우에만 Static 설정
            if (obj.GetComponent<MeshRenderer>() != null || obj.GetComponent<MeshFilter>() != null)
            {
#pragma warning disable CS0618
                var flags = GameObjectUtility.GetStaticEditorFlags(obj);
                flags |= StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(obj, flags);
#pragma warning restore CS0618
                EditorUtility.SetDirty(obj);
            }

            // 자식들도 처리
            foreach (Transform child in obj.transform)
            {
                SetNavigationStaticRecursive(child.gameObject);
            }
        }

        [MenuItem("Horror Game/NavMesh/Create NavMesh Surface")]
        public static void CreateNavMeshSurface()
        {
            var existing = Object.FindObjectOfType<NavMeshSurface>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("[NavMeshBaker] NavMeshSurface가 이미 존재합니다.");
                return;
            }

            var navMeshObj = new GameObject("NavMesh");
            var surface = navMeshObj.AddComponent<NavMeshSurface>();

            // 기본 설정
            surface.collectObjects = CollectObjects.All;

            Selection.activeGameObject = navMeshObj;
            Debug.Log("[NavMeshBaker] NavMeshSurface 생성됨. Bake NavMesh Surface 메뉴를 실행하세요.");
        }
    }
}
