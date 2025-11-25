using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRGame
{
    /// <summary>
    /// 텔레포트 가능한 영역을 정의하는 스크립트
    /// 바닥이나 플랫폼에 추가하여 텔레포트 목적지로 사용
    ///
    /// 사용법:
    /// 1. 바닥/플랫폼 오브젝트에 이 스크립트 추가
    /// 2. Collider 컴포넌트 필요 (없으면 자동 추가)
    /// </summary>
    [RequireComponent(typeof(TeleportationArea))]
    public class TeleportArea : MonoBehaviour
    {
        [Header("Teleport Settings")]
        [Tooltip("텔레포트 가능 여부")]
        public bool isTeleportable = true;

        [Header("Visual Feedback")]
        [Tooltip("텔레포트 가능 영역 색상")]
        public Color validColor = new Color(0f, 1f, 0.5f, 0.3f);

        [Tooltip("텔레포트 불가 영역 색상")]
        public Color invalidColor = new Color(1f, 0f, 0f, 0.3f);

        private TeleportationArea teleportationArea;
        private Renderer areaRenderer;
        private Material areaMaterial;

        private void Awake()
        {
            teleportationArea = GetComponent<TeleportationArea>();
            areaRenderer = GetComponent<Renderer>();

            // Collider 확인
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            // 머티리얼 설정
            if (areaRenderer != null)
            {
                areaMaterial = new Material(Shader.Find("Standard"));
                areaMaterial.SetFloat("_Mode", 3); // Transparent
                areaMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                areaMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                areaMaterial.SetInt("_ZWrite", 0);
                areaMaterial.DisableKeyword("_ALPHATEST_ON");
                areaMaterial.EnableKeyword("_ALPHABLEND_ON");
                areaMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                areaMaterial.renderQueue = 3000;
                areaRenderer.material = areaMaterial;
            }

            UpdateVisuals();
        }

        /// <summary>
        /// 텔레포트 가능 여부 설정
        /// </summary>
        public void SetTeleportable(bool value)
        {
            isTeleportable = value;
            if (teleportationArea != null)
            {
                teleportationArea.enabled = value;
            }
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (areaMaterial != null)
            {
                areaMaterial.color = isTeleportable ? validColor : invalidColor;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 값 변경 시 업데이트
            if (Application.isPlaying)
            {
                SetTeleportable(isTeleportable);
            }
        }
#endif
    }
}
