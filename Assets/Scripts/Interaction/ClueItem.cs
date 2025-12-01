using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// 단서 아이템 (USB, 라이터, 보안카드, 배터리, 기어)
    /// PickupItem을 확장하여 스토리 진행과 연동
    /// </summary>
    public class ClueItem : PickupItem
    {
        [Header("Clue Settings")]
        [Tooltip("단서 아이템 종류")]
        public ClueItemType clueType;

        [Tooltip("획득 시 표시할 메시지")]
        [TextArea(2, 4)]
        public string pickupMessage;

        [Header("Special Effects")]
        [Tooltip("획득 시 특수 효과 재생")]
        public ParticleSystem pickupEffect;

        [Tooltip("발견 힌트 (유령이 알려줌)")]
        public string ghostHint;

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            // 부모 클래스 수집 로직 실행
            base.OnSelectEntered(args);

            // 스토리 매니저에 단서 획득 알림
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.CollectClueItem(clueType);
            }

            // 특수 효과
            if (pickupEffect != null)
            {
                var effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
                effect.Play();
                Destroy(effect.gameObject, effect.main.duration);
            }
        }
    }

    /// <summary>
    /// USB 아이템 - 컴퓨터에서 정보 확인 가능
    /// </summary>
    public class USBItem : ClueItem
    {
        [Header("USB Content")]
        [TextArea(3, 6)]
        public string[] recordedMessages;

        public AudioClip[] audioRecordings;

        protected override void Awake()
        {
            base.Awake();
            clueType = ClueItemType.USB;

            if (itemData != null)
            {
                itemData.itemName = "USB 드라이브";
                itemData.description = "누군가의 기록이 담긴 USB";
            }
        }
    }

    /// <summary>
    /// 라이터 아이템 - 숨겨진 문양 발견 가능
    /// </summary>
    public class LighterItem : ClueItem
    {
        [Header("Lighter Settings")]
        [Tooltip("라이터 불빛")]
        public Light lighterLight;

        [Tooltip("불꽃 파티클")]
        public ParticleSystem flameEffect;

        [Tooltip("연료량 (초)")]
        public float fuelAmount = 60f;

        private bool isLit = false;
        private float currentFuel;

        protected override void Awake()
        {
            base.Awake();
            clueType = ClueItemType.Lighter;
            currentFuel = fuelAmount;

            if (itemData != null)
            {
                itemData.itemName = "라이터";
                itemData.description = "어둠을 밝힐 수 있는 라이터";
            }

            // 처음엔 꺼진 상태
            if (lighterLight != null) lighterLight.enabled = false;
            if (flameEffect != null) flameEffect.Stop();
        }

        private void Update()
        {
            if (isLit && currentFuel > 0)
            {
                currentFuel -= Time.deltaTime;

                if (currentFuel <= 0)
                {
                    TurnOff();
                }
            }
        }

        /// <summary>
        /// 라이터 켜기/끄기 토글
        /// </summary>
        public void ToggleLighter()
        {
            if (isLit)
            {
                TurnOff();
            }
            else
            {
                TurnOn();
            }
        }

        public void TurnOn()
        {
            if (currentFuel <= 0) return;

            isLit = true;
            if (lighterLight != null) lighterLight.enabled = true;
            if (flameEffect != null) flameEffect.Play();
        }

        public void TurnOff()
        {
            isLit = false;
            if (lighterLight != null) lighterLight.enabled = false;
            if (flameEffect != null) flameEffect.Stop();
        }

        public bool IsLit => isLit;
        public float FuelPercent => currentFuel / fuelAmount;
    }

    /// <summary>
    /// 보안카드 아이템 - 잠긴 문 열기
    /// </summary>
    public class SecurityCardItem : ClueItem
    {
        [Header("Card Settings")]
        [Tooltip("카드 ID (문과 매칭)")]
        public string cardId = "security_card_01";

        [Tooltip("카드 소유자 이름")]
        public string ownerName = "???";

        [Tooltip("사망 날짜")]
        public string deathDate = "19XX.XX.XX";

        protected override void Awake()
        {
            base.Awake();
            clueType = ClueItemType.SecurityCard;

            if (itemData != null)
            {
                itemData.itemName = "보안 카드";
                itemData.description = $"{ownerName}의 보안 카드\n{deathDate}";
                itemData.itemId = cardId;
            }
        }
    }

    /// <summary>
    /// 단서용 배터리 아이템 - 기계 장치 작동
    /// (손전등용 BatteryItem과 구분)
    /// </summary>
    public class ClueBatteryItem : ClueItem
    {
        [Header("Clue Battery Settings")]
        [Tooltip("배터리 용량")]
        public float batteryCapacity = 100f;

        protected override void Awake()
        {
            base.Awake();
            clueType = ClueItemType.Battery;

            if (itemData != null)
            {
                itemData.itemName = "단서 배터리";
                itemData.description = "기계 장치에 사용할 수 있는 배터리";
            }
        }
    }

    /// <summary>
    /// 기어 아이템 - 기계 장치 수리
    /// </summary>
    public class GearItem : ClueItem
    {
        [Header("Gear Settings")]
        [Tooltip("기어 크기")]
        public GearSize gearSize = GearSize.Medium;

        public enum GearSize { Small, Medium, Large }

        protected override void Awake()
        {
            base.Awake();
            clueType = ClueItemType.Gear;

            if (itemData != null)
            {
                itemData.itemName = "기어";
                itemData.description = "기계 장치에 사용할 수 있는 기어";
            }
        }
    }

    /// <summary>
    /// 최종 열쇠 아이템
    /// </summary>
    public class FinalKeyItem : ClueItem
    {
        [Header("Final Key Settings")]
        [Tooltip("탈출문 ID")]
        public string exitDoorId = "exit_door";

        protected override void Awake()
        {
            base.Awake();
            clueType = ClueItemType.FinalKey;

            if (itemData != null)
            {
                itemData.itemName = "탈출 열쇠";
                itemData.description = "탈출구를 열 수 있는 열쇠";
                itemData.itemType = InventoryItem.ItemType.Key;
                itemData.itemId = exitDoorId;
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // 스토리 매니저에 최종 열쇠 획득 알림
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.ObtainFinalKey();
            }
        }
    }
}
