using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 샘플 씬 설정 도구 - Material 생성 및 적용
    /// </summary>
    [InitializeOnLoad]
    public static class SampleSceneSetup
    {
        static SampleSceneSetup()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (scene.name == "SampleHorrorScene")
            {
                // 씬이 열릴 때 자동으로 색상 적용
                EditorApplication.delayCall += () =>
                {
                    if (ShouldApplyColors())
                    {
                        CreateAndApplyMaterials();
                    }
                };
            }
        }

        private static bool ShouldApplyColors()
        {
            // Floor의 Material이 기본 Material인지 확인
            var floor = GameObject.Find("Floor");
            if (floor != null)
            {
                var renderer = floor.GetComponent<MeshRenderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    return renderer.sharedMaterial.name == "Default-Material";
                }
            }
            return true;
        }

        private const string MaterialsPath = "Assets/Materials/SampleScene";

        [MenuItem("Tools/Sample Scene Setup/1. Create and Apply Materials")]
        public static void CreateAndApplyMaterials()
        {
            // Materials 폴더 생성
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "SampleScene");
            }

            // Material 생성 및 적용
            ApplyMaterial("Floor", CreateMaterial("M_Floor", new Color(0.3f, 0.3f, 0.35f))); // 어두운 회색 바닥
            ApplyMaterial("Wall_North", CreateMaterial("M_Wall", new Color(0.4f, 0.35f, 0.3f))); // 갈색 벽
            ApplyMaterial("Wall_South", GetMaterial("M_Wall"));
            ApplyMaterial("Wall_East", GetMaterial("M_Wall"));
            ApplyMaterial("Wall_West", GetMaterial("M_Wall"));
            ApplyMaterial("ExitDoor", CreateMaterial("M_Door", new Color(0.5f, 0.25f, 0.1f))); // 진한 갈색 문
            ApplyMaterial("Key_Exit", CreateMaterial("M_Key", new Color(1f, 0.84f, 0f))); // 금색 열쇠
            ApplyMaterial("Wardrobe_HidingSpot", CreateMaterial("M_Wardrobe", new Color(0.35f, 0.2f, 0.1f))); // 어두운 나무색 옷장

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SampleSceneSetup] Material 생성 및 적용 완료!");
        }

        [MenuItem("Tools/Sample Scene Setup/2. Add Interaction Components")]
        public static void AddInteractionComponents()
        {
            // Key_Exit에 XRGrabInteractable 추가
            var key = GameObject.Find("Key_Exit");
            if (key != null)
            {
                // Rigidbody 추가 (XRGrabInteractable에 필요)
                var rb = key.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = key.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }

                // XRGrabInteractable 추가
                var grabType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRGrabInteractable, Unity.XR.Interaction.Toolkit");
                if (grabType != null && key.GetComponent(grabType) == null)
                {
                    key.AddComponent(grabType);
                    Debug.Log("[SampleSceneSetup] Key_Exit에 XRGrabInteractable 추가됨");
                }
            }

            // ExitDoor에 XRSimpleInteractable 추가
            var door = GameObject.Find("ExitDoor");
            if (door != null)
            {
                var simpleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
                if (simpleType != null && door.GetComponent(simpleType) == null)
                {
                    door.AddComponent(simpleType);
                    Debug.Log("[SampleSceneSetup] ExitDoor에 XRSimpleInteractable 추가됨");
                }
            }

            // Wardrobe_HidingSpot에 XRSimpleInteractable 추가
            var wardrobe = GameObject.Find("Wardrobe_HidingSpot");
            if (wardrobe != null)
            {
                var simpleType = System.Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRSimpleInteractable, Unity.XR.Interaction.Toolkit");
                if (simpleType != null && wardrobe.GetComponent(simpleType) == null)
                {
                    wardrobe.AddComponent(simpleType);
                    Debug.Log("[SampleSceneSetup] Wardrobe_HidingSpot에 XRSimpleInteractable 추가됨");
                }
            }

            Debug.Log("[SampleSceneSetup] 상호작용 컴포넌트 추가 완료!");
        }

        [MenuItem("Tools/Sample Scene Setup/3. Bake NavMesh")]
        public static void BakeNavMesh()
        {
            // Floor를 Navigation Static으로 설정
            var floor = GameObject.Find("Floor");
            if (floor != null)
            {
                GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.NavigationStatic);
            }

            // 벽들을 Navigation Static으로 설정
            string[] walls = { "Wall_North", "Wall_South", "Wall_East", "Wall_West" };
            foreach (var wallName in walls)
            {
                var wall = GameObject.Find(wallName);
                if (wall != null)
                {
                    GameObjectUtility.SetStaticEditorFlags(wall, StaticEditorFlags.NavigationStatic);
                }
            }

            // NavMesh 베이크
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            Debug.Log("[SampleSceneSetup] NavMesh 베이크 완료!");
        }

        [MenuItem("Tools/Sample Scene Setup/Run All")]
        public static void RunAll()
        {
            CreateAndApplyMaterials();
            AddInteractionComponents();
            BakeNavMesh();
            Debug.Log("[SampleSceneSetup] 모든 설정 완료!");
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsPath}/{name}.mat";

            // 이미 존재하면 반환
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                return existing;
            }

            // 새 Material 생성
            var material = new Material(Shader.Find("Standard"));
            material.color = color;

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material GetMaterial(string name)
        {
            string path = $"{MaterialsPath}/{name}.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static void ApplyMaterial(string gameObjectName, Material material)
        {
            if (material == null) return;

            var go = GameObject.Find(gameObjectName);
            if (go != null)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    Debug.Log($"[SampleSceneSetup] {gameObjectName}에 {material.name} 적용됨");
                }
            }
        }
    }
}
