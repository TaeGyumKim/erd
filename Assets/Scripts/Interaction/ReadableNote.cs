using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// 읽을 수 있는 메모/문서
    /// 스토리, 힌트, 비밀번호 등 전달에 사용
    ///
    /// 사용법:
    /// 1. 메모/문서 오브젝트에 추가
    /// 2. noteTitle과 noteContent 설정
    /// 3. 필요시 noteImage 추가
    /// </summary>
    public class ReadableNote : XRSimpleInteractable
    {
        [Header("Note Content")]
        [Tooltip("메모 제목")]
        public string noteTitle = "메모";

        [Tooltip("메모 내용")]
        [TextArea(5, 15)]
        public string noteContent = "내용을 입력하세요...";

        [Tooltip("메모에 포함된 이미지 (선택)")]
        public Sprite noteImage;

        [Header("Note Settings")]
        [Tooltip("읽은 후 자동 닫기 시간 (0이면 수동 닫기)")]
        public float autoCloseTime = 0f;

        [Tooltip("읽으면 자동 수집 (인벤토리에 추가)")]
        public bool collectOnRead = false;

        [Tooltip("한 번만 읽을 수 있음")]
        public bool readOnce = false;

        [Tooltip("메모 ID (퀘스트 연동용)")]
        public string noteId;

        [Header("Visual Feedback")]
        [Tooltip("읽지 않은 상태 강조")]
        public bool highlightUnread = true;

        [Tooltip("강조 색상")]
        public Color unreadHighlightColor = new Color(1f, 1f, 0.5f, 1f);

        [Header("Audio")]
        public AudioClip paperSound;
        public AudioClip closeSound;

        [Header("Events")]
        public UnityEvent OnNoteOpened;
        public UnityEvent OnNoteClosed;
        public UnityEvent<string> OnNoteRead;  // noteId 전달

        public bool HasBeenRead { get; private set; }

        private Renderer noteRenderer;
        private Color originalColor;
        private bool isReading;

        protected override void Awake()
        {
            base.Awake();
            noteRenderer = GetComponent<Renderer>();
            if (noteRenderer != null)
            {
                originalColor = noteRenderer.material.color;
            }
        }

        private void Start()
        {
            if (highlightUnread && !HasBeenRead)
            {
                SetHighlight(true);
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            OpenNote();
        }

        /// <summary>
        /// 메모 열기
        /// </summary>
        public void OpenNote()
        {
            if (isReading) return;
            if (readOnce && HasBeenRead) return;

            isReading = true;
            HasBeenRead = true;

            // 소리 재생
            if (paperSound != null)
            {
                AudioSource.PlayClipAtPoint(paperSound, transform.position);
            }

            // 하이라이트 제거
            if (highlightUnread)
            {
                SetHighlight(false);
            }

            // UI에 메모 표시
            ShowNoteUI();

            OnNoteOpened?.Invoke();
            OnNoteRead?.Invoke(noteId);

            Debug.Log($"[ReadableNote] 메모 열림: {noteTitle}");

            // 자동 닫기
            if (autoCloseTime > 0)
            {
                Invoke(nameof(CloseNote), autoCloseTime);
            }

            // 자동 수집
            if (collectOnRead)
            {
                CollectNote();
            }
        }

        /// <summary>
        /// 메모 닫기
        /// </summary>
        public void CloseNote()
        {
            if (!isReading) return;

            isReading = false;

            // 소리 재생
            if (closeSound != null)
            {
                AudioSource.PlayClipAtPoint(closeSound, transform.position);
            }

            // UI 닫기
            HideNoteUI();

            OnNoteClosed?.Invoke();

            Debug.Log($"[ReadableNote] 메모 닫힘: {noteTitle}");
        }

        /// <summary>
        /// 메모 수집 (인벤토리에 추가)
        /// </summary>
        public void CollectNote()
        {
            var inventory = PlayerInventory.Instance;
            if (inventory != null)
            {
                // 인벤토리에 메모 추가 (아이템으로)
                var noteItem = new InventoryItem
                {
                    itemId = noteId,
                    itemName = noteTitle,
                    description = noteContent,
                    itemType = InventoryItem.ItemType.Document,
                    isConsumable = false
                };
                inventory.AddItem(noteItem);
            }

            // 오브젝트 제거
            gameObject.SetActive(false);
        }

        private void ShowNoteUI()
        {
            // NoteUI 싱글톤 찾기
            var noteUI = FindObjectOfType<NoteUI>();
            if (noteUI != null)
            {
                noteUI.ShowNote(noteTitle, noteContent, noteImage, () => CloseNote());
            }
            else
            {
                // UI가 없으면 콘솔에 출력
                Debug.Log($"=== {noteTitle} ===\n{noteContent}");
            }
        }

        private void HideNoteUI()
        {
            var noteUI = FindObjectOfType<NoteUI>();
            if (noteUI != null)
            {
                noteUI.HideNote();
            }
        }

        private void SetHighlight(bool highlight)
        {
            if (noteRenderer == null) return;

            if (highlight)
            {
                noteRenderer.material.color = unreadHighlightColor;
                // Emission 효과 (선택)
                noteRenderer.material.EnableKeyword("_EMISSION");
                noteRenderer.material.SetColor("_EmissionColor", unreadHighlightColor * 0.3f);
            }
            else
            {
                noteRenderer.material.color = originalColor;
                noteRenderer.material.DisableKeyword("_EMISSION");
            }
        }

        /// <summary>
        /// 메모 내용 가져오기
        /// </summary>
        public string GetContent()
        {
            return noteContent;
        }

        /// <summary>
        /// 메모에서 특정 텍스트 검색 (힌트용)
        /// </summary>
        public bool ContainsText(string searchText)
        {
            return noteContent.ToLower().Contains(searchText.ToLower());
        }
    }
}
