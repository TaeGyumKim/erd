using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 씬의 오브젝트들에 색상을 적용하는 유틸리티 컴포넌트
    /// 에디터와 런타임 모두에서 작동
    /// </summary>
    [ExecuteInEditMode]
    public class SceneColorSetup : MonoBehaviour
    {
        [Header("색상 설정")]
        [SerializeField] private Color floorColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color wallColor = new Color(0.4f, 0.35f, 0.3f);
        [SerializeField] private Color doorColor = new Color(0.6f, 0.3f, 0.1f);
        [SerializeField] private Color keyColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color wardrobeColor = new Color(0.35f, 0.2f, 0.1f);

        [Header("자동 적용")]
        [SerializeField] private bool applyOnStart = true;

        private void Start()
        {
            if (applyOnStart)
            {
                ApplyColors();
            }
        }

        [ContextMenu("Apply Colors")]
        public void ApplyColors()
        {
            ApplyColorToObject("Floor", floorColor);
            ApplyColorToObject("Wall_North", wallColor);
            ApplyColorToObject("Wall_South", wallColor);
            ApplyColorToObject("Wall_East", wallColor);
            ApplyColorToObject("Wall_West", wallColor);
            ApplyColorToObject("ExitDoor", doorColor);
            ApplyColorToObject("Key_Exit", keyColor);
            ApplyColorToObject("Wardrobe_HidingSpot", wardrobeColor);

            Debug.Log("[SceneColorSetup] 색상 적용 완료!");
        }

        private void ApplyColorToObject(string objectName, Color color)
        {
            var go = GameObject.Find(objectName);
            if (go != null)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    // 런타임에서는 material, 에디터에서는 sharedMaterial 사용
                    if (Application.isPlaying)
                    {
                        renderer.material.color = color;
                    }
                    else
                    {
                        // 에디터 모드에서는 새 Material 인스턴스 생성
                        var mat = new Material(Shader.Find("Standard"));
                        mat.color = color;
                        renderer.sharedMaterial = mat;
                    }
                }
            }
        }
    }
}
