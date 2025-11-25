using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// VR에서 잡을 수 있는 오브젝트
    /// 이 스크립트를 오브젝트에 추가하면 VR 컨트롤러로 잡을 수 있음
    ///
    /// 사용법:
    /// 1. 오브젝트에 이 스크립트 추가
    /// 2. Collider 컴포넌트 필요 (없으면 자동 추가)
    /// 3. Rigidbody 컴포넌트 필요 (없으면 자동 추가)
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GrabbableObject : MonoBehaviour
    {
        [Header("Grab Settings")]
        [Tooltip("잡았을 때 물리 시뮬레이션 유지")]
        public bool usePhysicsWhenGrabbed = false;

        [Tooltip("던질 때 속도 배율")]
        [Range(0.5f, 3f)]
        public float throwVelocityScale = 1.5f;

        [Header("Audio")]
        [Tooltip("잡을 때 재생할 사운드")]
        public AudioClip grabSound;

        [Tooltip("놓을 때 재생할 사운드")]
        public AudioClip releaseSound;

        private XRGrabInteractable grabInteractable;
        private AudioSource audioSource;

        private void Awake()
        {
            SetupComponents();
        }

        private void SetupComponents()
        {
            // XRGrabInteractable 설정
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.throwOnDetach = true;
                grabInteractable.throwVelocityScale = throwVelocityScale;

                // 이벤트 연결
                grabInteractable.selectEntered.AddListener(OnGrab);
                grabInteractable.selectExited.AddListener(OnRelease);
            }

            // Rigidbody 확인
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = true;
                rb.isKinematic = false;
            }

            // Collider 확인
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }

            // AudioSource 설정
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && (grabSound != null || releaseSound != null))
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D 사운드
            }
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            Debug.Log($"[GrabbableObject] {gameObject.name} 잡음");

            if (audioSource != null && grabSound != null)
            {
                audioSource.PlayOneShot(grabSound);
            }
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            Debug.Log($"[GrabbableObject] {gameObject.name} 놓음");

            if (audioSource != null && releaseSound != null)
            {
                audioSource.PlayOneShot(releaseSound);
            }
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrab);
                grabInteractable.selectExited.RemoveListener(OnRelease);
            }
        }
    }
}
