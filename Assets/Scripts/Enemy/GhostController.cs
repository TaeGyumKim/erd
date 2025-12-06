using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 유령 컨트롤러
    /// 스크립트된 이동 및 등장 시스템
    ///
    /// 기획서 기준:
    /// - ghost1: Room3 창문 뒤
    /// - ghost2: Room2 복도에서 뛰어감
    /// - ghost3: Room4 문을 열고 나옴
    /// </summary>
    public class GhostController : MonoBehaviour
    {
        [Header("Ghost Type")]
        public GhostType ghostType = GhostType.Window;

        [Header("Movement")]
        [Tooltip("NavMeshAgent (선택)")]
        public NavMeshAgent agent;

        [Tooltip("이동 속도")]
        public float moveSpeed = 5f;

        [Tooltip("달리기 속도")]
        public float runSpeed = 10f;

        [Header("Waypoints")]
        [Tooltip("이동 경로 (순서대로)")]
        public Transform[] waypoints;

        [Tooltip("목표 지점")]
        public Transform targetPosition;

        [Tooltip("시작 위치")]
        public Transform startPosition;

        [Header("Door Interaction")]
        [Tooltip("열 문 (ghost3용)")]
        public Door doorToOpen;

        [Header("Visual")]
        [Tooltip("유령 메시/모델")]
        public GameObject ghostVisual;

        [Tooltip("반투명 머티리얼")]
        public Material ghostMaterial;

        [Tooltip("나타나는 시간")]
        public float fadeInDuration = 0.5f;

        [Tooltip("사라지는 시간")]
        public float fadeOutDuration = 1f;

        [Header("Audio")]
        public AudioClip appearSound;
        public AudioClip runSound;
        public AudioClip screamSound;
        public AudioClip whisperSound;

        [Header("Behavior")]
        [Tooltip("나타난 후 대기 시간")]
        public float waitAfterAppear = 1f;

        [Tooltip("목적지 도착 후 사라지기")]
        public bool disappearAtDestination = true;

        [Header("Events")]
        public UnityEvent OnAppear;
        public UnityEvent OnDisappear;
        public UnityEvent OnReachDestination;
        public UnityEvent OnDoorOpened;

        public enum GhostType
        {
            Window,     // 창문 뒤에서 등장 (ghost1)
            Runner,     // 복도에서 뛰어감 (ghost2)
            DoorOpener  // 문 열고 나옴 (ghost3)
        }

        private AudioSource audioSource;
        private Renderer[] renderers;
        private bool isActive = false;
        private bool isMoving = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }

            // 렌더러 수집 (페이드용)
            if (ghostVisual != null)
            {
                renderers = ghostVisual.GetComponentsInChildren<Renderer>();
            }
            else
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            // NavMeshAgent 설정
            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.enabled = false; // 초기에는 비활성화
            }

            // 시작 위치 설정
            if (startPosition != null)
            {
                transform.position = startPosition.position;
                transform.rotation = startPosition.rotation;
            }
        }

        /// <summary>
        /// 유령 활성화 및 등장
        /// </summary>
        public void Appear()
        {
            if (isActive) return;

            gameObject.SetActive(true);
            StartCoroutine(AppearSequence());
        }

        private IEnumerator AppearSequence()
        {
            isActive = true;

            // 사운드
            if (appearSound != null)
            {
                audioSource.PlayOneShot(appearSound);
            }

            // 페이드 인
            yield return StartCoroutine(FadeIn());

            OnAppear?.Invoke();

            // 대기
            yield return new WaitForSeconds(waitAfterAppear);

            Debug.Log($"[GhostController] {gameObject.name} 등장");
        }

        /// <summary>
        /// 유령 사라짐
        /// </summary>
        public void Disappear()
        {
            if (!isActive) return;

            StartCoroutine(DisappearSequence());
        }

        private IEnumerator DisappearSequence()
        {
            // 페이드 아웃
            yield return StartCoroutine(FadeOut());

            isActive = false;
            isMoving = false;

            OnDisappear?.Invoke();

            gameObject.SetActive(false);
            Debug.Log($"[GhostController] {gameObject.name} 사라짐");
        }

        /// <summary>
        /// 도망가기 (ghost2 - 복도에서 뛰어감)
        /// </summary>
        public void RunAway()
        {
            if (isMoving) return;

            StartCoroutine(RunAwaySequence());
        }

        private IEnumerator RunAwaySequence()
        {
            isMoving = true;

            // 달리기 소리
            if (runSound != null)
            {
                audioSource.clip = runSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // NavMeshAgent 사용
            if (agent != null && targetPosition != null)
            {
                agent.enabled = true;
                agent.speed = runSpeed;
                agent.SetDestination(targetPosition.position);

                // 목적지 도착 대기
                while (agent.pathPending || agent.remainingDistance > 0.5f)
                {
                    yield return null;
                }
            }
            // 직접 이동 (웨이포인트)
            else if (waypoints != null && waypoints.Length > 0)
            {
                foreach (var waypoint in waypoints)
                {
                    yield return StartCoroutine(MoveToPosition(waypoint.position, runSpeed));
                }
            }
            // 단순 이동
            else if (targetPosition != null)
            {
                yield return StartCoroutine(MoveToPosition(targetPosition.position, runSpeed));
            }

            // 달리기 소리 정지
            audioSource.Stop();

            OnReachDestination?.Invoke();

            // 목적지에서 사라지기
            if (disappearAtDestination)
            {
                yield return new WaitForSeconds(0.5f);
                Disappear();
            }

            isMoving = false;
        }

        /// <summary>
        /// 문 열고 나타나기 (ghost3 - Room4)
        /// </summary>
        public void OpenDoorAndAppear()
        {
            StartCoroutine(OpenDoorSequence());
        }

        private IEnumerator OpenDoorSequence()
        {
            // 문 열기
            if (doorToOpen != null)
            {
                doorToOpen.OpenDoor();
                OnDoorOpened?.Invoke();

                yield return new WaitForSeconds(0.5f);
            }

            // 유령 등장
            yield return StartCoroutine(AppearSequence());

            // 비명/속삭임
            if (screamSound != null)
            {
                audioSource.PlayOneShot(screamSound);
            }

            // 플레이어 방향 바라보기
            LookAtPlayer();

            Debug.Log($"[GhostController] {gameObject.name} 문 열고 등장");
        }

        /// <summary>
        /// 창문 뒤에서 등장 (ghost1)
        /// </summary>
        public void AppearBehindWindow()
        {
            StartCoroutine(WindowAppearSequence());
        }

        private IEnumerator WindowAppearSequence()
        {
            // 등장
            yield return StartCoroutine(AppearSequence());

            // 속삭임
            if (whisperSound != null)
            {
                audioSource.PlayOneShot(whisperSound);
            }

            // 플레이어 응시
            LookAtPlayer();

            yield return new WaitForSeconds(3f);

            // 사라지기
            Disappear();
        }

        /// <summary>
        /// 위치로 직접 이동
        /// </summary>
        private IEnumerator MoveToPosition(Vector3 target, float speed)
        {
            while (Vector3.Distance(transform.position, target) > 0.5f)
            {
                Vector3 direction = (target - transform.position).normalized;
                transform.position += direction * speed * Time.deltaTime;

                // 이동 방향 바라보기
                if (direction.sqrMagnitude > 0.01f)
                {
                    direction.y = 0;
                    transform.rotation = Quaternion.LookRotation(direction);
                }

                yield return null;
            }

            transform.position = target;
        }

        /// <summary>
        /// 플레이어 바라보기
        /// </summary>
        public void LookAtPlayer()
        {
            Transform player = null;

            // VR 플레이어
            if (VRPlayer.Instance != null)
            {
                player = VRPlayer.Instance.transform;
            }
            // PC 플레이어
            else
            {
                var pcPlayer = FindFirstObjectByType<PCPlayerController>();
                if (pcPlayer != null)
                {
                    player = pcPlayer.transform;
                }
            }

            if (player != null)
            {
                Vector3 lookDir = player.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }

        #region Fade Effects

        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            // 초기 투명
            SetAlpha(0f);

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / fadeInDuration;
                SetAlpha(alpha);
                yield return null;
            }

            SetAlpha(1f);
        }

        private IEnumerator FadeOut()
        {
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / fadeOutDuration);
                SetAlpha(alpha);
                yield return null;
            }

            SetAlpha(0f);
        }

        private void SetAlpha(float alpha)
        {
            if (renderers == null) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color color = mat.color;
                        color.a = alpha;
                        mat.color = color;
                    }
                    else if (mat.HasProperty("_BaseColor"))
                    {
                        Color color = mat.GetColor("_BaseColor");
                        color.a = alpha;
                        mat.SetColor("_BaseColor", color);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 힌트 속삭임 재생
        /// </summary>
        public void PlayWhisper()
        {
            if (whisperSound != null)
            {
                audioSource.PlayOneShot(whisperSound);
            }
        }

        /// <summary>
        /// 비명 재생
        /// </summary>
        public void PlayScream()
        {
            if (screamSound != null)
            {
                audioSource.PlayOneShot(screamSound);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 시작 위치
            if (startPosition != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(startPosition.position, 0.3f);
                Gizmos.DrawLine(transform.position, startPosition.position);
            }

            // 목표 위치
            if (targetPosition != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetPosition.position, 0.3f);
                Gizmos.DrawLine(transform.position, targetPosition.position);
            }

            // 웨이포인트
            if (waypoints != null && waypoints.Length > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < waypoints.Length; i++)
                {
                    if (waypoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
                        if (i > 0 && waypoints[i - 1] != null)
                        {
                            Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
                        }
                    }
                }
            }
        }
#endif
    }
}
