using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 배터리 아이템
    /// 손전등 충전용
    ///
    /// 사용법:
    /// 1. 배터리 오브젝트에 추가
    /// 2. 줍기만 하면 자동으로 손전등 충전
    /// </summary>
    public class BatteryItem : PickupItem
    {
        [Header("Battery Settings")]
        [Tooltip("충전량")]
        public float chargeAmount = 50f;

        [Tooltip("즉시 사용 (줍자마자 충전)")]
        public bool useOnPickup = true;

        protected override void Awake()
        {
            // 아이템 데이터 자동 설정
            itemData = new InventoryItem
            {
                itemId = "battery_" + GetInstanceID(),
                itemName = "배터리",
                description = $"손전등 배터리를 {chargeAmount}% 충전합니다.",
                isConsumable = true,
                itemType = InventoryItem.ItemType.Battery
            };

            base.Awake();

            autoCollect = true;
        }

        /// <summary>
        /// 아이템 수집 오버라이드
        /// </summary>
        public new void Collect()
        {
            if (useOnPickup)
            {
                // 즉시 사용
                UseBattery();
            }
            else
            {
                // 인벤토리에 추가
                base.Collect();
            }
        }

        /// <summary>
        /// 배터리 사용
        /// </summary>
        public void UseBattery()
        {
            // VRPlayer의 손전등 찾기
            var player = VRPlayer.Instance;
            if (player != null)
            {
                var flashlight = player.GetComponentInChildren<VRFlashlight>();
                if (flashlight != null)
                {
                    flashlight.RechargeBattery(chargeAmount);

                    // 사운드 재생
                    if (pickupSound != null)
                    {
                        AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                    }

                    OnPickedUp?.Invoke();
                    Debug.Log($"[BatteryItem] 손전등 {chargeAmount}% 충전됨");

                    if (destroyOnCollect)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.LogWarning("[BatteryItem] 손전등을 찾을 수 없습니다!");
                }
            }
        }
    }
}
