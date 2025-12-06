using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 비밀번호로 열리는 상자
    /// Room4의 상자 시스템
    /// </summary>
    public class PasswordChest : UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
    {
        [Header("Password Settings")]
        [Tooltip("비밀번호")]
        public string correctPassword = "1234";

        [Tooltip("비밀번호 자릿수")]
        public int passwordLength = 4;

        [Tooltip("현재 입력된 비밀번호")]
        [SerializeField] private string currentInput = "";

        [Header("State")]
        [Tooltip("상자 열림 상태")]
        public bool isOpen = false;

        [Tooltip("잠금 상태")]
        public bool isLocked = true;

        [Header("Contents")]
        [Tooltip("상자 안의 아이템")]
        public GameObject containedItem;

        [Tooltip("아이템 스폰 위치")]
        public Transform itemSpawnPoint;

        [Header("Visual")]
        [Tooltip("상자 뚜껑")]
        public Transform lid;

        [Tooltip("열림 각도")]
        public float openAngle = -110f;

        [Tooltip("열리는 속도")]
        public float openSpeed = 2f;

        [Tooltip("비밀번호 디스플레이 (옵션)")]
        public TMPro.TextMeshPro passwordDisplay;

        [Header("Audio")]
        public AudioClip buttonPressSound;
        public AudioClip correctSound;
        public AudioClip wrongSound;
        public AudioClip openSound;
        public AudioClip itemPickupSound;

        [Header("Events")]
        public UnityEvent OnPasswordCorrect;
        public UnityEvent OnPasswordWrong;
        public UnityEvent OnChestOpen;
        public UnityEvent OnItemTaken;
        public UnityEvent<string> OnInputChanged;

        private AudioSource audioSource;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private bool itemTaken = false;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // 뚜껑 회전값 저장
            if (lid != null)
            {
                closedRotation = lid.localRotation;
                openRotation = closedRotation * Quaternion.Euler(openAngle, 0, 0);
            }

            // 아이템 숨기기
            if (containedItem != null)
            {
                containedItem.SetActive(false);
            }

            UpdateDisplay();
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (isOpen && !itemTaken)
            {
                // 이미 열려있으면 아이템 획득
                TakeItem();
            }
            else if (!isOpen && !isLocked)
            {
                // 잠금 해제됐으면 열기
                OpenChest();
            }
            else if (isLocked)
            {
                // 잠겨있으면 비밀번호 UI 표시
                ShowPasswordUI();
            }
        }

        /// <summary>
        /// 비밀번호 UI 표시
        /// </summary>
        private void ShowPasswordUI()
        {
            if (GamePopupUI.Instance != null)
            {
                GamePopupUI.Instance.ShowPasswordInput(
                    "비밀번호를 입력하세요",
                    passwordLength,
                    OnPasswordSubmit
                );
            }
            else
            {
                Debug.Log("[PasswordChest] GamePopupUI가 없습니다. 직접 입력해야 합니다.");
            }
        }

        /// <summary>
        /// 숫자 입력 (0-9)
        /// </summary>
        public void InputNumber(int number)
        {
            if (isOpen || !isLocked) return;
            if (currentInput.Length >= passwordLength) return;

            currentInput += number.ToString();

            // 사운드
            if (buttonPressSound != null)
            {
                audioSource.PlayOneShot(buttonPressSound);
            }

            UpdateDisplay();
            OnInputChanged?.Invoke(currentInput);

            // 자릿수 완료 시 자동 확인
            if (currentInput.Length >= passwordLength)
            {
                CheckPassword();
            }
        }

        /// <summary>
        /// 입력 지우기
        /// </summary>
        public void ClearInput()
        {
            currentInput = "";
            UpdateDisplay();
            OnInputChanged?.Invoke(currentInput);
        }

        /// <summary>
        /// 마지막 자리 지우기
        /// </summary>
        public void Backspace()
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                UpdateDisplay();
                OnInputChanged?.Invoke(currentInput);
            }
        }

        /// <summary>
        /// 비밀번호 제출 (팝업에서 호출)
        /// </summary>
        private void OnPasswordSubmit(string password)
        {
            currentInput = password;
            CheckPassword();
        }

        /// <summary>
        /// 비밀번호 확인
        /// </summary>
        public void CheckPassword()
        {
            if (currentInput == correctPassword)
            {
                // 정답
                isLocked = false;

                if (correctSound != null)
                {
                    audioSource.PlayOneShot(correctSound);
                }

                OnPasswordCorrect?.Invoke();
                Debug.Log("[PasswordChest] 비밀번호 정답!");

                // 자동으로 열기
                StartCoroutine(DelayedOpen(0.5f));
            }
            else
            {
                // 오답
                if (wrongSound != null)
                {
                    audioSource.PlayOneShot(wrongSound);
                }

                OnPasswordWrong?.Invoke();
                Debug.Log($"[PasswordChest] 비밀번호 오답: {currentInput}");

                // 입력 초기화
                ClearInput();
            }
        }

        private IEnumerator DelayedOpen(float delay)
        {
            yield return new WaitForSeconds(delay);
            OpenChest();
        }

        /// <summary>
        /// 상자 열기
        /// </summary>
        public void OpenChest()
        {
            if (isOpen || isLocked) return;

            isOpen = true;

            // 사운드
            if (openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            // 뚜껑 열기 애니메이션
            if (lid != null)
            {
                StartCoroutine(OpenLidAnimation());
            }

            // 아이템 표시
            if (containedItem != null)
            {
                containedItem.SetActive(true);

                // 스폰 위치로 이동
                if (itemSpawnPoint != null)
                {
                    containedItem.transform.position = itemSpawnPoint.position;
                }
            }

            OnChestOpen?.Invoke();
            Debug.Log("[PasswordChest] 상자 열림");
        }

        private IEnumerator OpenLidAnimation()
        {
            float elapsed = 0f;
            Quaternion startRot = lid.localRotation;

            while (elapsed < 1f / openSpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed * openSpeed;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                lid.localRotation = Quaternion.Slerp(startRot, openRotation, t);
                yield return null;
            }

            lid.localRotation = openRotation;
        }

        /// <summary>
        /// 아이템 획득
        /// </summary>
        public void TakeItem()
        {
            if (!isOpen || itemTaken) return;
            if (containedItem == null) return;

            itemTaken = true;

            // 사운드
            if (itemPickupSound != null)
            {
                audioSource.PlayOneShot(itemPickupSound);
            }

            // 인벤토리에 추가
            if (PlayerInventory.Instance != null)
            {
                // 아이템 타입에 따라 처리
                var keyItem = containedItem.GetComponent<KeyItem>();
                if (keyItem != null)
                {
                    PlayerInventory.Instance.AddKey(keyItem.keyId);
                }
            }

            // RoomProgressManager에 알림
            if (RoomProgressManager.Instance != null)
            {
                RoomProgressManager.Instance.OnItemCollected("chest_item");
            }

            OnItemTaken?.Invoke();
            Debug.Log("[PasswordChest] 아이템 획득");

            // 아이템 비활성화 또는 삭제
            containedItem.SetActive(false);
        }

        /// <summary>
        /// 디스플레이 업데이트
        /// </summary>
        private void UpdateDisplay()
        {
            if (passwordDisplay != null)
            {
                // 입력된 자릿수만큼 * 표시
                string display = "";
                for (int i = 0; i < passwordLength; i++)
                {
                    if (i < currentInput.Length)
                    {
                        display += "*";
                    }
                    else
                    {
                        display += "_";
                    }

                    if (i < passwordLength - 1) display += " ";
                }
                passwordDisplay.text = display;
            }
        }

        /// <summary>
        /// 외부에서 비밀번호 설정
        /// </summary>
        public void SetPassword(string newPassword)
        {
            correctPassword = newPassword;
            passwordLength = newPassword.Length;
        }

        /// <summary>
        /// RoomProgressManager에서 비밀번호 확인용
        /// </summary>
        public bool ValidatePassword(string input)
        {
            return input == correctPassword;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 상자 상태 표시
            Gizmos.color = isOpen ? Color.green : (isLocked ? Color.red : Color.yellow);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

            // 아이템 스폰 위치
            if (itemSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(itemSpawnPoint.position, 0.1f);
            }
        }
#endif
    }
}
