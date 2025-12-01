using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 카드 리더기 - 보안카드로 문/장치 잠금 해제
    /// </summary>
    public class CardReader : InteractableObject
    {
        [Header("Card Reader Settings")]
        [Tooltip("필요한 카드 ID")]
        public string requiredCardId = "security_card_01";

        [Tooltip("카드 삽입 위치")]
        public Transform cardSlot;

        [Tooltip("상태 표시 라이트")]
        public Light statusLight;

        [Tooltip("잠금 상태 색상")]
        public Color lockedColor = Color.red;

        [Tooltip("해제 상태 색상")]
        public Color unlockedColor = Color.green;

        [Header("Connected Objects")]
        [Tooltip("연결된 문")]
        public Door connectedDoor;

        [Tooltip("연결된 장치들")]
        public GameObject[] connectedDevices;

        [Header("Audio")]
        public AudioClip cardSwipeSound;
        public AudioClip accessGrantedSound;
        public AudioClip accessDeniedSound;
        public AudioClip doorUnlockSound;

        [Header("Events")]
        public UnityEvent OnCardAccepted;
        public UnityEvent OnCardDenied;
        public UnityEvent OnDoorUnlocked;

        private bool isUnlocked = false;
        private AudioSource audioSource;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // 초기 상태: 잠금
            SetLightColor(lockedColor);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (isUnlocked)
            {
                if (VRHUD.Instance != null)
                {
                    VRHUD.Instance.ShowStatus("이미 잠금이 해제되었습니다", 2f);
                }
                return;
            }

            // 보안카드 확인
            TrySwipeCard();
        }

        /// <summary>
        /// 카드 스와이프 시도
        /// </summary>
        public void TrySwipeCard()
        {
            // 스와이프 사운드
            if (cardSwipeSound != null)
            {
                audioSource.PlayOneShot(cardSwipeSound);
            }

            // 인벤토리에서 카드 확인
            bool hasCard = false;

            if (StoryProgressManager.Instance != null && StoryProgressManager.Instance.hasSecurityCard)
            {
                hasCard = true;
            }
            else if (PlayerInventory.Instance != null)
            {
                hasCard = PlayerInventory.Instance.HasItem(requiredCardId);
            }

            StartCoroutine(ProcessCard(hasCard));
        }

        private IEnumerator ProcessCard(bool hasCard)
        {
            yield return new WaitForSeconds(0.5f);

            if (hasCard)
            {
                // 접근 허용
                AccessGranted();
            }
            else
            {
                // 접근 거부
                AccessDenied();
            }
        }

        /// <summary>
        /// 접근 허용
        /// </summary>
        private void AccessGranted()
        {
            isUnlocked = true;

            // 사운드
            if (accessGrantedSound != null)
            {
                audioSource.PlayOneShot(accessGrantedSound);
            }

            // 라이트 색상 변경
            SetLightColor(unlockedColor);

            // 연결된 문 열기
            if (connectedDoor != null)
            {
                connectedDoor.Unlock();

                if (doorUnlockSound != null)
                {
                    audioSource.PlayOneShot(doorUnlockSound);
                }
            }

            // 연결된 장치 활성화
            if (connectedDevices != null)
            {
                foreach (var device in connectedDevices)
                {
                    if (device != null)
                    {
                        device.SetActive(true);
                    }
                }
            }

            OnCardAccepted?.Invoke();
            OnDoorUnlocked?.Invoke();

            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus("접근 허용됨", 2f);
            }

            Debug.Log("[CardReader] 카드 인식됨 - 잠금 해제");
        }

        /// <summary>
        /// 접근 거부
        /// </summary>
        private void AccessDenied()
        {
            // 사운드
            if (accessDeniedSound != null)
            {
                audioSource.PlayOneShot(accessDeniedSound);
            }

            // 라이트 깜빡임
            StartCoroutine(BlinkLight());

            OnCardDenied?.Invoke();

            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus("접근 거부 - 보안카드가 필요합니다", 2f);
            }

            Debug.Log("[CardReader] 접근 거부 - 카드 없음");
        }

        private IEnumerator BlinkLight()
        {
            for (int i = 0; i < 3; i++)
            {
                SetLightColor(Color.yellow);
                yield return new WaitForSeconds(0.2f);
                SetLightColor(lockedColor);
                yield return new WaitForSeconds(0.2f);
            }
        }

        private void SetLightColor(Color color)
        {
            if (statusLight != null)
            {
                statusLight.color = color;
            }
        }

        /// <summary>
        /// 강제 잠금 해제 (디버그용)
        /// </summary>
        [ContextMenu("Force Unlock")]
        public void ForceUnlock()
        {
            AccessGranted();
        }
    }
}
