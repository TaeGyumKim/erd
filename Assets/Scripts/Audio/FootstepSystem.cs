using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// 발소리 시스템
    /// 바닥 재질에 따라 다른 발소리 재생
    ///
    /// 사용법:
    /// 1. 플레이어에 이 스크립트 추가
    /// 2. 바닥 오브젝트에 PhysicMaterial 또는 Tag 설정
    /// 3. footstepSets에 재질별 사운드 추가
    /// </summary>
    public class FootstepSystem : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("발소리 재생할 AudioSource")]
        public AudioSource footstepAudioSource;

        [Header("Footstep Settings")]
        [Tooltip("걷기 발소리 간격")]
        public float walkStepInterval = 0.5f;

        [Tooltip("달리기 발소리 간격")]
        public float sprintStepInterval = 0.3f;

        [Tooltip("웅크리기 발소리 간격")]
        public float crouchStepInterval = 0.7f;

        [Tooltip("발소리 볼륨 (걷기)")]
        [Range(0, 1)]
        public float walkVolume = 0.3f;

        [Tooltip("발소리 볼륨 (달리기)")]
        [Range(0, 1)]
        public float sprintVolume = 0.6f;

        [Tooltip("발소리 볼륨 (웅크리기)")]
        [Range(0, 1)]
        public float crouchVolume = 0.1f;

        [Header("Surface Detection")]
        [Tooltip("바닥 감지 레이 길이")]
        public float rayDistance = 1.5f;

        [Tooltip("바닥 레이어")]
        public LayerMask groundLayer = ~0;

        [Header("Footstep Sounds")]
        public FootstepSet[] footstepSets;

        [System.Serializable]
        public class FootstepSet
        {
            public string surfaceTag = "Default";
            public AudioClip[] footstepClips;
            [Range(0.8f, 1.2f)]
            public float pitchMin = 0.9f;
            [Range(0.8f, 1.2f)]
            public float pitchMax = 1.1f;
        }

        // 기본 발소리 (태그가 없을 때)
        [Header("Default Footsteps")]
        public AudioClip[] defaultFootsteps;

        private VRPlayer vrPlayer;
        private float stepTimer;
        private int lastClipIndex = -1;
        private bool isMoving;
        private Vector3 lastPosition;

        private void Awake()
        {
            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
                footstepAudioSource.spatialBlend = 1f;
                footstepAudioSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            vrPlayer = GetComponent<VRPlayer>();
            if (vrPlayer == null)
            {
                vrPlayer = VRPlayer.Instance;
            }
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (vrPlayer == null) return;
            if (vrPlayer.IsHiding) return;

            // 이동 감지
            Vector3 horizontalMovement = transform.position - lastPosition;
            horizontalMovement.y = 0;
            isMoving = horizontalMovement.magnitude > 0.01f;
            lastPosition = transform.position;

            if (!isMoving) return;

            // 발소리 타이머
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0)
            {
                PlayFootstep();
                stepTimer = GetStepInterval();
            }
        }

        private float GetStepInterval()
        {
            if (vrPlayer.IsCrouching)
                return crouchStepInterval;
            if (vrPlayer.IsSprinting)
                return sprintStepInterval;
            return walkStepInterval;
        }

        private float GetVolume()
        {
            if (vrPlayer.IsCrouching)
                return crouchVolume;
            if (vrPlayer.IsSprinting)
                return sprintVolume;
            return walkVolume;
        }

        private void PlayFootstep()
        {
            AudioClip clip = GetFootstepClip();
            if (clip == null) return;

            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
            footstepAudioSource.volume = GetVolume();
            footstepAudioSource.PlayOneShot(clip);
        }

        private AudioClip GetFootstepClip()
        {
            // 바닥 재질 감지
            string surfaceTag = DetectSurface();
            AudioClip[] clips = GetClipsForSurface(surfaceTag);

            if (clips == null || clips.Length == 0)
            {
                clips = defaultFootsteps;
            }

            if (clips == null || clips.Length == 0)
                return null;

            // 같은 클립이 연속으로 재생되지 않도록
            int clipIndex;
            do
            {
                clipIndex = Random.Range(0, clips.Length);
            } while (clipIndex == lastClipIndex && clips.Length > 1);

            lastClipIndex = clipIndex;
            return clips[clipIndex];
        }

        private string DetectSurface()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance, groundLayer))
            {
                // 태그로 확인
                return hit.collider.tag;
            }
            return "Default";
        }

        private AudioClip[] GetClipsForSurface(string surfaceTag)
        {
            foreach (var set in footstepSets)
            {
                if (set.surfaceTag == surfaceTag)
                {
                    return set.footstepClips;
                }
            }
            return null;
        }

        /// <summary>
        /// 수동으로 발소리 재생 (계단 등)
        /// </summary>
        public void PlayStepManually(float volumeMultiplier = 1f)
        {
            AudioClip clip = GetFootstepClip();
            if (clip == null) return;

            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
            footstepAudioSource.volume = GetVolume() * volumeMultiplier;
            footstepAudioSource.PlayOneShot(clip);
        }
    }
}
