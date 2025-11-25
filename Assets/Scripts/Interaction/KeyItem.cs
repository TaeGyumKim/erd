using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 열쇠 아이템
    /// 특정 문을 열기 위한 열쇠
    ///
    /// 사용법:
    /// 1. 열쇠 오브젝트에 추가
    /// 2. keyId를 해당 문의 requiredKeyId와 동일하게 설정
    /// </summary>
    public class KeyItem : PickupItem
    {
        [Header("Key Settings")]
        [Tooltip("열쇠 ID (문의 requiredKeyId와 매칭)")]
        public string keyId = "key_01";

        [Tooltip("열쇠 이름")]
        public string keyName = "열쇠";

        [Tooltip("열쇠 설명")]
        [TextArea]
        public string keyDescription = "어딘가의 문을 열 수 있을 것 같다.";

        protected override void Awake()
        {
            // 아이템 데이터 자동 설정
            itemData = new InventoryItem
            {
                itemId = keyId,
                itemName = keyName,
                description = keyDescription,
                isConsumable = false, // 열쇠는 사라지지 않음 (문에서 자동 사용)
                itemType = InventoryItem.ItemType.Key
            };

            base.Awake();

            // 열쇠는 항상 자동 수집
            autoCollect = true;
        }

        /// <summary>
        /// 에디터에서 열쇠 ID 표시
        /// </summary>
        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            // 열쇠 아이콘 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            // 열쇠 ID 표시
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"Key: {keyId}"
            );
#endif
        }
    }
}
