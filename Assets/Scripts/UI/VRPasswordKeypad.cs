using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;
using System;

namespace HorrorGame
{
    /// <summary>
    /// VR용 비밀번호 키패드 UI
    /// World Space Canvas로 플레이어 앞에 생성되며,
    /// XR Ray Interactor로 버튼 클릭 가능
    /// </summary>
    public class VRPasswordKeypad : MonoBehaviour
    {
        public static VRPasswordKeypad Instance { get; private set; }

        [Header("Canvas Settings")]
        [Tooltip("키패드 Canvas")]
        public Canvas keypadCanvas;

        [Tooltip("키패드 패널")]
        public GameObject keypadPanel;

        [Tooltip("상호작용 오브젝트 앞에 위치 (true: 오브젝트 앞, false: 플레이어 앞)")]
        public bool positionNearTarget = true;

        [Tooltip("타겟 오브젝트로부터의 거리")]
        public float distanceFromTarget = 0.8f;

        [Tooltip("플레이어로부터의 거리 (positionNearTarget=false 시)")]
        public float distanceFromPlayer = 1.5f;

        [Tooltip("높이 오프셋")]
        public float heightOffset = 0.5f;

        [Header("UI Elements")]
        [Tooltip("제목 텍스트")]
        public TextMeshProUGUI titleText;

        [Tooltip("비밀번호 표시 텍스트")]
        public TextMeshProUGUI passwordDisplayText;

        [Tooltip("숫자 버튼들 (0-9)")]
        public Button[] numberButtons;

        [Tooltip("백스페이스 버튼")]
        public Button backspaceButton;

        [Tooltip("확인(Enter) 버튼")]
        public Button enterButton;

        [Tooltip("닫기(X) 버튼")]
        public Button closeButton;

        [Tooltip("클리어(C) 버튼")]
        public Button clearButton;

        [Header("Audio")]
        public AudioClip buttonClickSound;
        public AudioClip openSound;
        public AudioClip closeSound;

        [Header("Events")]
        public UnityEvent OnKeypadOpen;
        public UnityEvent OnKeypadClose;

        private AudioSource audioSource;
        private Transform playerCamera;
        private Transform currentTarget; // 현재 상호작용 타겟
        private Action<string> currentCallback;
        private string currentInput = "";
        private int currentPasswordLength = 4;
        private bool isOpen = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 5f;
            }

            SetupButtons();

