using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 살인마 AI
    /// 순찰, 플레이어 감지, 추적 기능
    ///
    /// 사용법:
    /// 1. 적 캐릭터에 이 스크립트 추가
    /// 2. NavMeshAgent 컴포넌트 필요
    /// 3. 순찰 지점들을 patrolPoints에 추가
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class KillerAI : MonoBehaviour
    {
        [Header("AI State")]
        public AIState currentState = AIState.Patrol;

        [Header("Detection - Vision")]
        [Tooltip("시야 거리")]
        public float viewDistance = 15f;

        [Tooltip("시야 각도")]
        [Range(0, 180)]
        public float viewAngle = 90f;

        [Tooltip("시야 체크 레이어")]
        public LayerMask obstacleMask;

        [Tooltip("플레이어 레이어")]
        public LayerMask playerMask;

        [Header("Detection - Hearing")]
        [Tooltip("소리 감지 거리")]
        public float hearingRange = 10f;

        [Tooltip("소리 기억 시간")]
        public float noiseMemoryTime = 5f;

        [Header("Patrol")]
        [Tooltip("순찰 지점들")]
        public Transform[] patrolPoints;

        [Tooltip("순찰 대기 시간")]
        public float patrolWaitTime = 2f;

        [Tooltip("순찰 속도")]
        public float patrolSpeed = 2f;

        [Header("Chase")]
        [Tooltip("추적 속도")]
        public float chaseSpeed = 4.5f;

        [Tooltip("추적 포기 시간")]
        public float chaseTimeout = 8f;

        [Tooltip("잡기 거리")]
        public float catchDistance = 1.5f;

        [Header("Search")]
        [Tooltip("수색 속도")]
        public float searchSpeed = 3f;

        [Tooltip("수색 시간")]
        public float searchTime = 10f;

        [Tooltip("수색 반경")]
        public float searchRadius = 5f;

        [Header("Audio")]
        public AudioClip footstepSound;
        public AudioClip chaseMusic;
        public AudioClip catchSound;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnPlayerSpotted;
        public UnityEngine.Events.UnityEvent OnPlayerLost;
        public UnityEngine.Events.UnityEvent OnPlayerCaught;

        public enum AIState
        {
            Patrol,     // 순찰
            Search,     // 수색 (소리 들음)
            Chase,      // 추적 (플레이어 발견)
            Investigate // 조사 (마지막 위치)
        }

        private NavMeshAgent agent;
        private Transform player;
        private AudioSource audioSource;

        private int currentPatrolIndex;
        private float stateTimer;
        private Vector3 lastKnownPosition;
        private Vector3 lastNoisePosition;
        private float lastNoiseTime;
        private bool hasNoiseToInvestigate;
        private bool isWaitingAtPatrolPoint;
        private bool isSearchingAround;
        private float searchAroundCooldown;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
            }
        }

        private void Start()
        {
            FindPlayer();

            // NavMesh 위로 위치 보정
            SnapToNavMesh();

            // NavMeshAgent 설정 최적화
            agent.speed = patrolSpeed;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
            agent.stoppingDistance = 0.5f;
            agent.autoBraking = true;
            agent.updatePosition = true; // NavMeshAgent가 직접 위치 이동 제어
            agent.updateRotation = true; // 이동 방향으로 회전

            SetState(AIState.Patrol);
        }

        /// <summary>
        /// NavMesh 위로 위치 스냅 (공중에 뜨는 것 방지)
        /// </summary>
        private void SnapToNavMesh()
        {
            NavMeshHit hit;
            // 현재 위치에서 가장 가까운 NavMesh 지점 찾기
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                // NavMeshAgent를 일시적으로 비활성화하고 위치 이동
                agent.enabled = false;
                transform.position = hit.position;
                agent.enabled = true;
                Debug.Log($"[KillerAI] NavMesh 위치로 스냅: {hit.position}");
            }
            else
            {
                Debug.LogWarning("[KillerAI] 근처에 NavMesh가 없습니다! NavMesh를 베이크하세요.");
            }
        }

        /// <summary>
        /// 플레이어 찾기 (VR 또는 PC)
        /// </summary>
        private void FindPlayer()
        {
            // VR 플레이어 먼저 찾기
            var vrPlayer = FindObjectOfType<VRPlayer>();
            if (vrPlayer != null)
            {
                player = vrPlayer.transform;
                Debug.Log("[KillerAI] VR 플레이어 발견");
                return;
            }

            // PC 플레이어 찾기
            var pcPlayer = FindObjectOfType<PCPlayerController>();
            if (pcPlayer != null)
            {
                player = pcPlayer.transform;
                Debug.Log("[KillerAI] PC 플레이어 발견");
                return;
            }

            // 태그로 찾기
            var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                player = taggedPlayer.transform;
                Debug.Log("[KillerAI] 태그로 플레이어 발견");
            }
        }

        private void Update()
        {
            if (player == null) return;

            // 플레이어 감지 체크
            bool canSeePlayer = CanSeePlayer();

            switch (currentState)
            {
                case AIState.Patrol:
                    UpdatePatrol();
                    if (canSeePlayer)
                    {
                        SetState(AIState.Chase);
                    }
                    else if (hasNoiseToInvestigate)
                    {
                        SetState(AIState.Search);
                    }
                    break;

                case AIState.Search:
                    UpdateSearch();
                    if (canSeePlayer)
                    {
                        SetState(AIState.Chase);
                    }
                    break;

                case AIState.Chase:
                    UpdateChase();
                    if (!canSeePlayer)
                    {
                        stateTimer -= Time.deltaTime;
                        if (stateTimer <= 0)
                        {
                            SetState(AIState.Investigate);
                        }
                    }
                    else
                    {
                        stateTimer = chaseTimeout;
                        lastKnownPosition = player.position;
                    }
                    break;

                case AIState.Investigate:
                    UpdateInvestigate();
                    if (canSeePlayer)
                    {
                        SetState(AIState.Chase);
                    }
                    break;
            }

            // 잡기 체크
            CheckCatch();
        }

        private void SetState(AIState newState)
        {
            if (currentState == newState) return;

            // 이전 상태 종료
            OnExitState(currentState);

            currentState = newState;
            Debug.Log($"[KillerAI] 상태 변경: {newState}");

            // 새 상태 시작
            OnEnterState(newState);
        }

        private void OnEnterState(AIState state)
        {
            switch (state)
            {
                case AIState.Patrol:
                    agent.speed = patrolSpeed;
                    GoToNextPatrolPoint();
                    break;

                case AIState.Search:
                    agent.speed = searchSpeed;
                    stateTimer = searchTime;
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(lastNoisePosition);
                    }
                    break;

                case AIState.Chase:
                    agent.speed = chaseSpeed;
                    stateTimer = chaseTimeout;
                    OnPlayerSpotted?.Invoke();
                    if (chaseMusic != null)
                    {
                        audioSource.clip = chaseMusic;
                        audioSource.loop = true;
                        audioSource.Play();
                    }
                    break;

                case AIState.Investigate:
                    agent.speed = searchSpeed;
                    stateTimer = searchTime;
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(lastKnownPosition);
                    }
                    OnPlayerLost?.Invoke();
                    break;
            }
        }

        private void OnExitState(AIState state)
        {
            // 공통 초기화
            StopAllCoroutines();
            isWaitingAtPatrolPoint = false;
            isSearchingAround = false;
            agent.isStopped = false;

            switch (state)
            {
                case AIState.Chase:
                    audioSource.Stop();
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            // NavMesh 위에 있는지 확인
            if (!agent.isOnNavMesh) return;

            // 이미 대기 중이면 스킵
            if (isWaitingAtPatrolPoint) return;

            // 목적지 도착 체크
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                StartCoroutine(WaitAndGoNext());
            }
        }

        private IEnumerator WaitAndGoNext()
        {
            isWaitingAtPatrolPoint = true;
            agent.isStopped = true;

            yield return new WaitForSeconds(patrolWaitTime);

            // 상태가 변경되었으면 중단
            if (currentState != AIState.Patrol)
            {
                isWaitingAtPatrolPoint = false;
                yield break;
            }

            agent.isStopped = false;
            GoToNextPatrolPoint();
            isWaitingAtPatrolPoint = false;
        }

        private void GoToNextPatrolPoint()
        {
            if (patrolPoints.Length == 0) return;
            if (!agent.isOnNavMesh) return;

            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        private void UpdateSearch()
        {
            stateTimer -= Time.deltaTime;
            searchAroundCooldown -= Time.deltaTime;

            // NavMesh 위에 있는지 확인
            if (!agent.isOnNavMesh)
            {
                // 수색 시간 초과 시 순찰로 복귀
                if (stateTimer <= 0)
                {
                    hasNoiseToInvestigate = false;
                    SetState(AIState.Patrol);
                }
                return;
            }

            // 목적지 도착 시 주변 수색 (쿨다운 적용)
            if (!agent.pathPending && agent.remainingDistance < 1f && searchAroundCooldown <= 0)
            {
                SearchAround();
            }

            // 수색 시간 초과
            if (stateTimer <= 0)
            {
                hasNoiseToInvestigate = false;
                SetState(AIState.Patrol);
            }
        }

        private void SearchAround()
        {
            if (!agent.isOnNavMesh) return;
            if (isSearchingAround) return;

            isSearchingAround = true;
            searchAroundCooldown = 2f; // 2초 쿨다운

            // 랜덤한 주변 위치로 이동
            Vector3 randomDirection = Random.insideUnitSphere * searchRadius;
            randomDirection.y = 0; // 수평 방향만
            randomDirection += transform.position;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, searchRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }

            isSearchingAround = false;
        }

        private void UpdateChase()
        {
            if (player == null) return;
            if (!agent.isOnNavMesh) return;

            // 플레이어가 숨어있으면 추적 중단
            if (IsPlayerHiding())
            {
                // 숨어있는 플레이어는 보이지 않음
                return;
            }

            agent.SetDestination(player.position);
        }

        private void UpdateInvestigate()
        {
            stateTimer -= Time.deltaTime;
            searchAroundCooldown -= Time.deltaTime;

            // NavMesh 위에 있는지 확인
            if (!agent.isOnNavMesh)
            {
                // 조사 시간 초과 시 순찰로 복귀
                if (stateTimer <= 0)
                {
                    SetState(AIState.Patrol);
                }
                return;
            }

            // 마지막 위치 도착 시 주변 수색 (쿨다운 적용)
            if (!agent.pathPending && agent.remainingDistance < 1f && searchAroundCooldown <= 0)
            {
                SearchAround();
            }

            // 조사 시간 초과
            if (stateTimer <= 0)
            {
                SetState(AIState.Patrol);
            }
        }

        /// <summary>
        /// 플레이어가 숨어있는지 체크
        /// </summary>
        private bool IsPlayerHiding()
        {
            // VR 플레이어 숨기 체크
            var vrPlayer = player.GetComponent<VRPlayer>();
            if (vrPlayer != null && vrPlayer.IsHiding)
            {
                return true;
            }

            // PC 플레이어 숨기 체크
            var pcPlayer = player.GetComponent<PCPlayerController>();
            if (pcPlayer != null && pcPlayer.IsHiding)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 플레이어를 볼 수 있는지 체크
        /// </summary>
        public bool CanSeePlayer()
        {
            if (player == null) return false;

            // 숨어있는 플레이어는 보이지 않음
            if (IsPlayerHiding())
            {
                return false;
            }

            Vector3 eyePosition = transform.position + Vector3.up * 1.6f; // 눈 높이
            Vector3 playerCenter = player.position + Vector3.up * 0.9f; // 플레이어 중심
            Vector3 directionToPlayer = playerCenter - eyePosition;
            float distanceToPlayer = directionToPlayer.magnitude;

            // 거리 체크
            if (distanceToPlayer > viewDistance)
            {
                return false;
            }

            // 각도 체크
            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle > viewAngle / 2)
            {
                return false;
            }

            // 장애물 체크 (레이캐스트)
            // 레이어 마스크가 설정되지 않았으면 기본값 사용
            LayerMask raycastMask = obstacleMask | playerMask;
            if (raycastMask == 0)
            {
                raycastMask = ~0; // 모든 레이어 체크
            }

            RaycastHit hit;
            if (Physics.Raycast(eyePosition, directionToPlayer.normalized, out hit, distanceToPlayer, raycastMask))
            {
                // 플레이어 또는 플레이어의 자식 오브젝트에 히트
                if (hit.transform == player || hit.transform.IsChildOf(player))
                {
                    return true;
                }
                // 장애물에 막힘
                return false;
            }

            // 레이캐스트가 아무것도 맞추지 못함 = 장애물 없음 = 플레이어 보임
            return true;
        }

        /// <summary>
        /// 소리 감지
        /// </summary>
        public void HearNoise(Vector3 noisePosition)
        {
            float distance = Vector3.Distance(transform.position, noisePosition);

            if (distance <= hearingRange)
            {
                lastNoisePosition = noisePosition;
                lastNoiseTime = Time.time;
                hasNoiseToInvestigate = true;

                Debug.Log($"[KillerAI] 소리 감지! 거리: {distance:F1}m");

                // 순찰 중이면 수색 모드로 전환
                if (currentState == AIState.Patrol)
                {
                    SetState(AIState.Search);
                }
            }
        }

        /// <summary>
        /// 플레이어 잡기 체크
        /// </summary>
        private void CheckCatch()
        {
            if (player == null) return;
            if (currentState != AIState.Chase) return;

            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= catchDistance)
            {
                CatchPlayer();
            }
        }

        /// <summary>
        /// 플레이어 잡기
        /// </summary>
        private void CatchPlayer()
        {
            Debug.Log("[KillerAI] 플레이어를 잡았습니다!");

            agent.isStopped = true;

            if (catchSound != null)
            {
                audioSource.PlayOneShot(catchSound);
            }

            // VR 플레이어에게 알림
            var vrPlayer = player.GetComponent<VRPlayer>();
            if (vrPlayer != null)
            {
                vrPlayer.GetCaught();
            }

            // PC 플레이어에게 알림
            var pcPlayer = player.GetComponent<PCPlayerController>();
            if (pcPlayer != null)
            {
                pcPlayer.GetCaught();
            }

            // 게임 매니저에 게임오버 알림
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.GameOver("살인마에게 잡혔습니다!");
            }

            OnPlayerCaught?.Invoke();
        }

        /// <summary>
        /// 특정 방 주변 순찰 (RoomProgressManager에서 호출)
        /// </summary>
        public void PatrolRoom(Transform roomCenter)
        {
            if (roomCenter == null) return;

            StartCoroutine(PatrolRoomCoroutine(roomCenter));
        }

        private IEnumerator PatrolRoomCoroutine(Transform roomCenter)
        {
            // 방 순찰 모드로 전환
            SetState(AIState.Search);

            // 방 중심으로 이동
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(roomCenter.position);
            }

            // 방 도착 대기
            while (agent.pathPending || agent.remainingDistance > 2f)
            {
                yield return null;
            }

            // 방 주변 순찰 (3~4번 랜덤 위치로 이동)
            int patrolCount = Random.Range(3, 5);
            for (int i = 0; i < patrolCount; i++)
            {
                // 랜덤 위치 생성
                Vector3 randomOffset = Random.insideUnitSphere * searchRadius;
                randomOffset.y = 0;
                Vector3 targetPos = roomCenter.position + randomOffset;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(targetPos, out hit, searchRadius, NavMesh.AllAreas))
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.SetDestination(hit.position);
                    }

                    // 이동 완료 대기
                    while (agent.pathPending || agent.remainingDistance > 0.5f)
                    {
                        yield return null;
                    }

                    // 잠시 대기 (주변 살피기)
                    yield return new WaitForSeconds(Random.Range(1f, 2f));
                }
            }

            // 순찰 완료 후 다시 일반 순찰로
            SetState(AIState.Patrol);
            Debug.Log("[KillerAI] 방 순찰 완료, 일반 순찰로 복귀");
        }

        /// <summary>
        /// 특정 위치로 이동 (스크립트 이벤트용)
        /// </summary>
        public void MoveToPosition(Vector3 position)
        {
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(position);
            }
        }

        /// <summary>
        /// 살인마 일시 정지
        /// </summary>
        public void Pause()
        {
            if (agent != null)
            {
                agent.isStopped = true;
            }
        }

        /// <summary>
        /// 살인마 재개
        /// </summary>
        public void Resume()
        {
            if (agent != null)
            {
                agent.isStopped = false;
            }
        }

        // 에디터에서 시야 범위 시각화
        private void OnDrawGizmosSelected()
        {
            // 시야 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, viewDistance);

            // 시야 각도
            Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * transform.forward * viewDistance;
            Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * transform.forward * viewDistance;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

            // 청각 범위
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            // 잡기 범위
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, catchDistance);
        }
    }
}
