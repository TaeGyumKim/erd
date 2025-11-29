using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// 상호작용 가능한 오브젝트의 기본 클래스
    /// 문, 서랍, 스위치 등에 사용
    /// </summary>
    public class InteractableObject : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
    {
        [Header("Interaction Settings")]
        [Tooltip("상호작용 가능 여부")]
        public bool canInteract = true;

        [Tooltip("상호작용 시 표시할 텍스트")]
        public string interactionText = "상호작용";

        [Tooltip("상호작용 시 소리")]
        public AudioClip interactSound;

        [Header("Highlight")]
        [Tooltip("하이라이트 색상")]
        public Color highlightColor = new Color(1f, 1f, 0f, 0.3f);

        [Tooltip("하이라이트 시 외곽선 두께")]
        public float outlineWidth = 0.02f;

        [Header("Events")]
        public UnityEvent OnInteract;
        public UnityEvent OnHighlight;
        public UnityEvent OnUnhighlight;

        protected AudioSource audioSource;
        protected Renderer objectRenderer;
        protected Material originalMaterial;
        protected bool isHighlighted;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && interactSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }

            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                originalMaterial = objectRenderer.material;
            }
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);

            if (canInteract)
            {
                Highlight();
            }
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);
            Unhighlight();
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (canInteract)
            {
                Interact();
            }
        }

        /// <summary>
        /// 상호작용 실행
        /// </summary>
        public virtual void Interact()
        {
            if (!canInteract) return;

            if (interactSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(interactSound);
            }

            OnInteract?.Invoke();
            Debug.Log($"[InteractableObject] {gameObject.name} 상호작용");
        }

        /// <summary>
        /// 하이라이트 표시
        /// </summary>
        protected virtual void Highlight()
        {
            if (isHighlighted) return;
            isHighlighted = true;

            OnHighlight?.Invoke();
        }

        /// <summary>
        /// 하이라이트 해제
        /// </summary>
        protected virtual void Unhighlight()
        {
            if (!isHighlighted) return;
            isHighlighted = false;

            OnUnhighlight?.Invoke();
        }

        /// <summary>
        /// 상호작용 가능 여부 설정
        /// </summary>
        public void SetInteractable(bool value)
        {
            canInteract = value;
        }
    }
}
