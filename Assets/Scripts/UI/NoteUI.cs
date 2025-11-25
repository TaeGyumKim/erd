using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace HorrorGame
{
    /// <summary>
    /// 메모/문서 표시 UI
    /// VR 공간에서 메모 내용을 보여줌
    ///
    /// 사용법:
    /// 1. Canvas에 이 스크립트 추가
    /// 2. UI 요소들 연결
    /// </summary>
    public class NoteUI : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject notePanel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI contentText;
        public Image noteImage;
        public Button closeButton;

        [Header("Animation")]
        public float fadeInDuration = 0.3f;
        public float fadeOutDuration = 0.2f;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;

        private CanvasGroup canvasGroup;
        private Action onCloseCallback;
        private AudioSource audioSource;
        private bool isAnimating;

        private void Awake()
        {
            canvasGroup = notePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = notePanel.AddComponent<CanvasGroup>();
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // 닫기 버튼 이벤트
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideNote);
            }

            // 초기 상태: 숨김
            notePanel.SetActive(false);
        }

        /// <summary>
        /// 메모 표시
        /// </summary>
        public void ShowNote(string title, string content, Sprite image = null, Action onClose = null)
        {
            if (isAnimating) return;

            onCloseCallback = onClose;

            // 내용 설정
            if (titleText != null)
                titleText.text = title;

            if (contentText != null)
                contentText.text = content;

            if (noteImage != null)
            {
                if (image != null)
                {
                    noteImage.sprite = image;
                    noteImage.gameObject.SetActive(true);
                }
                else
                {
                    noteImage.gameObject.SetActive(false);
                }
            }

            // 사운드
            if (openSound != null)
            {
                audioSource.PlayOneShot(openSound);
            }

            // 애니메이션
            notePanel.SetActive(true);
            StartCoroutine(FadeIn());

            // 게임 일시정지 (선택)
            // Time.timeScale = 0;
        }

        /// <summary>
        /// 메모 숨기기
        /// </summary>
        public void HideNote()
        {
            if (isAnimating) return;
            if (!notePanel.activeSelf) return;

            // 사운드
            if (closeSound != null)
            {
                audioSource.PlayOneShot(closeSound);
            }

            StartCoroutine(FadeOut());

            // 콜백
            onCloseCallback?.Invoke();
            onCloseCallback = null;

            // 게임 재개
            // Time.timeScale = 1;
        }

        private System.Collections.IEnumerator FadeIn()
        {
            isAnimating = true;
            canvasGroup.alpha = 0;

            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = elapsed / fadeInDuration;
                yield return null;
            }

            canvasGroup.alpha = 1;
            isAnimating = false;
        }

        private System.Collections.IEnumerator FadeOut()
        {
            isAnimating = true;

            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1 - (elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0;
            notePanel.SetActive(false);
            isAnimating = false;
        }

        /// <summary>
        /// 메모가 표시 중인지
        /// </summary>
        public bool IsShowing()
        {
            return notePanel.activeSelf && !isAnimating;
        }
    }
}
