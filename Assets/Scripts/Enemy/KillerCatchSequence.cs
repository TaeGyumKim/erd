using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 살인마가 플레이어를 잡았을 때 연출 시퀀스
    /// - 플레이어를 살인마 앞으로 끌어당김
    /// - 카메라 흔들림 효과
    /// - 살인마가 플레이어를 흔드는 연출
    /// - 화면 페이드 아웃
    /// </summary>
    public class KillerCatchSequence : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("KillerAI (자동 탐색됨)")]
        public KillerAI killerAI;

        [Tooltip("KillerAnimator (자동 탐색됨)")]
        public KillerAnimator killerAnimator;

        [Header("Catch Position")]
        [Tooltip("플레이어를 잡는 위치 오프셋 (살인마 기준) - 얼굴이 보이도록 거리 유지")]
        public Vector3 catchPositionOffset = new Vector3(0f, 0f, 1.5f);

        [Tooltip("플레이어를 끌어당기는 시간")]
        public float pullDuration = 0.3f;

        [Header("Shake Effect - 흔들기 연출")]
        [Tooltip("흔들기 활성화")]
        public bool enableShake = true;

        [Tooltip("흔들기 강도")]
        public float shakeIntensity = 0.15f;

        [Tooltip("흔들기 속도")]
        public float shakeSpeed = 25f;

        [Tooltip("흔들기 지속 시간")]
        public float shakeDuration = 2f;

        [Tooltip("좌우 흔들기 횟수")]
        public int shakeCount = 4;

        [Header("Camera Effects")]
        [Tooltip("카메라 흔들림 강도")]
        public float cameraShakeIntensity = 0.1f;

        [Tooltip("화면 페이드 아웃 시간")]
        public float fadeOutDuration = 1.5f;

        [Tooltip("페이드 시작 딜레이 (흔들기 후)")]
        public float fadeStartDelay = 1.0f;

        [Header("Audio")]
        [Tooltip("잡힐 때 재생할 사운드")]
        public AudioClip catchSound;

        [Tooltip("흔들 때 재생할 사운드")]
        public AudioClip shakeSound;

        [Tooltip("게임오버 사운드")]
        public AudioClip gameOverSound;

        [Header("Animation")]
        [Tooltip("잡기 애니메이션 트리거 이름")]
        public string catchAnimationTrigger = "Catch";

        [Header("Events")]
        public UnityEvent OnCatchStart;
        public UnityEvent OnShakeStart;
        public UnityEvent OnCatchComplete;

        private AudioSource audioSource;
        private Transform playerTransform;
        private Camera playerCamera;
        private bool isSequenceRunning;
        private Vector3 originalCameraLocalPos;

        private void Awake()
        {
            if (killerAI == null)
                killerAI = GetComponent<KillerAI>();

            if (killerAnimator == null)
                killerAnimator = GetComponent<KillerAnimator>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            // KillerAI의 OnPlayerCaught 이벤트에 연결
            if (killerAI != null)
            {
                killerAI.OnPlayerCaught.AddListener(StartCatchSequence);
            }
        }

        private void OnDestroy()
        {
            if (killerAI != null)
            {
                killerAI.OnPlayerCaught.RemoveListener(StartCatchSequence);
            }
        }

        /// <summary>
        /// 잡기 시퀀스 시작
        /// </summary>
        public void StartCatchSequence()
        {
            if (isSequenceRunning) return;

            // 플레이어 찾기
            playerTransform = FindPlayerTransform();
            if (playerTransform == null)
            {
                Debug.LogWarning("[KillerCatchSequence] 플레이어를 찾을 수 없습니다!");
                return;
            }

            // 플레이어 카메라 찾기
            playerCamera = FindPlayerCamera();

            StartCoroutine(CatchSequenceCoroutine());
        }

        private Transform FindPlayerTransform()
        {
            // VR 플레이어
            var vrPlayer = FindFirstObjectByType<VRPlayer>();
            if (vrPlayer != null) return vrPlayer.transform;

            // PC 플레이어
            var pcPlayer = FindFirstObjectByType<PCPlayerController>();
            if (pcPlayer != null) return pcPlayer.transform;

            // 태그로 찾기
            var tagged = GameObject.FindGameObjectWithTag("Player");
            return tagged?.transform;
        }

        private Camera FindPlayerCamera()
        {
            // VR 카메라 (Main Camera 또는 XR Origin의 카메라)
            var mainCam = Camera.main;
            if (mainCam != null) return mainCam;

            // XR Origin 내부 카메라
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                return xrOrigin.Camera;
            }

            return null;
        }

        private IEnumerator CatchSequenceCoroutine()
        {
            isSequenceRunning = true;
            OnCatchStart?.Invoke();

            Debug.Log("[KillerCatchSequence] 잡기 시퀀스 시작");

            // 1. 잡기 사운드 재생
            if (catchSound != null)
            {
                audioSource.PlayOneShot(catchSound);
            }

            // 2. 잡기 애니메이션 트리거
            if (killerAnimator != null)
            {
                killerAnimator.TriggerAnimation(catchAnimationTrigger);
            }

            // 3. 플레이어를 살인마 앞으로 끌어당김
            yield return StartCoroutine(PullPlayerToKiller());

            // 4. 살인마가 플레이어를 바라보도록
            LookAtPlayer();

            // 5. 흔들기 연출
            if (enableShake)
            {
                OnShakeStart?.Invoke();

                if (shakeSound != null)
                {
                    audioSource.PlayOneShot(shakeSound);
                }

                yield return StartCoroutine(ShakePlayerCoroutine());
            }

            // 6. 페이드 아웃 전 딜레이
            yield return new WaitForSeconds(fadeStartDelay);

            // 7. 게임오버 사운드
            if (gameOverSound != null)
            {
                audioSource.PlayOneShot(gameOverSound);
            }

            // 8. 화면 페이드 아웃
            yield return StartCoroutine(FadeOutCoroutine());

            isSequenceRunning = false;
            OnCatchComplete?.Invoke();

            Debug.Log("[KillerCatchSequence] 잡기 시퀀스 완료");
        }

        /// <summary>
        /// 플레이어를 살인마 앞으로 끌어당김
        /// </summary>
        private IEnumerator PullPlayerToKiller()
        {
            if (playerTransform == null) yield break;

            Vector3 startPos = playerTransform.position;
            Vector3 targetPos = transform.position + transform.TransformDirection(catchPositionOffset);
            targetPos.y = startPos.y; // 높이는 유지

            float elapsed = 0f;

            while (elapsed < pullDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pullDuration;
                t = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

                playerTransform.position = Vector3.Lerp(startPos, targetPos, t);

                yield return null;
            }

            playerTransform.position = targetPos;
        }

        /// <summary>
        /// 살인마가 플레이어를 바라보도록
        /// </summary>
        private void LookAtPlayer()
        {
            if (playerTransform == null) return;

            Vector3 lookDir = playerTransform.position - transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDir);
            }

            // 플레이어도 살인마를 바라보도록
            Vector3 playerLookDir = transform.position - playerTransform.position;
            playerLookDir.y = 0f;

            if (playerLookDir.sqrMagnitude > 0.01f)
            {
                playerTransform.rotation = Quaternion.LookRotation(playerLookDir);
            }
        }

        /// <summary>
        /// 플레이어 흔들기 연출
        /// </summary>
        private IEnumerator ShakePlayerCoroutine()
        {
            if (playerTransform == null) yield break;

            // 카메라 원래 위치 저장
            if (playerCamera != null)
            {
                originalCameraLocalPos = playerCamera.transform.localPosition;
            }

            float elapsed = 0f;
            Vector3 basePosition = playerTransform.position;
            Quaternion baseRotation = playerTransform.rotation;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;

                // 좌우로 흔들기
                float shakePhase = Mathf.Sin(elapsed * shakeSpeed);
                float intensity = shakeIntensity * (1f - (elapsed / shakeDuration)); // 점점 감소

                // 플레이어 위치 흔들기
                Vector3 shakeOffset = transform.right * shakePhase * intensity;
                playerTransform.position = basePosition + shakeOffset;

                // 플레이어 회전 흔들기 (머리 좌우로 흔듦)
                float rotationShake = shakePhase * intensity * 30f; // 각도
                playerTransform.rotation = baseRotation * Quaternion.Euler(0f, rotationShake, rotationShake * 0.5f);

                // 카메라 흔들림 추가
                if (playerCamera != null)
                {
                    Vector3 camShake = new Vector3(
                        Random.Range(-1f, 1f) * cameraShakeIntensity * intensity,
                        Random.Range(-1f, 1f) * cameraShakeIntensity * intensity,
                        0f
                    );
                    playerCamera.transform.localPosition = originalCameraLocalPos + camShake;
                }

                yield return null;
            }

            // 원래 위치로 복원
            playerTransform.position = basePosition;
            playerTransform.rotation = baseRotation;

            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = originalCameraLocalPos;
            }
        }

        /// <summary>
        /// 화면 페이드 아웃
        /// </summary>
        private IEnumerator FadeOutCoroutine()
        {
            // 페이드 아웃 효과 (간단한 구현)
            // 실제로는 UI Canvas에 검은 화면을 페이드하는 방식 권장
            yield return new WaitForSeconds(fadeOutDuration);
        }

        /// <summary>
        /// 외부에서 시퀀스 강제 정지
        /// </summary>
        public void StopSequence()
        {
            StopAllCoroutines();
            isSequenceRunning = false;

            // 카메라 위치 복원
            if (playerCamera != null)
            {
                playerCamera.transform.localPosition = originalCameraLocalPos;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (killerAI == null)
                killerAI = GetComponent<KillerAI>();
            if (killerAnimator == null)
                killerAnimator = GetComponent<KillerAnimator>();
        }

        private void OnDrawGizmosSelected()
        {
            // 잡기 위치 표시
            Gizmos.color = Color.red;
            Vector3 catchPos = transform.position + transform.TransformDirection(catchPositionOffset);
            Gizmos.DrawWireSphere(catchPos, 0.3f);
            Gizmos.DrawLine(transform.position, catchPos);
        }
#endif
    }
}
