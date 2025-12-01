using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 스토리 진행 관리자
    /// 도입 → 중반 → 최종 스토리 진행 관리
    ///
    /// "Run 'Till the End" 게임 스토리:
    /// - 플레이어가 저택에서 깨어남
    /// - 단서 아이템을 찾아 탈출구 열쇠 획득
    /// - 살인마와 유령을 피해 탈출
    /// </summary>
    public class StoryProgressManager : MonoBehaviour
    {
        public static StoryProgressManager Instance { get; private set; }

        [Header("Story State")]
        public StoryPhase currentPhase = StoryPhase.Introduction;

        [Header("Clue Items Required")]
        [Tooltip("USB 획득 여부")]
        public bool hasUSB = false;

        [Tooltip("라이터 획득 여부")]
        public bool hasLighter = false;

        [Tooltip("보안카드 획득 여부")]
        public bool hasSecurityCard = false;

        [Tooltip("배터리 획득 여부")]
        public bool hasBattery = false;

        [Tooltip("기어 획득 여부")]
        public bool hasGear = false;

        [Header("Story Progress")]
        [Tooltip("USB 메시지 확인")]
        public bool usbMessageRead = false;

        [Tooltip("벽 문양 발견")]
        public bool wallSymbolFound = false;

        [Tooltip("유령 메시지 수신")]
        public bool ghostMessageReceived = false;

        [Header("Final Key")]
        [Tooltip("탈출 열쇠 획득 가능")]
        public bool canObtainFinalKey = false;

        [Tooltip("탈출 열쇠 획득")]
        public bool hasFinalKey = false;

        [Header("UI References")]
        public GameObject introMessageUI;
        public GameObject missionUI;

        [Header("Audio")]
        public AudioClip introAmbience;
        public AudioClip tensionMusic;
        public AudioClip chaseMusic;
        public AudioClip victoryMusic;

        [Header("Timing")]
        [Tooltip("도입부 메시지 표시 시간")]
        public float introMessageDuration = 5f;

        [Tooltip("킬러 등장까지 대기 시간")]
        public float killerAppearDelay = 30f;

        [Header("Events")]
        public UnityEvent OnIntroStart;
        public UnityEvent OnIntroEnd;
        public UnityEvent OnKillerAppear;
        public UnityEvent OnClueFound;
        public UnityEvent OnFinalKeyObtained;
        public UnityEvent OnEscapeStart;
        public UnityEvent OnEscapeSuccess;
        public UnityEvent<string> OnStoryMessage;

        public enum StoryPhase
        {
            Introduction,   // 도입: 방에서 깨어남
            Exploration,    // 탐색: 단서 찾기
            ClueCollection, // 단서 수집: USB, 라이터, 보안카드 등
            FinalPuzzle,    // 최종 퍼즐: 단서 조합
            Escape          // 탈출: 열쇠로 문 열고 탈출
        }

        private AudioSource audioSource;
        private bool killerActivated = false;

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
            }
        }

        private void Start()
        {
            StartCoroutine(StartIntroduction());
        }

        /// <summary>
        /// 도입부 시작
        /// </summary>
        private IEnumerator StartIntroduction()
        {
            currentPhase = StoryPhase.Introduction;
            OnIntroStart?.Invoke();

            // 배경 음악
            if (introAmbience != null)
            {
                audioSource.clip = introAmbience;
                audioSource.loop = true;
                audioSource.Play();
            }

            // 도입 메시지 표시
            ShowStoryMessage("어두운 방에서 눈을 뜬다...\n\n이곳에서 빠져나가야 한다.");

            yield return new WaitForSeconds(introMessageDuration);

            // 미션 시작
            StartExploration();
        }

        /// <summary>
        /// 탐색 단계 시작
        /// </summary>
        public void StartExploration()
        {
            currentPhase = StoryPhase.Exploration;
            OnIntroEnd?.Invoke();

            // 목표 설정
            if (ObjectiveSystem.Instance != null)
            {
                ObjectiveSystem.Instance.AddObjective("find_usb", "USB 찾기", "컴퓨터에서 정보를 얻을 USB를 찾으세요");
                ObjectiveSystem.Instance.AddObjective("find_lighter", "라이터 찾기", "어둠 속 숨겨진 것을 밝힐 라이터를 찾으세요");
                ObjectiveSystem.Instance.AddObjective("find_card", "보안카드 찾기", "잠긴 문을 열 보안카드를 찾으세요");
                ObjectiveSystem.Instance.AddObjective("escape", "탈출하기", "모든 단서를 모아 탈출구를 찾으세요");
            }

            // 킬러 등장 예약
            StartCoroutine(ActivateKillerAfterDelay());

            ShowStoryMessage("단서를 찾아 이곳에서 탈출해야 한다...");
        }

        /// <summary>
        /// 킬러 등장
        /// </summary>
        private IEnumerator ActivateKillerAfterDelay()
        {
            yield return new WaitForSeconds(killerAppearDelay);

            if (!killerActivated)
            {
                ActivateKiller();
            }
        }

        /// <summary>
        /// 킬러 활성화
        /// </summary>
        public void ActivateKiller()
        {
            if (killerActivated) return;

            killerActivated = true;
            OnKillerAppear?.Invoke();

            // 긴장감 음악으로 전환
            if (tensionMusic != null)
            {
                audioSource.clip = tensionMusic;
                audioSource.Play();
            }

            ShowStoryMessage("발소리가 들린다... 누군가 들어왔다!");

            // 킬러 AI 활성화
            var killers = FindObjectsOfType<KillerAI>();
            foreach (var killer in killers)
            {
                killer.gameObject.SetActive(true);
            }

            Debug.Log("[StoryProgress] 살인마 등장!");
        }

        /// <summary>
        /// 단서 아이템 획득
        /// </summary>
        public void CollectClueItem(ClueItemType itemType)
        {
            currentPhase = StoryPhase.ClueCollection;

            switch (itemType)
            {
                case ClueItemType.USB:
                    hasUSB = true;
                    ObjectiveSystem.Instance?.CompleteObjective("find_usb");
                    ShowStoryMessage("USB를 획득했다. 컴퓨터에서 확인해보자.");
                    break;

                case ClueItemType.Lighter:
                    hasLighter = true;
                    ObjectiveSystem.Instance?.CompleteObjective("find_lighter");
                    ShowStoryMessage("라이터를 획득했다. 어두운 곳을 비춰보자.");
                    break;

                case ClueItemType.SecurityCard:
                    hasSecurityCard = true;
                    ObjectiveSystem.Instance?.CompleteObjective("find_card");
                    ShowStoryMessage("보안카드를 획득했다. 잠긴 문을 열 수 있다.");
                    break;

                case ClueItemType.Battery:
                    hasBattery = true;
                    ShowStoryMessage("배터리를 획득했다.");
                    break;

                case ClueItemType.Gear:
                    hasGear = true;
                    ShowStoryMessage("기어를 획득했다. 기계 장치에 사용할 수 있을 것 같다.");
                    break;
            }

            OnClueFound?.Invoke();
            CheckFinalKeyCondition();
        }

        /// <summary>
        /// USB 메시지 확인
        /// </summary>
        public void ReadUSBMessage()
        {
            if (!hasUSB) return;

            usbMessageRead = true;
            ShowStoryMessage("USB 기록:\n\"그를 멈춰야 한다... 열쇠는 벽의 문양 뒤에...\"");

            // 유령 메시지 트리거
            TriggerGhostMessage();
            CheckFinalKeyCondition();
        }

        /// <summary>
        /// 라이터로 벽 문양 발견
        /// </summary>
        public void DiscoverWallSymbol()
        {
            if (!hasLighter) return;

            wallSymbolFound = true;
            ShowStoryMessage("벽에 숨겨진 문양이 드러났다!\n탈출구의 위치가 표시되어 있다...");
            CheckFinalKeyCondition();
        }

        /// <summary>
        /// 유령 메시지 트리거
        /// </summary>
        public void TriggerGhostMessage()
        {
            if (ghostMessageReceived) return;

            ghostMessageReceived = true;
            ShowStoryMessage("\"도와줘... 그를 멈춰줘...\"\n\n유령의 속삭임이 들린다...");

            // 유령 AI에게 알림
            var ghosts = FindObjectsOfType<GhostAI>();
            foreach (var ghost in ghosts)
            {
                ghost.StartHelping();
            }
        }

        /// <summary>
        /// 최종 열쇠 조건 확인
        /// </summary>
        private void CheckFinalKeyCondition()
        {
            // 모든 단서를 모으면 최종 열쇠 획득 가능
            if (hasUSB && hasLighter && hasSecurityCard && usbMessageRead && wallSymbolFound)
            {
                canObtainFinalKey = true;
                currentPhase = StoryPhase.FinalPuzzle;

                ShowStoryMessage("모든 단서를 모았다!\n탈출구의 열쇠를 찾을 수 있다.");

                if (ObjectiveSystem.Instance != null)
                {
                    ObjectiveSystem.Instance.AddObjective("find_final_key", "탈출 열쇠 찾기", "단서가 가리키는 곳에서 열쇠를 찾으세요");
                }
            }
        }

        /// <summary>
        /// 최종 열쇠 획득
        /// </summary>
        public void ObtainFinalKey()
        {
            if (!canObtainFinalKey) return;

            hasFinalKey = true;
            currentPhase = StoryPhase.Escape;

            ObjectiveSystem.Instance?.CompleteObjective("find_final_key");
            OnFinalKeyObtained?.Invoke();

            ShowStoryMessage("탈출 열쇠를 획득했다!\n출구로 달려가자!");
        }

        /// <summary>
        /// 탈출 시도
        /// </summary>
        public void TryEscape()
        {
            if (!hasFinalKey)
            {
                ShowStoryMessage("열쇠가 필요하다...");
                return;
            }

            OnEscapeStart?.Invoke();
            StartCoroutine(EscapeSequence());
        }

        /// <summary>
        /// 탈출 시퀀스
        /// </summary>
        private IEnumerator EscapeSequence()
        {
            // 승리 음악
            if (victoryMusic != null)
            {
                audioSource.clip = victoryMusic;
                audioSource.loop = false;
                audioSource.Play();
            }

            ShowStoryMessage("문이 열린다...\n\n\"이제 자유다.\"");

            yield return new WaitForSeconds(3f);

            OnEscapeSuccess?.Invoke();

            // 게임 매니저에 승리 알림
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.Victory();
            }
        }

        /// <summary>
        /// 스토리 메시지 표시
        /// </summary>
        public void ShowStoryMessage(string message)
        {
            OnStoryMessage?.Invoke(message);

            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus(message, 5f);
            }

            Debug.Log($"[Story] {message}");
        }

        /// <summary>
        /// 간단 모드: 필수 아이템만 필요
        /// </summary>
        public void SetSimpleMode()
        {
            // USB, 라이터, 보안카드만 필요
            usbMessageRead = true;
            wallSymbolFound = true;
            ghostMessageReceived = true;
        }
    }

    /// <summary>
    /// 단서 아이템 종류
    /// </summary>
    public enum ClueItemType
    {
        USB,
        Lighter,
        SecurityCard,
        Battery,
        Gear,
        FinalKey
    }
}
