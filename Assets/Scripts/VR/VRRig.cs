using UnityEngine;


namespace HorrorGame
{
    /// <summary>
    /// VR 플레이어 리그를 관리하는 메인 클래스
    /// XR Origin에 추가하여 사용
    /// </summary>
    public class VRRig : MonoBehaviour
    {
        [Header("Controllers")]
        [Tooltip("왼손 컨트롤러")]
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor leftHandController;

        [Tooltip("오른손 컨트롤러")]
        public UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor rightHandController;

        [Header("Locomotion")]
        [Tooltip("텔레포트 이동 사용 여부")]
        public bool useTeleportation = true;

        [Tooltip("연속 이동 사용 여부")]
        public bool useContinuousMove = true;

        [Tooltip("이동 속도")]
        public float moveSpeed = 2f;

        private void Awake()
        {
            // 컨트롤러 자동 찾기
            if (leftHandController == null || rightHandController == null)
            {
                var interactors = GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRDirectInteractor>();
                foreach (var interactor in interactors)
                {
                    if (interactor.name.ToLower().Contains("left"))
                        leftHandController = interactor;
                    else if (interactor.name.ToLower().Contains("right"))
                        rightHandController = interactor;
                }
            }
        }

        private void Start()
        {
            Debug.Log("[VRRig] VR Rig 초기화 완료");
        }
    }
}
