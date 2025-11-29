using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// Quest Link 연결 상태를 관리하는 매니저
    /// VR 연결/해제 이벤트를 처리
    ///
    /// 사용법:
    /// 1. 씬에 빈 GameObject 생성
    /// 2. 이 스크립트 추가
    /// 3. OnVRConnected/OnVRDisconnected 이벤트에 동작 연결
    /// </summary>
    public class QuestLinkManager : MonoBehaviour
    {
        public static QuestLinkManager Instance { get; private set; }

        [Header("Connection Settings")]
        [Tooltip("연결 실패 시 자동 재시도")]
        public bool autoRetryConnection = true;

        [Tooltip("재시도 간격 (초)")]
        public float retryInterval = 2f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnVRConnected;
        public UnityEngine.Events.UnityEvent OnVRDisconnected;
        public UnityEngine.Events.UnityEvent<string> OnVRError;

        // 상태
        public bool IsVRConnected { get; private set; }
        public string ConnectedDeviceName { get; private set; }

        private bool wasConnected = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(CheckVRConnection());
        }

        private IEnumerator CheckVRConnection()
        {
            while (true)
            {
                CheckConnection();
                yield return new WaitForSeconds(1f);
            }
        }

        private void CheckConnection()
        {
            var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(xrDisplaySubsystems);

            bool currentlyConnected = false;

            foreach (var subsystem in xrDisplaySubsystems)
            {
                if (subsystem.running)
                {
                    currentlyConnected = true;
                    break;
                }
            }

            // 연결 상태 변경 감지
            if (currentlyConnected && !wasConnected)
            {
                OnConnected();
            }
            else if (!currentlyConnected && wasConnected)
            {
                OnDisconnected();
            }

            wasConnected = currentlyConnected;
            IsVRConnected = currentlyConnected;
        }

        private void OnConnected()
        {
            // 연결된 디바이스 이름 가져오기
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);

            if (inputDevices.Count > 0)
            {
                ConnectedDeviceName = inputDevices[0].name;
            }
            else
            {
                ConnectedDeviceName = "Unknown VR Device";
            }

            Debug.Log($"[QuestLinkManager] VR 연결됨: {ConnectedDeviceName}");
            OnVRConnected?.Invoke();
        }

        private void OnDisconnected()
        {
            Debug.Log("[QuestLinkManager] VR 연결 해제됨");
            ConnectedDeviceName = "";
            OnVRDisconnected?.Invoke();

            if (autoRetryConnection)
            {
                StartCoroutine(RetryConnection());
            }
        }

        private IEnumerator RetryConnection()
        {
            Debug.Log("[QuestLinkManager] VR 재연결 시도 중...");

            yield return new WaitForSeconds(retryInterval);

            if (!IsVRConnected)
            {
                // XR 재초기화 시도
                var xrManager = XRGeneralSettings.Instance?.Manager;
                if (xrManager != null && !xrManager.isInitializationComplete)
                {
                    xrManager.InitializeLoaderSync();
                    if (xrManager.activeLoader != null)
                    {
                        xrManager.StartSubsystems();
                    }
                }
            }
        }

        /// <summary>
        /// VR 수동 연결 시도
        /// </summary>
        public void TryConnect()
        {
            StartCoroutine(RetryConnection());
        }

        /// <summary>
        /// VR 연결 해제
        /// </summary>
        public void Disconnect()
        {
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager != null)
            {
                xrManager.StopSubsystems();
                xrManager.DeinitializeLoader();
                Debug.Log("[QuestLinkManager] VR 연결 해제 요청됨");
            }
        }
    }
}
