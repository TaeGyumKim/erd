using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 방 기반 게임 진행 관리자
    /// 기획서 기반 9단계 진행 시스템
    ///
    /// 진행 순서:
    /// (1) Room1: 시작 → 포크(열쇠) 획득 → Room1 문 열기
    /// (2) Room2: 복도 탐험 → 배럴 위 열쇠 획득 → Room3 문 열기
    /// (3) Room3: 책 확인 → 스토리 팝업
    /// (4) Room3: 불 켜짐 → 창문 뒤 살인마 등장
    /// (5) Room2: 유령 뛰어감 → 숨기 장소로 이동
    /// (6) Room2: Room4에서 유령 나옴 → 살인마 순찰
    /// (7) Room4: 상자 열기 → 오브젝트 획득
    /// (8) Room5: 탈출문 열기 → 탈출
    /// (9) 탈출 성공 팝업
    /// </summary>
    public class RoomProgressManager : MonoBehaviour
    {
        public static RoomProgressManager Instance { get; private set; }

        [Header("Current Progress")]
        [SerializeField] private GameStage currentStage = GameStage.Room1_Start;
        public GameStage CurrentStage => currentStage;

        [Header("Room References")]
        [Tooltip("Room1 - 시작 방")]
        public Transform room1;
        [Tooltip("Room2 - 복도")]
        public Transform room2;
        [Tooltip("Room3 - 책이 있는 방")]
        public Transform room3;
        [Tooltip("Room4 - 상자가 있는 방")]
        public Transform room4;
        [Tooltip("Room5 - 탈출구")]
        public Transform room5;

        [Header("Key Items")]
        [Tooltip("Room1 포크 열쇠")]
        public GameObject forkKeyRoom1;
        [Tooltip("Room2 열쇠 (배럴 위)")]
        public GameObject keyRoom2;
        [Tooltip("Room3 책")]
        public GameObject bookRoom3;
        [Tooltip("Room4 상자")]
        public GameObject chestRoom4;
        [Tooltip("Room5 탈출문 (Panel_Wood)")]
        public GameObject exitDoorRoom5;

        [Header("Doors")]
        public Door doorRoom1;
        public Door doorRoom3;
        public Door doorRoom4;

        [Header("Lighting - Room3")]
        [Tooltip("Room3의 라이트들 (책 확인 후 켜짐)")]
        public Light[] room3Lights;

        [Header("Characters")]
        public KillerAI killer;
        public Transform killerSpawnPoint;
        public Transform killerWindowPosition; // 창문 뒤 위치

        [Tooltip("유령들")]
        public GhostController ghost1; // Room3 창문
        public GhostController ghost2; // Room2 복도
        public GhostController ghost3; // Room4 문

        [Header("Hiding")]
        public HidingSpot hidingSpotRoom2;
        [Tooltip("숨기 제한 시간")]
        public float hideTimeLimit = 10f;

        [Header("Password System")]
        [Tooltip("Room4 상자 비밀번호")]
        public string chestPassword = "1234";
        [Tooltip("상자 안의 오브젝트 (탈출 열쇠)")]
        public GameObject chestItem;

        [Header("Audio")]
        public AudioClip horrorBGM;
        public AudioClip killerScreamSound;
        public AudioClip wheelchairFallSound;
        public AudioClip ghostRunSound;
        public AudioClip escapeMusic;

        [Header("Timing")]
        [Tooltip("책 팝업 후 불 켜지기까지 딜레이")]
        public float lightOnDelay = 2f;
        [Tooltip("살인마 등장 후 숨기까지 제한 시간")]
        public float hideTimeLimitAfterKiller = 10f;
        [Tooltip("브금 페이드아웃 시간")]
        public float bgmFadeOutDuration = 5f;

        [Header("Events")]
        public UnityEvent OnGameStart;
        public UnityEvent<GameStage> OnStageChanged;
        public UnityEvent<string> OnShowPopup;
        public UnityEvent OnKillerAppear;
        public UnityEvent OnHideSuccess;
        public UnityEvent OnHideFail;
        public UnityEvent OnEscapeSuccess;
        public UnityEvent OnGameOver;

        private AudioSource audioSource;
        private bool isHiding = false;
        private float hideTimer = 0f;
        private bool bgmPlaying = false;

        public enum GameStage
        {
            Room1_Start,           // (1) 침대에서 시작
            Room1_GetForkKey,      // (1) 포크 열쇠 획득
            Room1_OpenDoor,        // (1) Room1 문 열기
            Room2_Explore,         // (2) 복도 탐험
            Room2_GetKey,          // (2) 열쇠 획득
            Room3_Enter,           // (2) Room3 진입
            Room3_ReadBook,        // (3) 책 확인
            Room3_KillerAppear,    // (4) 살인마 창문 등장
            Room2_RunToHide,       // (5) 숨기 장소로 이동
            Room2_Hiding,          // (6) 숨어있는 상태
            Room2_SafeToMove,      // (6) 안전하게 이동 가능
            Room4_Enter,           // (7) Room4 진입
            Room4_OpenChest,       // (7) 상자 열기
            Room5_Escape,          // (8) 탈출
            GameClear              // (9) 게임 클리어
        }

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
            InitializeGame();
        }

        /// <summary>
        /// 게임 초기화
        /// </summary>
        private void InitializeGame()
        {
            // 살인마 비활성화
            if (killer != null)
            {
                killer.gameObject.SetActive(false);
            }

            // Room3 라이트 끄기
            SetRoom3Lights(false);

            // 유령들 비활성화
            if (ghost1 != null) ghost1.gameObject.SetActive(false);
            if (ghost2 != null) ghost2.gameObject.SetActive(false);
            if (ghost3 != null) ghost3.gameObject.SetActive(false);

            // Room4 문 열린 상태로 (열쇠 없음)
            if (doorRoom4 != null)
            {
                doorRoom4.isLocked = false;
                doorRoom4.OpenDoor();
            }

            SetStage(GameStage.Room1_Start);
            OnGameStart?.Invoke();

            Debug.Log("[RoomProgress] 게임 시작 - Room1에서 시작");
        }

        /// <summary>
        /// 스테이지 변경
        /// </summary>
        public void SetStage(GameStage newStage)
        {
            if (currentStage == newStage) return;

            GameStage previousStage = currentStage;
            currentStage = newStage;

            Debug.Log($"[RoomProgress] 스테이지 변경: {previousStage} → {newStage}");
            OnStageChanged?.Invoke(newStage);

            // 스테이지별 처리
            HandleStageChange(newStage);
        }

        /// <summary>
        /// 스테이지별 처리
        /// </summary>
        private void HandleStageChange(GameStage stage)
        {
            switch (stage)
            {
                case GameStage.Room1_Start:
                    ShowPopup("어두운 방에서 눈을 떴다...\n주변을 둘러보자.");
                    break;

                case GameStage.Room1_GetForkKey:
                    ShowPopup("포크를 획득했다.\n문을 열 수 있을 것 같다.");
                    break;

                case GameStage.Room2_Explore:
                    ShowPopup("복도로 나왔다.\n열쇠를 찾아보자.");
                    break;

                case GameStage.Room2_GetKey:
                    ShowPopup("열쇠를 획득했다!\nRoom3의 문을 열 수 있다.");
                    break;

                case GameStage.Room3_ReadBook:
                    // 책 팝업은 InteractableBook에서 처리
                    StartCoroutine(Room3BookSequence());
                    break;

                case GameStage.Room3_KillerAppear:
                    StartCoroutine(KillerAppearSequence());
                    break;

                case GameStage.Room2_RunToHide:
                    StartCoroutine(RunToHideSequence());
                    break;

                case GameStage.Room2_Hiding:
                    StartCoroutine(HidingSequence());
                    break;

                case GameStage.Room2_SafeToMove:
                    ShowPopup("살인마가 떠났다...\n조심히 이동하자.");
                    break;

                case GameStage.Room4_OpenChest:
                    ShowPopup("상자를 열었다!\n탈출 열쇠를 획득했다.");
                    break;

                case GameStage.Room5_Escape:
                    StartCoroutine(EscapeSequence());
                    break;

                case GameStage.GameClear:
                    StartCoroutine(GameClearSequence());
                    break;
            }
        }

        #region Stage Sequences

        /// <summary>
        /// Room3 책 읽기 시퀀스
        /// </summary>
        private IEnumerator Room3BookSequence()
        {
            // 책 팝업 표시 (InteractableBook에서 호출됨)
            yield return new WaitForSeconds(lightOnDelay);

            // 팝업 닫힌 후 불 켜짐
            SetRoom3Lights(true);

            // 살인마 등장으로 전환
            SetStage(GameStage.Room3_KillerAppear);
        }

        /// <summary>
        /// 살인마 등장 시퀀스
        /// </summary>
        private IEnumerator KillerAppearSequence()
        {
            // 휠체어 넘어지는 소리 (선택)
            if (wheelchairFallSound != null)
            {
                audioSource.PlayOneShot(wheelchairFallSound);
            }

            yield return new WaitForSeconds(0.5f);

            // 창문 뒤에 살인마 등장
            if (killer != null && killerWindowPosition != null)
            {
                killer.transform.position = killerWindowPosition.position;
                killer.transform.LookAt(room3.position);
                killer.gameObject.SetActive(true);
            }

            // 살인마 고함
            if (killerScreamSound != null)
            {
                audioSource.PlayOneShot(killerScreamSound);
            }

            // 호러 BGM 시작
            StartHorrorBGM();

            OnKillerAppear?.Invoke();
            ShowPopup("!!!\n빨리 숨어야 한다!");

            yield return new WaitForSeconds(1f);

            // 숨기 단계로 전환
            SetStage(GameStage.Room2_RunToHide);
        }

        /// <summary>
        /// 숨으러 뛰어가는 시퀀스
        /// </summary>
        private IEnumerator RunToHideSequence()
        {
            // ghost2 - 복도에서 뛰어감
            if (ghost2 != null)
            {
                ghost2.gameObject.SetActive(true);
                ghost2.RunAway();
            }

            // 숨기 제한 시간 시작
            hideTimer = hideTimeLimitAfterKiller;

            while (hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;

                // 숨기 장소에 들어갔는지 체크
                if (IsPlayerHiding())
                {
                    SetStage(GameStage.Room2_Hiding);
                    yield break;
                }

                yield return null;
            }

            // 시간 내에 숨지 못함 - 게임오버
            OnHideFail?.Invoke();
            TriggerGameOver("살인마에게 잡혔다!");
        }

        /// <summary>
        /// 숨어있는 시퀀스
        /// </summary>
        private IEnumerator HidingSequence()
        {
            OnHideSuccess?.Invoke();

            // ghost3 - Room4에서 문 열고 나옴
            yield return new WaitForSeconds(1f);
            if (ghost3 != null)
            {
                ghost3.gameObject.SetActive(true);
                ghost3.OpenDoorAndAppear();
            }

            // 살인마 Room2 순찰
            if (killer != null)
            {
                killer.PatrolRoom(room2);
            }

            // BGM 페이드아웃
            yield return StartCoroutine(FadeOutBGM(bgmFadeOutDuration));

            // 안전하게 이동 가능
            SetStage(GameStage.Room2_SafeToMove);
        }

        /// <summary>
        /// 탈출 시퀀스
        /// </summary>
        private IEnumerator EscapeSequence()
        {
            // 탈출문이 천천히 위로 열림
            if (exitDoorRoom5 != null)
            {
                var slideDoor = exitDoorRoom5.GetComponent<SlidingDoor>();
                if (slideDoor != null)
                {
                    slideDoor.OpenSlowly();
                }
            }

            // 유령 뛰어오는 소리
            if (ghostRunSound != null)
            {
                audioSource.PlayOneShot(ghostRunSound);
            }

            yield return new WaitForSeconds(3f);

            SetStage(GameStage.GameClear);
        }

        /// <summary>
        /// 게임 클리어 시퀀스
        /// </summary>
        private IEnumerator GameClearSequence()
        {
            // 탈출 음악
            if (escapeMusic != null)
            {
                audioSource.clip = escapeMusic;
                audioSource.loop = false;
                audioSource.Play();
            }

            ShowPopup("탈출 성공!\n\n축하합니다!");
            OnEscapeSuccess?.Invoke();

            yield return new WaitForSeconds(5f);

            // 게임 종료 처리
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.Victory();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Room3 라이트 설정
        /// </summary>
        public void SetRoom3Lights(bool on)
        {
            if (room3Lights == null) return;

            foreach (var light in room3Lights)
            {
                if (light != null)
                {
                    light.enabled = on;
                }
            }

            Debug.Log($"[RoomProgress] Room3 라이트 {(on ? "켜짐" : "꺼짐")}");
        }

        /// <summary>
        /// 플레이어가 숨어있는지 체크
        /// </summary>
        private bool IsPlayerHiding()
        {
            // VRPlayer 체크
            if (VRPlayer.Instance != null && VRPlayer.Instance.IsHiding)
            {
                return true;
            }

            // PCPlayerController 체크
            var pcPlayer = FindFirstObjectByType<PCPlayerController>();
            if (pcPlayer != null && pcPlayer.IsHiding)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 호러 BGM 시작
        /// </summary>
        private void StartHorrorBGM()
        {
            if (horrorBGM != null && !bgmPlaying)
            {
                audioSource.clip = horrorBGM;
                audioSource.loop = true;
                audioSource.Play();
                bgmPlaying = true;
            }
        }

        /// <summary>
        /// BGM 페이드아웃
        /// </summary>
        private IEnumerator FadeOutBGM(float duration)
        {
            if (!bgmPlaying) yield break;

            float startVolume = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = startVolume;
            bgmPlaying = false;
        }

        /// <summary>
        /// 팝업 표시
        /// </summary>
        public void ShowPopup(string message)
        {
            OnShowPopup?.Invoke(message);

            // VRHUD 연동
            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus(message, 5f);
            }

            Debug.Log($"[RoomProgress] 팝업: {message}");
        }

        /// <summary>
        /// 게임오버
        /// </summary>
        public void TriggerGameOver(string reason)
        {
            OnGameOver?.Invoke();

            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.GameOver(reason);
            }

            Debug.Log($"[RoomProgress] 게임오버: {reason}");
        }

        #endregion

        #region Public Methods for External Calls

        /// <summary>
        /// 아이템 획득 처리
        /// </summary>
        public void OnItemCollected(string itemId)
        {
            switch (itemId)
            {
                case "fork_key_room1":
                    if (currentStage == GameStage.Room1_Start)
                    {
                        SetStage(GameStage.Room1_GetForkKey);
                    }
                    break;

                case "key_room2":
                    if (currentStage == GameStage.Room2_Explore)
                    {
                        SetStage(GameStage.Room2_GetKey);
                    }
                    break;

                case "chest_item":
                    if (currentStage == GameStage.Room4_Enter)
                    {
                        SetStage(GameStage.Room4_OpenChest);
                    }
                    break;
            }
        }

        /// <summary>
        /// 문 열림 처리
        /// </summary>
        public void OnDoorOpened(string doorId)
        {
            switch (doorId)
            {
                case "door_room1":
                    if (currentStage == GameStage.Room1_GetForkKey)
                    {
                        SetStage(GameStage.Room2_Explore);
                    }
                    break;

                case "door_room3":
                    if (currentStage == GameStage.Room2_GetKey)
                    {
                        SetStage(GameStage.Room3_Enter);
                    }
                    break;
            }
        }

        /// <summary>
        /// 책 읽기 완료
        /// </summary>
        public void OnBookRead()
        {
            if (currentStage == GameStage.Room3_Enter)
            {
                SetStage(GameStage.Room3_ReadBook);
            }
        }

        /// <summary>
        /// Room4 진입
        /// </summary>
        public void OnEnterRoom4()
        {
            if (currentStage == GameStage.Room2_SafeToMove)
            {
                SetStage(GameStage.Room4_Enter);
            }
        }

        /// <summary>
        /// 상자 비밀번호 확인
        /// </summary>
        public bool CheckChestPassword(string input)
        {
            return input == chestPassword;
        }

        /// <summary>
        /// 탈출문 사용
        /// </summary>
        public void OnUseExitDoor()
        {
            if (currentStage == GameStage.Room4_OpenChest)
            {
                SetStage(GameStage.Room5_Escape);
            }
        }

        /// <summary>
        /// 숨기 장소 진입
        /// </summary>
        public void OnEnterHidingSpot()
        {
            if (currentStage == GameStage.Room2_RunToHide)
            {
                SetStage(GameStage.Room2_Hiding);
            }
        }

        /// <summary>
        /// 숨기 장소에서 나옴
        /// </summary>
        public void OnExitHidingSpot()
        {
            if (currentStage == GameStage.Room2_Hiding)
            {
                // BGM이 아직 재생 중이면 잡힘
                if (bgmPlaying)
                {
                    TriggerGameOver("너무 일찍 나왔다!");
                }
            }
        }

        #endregion
    }
}
