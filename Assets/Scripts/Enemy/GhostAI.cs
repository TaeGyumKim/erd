using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System.Collections;

namespace HorrorGame
{
    /// <summary>
    /// 유령(조수) AI
    /// 플레이어를 도와주는 유령 캐릭터
    /// - 환영이나 목소리로 접근
    /// - "도와달라", "그를 멈춰달라" 메시지 전달
    /// - 단서 위치 힌트 제공
    /// </summary>
    public class GhostAI : MonoBehaviour
    {
        [Header("Ghost State")]
        public GhostState currentState = GhostState.Idle;

        [Header("Movement")]
        [Tooltip("이동 속도")]
        public float moveSpeed = 2f;

        [Tooltip("플레이어 주변 배회 거리")]
        public float wanderRadius = 10f;

        [Tooltip("플레이어 접근 최소 거리")]
        public float minDistanceToPlayer = 3f;

        [Tooltip("힌트 제공 거리")]
        public float hintDistance = 5f;

        [Header("Appearance")]
        [Tooltip("유령 렌더러")]
        public Renderer ghostRenderer;

        [Tooltip("기본 투명도")]
        [Range(0f, 1f)]
        public float baseAlpha = 0.3f;

        [Tooltip("나타날 때 투명도")]
        [Range(0f, 1f)]
        public float visibleAlpha = 0.7f;

        [Tooltip("페이드 속도")]
        public float fadeSpeed = 1f;

        [Header("Audio")]
        public AudioClip[] whisperClips;
        public AudioClip appearSound;
        public AudioClip disappearSound;
        public AudioClip helpMessage;

        [Tooltip("속삭임 간격 (초)")]
        public float whisperInterval = 10f;

        [Header("Messages")]
        [TextArea(2, 3)]
        public string[] helpMessages = new string[]
        {
            "도와줘...",
            "그를 멈춰줘...",
            "열쇠를 찾아...",
            "저택을 탈출해..."
        };

        [Header("Hints")]
        [Tooltip("힌트로 안내할 오브젝트들")]
        public Transform[] hintTargets;

        [Tooltip("현재 힌트 인덱스")]
        public int currentHintIndex = 0;

        [Header("Events")]
        public UnityEvent OnGhostAppear;
        public UnityEvent OnGhostDisappear;
        public UnityEvent<string> OnGhostSpeak;
        public UnityEvent OnHintGiven;

        public enum GhostState
        {
            Idle,           // 대기
            Wandering,      // 배회
            Approaching,    // 플레이어 접근
            Speaking,       // 메시지 전달
            Guiding,        // 힌트 안내
            Fleeing,        // 살인마 피해 도망
            Hidden          // 숨김
        }

        private NavMeshAgent agent;
        private AudioSource audioSource;
        private Transform playerTransform;
        private Material ghostMaterial;
        private float currentAlpha;
        private float lastWhisperTime;
        private bool isHelping = false;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = minDistanceToPlayer;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.maxDistance = 15f;
            }

