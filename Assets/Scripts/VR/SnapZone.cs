using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRGame
{
    /// <summary>
    /// 오브젝트를 특정 위치에 스냅(고정)시키는 영역
    /// 퍼즐이나 아이템 배치에 사용
    ///
    /// 사용법:
    /// 1. 빈 GameObject에 이 스크립트 추가
    /// 2. acceptedTags에 스냅 가능한 오브젝트의 태그 설정
    /// 3. OnObjectSnapped 이벤트로 스냅 시 동작 정의
    /// </summary>
    [RequireComponent(typeof(XRSocketInteractor))]
    public class SnapZone : MonoBehaviour
    {
        [Header("Snap Settings")]
        [Tooltip("이 스냅존에 들어올 수 있는 오브젝트 태그들")]
        public string[] acceptedTags = { "Grabbable" };

        [Tooltip("스냅 시 오브젝트 고정 여부")]
        public bool lockOnSnap = false;

        [Header("Visual")]
        [Tooltip("스냅존 표시 오브젝트 (옵션)")]
        public GameObject highlightObject;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent<GameObject> OnObjectSnapped;
        public UnityEngine.Events.UnityEvent<GameObject> OnObjectRemoved;

        private XRSocketInteractor socketInteractor;
        private GameObject snappedObject;

        private void Awake()
        {
            socketInteractor = GetComponent<XRSocketInteractor>();

            // 이벤트 연결
            socketInteractor.selectEntered.AddListener(OnSnap);
            socketInteractor.selectExited.AddListener(OnRemove);

            // 하이라이트 초기 상태
            if (highlightObject != null)
            {
                highlightObject.SetActive(true);
            }
        }

        private void OnSnap(SelectEnterEventArgs args)
        {
            var obj = args.interactableObject.transform.gameObject;

            // 태그 확인
            bool accepted = false;
            foreach (var tag in acceptedTags)
            {
                if (obj.CompareTag(tag))
                {
                    accepted = true;
                    break;
                }
            }

            // 태그가 비어있으면 모든 오브젝트 허용
            if (acceptedTags.Length == 0)
            {
                accepted = true;
            }

            if (accepted)
            {
                snappedObject = obj;

                // 하이라이트 숨기기
                if (highlightObject != null)
                {
                    highlightObject.SetActive(false);
                }

                // 오브젝트 고정
                if (lockOnSnap)
                {
                    var grabInteractable = obj.GetComponent<XRGrabInteractable>();
                    if (grabInteractable != null)
                    {
                        grabInteractable.enabled = false;
                    }
                }

                OnObjectSnapped?.Invoke(obj);
                Debug.Log($"[SnapZone] {obj.name}이(가) 스냅됨");
            }
        }

        private void OnRemove(SelectExitEventArgs args)
        {
            var obj = args.interactableObject.transform.gameObject;

            if (obj == snappedObject)
            {
                // 하이라이트 표시
                if (highlightObject != null)
                {
                    highlightObject.SetActive(true);
                }

                OnObjectRemoved?.Invoke(obj);
                Debug.Log($"[SnapZone] {obj.name}이(가) 제거됨");

                snappedObject = null;
            }
        }

        /// <summary>
        /// 현재 스냅된 오브젝트 반환
        /// </summary>
        public GameObject GetSnappedObject()
        {
            return snappedObject;
        }

        /// <summary>
        /// 스냅된 오브젝트가 있는지 확인
        /// </summary>
        public bool HasSnappedObject()
        {
            return snappedObject != null;
        }

        private void OnDestroy()
        {
            if (socketInteractor != null)
            {
                socketInteractor.selectEntered.RemoveListener(OnSnap);
                socketInteractor.selectExited.RemoveListener(OnRemove);
            }
        }
    }
}
