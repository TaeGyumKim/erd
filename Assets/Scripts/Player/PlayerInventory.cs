using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 플레이어 인벤토리 시스템
    /// 열쇠, 아이템 등을 관리
    ///
    /// 사용법:
    /// 1. VRPlayer 오브젝트에 추가
    /// 2. PlayerInventory.Instance로 접근
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        [Header("Inventory Settings")]
        [Tooltip("최대 아이템 수")]
        public int maxItems = 10;

        [Header("Current Items")]
        [Tooltip("보유 중인 열쇠 목록")]
        public List<string> keys = new List<string>();

        [Tooltip("보유 중인 아이템 목록")]
        public List<InventoryItem> items = new List<InventoryItem>();

        [Header("Events")]
        public UnityEngine.Events.UnityEvent<string> OnKeyCollected;
        public UnityEngine.Events.UnityEvent<InventoryItem> OnItemCollected;
        public UnityEngine.Events.UnityEvent<InventoryItem> OnItemUsed;
        public UnityEngine.Events.UnityEvent<InventoryItem> OnItemDropped;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 열쇠 보유 여부 확인
        /// </summary>
        public bool HasKey(string keyId)
        {
            return keys.Contains(keyId);
        }

        /// <summary>
        /// 열쇠 추가
        /// </summary>
        public void AddKey(string keyId)
        {
            if (!keys.Contains(keyId))
            {
                keys.Add(keyId);
                OnKeyCollected?.Invoke(keyId);
                Debug.Log($"[PlayerInventory] 열쇠 획득: {keyId}");
            }
        }

        /// <summary>
        /// 열쇠 사용 (제거)
        /// </summary>
        public bool UseKey(string keyId)
        {
            if (keys.Contains(keyId))
            {
                keys.Remove(keyId);
                Debug.Log($"[PlayerInventory] 열쇠 사용: {keyId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 아이템 추가
        /// </summary>
        public bool AddItem(InventoryItem item)
        {
            if (items.Count >= maxItems)
            {
                Debug.Log("[PlayerInventory] 인벤토리가 가득 찼습니다!");
                return false;
            }

            items.Add(item);
            OnItemCollected?.Invoke(item);
            Debug.Log($"[PlayerInventory] 아이템 획득: {item.itemName}");
            return true;
        }

        /// <summary>
        /// 아이템 보유 여부 확인
        /// </summary>
        public bool HasItem(string itemId)
        {
            return items.Exists(item => item.itemId == itemId);
        }

        /// <summary>
        /// 아이템 가져오기
        /// </summary>
        public InventoryItem GetItem(string itemId)
        {
            return items.Find(item => item.itemId == itemId);
        }

        /// <summary>
        /// 아이템 사용
        /// </summary>
        public bool UseItem(string itemId)
        {
            var item = GetItem(itemId);
            if (item != null)
            {
                if (item.isConsumable)
                {
                    items.Remove(item);
                }
                OnItemUsed?.Invoke(item);
                Debug.Log($"[PlayerInventory] 아이템 사용: {item.itemName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 아이템 버리기
        /// </summary>
        public bool DropItem(string itemId)
        {
            var item = GetItem(itemId);
            if (item != null)
            {
                items.Remove(item);
                OnItemDropped?.Invoke(item);
                Debug.Log($"[PlayerInventory] 아이템 버림: {item.itemName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 인벤토리 초기화
        /// </summary>
        public void ClearInventory()
        {
            keys.Clear();
            items.Clear();
            Debug.Log("[PlayerInventory] 인벤토리 초기화");
        }

        /// <summary>
        /// 열쇠 개수
        /// </summary>
        public int GetKeyCount()
        {
            return keys.Count;
        }

        /// <summary>
        /// 아이템 개수
        /// </summary>
        public int GetItemCount()
        {
            return items.Count;
        }
    }

    /// <summary>
    /// 인벤토리 아이템 데이터
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public string itemName;
        public string description;
        public Sprite icon;
        public bool isConsumable = true;
        public ItemType itemType;

        public enum ItemType
        {
            Key,
            Battery,
            Document,
            Tool,
            Other
        }
    }
}
