using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace HorrorGame
{
    /// <summary>
    /// VR 편의 설정
    /// 멀미 방지를 위한 비네트, 스냅턴 등 설정
    ///
    /// 사용법:
    /// 1. XR Origin 또는 매니저에 추가
    /// 2. 필요한 옵션 활성화
    /// </summary>
    public class VRComfortSettings : MonoBehaviour
    {
        public static VRComfortSettings Instance { get; private set; }

        [Header("Movement Vignette")]
        [Tooltip("이동 시 비네트 효과 (멀미 감소, URP 필요)")]
        public bool useMovementVignette = false;

        [Tooltip("비네트 강도")]
        [Range(0, 1)]
        public float vignetteIntensity = 0.5f;

        [Tooltip("비네트 적용 속도")]
        public float vignetteSmoothness = 5f;

        [Header("Snap Turn")]
        [Tooltip("스냅 회전 사용 (부드러운 회전 대신)")]
        public bool useSnapTurn = true;

        [Tooltip("스냅 회전 각도")]
        public float snapTurnAngle = 45f;

        [Tooltip("스냅 회전 쿨다운")]
        public float snapTurnCooldown = 0.3f;

        [Header("Teleport Settings")]
        [Tooltip("텔레포트 시 페이드 효과 (URP 필요)")]
        public bool teleportFade = false;

        [Tooltip("페이드 시간")]
        public float fadeTime = 0.2f;

        [Header("Seated Mode")]
        [Tooltip("앉아서 플레이 모드")]
        public bool seatedMode = false;

        [Tooltip("앉은 자세 높이 오프셋")]
        public float seatedHeightOffset = 0.5f;

        [Header("FOV Reduction")]
        [Tooltip("빠른 이동 시 시야 좁히기")]
        public bool useFOVReduction = true;

        [Tooltip("최소 FOV 비율")]
        [Range(0.5f, 1f)]
        public float minFOVRatio = 0.7f;

        [Header("Height Calibration")]
        [Tooltip("현재 플레이어 높이")]
        public float playerHeight = 1.7f;

        [Header("References")]
        public Transform xrOrigin;
        public Transform cameraTransform;

#if UNITY_URP
        private Volume postProcessVolume;
        private Vignette vignette;
#endif
        private float currentVignetteIntensity;
        private Vector3 lastPosition;
        private float lastSnapTime;
        private bool isTeleporting;

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
        }

        private void Start()
        {
#if UNITY_URP
            // Post Processing 찾기
            postProcessVolume = FindObjectOfType<Volume>();
            if (postProcessVolume != null)
            {
                postProcessVolume.profile.TryGet(out vignette);
            }
#endif

            // XR Origin 찾기
            if (xrOrigin == null)
            {
                xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>()?.transform;
            }

            if (cameraTransform == null && xrOrigin != null)
            {
                var cam = xrOrigin.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cameraTransform = cam.transform;
                }
            }

            lastPosition = transform.position;

            // 앉아서 플레이 모드 적용
            if (seatedMode)
            {
                ApplySeatedMode();
            }
        }

        private void Update()
        {
            if (useMovementVignette)
            {
                UpdateMovementVignette();
            }

            lastPosition = transform.position;
        }

        private void UpdateMovementVignette()
        {
#if UNITY_URP
            if (vignette == null) return;

            // 이동 속도 계산
            Vector3 movement = transform.position - lastPosition;
            float speed = movement.magnitude / Time.deltaTime;

            // 목표 비네트 강도
            float targetIntensity = 0;
            if (speed > 0.5f) // 이동 중
            {
                targetIntensity = Mathf.Clamp01(speed / 5f) * vignetteIntensity;
            }

            // 부드럽게 적용
            currentVignetteIntensity = Mathf.Lerp(
                currentVignetteIntensity,
                targetIntensity,
                Time.deltaTime * vignetteSmoothness
            );

            // HeartbeatEffect와 충돌 방지
            var heartbeat = FindObjectOfType<HeartbeatEffect>();
            if (heartbeat == null || !heartbeat.useVignette)
            {
                vignette.intensity.value = currentVignetteIntensity;
            }
#endif
        }

        /// <summary>
        /// 스냅 회전 수행
        /// </summary>
        public void SnapTurn(bool right)
        {
            if (!useSnapTurn) return;
            if (Time.time - lastSnapTime < snapTurnCooldown) return;

            float angle = right ? snapTurnAngle : -snapTurnAngle;

            if (xrOrigin != null)
            {
                xrOrigin.Rotate(0, angle, 0);
            }

            lastSnapTime = Time.time;
        }

        /// <summary>
        /// 텔레포트 (페이드 효과 포함)
        /// </summary>
        public void Teleport(Vector3 destination)
        {
            if (isTeleporting) return;

            if (teleportFade)
            {
                StartCoroutine(TeleportWithFade(destination));
            }
            else
            {
                transform.position = destination;
            }
        }

        private System.Collections.IEnumerator TeleportWithFade(Vector3 destination)
        {
            isTeleporting = true;

            // 페이드 아웃
            yield return FadeScreen(true);

            // 이동
            transform.position = destination;

            // 잠시 대기
            yield return new WaitForSeconds(0.1f);

            // 페이드 인
            yield return FadeScreen(false);

            isTeleporting = false;
        }

        private System.Collections.IEnumerator FadeScreen(bool fadeOut)
        {
#if UNITY_URP
            if (vignette == null) yield break;

            float startIntensity = vignette.intensity.value;
            float endIntensity = fadeOut ? 1f : 0f;

            float elapsed = 0;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                vignette.intensity.value = Mathf.Lerp(startIntensity, endIntensity, elapsed / fadeTime);
                yield return null;
            }

            vignette.intensity.value = endIntensity;
