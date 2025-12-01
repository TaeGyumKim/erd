using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 숨겨진 벽 문양 - 라이터로 비추면 나타남
    /// UV/형광 느낌으로 "보이지 않던 것"이 드러남
    /// </summary>
    public class HiddenWallSymbol : MonoBehaviour
    {
        [Header("Symbol Settings")]
        [Tooltip("문양 렌더러")]
        public Renderer symbolRenderer;

        [Tooltip("숨겨진 상태 머티리얼")]
        public Material hiddenMaterial;

        [Tooltip("드러난 상태 머티리얼 (발광)")]
        public Material revealedMaterial;

        [Tooltip("문양 발견에 필요한 라이터 거리")]
        public float lighterDetectionRange = 3f;

        [Tooltip("문양 발견에 필요한 조명 시간")]
        public float revealTime = 2f;

        [Header("Visual Effects")]
        [Tooltip("발견 시 파티클")]
        public ParticleSystem revealEffect;

        [Tooltip("발광 색상")]
        public Color glowColor = new Color(0.5f, 0f, 1f, 1f); // 보라색 UV 느낌

        [Tooltip("발광 강도")]
        public float glowIntensity = 2f;

        [Header("Audio")]
        public AudioClip revealSound;
        public AudioClip ambientHumSound;

        [Header("Events")]
        public UnityEvent OnSymbolRevealed;
        public UnityEvent OnSymbolHidden;

        private bool isRevealed = false;
        private bool isBeingLit = false;
        private float currentLitTime = 0f;
        private AudioSource audioSource;
        private Material instanceMaterial;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // 처음엔 숨김 상태
            if (symbolRenderer != null)
            {
                if (hiddenMaterial != null)
                {
                    symbolRenderer.material = hiddenMaterial;
                }
                else
                {
                    // 완전 투명
                    instanceMaterial = symbolRenderer.material;
                    SetAlpha(0f);
                }
            }
        }

        private void Update()
        {
            if (isRevealed) return;

            // 라이터 감지
            CheckForLighter();
        }

        /// <summary>
        /// 라이터 감지
        /// </summary>
        private void CheckForLighter()
        {
            // 플레이어가 라이터를 가지고 있는지 확인
            if (StoryProgressManager.Instance == null || !StoryProgressManager.Instance.hasLighter)
            {
                ResetLitProgress();
                return;
            }

            // 씬에서 켜진 라이터 찾기
            var lighters = FindObjectsOfType<LighterItem>();
            bool foundLitLighter = false;

            foreach (var lighter in lighters)
            {
                if (lighter.IsLit)
                {
                    float distance = Vector3.Distance(transform.position, lighter.transform.position);
                    if (distance <= lighterDetectionRange)
                    {
                        // 라이터가 범위 내에 있고 켜져 있음
                        foundLitLighter = true;
                        UpdateLitProgress();
                        break;
                    }
                }
            }

            // 대안: 플레이어가 라이터를 가지고 있고 가까이 있으면
            if (!foundLitLighter && VRPlayer.Instance != null)
            {
                float playerDistance = Vector3.Distance(transform.position, VRPlayer.Instance.transform.position);
                if (playerDistance <= lighterDetectionRange && StoryProgressManager.Instance.hasLighter)
                {
                    // 간단 모드: 라이터만 가지고 있으면 발견 가능
                    foundLitLighter = true;
                    UpdateLitProgress();
                }
            }

            if (!foundLitLighter)
            {
                ResetLitProgress();
            }
        }

        private void UpdateLitProgress()
        {
            if (!isBeingLit)
            {
                isBeingLit = true;
                // 부분적으로 드러나기 시작
                StartCoroutine(PartialReveal());
            }

            currentLitTime += Time.deltaTime;

            if (currentLitTime >= revealTime)
            {
                RevealSymbol();
            }
        }

        private void ResetLitProgress()
        {
            if (isBeingLit && !isRevealed)
            {
                isBeingLit = false;
                currentLitTime = 0f;
                // 다시 숨기기
                if (symbolRenderer != null && hiddenMaterial != null)
                {
                    symbolRenderer.material = hiddenMaterial;
                }
            }
        }

        /// <summary>
        /// 부분적으로 드러남 (비추는 중)
        /// </summary>
        private IEnumerator PartialReveal()
        {
            float progress = 0f;

            while (isBeingLit && !isRevealed)
            {
                progress = currentLitTime / revealTime;

                if (instanceMaterial != null)
                {
                    SetAlpha(progress * 0.5f);
                }

                yield return null;
            }
        }

        /// <summary>
        /// 문양 완전히 드러남
        /// </summary>
        public void RevealSymbol()
        {
            if (isRevealed) return;

            isRevealed = true;

            // 머티리얼 변경
            if (symbolRenderer != null)
            {
                if (revealedMaterial != null)
                {
                    symbolRenderer.material = revealedMaterial;
                }
                else
                {
                    // 발광 효과
                    instanceMaterial = symbolRenderer.material;
                    instanceMaterial.EnableKeyword("_EMISSION");
                    instanceMaterial.SetColor("_EmissionColor", glowColor * glowIntensity);
                    SetAlpha(1f);
                }
            }

            // 이펙트
            if (revealEffect != null)
            {
                revealEffect.Play();
            }

            // 사운드
            if (revealSound != null)
            {
                audioSource.PlayOneShot(revealSound);
            }

            if (ambientHumSound != null)
            {
                audioSource.clip = ambientHumSound;
                audioSource.loop = true;
                audioSource.volume = 0.3f;
                audioSource.Play();
            }

            // 스토리 진행
            if (StoryProgressManager.Instance != null)
            {
                StoryProgressManager.Instance.DiscoverWallSymbol();
            }

            OnSymbolRevealed?.Invoke();

            Debug.Log("[HiddenWallSymbol] 벽 문양이 드러났습니다!");
        }

        private void SetAlpha(float alpha)
        {
            if (instanceMaterial != null)
            {
                Color color = instanceMaterial.color;
                color.a = alpha;
                instanceMaterial.color = color;
            }
        }

        /// <summary>
        /// 문양 강제 표시 (디버그용)
        /// </summary>
        [ContextMenu("Force Reveal")]
        public void ForceReveal()
        {
            RevealSymbol();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isRevealed ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, lighterDetectionRange);
        }
    }
}
