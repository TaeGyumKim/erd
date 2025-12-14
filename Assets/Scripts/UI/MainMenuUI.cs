using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 메인 메뉴 UI
    /// 게임 시작, 설정, 종료 등 메인 메뉴 기능
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("Main Menu Buttons")]
        [Tooltip("게임 시작 버튼")]
        public Button startButton;

        [Tooltip("계속하기 버튼")]
        public Button continueButton;

        [Tooltip("설정 버튼")]
        public Button settingsButton;

        [Tooltip("종료 버튼")]
        public Button quitButton;

        [Header("Panels")]
        [Tooltip("메인 메뉴 패널")]
        public GameObject mainMenuPanel;

        [Tooltip("설정 패널")]
        public GameObject settingsPanel;

        [Tooltip("난이도 선택 패널")]
        public GameObject difficultyPanel;

        [Header("Title")]
        [Tooltip("게임 타이틀 텍스트")]
        public TextMeshProUGUI titleText;

        [Tooltip("서브 타이틀")]
        public TextMeshProUGUI subtitleText;

        [Header("Difficulty Selection")]
        [Tooltip("쉬움 버튼")]
        public Button easyButton;

        [Tooltip("보통 버튼")]
        public Button normalButton;

        [Tooltip("어려움 버튼")]
        public Button hardButton;

        [Tooltip("뒤로가기 버튼")]
        public Button backButton;

        [Header("Settings")]
        [Tooltip("마스터 볼륨 슬라이더")]
        public Slider masterVolumeSlider;

        [Tooltip("BGM 볼륨 슬라이더")]
        public Slider bgmVolumeSlider;

        [Tooltip("SFX 볼륨 슬라이더")]
        public Slider sfxVolumeSlider;

        [Tooltip("VR 모드 토글")]
        public Toggle vrModeToggle;

        [Tooltip("튜토리얼 토글")]
        public Toggle tutorialToggle;

        [Tooltip("설정 적용 버튼")]
        public Button applySettingsButton;

        [Tooltip("설정 뒤로가기 버튼")]
        public Button settingsBackButton;

        [Header("Scene Names")]
        [Tooltip("게임 씬 이름")]
        public string gameSceneName = "SampleScene";

        [Header("Audio")]
        public AudioClip menuMusic;
        public AudioClip buttonClickSound;
        public AudioClip buttonHoverSound;

        [Header("Visual Effects")]
        [Tooltip("배경 이미지")]
        public Image backgroundImage;

        [Tooltip("페이드 이미지")]
        public Image fadeImage;

        [Tooltip("페이드 시간")]
        public float fadeDuration = 1f;

        private AudioSource audioSource;
        private GameDifficulty selectedDifficulty = GameDifficulty.Normal;

        public enum GameDifficulty
        {
            Easy,
            Normal,
            Hard
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            // 버튼 이벤트 연결
            SetupButtons();

            // 패널 초기화
            ShowMainMenu();

            // 배경 음악 재생
            PlayMenuMusic();

            // 페이드 인
            if (fadeImage != null)
            {
                StartCoroutine(FadeIn());
            }

            // 저장된 설정 로드
            LoadSettings();

            // 계속하기 버튼 상태
            UpdateContinueButton();
        }

        private void SetupButtons()
        {
            // 메인 메뉴 버튼
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
                AddButtonSounds(startButton);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                AddButtonSounds(continueButton);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
                AddButtonSounds(settingsButton);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
                AddButtonSounds(quitButton);
            }

            // 난이도 버튼
            if (easyButton != null)
            {
                easyButton.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Easy));
                AddButtonSounds(easyButton);
            }

            if (normalButton != null)
            {
                normalButton.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Normal));
                AddButtonSounds(normalButton);
            }

            if (hardButton != null)
            {
                hardButton.onClick.AddListener(() => SelectDifficulty(GameDifficulty.Hard));
                AddButtonSounds(hardButton);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
                AddButtonSounds(backButton);
            }

            // 설정 버튼
            if (applySettingsButton != null)
            {
                applySettingsButton.onClick.AddListener(ApplySettings);
                AddButtonSounds(applySettingsButton);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
                AddButtonSounds(settingsBackButton);
            }

            // 슬라이더 이벤트
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
        }

        private void AddButtonSounds(Button button)
        {
            // 호버 이벤트는 EventTrigger로 추가해야 하지만 간단히 클릭 사운드만 추가
        }

        private void PlayButtonSound()
        {
            if (buttonClickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }

        #region Panel Navigation

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (difficultyPanel != null) difficultyPanel.SetActive(false);
        }

        public void ShowDifficultyPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (difficultyPanel != null) difficultyPanel.SetActive(true);
        }

        public void ShowSettingsPanel()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        #endregion

        #region Button Handlers

        private void OnStartClicked()
        {
            PlayButtonSound();
            ShowDifficultyPanel();
        }

        private void OnContinueClicked()
        {
            PlayButtonSound();
            // 저장된 게임 로드 (체크포인트 시스템과 연동)
            StartGame(true);
        }

        private void OnSettingsClicked()
        {
            PlayButtonSound();
            ShowSettingsPanel();
        }

        private void OnQuitClicked()
        {
            PlayButtonSound();
            StartCoroutine(QuitGame());
        }

        private void OnBackClicked()
        {
            PlayButtonSound();
            ShowMainMenu();
        }

        private void OnSettingsBackClicked()
        {
            PlayButtonSound();
            SaveSettings();
            ShowMainMenu();
        }

        private void SelectDifficulty(GameDifficulty difficulty)
        {
            PlayButtonSound();
            selectedDifficulty = difficulty;
            StartGame(false);
        }

        #endregion

        #region Game Start

        private void StartGame(bool loadSave)
        {
            // 난이도 저장
            PlayerPrefs.SetInt("GameDifficulty", (int)selectedDifficulty);
            PlayerPrefs.SetInt("LoadSave", loadSave ? 1 : 0);
            PlayerPrefs.Save();

            StartCoroutine(LoadGameScene());
        }

        private IEnumerator LoadGameScene()
        {
            // 페이드 아웃
            if (fadeImage != null)
            {
                yield return StartCoroutine(FadeOut());
            }

            // 씬 로드
            SceneManager.LoadScene(gameSceneName);
        }

        private IEnumerator QuitGame()
        {
            // 페이드 아웃
            if (fadeImage != null)
            {
                yield return StartCoroutine(FadeOut());
            }

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        #endregion

        #region Settings

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }

        private void ApplySettings()
        {
            PlayButtonSound();
            SaveSettings();
            Debug.Log("[MainMenu] 설정 적용됨");
        }

        private void SaveSettings()
        {
            if (masterVolumeSlider != null)
            {
                PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
            }
            if (bgmVolumeSlider != null)
            {
                PlayerPrefs.SetFloat("BGMVolume", bgmVolumeSlider.value);
            }
            if (sfxVolumeSlider != null)
            {
                PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
            }
            if (vrModeToggle != null)
            {
                PlayerPrefs.SetInt("VRMode", vrModeToggle.isOn ? 1 : 0);
            }
            if (tutorialToggle != null)
            {
                PlayerPrefs.SetInt("Tutorial", tutorialToggle.isOn ? 1 : 0);
            }

            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            }
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            }
            if (vrModeToggle != null)
            {
                vrModeToggle.isOn = PlayerPrefs.GetInt("VRMode", 1) == 1;
            }
            if (tutorialToggle != null)
            {
                tutorialToggle.isOn = PlayerPrefs.GetInt("Tutorial", 1) == 1;
            }

            // 마스터 볼륨 적용
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        }

        private void UpdateContinueButton()
        {
            // 저장된 게임이 있는지 확인
            bool hasSave = PlayerPrefs.HasKey("SavedCheckpoint");

            if (continueButton != null)
            {
                continueButton.interactable = hasSave;
            }
        }

        #endregion

        #region Audio

        private void PlayMenuMusic()
        {
            if (menuMusic != null && audioSource != null)
            {
                audioSource.clip = menuMusic;
                audioSource.loop = true;
                audioSource.volume = 0.5f;
                audioSource.Play();
            }
        }

        #endregion

        #region Fade Effects

        private IEnumerator FadeIn()
        {
            fadeImage.color = Color.black;
            fadeImage.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                Color c = fadeImage.color;
                c.a = 1f - (elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }

            fadeImage.gameObject.SetActive(false);
        }

        private IEnumerator FadeOut()
        {
            fadeImage.color = new Color(0, 0, 0, 0);
            fadeImage.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                Color c = fadeImage.color;
                c.a = elapsed / fadeDuration;
                fadeImage.color = c;
                yield return null;
            }

            fadeImage.color = Color.black;
        }

        #endregion

        /// <summary>
        /// 선택된 난이도 반환
        /// </summary>
        public static GameDifficulty GetSelectedDifficulty()
        {
            return (GameDifficulty)PlayerPrefs.GetInt("GameDifficulty", 1);
        }

        /// <summary>
        /// 저장된 게임 로드 여부
        /// </summary>
        public static bool ShouldLoadSave()
        {
            return PlayerPrefs.GetInt("LoadSave", 0) == 1;
        }
    }
}
