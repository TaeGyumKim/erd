using UnityEngine;
using UnityEngine.XR.Hands;
using System;

namespace VRGame
{
    /// <summary>
    /// Meta Quest 3 핸드 트래킹 지원
    /// 손 제스처 인식 및 핀치(집기) 감지
    ///
    /// 사용법:
    /// 1. XR Origin에 이 스크립트 추가
    /// 2. OnPinchStart/OnPinchEnd 이벤트에 동작 연결
    ///
    /// 주의: Package Manager에서 XR Hands 샘플을 Import해야 합니다!
    /// Window > Package Manager > XR Hands > Samples > HandVisualizer
    /// </summary>
    public class Quest3HandTracking : MonoBehaviour
    {
        [Header("Hand Settings")]
        [Tooltip("핸드 트래킹 활성화")]
        public bool enableHandTracking = true;

        [Header("Pinch Detection")]
        [Tooltip("핀치로 인식할 거리 (미터)")]
        public float pinchThreshold = 0.02f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent<Hand> OnPinchStart;
        public UnityEngine.Events.UnityEvent<Hand> OnPinchEnd;
        public UnityEngine.Events.UnityEvent<Hand, Vector3> OnPinchMove;

        public enum Hand { Left, Right }

        // 핀치 상태
        public bool IsLeftPinching { get; private set; }
        public bool IsRightPinching { get; private set; }

        // 핀치 위치
        public Vector3 LeftPinchPosition { get; private set; }
        public Vector3 RightPinchPosition { get; private set; }

        private XRHandSubsystem handSubsystem;
        private bool wasLeftPinching = false;
        private bool wasRightPinching = false;

        private void OnEnable()
        {
            // XR Hand Subsystem 찾기
            var handSubsystems = new System.Collections.Generic.List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(handSubsystems);

            if (handSubsystems.Count > 0)
            {
                handSubsystem = handSubsystems[0];
                handSubsystem.updatedHands += OnHandsUpdated;
                Debug.Log("[Quest3HandTracking] 핸드 트래킹 초기화 완료");
            }
            else
            {
                Debug.LogWarning("[Quest3HandTracking] XR Hand Subsystem을 찾을 수 없습니다. " +
                    "Edit > Project Settings > XR Plug-in Management > OpenXR에서 " +
                    "Hand Tracking Subsystem을 활성화하세요.");
            }
        }

        private void OnDisable()
        {
            if (handSubsystem != null)
            {
                handSubsystem.updatedHands -= OnHandsUpdated;
            }
        }

        private void OnHandsUpdated(XRHandSubsystem subsystem,
            XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (!enableHandTracking) return;

            // 왼손 처리
            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0)
            {
                ProcessHand(subsystem.leftHand, Hand.Left);
            }

            // 오른손 처리
            if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0)
            {
                ProcessHand(subsystem.rightHand, Hand.Right);
            }
        }

        private void ProcessHand(XRHand hand, Hand handType)
        {
            // 엄지와 검지 끝 위치 가져오기
            if (hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbPose) &&
                hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexPose))
            {
                float distance = Vector3.Distance(thumbPose.position, indexPose.position);
                bool isPinching = distance < pinchThreshold;
                Vector3 pinchPosition = (thumbPose.position + indexPose.position) / 2f;

                if (handType == Hand.Left)
                {
                    IsLeftPinching = isPinching;
                    LeftPinchPosition = pinchPosition;

                    if (isPinching && !wasLeftPinching)
                    {
                        OnPinchStart?.Invoke(Hand.Left);
                    }
                    else if (!isPinching && wasLeftPinching)
                    {
                        OnPinchEnd?.Invoke(Hand.Left);
                    }
                    else if (isPinching)
                    {
                        OnPinchMove?.Invoke(Hand.Left, pinchPosition);
                    }

                    wasLeftPinching = isPinching;
                }
                else
                {
                    IsRightPinching = isPinching;
                    RightPinchPosition = pinchPosition;

                    if (isPinching && !wasRightPinching)
                    {
                        OnPinchStart?.Invoke(Hand.Right);
                    }
                    else if (!isPinching && wasRightPinching)
                    {
                        OnPinchEnd?.Invoke(Hand.Right);
                    }
                    else if (isPinching)
                    {
                        OnPinchMove?.Invoke(Hand.Right, pinchPosition);
                    }

                    wasRightPinching = isPinching;
                }
            }
        }

        /// <summary>
        /// 핸드 트래킹 토글
        /// </summary>
        public void SetHandTrackingEnabled(bool enabled)
        {
            enableHandTracking = enabled;
            Debug.Log($"[Quest3HandTracking] 핸드 트래킹: {(enabled ? "활성화" : "비활성화")}");
        }
    }
}