            if (ghostRenderer != null)
            {
                ghostMaterial = ghostRenderer.material;
                currentAlpha = baseAlpha;
                SetAlpha(currentAlpha);
            }
        }

        private void Start()
        {
            // 플레이어 찾기
            if (VRPlayer.Instance != null)
            {
                playerTransform = VRPlayer.Instance.transform;
            }
            else
            {
                var player = FindObjectOfType<VRPlayer>();
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            // 처음엔 숨김 상태
            currentState = GhostState.Hidden;
            SetAlpha(0f);
        }

        private void Update()
        {
            if (playerTransform == null) return;

            switch (currentState)
            {
                case GhostState.Idle:
                    UpdateIdle();
                    break;
                case GhostState.Wandering:
                    UpdateWandering();
                    break;
                case GhostState.Approaching:
                    UpdateApproaching();
                    break;
                case GhostState.Guiding:
                    UpdateGuiding();
                    break;
                case GhostState.Fleeing:
                    UpdateFleeing();
                    break;
            }

            // 주기적 속삭임
            if (isHelping && Time.time - lastWhisperTime > whisperInterval)
            {
                PlayWhisper();
                lastWhisperTime = Time.time;
            }
        }

        /// <summary>
        /// 유령 도움 시작
        /// </summary>
        public void StartHelping()
        {
            if (isHelping) return;

            isHelping = true;
            currentState = GhostState.Wandering;

            StartCoroutine(FadeIn());
            OnGhostAppear?.Invoke();

            if (appearSound != null)
            {
                audioSource.PlayOneShot(appearSound);
            }

            Debug.Log("[GhostAI] 유령이 도움을 시작합니다");
        }

        /// <summary>
        /// 유령 숨기기
        /// </summary>
        public void Hide()
        {
            currentState = GhostState.Hidden;
            StartCoroutine(FadeOut());
            OnGhostDisappear?.Invoke();

            if (disappearSound != null)
            {
                audioSource.PlayOneShot(disappearSound);
            }
        }

        private void UpdateIdle()
        {
            // 플레이어가 가까우면 접근
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance < wanderRadius)
            {
                currentState = GhostState.Approaching;
            }
        }

        private void UpdateWandering()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            // 플레이어 주변 랜덤 위치로 이동
            if (!agent.hasPath || agent.remainingDistance < 1f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += playerTransform.position;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }

            // 가끔 플레이어에게 접근
            if (Random.value < 0.01f)
            {
                currentState = GhostState.Approaching;
            }
        }

        private void UpdateApproaching()
        {
            if (agent == null || !agent.isOnNavMesh) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (distance > minDistanceToPlayer)
            {
                // 플레이어 방향으로 이동
                agent.SetDestination(playerTransform.position);
            }
            else
            {
                // 메시지 전달
                agent.ResetPath();
                StartCoroutine(SpeakToPlayer());
            }
        }

        private void UpdateGuiding()
        {
            if (agent == null || !agent.isOnNavMesh) return;
            if (hintTargets == null || hintTargets.Length == 0) return;

            // 현재 힌트 타겟으로 이동
            if (currentHintIndex < hintTargets.Length && hintTargets[currentHintIndex] != null)
            {
                Transform target = hintTargets[currentHintIndex];

                if (!agent.hasPath || agent.remainingDistance < 1f)
                {
                    agent.SetDestination(target.position);
                }

                // 타겟 근처에 도착하면 힌트 제공
                if (Vector3.Distance(transform.position, target.position) < 2f)
                {
                    OnHintGiven?.Invoke();
                    currentHintIndex++;

                    // 힌트 다 줬으면 배회 모드로
                    if (currentHintIndex >= hintTargets.Length)
                    {
                        currentState = GhostState.Wandering;
                    }
                }
            }
        }

        private void UpdateFleeing()
        {
            // 살인마에게서 도망
            var killers = FindObjectsOfType<KillerAI>();
            if (killers.Length == 0)
            {
                currentState = GhostState.Wandering;
                return;
            }

            // 가장 가까운 살인마에게서 도망
            Transform nearestKiller = null;
            float nearestDistance = float.MaxValue;

            foreach (var killer in killers)
            {
                float dist = Vector3.Distance(transform.position, killer.transform.position);
                if (dist < nearestDistance)
                {
                    nearestDistance = dist;
                    nearestKiller = killer.transform;
                }
            }

            if (nearestKiller != null && nearestDistance < 10f)
            {
                // 반대 방향으로 도망
                Vector3 fleeDirection = (transform.position - nearestKiller.position).normalized;
                Vector3 fleeTarget = transform.position + fleeDirection * 10f;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(fleeTarget, out hit, 10f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }
            else
            {
                currentState = GhostState.Wandering;
            }
        }

        /// <summary>
        /// 플레이어에게 말하기
        /// </summary>
        private IEnumerator SpeakToPlayer()
        {
            currentState = GhostState.Speaking;

            // 투명도 높이기
            yield return StartCoroutine(FadeTo(visibleAlpha));

            // 메시지 선택
            string message = helpMessages[Random.Range(0, helpMessages.Length)];
            OnGhostSpeak?.Invoke(message);

            // UI에 표시
            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus($"유령: \"{message}\"", 3f);
            }

            // 오디오 재생
            if (helpMessage != null)
            {
                audioSource.PlayOneShot(helpMessage);
            }

            yield return new WaitForSeconds(3f);

            // 투명도 낮추기
            yield return StartCoroutine(FadeTo(baseAlpha));

            // 힌트 타겟이 있으면 안내 모드로
            if (hintTargets != null && currentHintIndex < hintTargets.Length)
            {
                currentState = GhostState.Guiding;
            }
            else
            {
                currentState = GhostState.Wandering;
            }
        }

        /// <summary>
        /// 속삭임 재생
        /// </summary>
        private void PlayWhisper()
        {
            if (whisperClips != null && whisperClips.Length > 0)
            {
                AudioClip clip = whisperClips[Random.Range(0, whisperClips.Length)];
                audioSource.PlayOneShot(clip, 0.5f);
            }
        }

        /// <summary>
        /// 힌트 타겟 설정
        /// </summary>
        public void SetHintTarget(Transform target)
        {
            if (hintTargets == null)
            {
                hintTargets = new Transform[1];
            }

            // 배열 확장
            System.Array.Resize(ref hintTargets, hintTargets.Length + 1);
            hintTargets[hintTargets.Length - 1] = target;
        }

        /// <summary>
        /// 특정 위치로 안내 시작
        /// </summary>
        public void GuideToLocation(Transform target)
        {
            SetHintTarget(target);
            currentHintIndex = hintTargets.Length - 1;
            currentState = GhostState.Guiding;
        }

        #region 투명도 조절

        private IEnumerator FadeIn()
        {
            yield return FadeTo(baseAlpha);
        }

        private IEnumerator FadeOut()
        {
            yield return FadeTo(0f);
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            while (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
            {
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
                SetAlpha(currentAlpha);
                yield return null;
            }

            currentAlpha = targetAlpha;
            SetAlpha(currentAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (ghostMaterial != null)
            {
                Color color = ghostMaterial.color;
                color.a = alpha;
                ghostMaterial.color = color;
            }
        }

        #endregion

        /// <summary>
        /// 살인마 감지 시 도망
        /// </summary>
        public void OnKillerNearby()
        {
            currentState = GhostState.Fleeing;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, wanderRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer);

            // 힌트 타겟 표시
            if (hintTargets != null)
            {
                Gizmos.color = Color.green;
                foreach (var target in hintTargets)
                {
                    if (target != null)
                    {
                        Gizmos.DrawLine(transform.position, target.position);
                        Gizmos.DrawWireSphere(target.position, 1f);
                    }
                }
            }
        }
    }
}
