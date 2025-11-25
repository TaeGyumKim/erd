using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// VR 컨트롤러 손 모델 표시 및 애니메이션
    /// 컨트롤러 입력에 따라 손 모양이 변함
    ///
    /// 사용법:
    /// 1. 손 모델(프리팹)을 handModelPrefab에 할당
    /// 2. Left Hand 또는 Right Hand 컨트롤러 오브젝트에 추가
    /// </summary>
    public class HandPresence : MonoBehaviour
    {
        [Header("Hand Model")]
        [Tooltip("손 모델 프리팹")]
        public GameObject handModelPrefab;

        [Tooltip("컨트롤러 모델 표시 여부")]
        public bool showController = false;

        [Header("Hand Type")]
        [Tooltip("왼손/오른손 설정")]
        public InputDeviceCharacteristics controllerCharacteristics;

        private InputDevice targetDevice;
        private GameObject spawnedHandModel;
        private Animator handAnimator;

        private void Start()
        {
            TryInitialize();
        }

        private void TryInitialize()
        {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

            if (devices.Count > 0)
            {
                targetDevice = devices[0];

                if (handModelPrefab != null)
                {
                    spawnedHandModel = Instantiate(handModelPrefab, transform);
                    handAnimator = spawnedHandModel.GetComponent<Animator>();
                }
            }
        }

        private void Update()
        {
            // 디바이스가 유효하지 않으면 다시 초기화 시도
            if (!targetDevice.isValid)
            {
                TryInitialize();
                return;
            }

            // 손 애니메이션 업데이트
            UpdateHandAnimation();
        }

        private void UpdateHandAnimation()
        {
            if (handAnimator == null) return;

            // 트리거 값 (검지 손가락)
            if (targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
            {
                handAnimator.SetFloat("Trigger", triggerValue);
            }

            // 그립 값 (나머지 손가락)
            if (targetDevice.TryGetFeatureValue(CommonUsages.grip, out float gripValue))
            {
                handAnimator.SetFloat("Grip", gripValue);
            }
        }
    }
}
