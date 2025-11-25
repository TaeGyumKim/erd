using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// VR 손전등
    /// 컨트롤러에 부착하여 사용
    /// 배터리 시스템 포함
    /// </summary>
    public class VRFlashlight : MonoBehaviour
    {
        [Header("Light Settings")]
        [Tooltip("손전등 Light 컴포넌트")]
        public Light flashlightLight;

        [Tooltip("손전등 밝기")]
        public float lightIntensity = 2f;

        [Tooltip("손전등 범위")]
        public float lightRange = 15f;

        [Tooltip("손전등 각도")]
        public float spotAngle = 45f;

        [Header("Battery")]
        [Tooltip("배터리 사용 여부")]
        public bool useBattery = true;

        [Tooltip("최대 배터리")]
        public float maxBattery = 100f;

        [Tooltip("현재 배터리")]
        public float currentBattery;

        [Tooltip("배터리 소모율 (초당)")]
        public float batteryDrainRate = 2f;

        [Header("Effects")]
        [Tooltip("배터리 부족 시 깜빡임")]
        public bool flickerOnLowBattery = true;

        [Tooltip("깜빡임 시작 배터리 %")]
        [Range(0f, 0.5f)]
        public float flickerThreshold = 0.2f;

        [Header("Audio")]
        public AudioClip turnOnSound;
        public AudioClip turnOffSound;
        public AudioClip flickerSound;

        public bool IsOn { get; private set; }
        public float BatteryPercent => currentBattery / maxBattery;

        public UnityEngine.Events.UnityEvent<float> OnBatteryChanged;
        public UnityEngine.Events.UnityEvent OnBatteryDepleted;

        private AudioSource audioSource;
        private float flickerTimer;
        private bool isFlickering;

        private void Awake()
        {
            currentBattery = maxBattery;

            // Light 컴포넌트 자동 찾기/생성
            if (flashlightLight == null)
            {
                flashlightLight = GetComponentInChildren<Light>();
                if (flashlightLight == null)
                {
                    GameObject lightObj = new GameObject("Flashlight Light");
                    lightObj.transform.SetParent(transform);
                    lightObj.transform.localPosition = Vector3.zero;
                    lightObj.transform.localRotation = Quaternion.identity;
                    flashlightLight = lightObj.AddComponent<Light>();
                }
            }

            // Light 설정
            flashlightLight.type = LightType.Spot;
            flashlightLight.intensity = lightIntensity;
            flashlightLight.range = lightRange;
            flashlightLight.spotAngle = spotAngle;
            flashlightLight.enabled = false;

            // AudioSource 설정
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            if (IsOn)
            {
                UpdateBattery();
                UpdateFlicker();
            }
        }

        private void UpdateBattery()
        {
            if (!useBattery) return;

            currentBattery -= batteryDrainRate * Time.deltaTime;
            currentBattery = Mathf.Max(0, currentBattery);

            OnBatteryChanged?.Invoke(BatteryPercent);

            if (currentBattery <= 0)
            {
                TurnOff();
                OnBatteryDepleted?.Invoke();
            }
        }

        private void UpdateFlicker()
        {
            if (!flickerOnLowBattery || !useBattery) return;

            if (BatteryPercent <= flickerThreshold)
            {
                flickerTimer -= Time.deltaTime;
                if (flickerTimer <= 0)
                {
                    isFlickering = !isFlickering;
                    flashlightLight.enabled = !isFlickering;

                    // 랜덤한 깜빡임 간격
                    flickerTimer = Random.Range(0.05f, 0.3f);

                    if (isFlickering && flickerSound != null)
                    {
                        audioSource.PlayOneShot(flickerSound, 0.3f);
                    }
                }
            }
        }

        /// <summary>
        /// 손전등 켜기/끄기 토글
        /// </summary>
        public void Toggle()
        {
            if (IsOn)
                TurnOff();
            else
                TurnOn();
        }

        /// <summary>
        /// 손전등 켜기
        /// </summary>
        public void TurnOn()
        {
            if (useBattery && currentBattery <= 0)
            {
                Debug.Log("[VRFlashlight] 배터리가 없습니다!");
                return;
            }

            IsOn = true;
            flashlightLight.enabled = true;
            isFlickering = false;

            if (turnOnSound != null)
            {
                audioSource.PlayOneShot(turnOnSound);
            }

            Debug.Log("[VRFlashlight] 손전등 켜짐");
        }

        /// <summary>
        /// 손전등 끄기
        /// </summary>
        public void TurnOff()
        {
            IsOn = false;
            flashlightLight.enabled = false;

            if (turnOffSound != null)
            {
                audioSource.PlayOneShot(turnOffSound);
            }

            Debug.Log("[VRFlashlight] 손전등 꺼짐");
        }

        /// <summary>
        /// 배터리 충전
        /// </summary>
        public void RechargeBattery(float amount)
        {
            currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
            OnBatteryChanged?.Invoke(BatteryPercent);
            Debug.Log($"[VRFlashlight] 배터리 충전: {BatteryPercent * 100:F0}%");
        }

        /// <summary>
        /// 배터리 완충
        /// </summary>
        public void FullRecharge()
        {
            currentBattery = maxBattery;
            OnBatteryChanged?.Invoke(BatteryPercent);
        }
    }
}
