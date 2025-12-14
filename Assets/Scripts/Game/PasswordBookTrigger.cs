using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 비밀번호 책 트리거
    /// 책을 열면 랜덤 비밀번호를 생성하고 GamePopupUI로 표시한 후,
    /// 살인마를 활성화하고 조명을 켜서 공포 분위기 연출
    ///
    /// 사용법:
    /// 1. book - Room3에 추가 (ReadableNote와 함께 사용)
    /// 2. killerObject, lightingParent는 자동 검색됨
    /// 3. passwordChest 연결 (비밀번호 공유, 옵션)
    /// </summary>
    public class PasswordBookTrigger : MonoBehaviour
    {
        [Header("Password Settings")]
        [Tooltip("비밀번호 길이")]
        public int passwordLength = 4;

        [Tooltip("생성된 비밀번호")]
        [SerializeField] private string generatedPassword;

        [Header("UI Settings")]
        [Tooltip("팝업 표시 시간")]
        public float popupDisplayTime = 3f;

        [Tooltip("팝업 제목")]
        public string popupTitle = "낡은 메모";

        [Tooltip("팝업 내용 포맷 ({0}에 비밀번호 삽입)")]
        [TextArea(3, 5)]
        public string popupContentFormat = "...지하실의 비밀번호는...\n\n<size=48><b>{0}</b></size>\n\n...절대 잊지 말 것...";

        [Header("References")]
        [Tooltip("살인마 오브젝트")]
        public GameObject killerObject;

        [Tooltip("조명 부모 오브젝트 (자식 Light들이 켜짐)")]
        public Transform lightingParent;

        [Tooltip("비밀번호 상자 (비밀번호 공유)")]
        public PasswordChest passwordChest;

        [Tooltip("비밀번호 패널 (비밀번호 공유)")]
        public PasswordPanel passwordPanel;

        [Header("Killer Settings")]
        [Tooltip("살인마 활성화 지연 시간")]
        public float killerActivationDelay = 0.5f;

        [Header("Audio")]
        public AudioClip bookOpenSound;
        public AudioClip warningSound;
        public AudioClip lightOnSound;

        [Header("Events")]
        public UnityEvent OnBookOpened;
        public UnityEvent OnPopupClosed;
        public UnityEvent OnKillerActivated;
        public UnityEvent OnLightsOn;

        private bool hasBeenTriggered = false;
        private AudioSource audioSource;
        private ReadableNote readableNote;

        /// <summary>
        /// 생성된 비밀번호 (외부 접근용)
        /// </summary>
        public string GeneratedPassword => generatedPassword;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            readableNote = GetComponent<ReadableNote>();

            // 비밀번호 미리 생성
            GeneratePassword();

            // 살인마 오브젝트 찾기 및 비활성화 (Awake에서 먼저 실행)
            if (killerObject == null)
            {
                killerObject = GameObject.Find("Killer");
            }
            if (killerObject != null)
            {
                killerObject.SetActive(false);
                Debug.Log("[PasswordBookTrigger] Killer 오브젝트 비활성화됨 (Awake)");
            }
            else
            {
                Debug.LogWarning("[PasswordBookTrigger] Killer 오브젝트를 찾을 수 없습니다!");
            }

            // 조명 오브젝트 찾기 및 끄기 (Awake에서 먼저 실행)
            // 중복된 이름의 오브젝트가 있을 수 있으므로 자식 Light가 있는 것을 찾음
            if (lightingParent == null)
            {
                var allLightingObjects = GameObject.FindObjectsOfType<Transform>();
                foreach (var t in allLightingObjects)
                {
                    if (t.name == "Lighting - Room3" && t.GetComponentInChildren<Light>(true) != null)
                    {
                        lightingParent = t;
                        Debug.Log($"[PasswordBookTrigger] 올바른 Lighting - Room3 발견 (자식 Light 있음)");
                        break;
                    }
                }

                // 못 찾으면 기존 방식으로 시도
                if (lightingParent == null)
                {
                    var lightingObj = GameObject.Find("Lighting - Room3");
                    if (lightingObj != null)
                    {
                        lightingParent = lightingObj.transform;
                    }
                }
            }
            if (lightingParent != null)
            {
                SetLights(false);
                Debug.Log("[PasswordBookTrigger] Lighting - Room3 조명 꺼짐 (Awake)");
            }
        }

        private void Start()
        {
            // ReadableNote의 이벤트에 연결 및 내용 업데이트
            if (readableNote != null)
            {
                readableNote.OnNoteOpened.AddListener(TriggerBookEvent);

                // 책 내용에 비밀번호 표시
                readableNote.noteContent = string.Format(popupContentFormat, generatedPassword);
                Debug.Log($"[PasswordBookTrigger] ReadableNote 이벤트에 연결됨, 내용 업데이트: {generatedPassword}");
            }

            // 비밀번호 상자에 비밀번호 설정
            if (passwordChest != null)
            {
                passwordChest.correctPassword = generatedPassword;
                Debug.Log($"[PasswordBookTrigger] 비밀번호 상자에 비밀번호 설정: {generatedPassword}");
            }

            // 비밀번호 패널에 비밀번호 설정 (자동 검색)
            if (passwordPanel == null)
            {
                passwordPanel = FindObjectOfType<PasswordPanel>();
            }
            if (passwordPanel != null)
            {
                passwordPanel.SetPassword(generatedPassword);
                Debug.Log($"[PasswordBookTrigger] 비밀번호 패널에 비밀번호 설정: {generatedPassword}");
            }
        }

        /// <summary>
        /// 랜덤 비밀번호 생성
        /// </summary>
        private void GeneratePassword()
        {
            generatedPassword = "";
            for (int i = 0; i < passwordLength; i++)
            {
                generatedPassword += Random.Range(0, 10).ToString();
            }
            Debug.Log($"[PasswordBookTrigger] 비밀번호 생성됨: {generatedPassword}");
        }

        /// <summary>
        /// 책 이벤트 트리거
        /// </summary>
        public void TriggerBookEvent()
        {
            if (hasBeenTriggered) return;
            hasBeenTriggered = true;

            Debug.Log("[PasswordBookTrigger] 책 열림 - 비밀번호 표시 시작");

            // 사운드 재생
            if (bookOpenSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(bookOpenSound);
            }

            OnBookOpened?.Invoke();

            // GamePopupUI로 팝업 표시
            string content = string.Format(popupContentFormat, generatedPassword);
            if (GamePopupUI.Instance != null)
            {
                // 자동 닫기 설정
                GamePopupUI.Instance.autoClose = true;
                GamePopupUI.Instance.autoCloseTime = popupDisplayTime;
                GamePopupUI.Instance.ShowPopup(popupTitle, content, OnPopupClosedCallback);
                Debug.Log($"[PasswordBookTrigger] GamePopupUI로 팝업 표시: {popupTitle}");
            }
            else
            {
                Debug.LogWarning("[PasswordBookTrigger] GamePopupUI.Instance가 없습니다!");
                // GamePopupUI가 없으면 바로 이벤트 트리거
                StartCoroutine(WaitAndTriggerEvents());
            }
        }

        /// <summary>
        /// 팝업 닫힘 콜백
        /// </summary>
        private void OnPopupClosedCallback()
        {
            Debug.Log("[PasswordBookTrigger] 팝업 닫힘 콜백 호출됨");
            StartCoroutine(TriggerEventsAfterPopup());
        }

        /// <summary>
        /// 팝업 닫힌 후 이벤트 트리거 (콜백용)
        /// </summary>
        private IEnumerator TriggerEventsAfterPopup()
        {
            Debug.Log("[PasswordBookTrigger] 팝업 닫힘 - 이벤트 트리거");

            // 경고음 재생
            if (warningSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(warningSound);
            }

            OnPopupClosed?.Invoke();

            // 조명 켜기
            yield return new WaitForSeconds(0.3f);
            TurnOnLights();

            // 살인마 활성화
            yield return new WaitForSeconds(killerActivationDelay);
            ActivateKiller();
        }

        /// <summary>
        /// 팝업 닫힌 후 이벤트 트리거 (GamePopupUI 없을 때 폴백)
        /// </summary>
        private IEnumerator WaitAndTriggerEvents()
        {
            // 팝업 표시 시간 대기
            yield return new WaitForSeconds(popupDisplayTime);

            // 경고음 재생
            if (warningSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(warningSound);
            }

            OnPopupClosed?.Invoke();

            // 조명 켜기
            yield return new WaitForSeconds(0.3f);
            TurnOnLights();

            // 살인마 활성화
            yield return new WaitForSeconds(killerActivationDelay);
            ActivateKiller();
        }

        /// <summary>
        /// 살인마 활성화
        /// </summary>
        private void ActivateKiller()
        {
            if (killerObject == null)
            {
                // 씬에서 Killer 찾기
                killerObject = GameObject.Find("Killer");
            }

            if (killerObject != null)
            {
                killerObject.SetActive(true);

                // KillerAI 활성화 및 추적 시작
                var killerAI = killerObject.GetComponent<KillerAI>();
                if (killerAI != null)
                {
                    killerAI.enabled = true;
                    killerAI.StartChasing(); // 즉시 추적 시작
                    Debug.Log("[PasswordBookTrigger] 살인마 활성화 및 추적 시작!");
                }
                else
                {
                    Debug.LogWarning("[PasswordBookTrigger] KillerAI 컴포넌트를 찾을 수 없습니다.");
                }

                OnKillerActivated?.Invoke();
            }
            else
            {
                Debug.LogWarning("[PasswordBookTrigger] Killer 오브젝트를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 조명 켜기
        /// </summary>
        private void TurnOnLights()
        {
            if (lightingParent == null)
            {
                // 씬에서 Lighting - Room3 찾기 (자식 Light가 있는 것)
                var allLightingObjects = GameObject.FindObjectsOfType<Transform>();
                foreach (var t in allLightingObjects)
                {
                    if (t.name == "Lighting - Room3" && t.GetComponentInChildren<Light>(true) != null)
                    {
                        lightingParent = t;
                        break;
                    }
                }
            }

            if (lightingParent != null)
            {
                // LightingEventTrigger가 있으면 사용 (깜빡임 효과 등)
                var lightingTrigger = lightingParent.GetComponent<LightingEventTrigger>();
                if (lightingTrigger != null)
                {
                    lightingTrigger.TriggerLights();
                    Debug.Log("[PasswordBookTrigger] LightingEventTrigger.TriggerLights() 호출");
                }
                else
                {
                    // LightingEventTrigger가 없으면 직접 켜기
                    SetLights(true);
                }

                // 사운드 (LightingEventTrigger가 없을 때만)
                if (lightingTrigger == null && lightOnSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(lightOnSound);
                }

                Debug.Log("[PasswordBookTrigger] Lighting-Room3 조명 켜짐!");
                OnLightsOn?.Invoke();
            }
            else
            {
                Debug.LogWarning("[PasswordBookTrigger] Lighting-Room3를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// 조명 켜기/끄기
        /// </summary>
        private void SetLights(bool on)
        {
            if (lightingParent == null) return;

            // 자식의 모든 Light 컴포넌트 찾기
            var lights = lightingParent.GetComponentsInChildren<Light>(true);
            foreach (var light in lights)
            {
                light.enabled = on;
            }

            Debug.Log($"[PasswordBookTrigger] {lights.Length}개 조명 {(on ? "켜짐" : "꺼짐")}");
        }

        /// <summary>
        /// 수동으로 트리거 (테스트용)
        /// </summary>
        [ContextMenu("Trigger Book Event")]
        public void TriggerManually()
        {
            hasBeenTriggered = false; // 테스트용 리셋
            TriggerBookEvent();
        }

        /// <summary>
        /// 비밀번호 재생성 (테스트용)
        /// </summary>
        [ContextMenu("Regenerate Password")]
        public void RegeneratePassword()
        {
            GeneratePassword();
            if (passwordChest != null)
            {
                passwordChest.correctPassword = generatedPassword;
            }
        }
    }
}
