using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 컴퓨터 단말기 - USB를 꽂아 정보 확인
    /// </summary>
    public class ComputerTerminal : InteractableObject
    {
        [Header("Terminal Settings")]
        [Tooltip("USB 삽입 위치")]
        public Transform usbSlot;

        [Tooltip("화면 디스플레이")]
        public Renderer screenDisplay;

        [Tooltip("화면 꺼짐 머티리얼")]
        public Material screenOffMaterial;

        [Tooltip("화면 켜짐 머티리얼")]
        public Material screenOnMaterial;

        [Tooltip("메시지 표시 머티리얼")]
        public Material screenMessageMaterial;

        [Header("USB Content")]
        [TextArea(3, 6)]
        public string[] usbMessages = new string[]
        {
            "연구 기록 #127",
            "실험체가 탈출했다...",
            "그를 멈춰야 한다... 열쇠는 벽의 문양 뒤에...",
            "이 기록을 보는 자여, 서둘러 탈출하라."
        };

        [Header("Audio")]
        public AudioClip usbInsertSound;
        public AudioClip computerOnSound;
        public AudioClip typingSound;
        public AudioClip errorSound;

        [Header("Events")]
        public UnityEvent OnUSBInserted;
        public UnityEvent OnMessageDisplayed;
        public UnityEvent<string> OnMessageRead;

        private bool hasUSB = false;
        private bool isDisplayingMessage = false;
        private int currentMessageIndex = 0;
        private AudioSource audioSource;

        protected override void Awake()
        {
            base.Awake();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // 처음엔 화면 꺼짐
            if (screenDisplay != null && screenOffMaterial != null)
            {
                screenDisplay.material = screenOffMaterial;
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            if (!hasUSB)
            {
                // USB가 없으면 에러
                if (errorSound != null)
                {
                    audioSource.PlayOneShot(errorSound);
                }

                if (VRHUD.Instance != null)
                {
                    VRHUD.Instance.ShowStatus("USB가 필요합니다", 2f);
                }
            }
            else if (!isDisplayingMessage)
            {
                // 다음 메시지 표시
                ShowNextMessage();
            }
        }

        /// <summary>
        /// USB 삽입
        /// </summary>
        public void InsertUSB()
        {
            if (hasUSB) return;

            // 인벤토리에서 USB 확인
            if (StoryProgressManager.Instance != null && !StoryProgressManager.Instance.hasUSB)
            {
                if (VRHUD.Instance != null)
                {
                    VRHUD.Instance.ShowStatus("USB를 먼저 찾아야 합니다", 2f);
                }
                return;
            }

            hasUSB = true;

            // 사운드
            if (usbInsertSound != null)
            {
                audioSource.PlayOneShot(usbInsertSound);
            }

            // 화면 켜기
            StartCoroutine(TurnOnScreen());

            OnUSBInserted?.Invoke();
            Debug.Log("[ComputerTerminal] USB 삽입됨");
        }

        private IEnumerator TurnOnScreen()
        {
            yield return new WaitForSeconds(0.5f);

            if (computerOnSound != null)
            {
                audioSource.PlayOneShot(computerOnSound);
            }

            if (screenDisplay != null && screenOnMaterial != null)
            {
                screenDisplay.material = screenOnMaterial;
            }

            yield return new WaitForSeconds(1f);

            // 첫 메시지 표시
            ShowNextMessage();
        }

        /// <summary>
        /// 다음 메시지 표시
        /// </summary>
        public void ShowNextMessage()
        {
            if (usbMessages == null || usbMessages.Length == 0) return;
            if (currentMessageIndex >= usbMessages.Length)
            {
                // 모든 메시지 읽음
                OnAllMessagesRead();
                return;
            }

            StartCoroutine(DisplayMessage(usbMessages[currentMessageIndex]));
            currentMessageIndex++;
        }

        private IEnumerator DisplayMessage(string message)
        {
            isDisplayingMessage = true;

            if (screenDisplay != null && screenMessageMaterial != null)
            {
                screenDisplay.material = screenMessageMaterial;
            }

            // 타이핑 사운드
            if (typingSound != null)
            {
                audioSource.PlayOneShot(typingSound);
            }

            // HUD에 메시지 표시
            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus(message, 5f);
            }

            OnMessageRead?.Invoke(message);

            yield return new WaitForSeconds(3f);

            isDisplayingMessage = false;
            OnMessageDisplayed?.Invoke();
        }

        private void OnAllMessagesRead()
        {
            // 스토리 진행
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.ReadUSBMessage();
            }

            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus("모든 기록을 확인했습니다.\n벽의 문양을 찾아보세요.", 4f);
            }
        }
    }
}
