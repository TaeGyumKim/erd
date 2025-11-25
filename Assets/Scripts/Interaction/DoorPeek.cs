using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace HorrorGame
{
    /// <summary>
    /// 문틈 엿보기
    /// 문을 살짝 열어 안을 확인하는 기능
    ///
    /// 사용법:
    /// 1. Door 오브젝트에 추가
    /// 2. 문 손잡이 위치에 peekHandle 설정
    /// </summary>
    public class DoorPeek : XRSimpleInteractable
    {
        [Header("References")]
        [Tooltip("연결된 문")]
        public Door door;

        [Tooltip("엿보기 손잡이 Transform")]
        public Transform peekHandle;

        [Header("Peek Settings")]
        [Tooltip("최대 엿보기 각도")]
        public float maxPeekAngle = 15f;

        [Tooltip("엿보기 속도")]
        public float peekSpeed = 30f;

        [Tooltip("문이 닫히는 속도")]
        public float closeSpeed = 20f;

        [Tooltip("엿보기 중 소음 발생")]
        public bool makesNoise = true;

        [Tooltip("소음 반경")]
        public float noiseRadius = 3f;

        [Header("Audio")]
        public AudioClip creakSound;

        [Range(0, 1)]
        public float creakVolume = 0.3f;

        [Header("State")]
        public bool isPeeking;
        public float currentPeekAngle;

        private AudioSource audioSource;
        private bool isGrabbed;
        private Vector3 grabStartPosition;
        private float grabStartAngle;
        private Transform interactorTransform;

        protected override void Awake()
        {
            base.Awake();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // Door 자동 찾기
            if (door == null)
            {
                door = GetComponentInParent<Door>();
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // 문이 잠겨있으면 엿보기 불가
            if (door != null && door.IsLocked)
            {
                return;
            }

            isGrabbed = true;
            interactorTransform = args.interactorObject.transform;
            grabStartPosition = interactorTransform.position;
            grabStartAngle = currentPeekAngle;

            isPeeking = true;
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            isGrabbed = false;
            interactorTransform = null;
        }

        private void Update()
        {
            if (isGrabbed && interactorTransform != null)
            {
                UpdatePeek();
            }
            else if (isPeeking && currentPeekAngle > 0)
            {
                // 천천히 닫힘
                CloseDoorSlowly();
            }
        }

        private void UpdatePeek()
        {
            // 손잡이와 컨트롤러 사이 거리로 열림 정도 계산
            Vector3 pullDirection = interactorTransform.position - grabStartPosition;

            // 문에서 멀어지는 방향 계산
            Vector3 doorForward = door != null ? door.transform.forward : transform.forward;
            float pullAmount = Vector3.Dot(pullDirection, doorForward);

            // 각도 계산
            float targetAngle = grabStartAngle + (pullAmount * peekSpeed);
            targetAngle = Mathf.Clamp(targetAngle, 0, maxPeekAngle);

            // 부드럽게 적용
            float previousAngle = currentPeekAngle;
            currentPeekAngle = Mathf.Lerp(currentPeekAngle, targetAngle, Time.deltaTime * 10f);

            // 문에 회전 적용
            if (door != null)
            {
                door.SetPeekAngle(currentPeekAngle);
            }

            // 삐걱 소리
            if (Mathf.Abs(currentPeekAngle - previousAngle) > 0.5f)
            {
                PlayCreakSound();

                // 소음 발생
                if (makesNoise && VRPlayer.Instance != null)
                {
                    VRPlayer.Instance.MakeNoise(noiseRadius);
                }
            }
        }

        private void CloseDoorSlowly()
        {
            currentPeekAngle = Mathf.MoveTowards(currentPeekAngle, 0, closeSpeed * Time.deltaTime);

            if (door != null)
            {
                door.SetPeekAngle(currentPeekAngle);
            }

            if (currentPeekAngle <= 0.1f)
            {
                currentPeekAngle = 0;
                isPeeking = false;

                if (door != null)
                {
                    door.SetPeekAngle(0);
                }
            }
        }

        private void PlayCreakSound()
        {
            if (creakSound != null && !audioSource.isPlaying)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.volume = creakVolume;
                audioSource.PlayOneShot(creakSound);
            }
        }

        /// <summary>
        /// 엿보기 가능 여부
        /// </summary>
        public bool CanPeek()
        {
            if (door == null) return true;
            return !door.IsLocked && !door.IsOpen;
        }

        /// <summary>
        /// 강제로 엿보기 (이벤트용)
        /// </summary>
        public void ForcePeek(float angle)
        {
            isPeeking = true;
            StartCoroutine(AnimatePeek(angle));
        }

        private System.Collections.IEnumerator AnimatePeek(float targetAngle)
        {
            while (Mathf.Abs(currentPeekAngle - targetAngle) > 0.5f)
            {
                currentPeekAngle = Mathf.MoveTowards(currentPeekAngle, targetAngle, peekSpeed * Time.deltaTime);

                if (door != null)
                {
                    door.SetPeekAngle(currentPeekAngle);
                }

                PlayCreakSound();
                yield return null;
            }
        }
    }
}
