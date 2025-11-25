using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// 주울 수 있는 아이템
    /// 열쇠, 배터리, 문서 등에 사용
    ///
    /// 사용법:
    /// 1. 아이템 오브젝트에 추가
    /// 2. Collider (Trigger) 필요
    /// 3. itemData 설정
    /// </summary>
    public class PickupItem : XRGrabInteractable
    {
        [Header("Item Data")]
        [Tooltip("아이템 정보")]
        public InventoryItem itemData;

        [Header("Pickup Settings")]
        [Tooltip("자동 수집 (주우면 바로 인벤토리로)")]
        public bool autoCollect = true;

        [Tooltip("수집 후 오브젝트 제거")]
        public bool destroyOnCollect = true;

        [Header("Audio")]
        public AudioClip pickupSound;

        [Header("Visual")]
        [Tooltip("아이템 발광 효과")]
        public bool glowEffect = true;

        [Tooltip("발광 색상")]
        public Color glowColor = new Color(1f, 1f, 0.5f, 1f);

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnPickedUp;

        private AudioSource audioSource;
        private Renderer itemRenderer;
        private Material originalMaterial;
        private bool isCollected;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && pickupSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }

            itemRenderer = GetComponent<Renderer>();
            if (itemRenderer != null)
            {
                originalMaterial = itemRenderer.material;
            }

            // 기본 아이템 데이터 설정
            if (itemData == null)
            {
                itemData = new InventoryItem
                {
                    itemId = gameObject.name,
                    itemName = gameObject.name,
                    description = "",
                    isConsumable = true,
                    itemType = InventoryItem.ItemType.Other
                };
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (autoCollect && !isCollected)
            {
                Collect();
            }
        }

        /// <summary>
        /// 아이템 수집
        /// </summary>
        public void Collect()
        {
            if (isCollected) return;

            // 인벤토리에 추가
            if (PlayerInventory.Instance != null)
            {
                bool added = false;

                // 열쇠 타입이면 열쇠로 추가
                if (itemData.itemType == InventoryItem.ItemType.Key)
                {
                    PlayerInventory.Instance.AddKey(itemData.itemId);
                    added = true;
                }
                else
                {
                    added = PlayerInventory.Instance.AddItem(itemData);
                }

                if (added)
                {
                    isCollected = true;

                    // 사운드 재생
                    if (pickupSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(pickupSound);
                    }

                    OnPickedUp?.Invoke();

                    if (destroyOnCollect)
                    {
                        // 사운드 재생 후 삭제
                        if (pickupSound != null)
                        {
                            Destroy(gameObject, pickupSound.length);
                        }
                        else
                        {
                            Destroy(gameObject);
                        }
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }

                    Debug.Log($"[PickupItem] {itemData.itemName} 수집됨");
                }
            }
            else
            {
                Debug.LogWarning("[PickupItem] PlayerInventory를 찾을 수 없습니다!");
            }
        }

        protected override void OnHoverEntered(HoverEnterEventArgs args)
        {
            base.OnHoverEntered(args);

            if (glowEffect && itemRenderer != null)
            {
                // 발광 효과
                itemRenderer.material.EnableKeyword("_EMISSION");
                itemRenderer.material.SetColor("_EmissionColor", glowColor);
            }
        }

        protected override void OnHoverExited(HoverExitEventArgs args)
        {
            base.OnHoverExited(args);

            if (glowEffect && itemRenderer != null && originalMaterial != null)
            {
                // 발광 효과 제거
                itemRenderer.material.DisableKeyword("_EMISSION");
            }
        }
    }
}