            // 시작 시 숨기기
            if (keypadPanel != null)
            {
                keypadPanel.SetActive(false);
            }
        }

        private void Start()
        {
            // 플레이어 카메라 찾기
            FindPlayerCamera();
        }

        private void FindPlayerCamera()
        {
            // VR 카메라 찾기
            if (Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
            else
            {
                var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    playerCamera = xrOrigin.Camera?.transform;
                }
            }

            if (playerCamera == null)
            {
                Debug.LogWarning("[VRPasswordKeypad] 플레이어 카메라를 찾을 수 없습니다.");
            }
        }

        private void SetupButtons()
        {
            // 숫자 버튼들 설정
            if (numberButtons != null)
            {
                for (int i = 0; i < numberButtons.Length && i < 10; i++)
                {
                    int number = i;
                    if (numberButtons[i] != null)
                    {
                        numberButtons[i].onClick.AddListener(() => OnNumberClick(number));
                    }
                }
            }

            // 백스페이스 버튼
            if (backspaceButton != null)
            {
                backspaceButton.onClick.AddListener(OnBackspaceClick);
            }

            // Enter 버튼
            if (enterButton != null)
            {
                enterButton.onClick.AddListener(OnEnterClick);
            }

            // 닫기 버튼
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            // 클리어 버튼
            if (clearButton != null)
            {
                clearButton.onClick.AddListener(OnClearClick);
            }
        }

        /// <summary>
        /// 키패드 열기 (타겟 오브젝트 지정)
        /// </summary>
        public void Open(string title, int passwordLength, Action<string> onSubmit, Transform target = null)
        {
            // 이미 열려있으면 닫고 다시 열기
            if (isOpen)
            {
                Close();
            }

            if (playerCamera == null)
            {
                FindPlayerCamera();
            }

            currentPasswordLength = passwordLength;
            currentCallback = onSubmit;
            currentInput = "";
            currentTarget = target;

            // 제목 설정
            if (titleText != null)
            {
                titleText.text = title;
            }

            UpdatePasswordDisplay();

            // 위치 설정
            if (positionNearTarget && currentTarget != null)
            {
                PositionNearTarget();
            }
            else if (playerCamera != null)
            {
                PositionInFrontOfPlayer();
            }

            // 패널 활성화
            if (keypadPanel != null)
            {
                keypadPanel.SetActive(true);
                Debug.Log($"[VRPasswordKeypad] 키패드 패널 활성화됨, 위치: {transform.position}");
            }
            else
            {
                Debug.LogError("[VRPasswordKeypad] keypadPanel이 null입니다!");
            }

            isOpen = true;

            // 사운드
            if (openSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            OnKeypadOpen?.Invoke();
            Debug.Log($"[VRPasswordKeypad] 키패드 열림: {title}, {passwordLength}자리, 타겟: {(target != null ? target.name : "없음")}");
        }

        /// <summary>
        /// 타겟 오브젝트 앞에 위치시키기
        /// </summary>
        private void PositionNearTarget()
        {
            if (currentTarget == null || playerCamera == null) return;

            // 타겟에서 플레이어 방향으로 거리만큼 이동
            Vector3 dirToPlayer = (playerCamera.position - currentTarget.position).normalized;
            dirToPlayer.y = 0; // 수평으로만
            dirToPlayer.Normalize();

            Vector3 position = currentTarget.position + dirToPlayer * distanceFromTarget;
            position.y = playerCamera.position.y + heightOffset;

            transform.position = position;

            // 플레이어를 바라보게 회전
            transform.LookAt(playerCamera);
            transform.Rotate(0, 180, 0);

            Debug.Log($"[VRPasswordKeypad] 타겟 앞에 위치: {position}, 타겟: {currentTarget.name}");
        }

        /// <summary>
        /// 플레이어 앞에 위치시키기
        /// </summary>
        private void PositionInFrontOfPlayer()
        {
            if (playerCamera == null) return;

            // 플레이어 앞 위치 계산
            Vector3 forward = playerCamera.forward;
            forward.y = 0; // 수평으로만
            forward.Normalize();

            Vector3 position = playerCamera.position + forward * distanceFromPlayer;
            position.y = playerCamera.position.y + heightOffset;

            transform.position = position;

            // 플레이어를 바라보게 회전
            transform.LookAt(playerCamera);
            transform.Rotate(0, 180, 0); // 뒤집기 (Canvas가 플레이어를 향하도록)
        }

        /// <summary>
        /// 키패드 닫기
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;

            // 패널 비활성화
            if (keypadPanel != null)
            {
                keypadPanel.SetActive(false);
            }

            // 사운드
            if (closeSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(closeSound);
            }

            // 콜백 초기화 (취소)
            currentCallback = null;
            currentInput = "";

            OnKeypadClose?.Invoke();
            Debug.Log("[VRPasswordKeypad] 키패드 닫힘 (취소)");
        }

        /// <summary>
        /// 숫자 버튼 클릭
        /// </summary>
        private void OnNumberClick(int number)
        {
            if (currentInput.Length >= currentPasswordLength) return;

            PlayButtonSound();

            currentInput += number.ToString();
            UpdatePasswordDisplay();

            Debug.Log($"[VRPasswordKeypad] 숫자 입력: {number}, 현재: {currentInput}");

            // 자릿수 완료 시 자동 제출
            if (currentInput.Length >= currentPasswordLength)
            {
                // 잠시 대기 후 제출 (디스플레이 업데이트 보여주기 위해)
                Invoke(nameof(SubmitPassword), 0.3f);
            }
        }

        /// <summary>
        /// 백스페이스 클릭
        /// </summary>
        private void OnBackspaceClick()
        {
            if (currentInput.Length > 0)
            {
                PlayButtonSound();
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                UpdatePasswordDisplay();
            }
        }

        /// <summary>
        /// 클리어 클릭
        /// </summary>
        private void OnClearClick()
        {
            PlayButtonSound();
            currentInput = "";
            UpdatePasswordDisplay();
        }

        /// <summary>
        /// Enter 클릭
        /// </summary>
        private void OnEnterClick()
        {
            if (currentInput.Length > 0)
            {
                SubmitPassword();
            }
        }

        /// <summary>
        /// 비밀번호 제출
        /// </summary>
        private void SubmitPassword()
        {
            if (!isOpen) return;

            PlayButtonSound();

            string password = currentInput;
            Action<string> callback = currentCallback;

            // 상태 초기화
            isOpen = false;
            currentCallback = null;
            currentInput = "";

            // 패널 비활성화
            if (keypadPanel != null)
            {
                keypadPanel.SetActive(false);
            }

            // 콜백 실행
            callback?.Invoke(password);

            OnKeypadClose?.Invoke();
            Debug.Log($"[VRPasswordKeypad] 비밀번호 제출: {password}");
        }

        /// <summary>
        /// 비밀번호 디스플레이 업데이트
        /// </summary>
        private void UpdatePasswordDisplay()
        {
            if (passwordDisplayText == null) return;

            string display = "";
            for (int i = 0; i < currentPasswordLength; i++)
            {
                if (i < currentInput.Length)
                {
                    display += currentInput[i]; // 입력된 숫자 표시
                }
                else
                {
                    display += "_"; // 빈 자리
                }

                if (i < currentPasswordLength - 1)
                {
                    display += " ";
                }
            }

            passwordDisplayText.text = display;
            Debug.Log($"[VRPasswordKeypad] 디스플레이 업데이트: {display}");
        }

        private void PlayButtonSound()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }

        /// <summary>
        /// 키패드가 열려있는지 확인
        /// </summary>
        public bool IsOpen => isOpen;

        private void Update()
        {
            // 열려있을 때 항상 플레이어를 바라보게 회전
            if (isOpen && playerCamera != null)
            {
                // 부드럽게 플레이어를 향해 회전
                Vector3 lookPos = playerCamera.position;
                lookPos.y = transform.position.y; // 수평으로만
                transform.LookAt(lookPos);
                transform.Rotate(0, 180, 0);
            }
        }
    }
}