#else
            yield break;
#endif
        }

        /// <summary>
        /// 앉아서 플레이 모드 적용
        /// </summary>
        public void ApplySeatedMode()
        {
            seatedMode = true;

            if (xrOrigin != null)
            {
                Vector3 pos = xrOrigin.localPosition;
                pos.y += seatedHeightOffset;
                xrOrigin.localPosition = pos;
            }
        }

        /// <summary>
        /// 서서 플레이 모드로 전환
        /// </summary>
        public void ApplyStandingMode()
        {
            if (seatedMode && xrOrigin != null)
            {
                Vector3 pos = xrOrigin.localPosition;
                pos.y -= seatedHeightOffset;
                xrOrigin.localPosition = pos;
            }

            seatedMode = false;
        }

        /// <summary>
        /// 높이 보정
        /// </summary>
        public void CalibrateHeight()
        {
            if (cameraTransform != null && xrOrigin != null)
            {
                float currentHeight = cameraTransform.localPosition.y;
                float offset = playerHeight - currentHeight;

                Vector3 pos = xrOrigin.localPosition;
                pos.y += offset;
                xrOrigin.localPosition = pos;
            }
        }

        /// <summary>
        /// 비네트 강도 설정
        /// </summary>
        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 스냅 턴 각도 설정
        /// </summary>
        public void SetSnapTurnAngle(float angle)
        {
            snapTurnAngle = Mathf.Clamp(angle, 15f, 90f);
        }

        /// <summary>
        /// 설정 저장
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetInt("VR_UseSnapTurn", useSnapTurn ? 1 : 0);
            PlayerPrefs.SetFloat("VR_SnapTurnAngle", snapTurnAngle);
            PlayerPrefs.SetFloat("VR_VignetteIntensity", vignetteIntensity);
            PlayerPrefs.SetInt("VR_SeatedMode", seatedMode ? 1 : 0);
            PlayerPrefs.SetFloat("VR_PlayerHeight", playerHeight);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 설정 불러오기
        /// </summary>
        public void LoadSettings()
        {
            useSnapTurn = PlayerPrefs.GetInt("VR_UseSnapTurn", 1) == 1;
            snapTurnAngle = PlayerPrefs.GetFloat("VR_SnapTurnAngle", 45f);
            vignetteIntensity = PlayerPrefs.GetFloat("VR_VignetteIntensity", 0.5f);
            seatedMode = PlayerPrefs.GetInt("VR_SeatedMode", 0) == 1;
            playerHeight = PlayerPrefs.GetFloat("VR_PlayerHeight", 1.7f);

            if (seatedMode)
            {
                ApplySeatedMode();
            }
        }
    }
}
