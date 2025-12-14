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
        [Tooltip("추적 속도 (플레이어 달리기 속도보다 약간 느림)")]
        public float chaseSpeed = 5.5f;

        [Tooltip("추적 포기 시간 (시야에서 사라진 후)")]
        public float chaseTimeout = 8f;

        [Tooltip("최대 추적 시간 (시야에 있어도 이 시간 후 잠시 멈춤)")]
        public float maxChaseTime = 30f;

        [Tooltip("추적 휴식 시간 (최대 추적 후 순찰 전환 시간)")]
        public float chaseRestTime = 5f;

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
        public AudioClip stunSound;

        [Header("Flashlight Stun - 손전등 스턴")]
        [Tooltip("손전등 스턴 활성화")]
        public bool canBeStunned = true;

        [Tooltip("스턴 지속 시간 (5초로 증가)")]
        public float stunDuration = 5f;

        [Tooltip("스턴 후 어그로 해제")]
        public bool loseAggroAfterStun = true;

        [Tooltip("스턴 쿨다운 (연속 스턴 방지)")]
        public float stunCooldown = 8f;

        [Header("Crouch Detection - 앉기 감지")]
        [Tooltip("앉은 플레이어 감지 거리 배율 (1.0 = 100%)")]
        [Range(0.1f, 1f)]
        public float crouchDetectionMultiplier = 0.4f;

        [Tooltip("앉은 플레이어 소음 감지 거리 배율")]
        [Range(0.1f, 1f)]
        public float crouchHearingMultiplier = 0.3f;

        [Header("Events")]
        public UnityEngine.Events.UnityEvent OnPlayerSpotted;
        public UnityEngine.Events.UnityEvent OnPlayerLost;
        public UnityEngine.Events.UnityEvent OnPlayerCaught;
        public UnityEngine.Events.UnityEvent OnStunned;

        public enum AIState
        {
            Patrol,     // 순찰
            Search,     // 수색 (소리 들음)
            Chase,      // 추적 (플레이어 발견)
            Investigate, // 조사 (마지막 위치)
            Stunned     // 스턴 (손전등에 맞음)
        }

        private NavMeshAgent agent;
        private Transform player;
        private AudioSource audioSource;

        private int currentPatrolIndex;
        private float stateTimer;
        private float totalChaseTime; // 총 추적 시간 (maxChaseTime 체크용)
        private Vector3 lastKnownPosition;
        private Vector3 lastNoisePosition;
        private float lastNoiseTime;
        private bool hasNoiseToInvestigate;
        private bool isWaitingAtPatrolPoint;
        private bool isSearchingAround;
        private float searchAroundCooldown;
        private bool isResting; // 추적 휴식 중

        // 스턴 관련
        private bool isStunned;
        private float stunEndTime;
        private float lastStunTime;

        /// <summary>
        /// 스턴 상태인지 확인
        /// </summary>
        public bool IsStunned => isStunned;

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

            // 순찰 지점 자동 탐색
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                AutoFindPatrolPoints();
            }

            // 순찰 지점 로그
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                Debug.LogWarning("[KillerAI] 순찰 지점(patrolPoints)이 설정되지 않았습니다! 씬에 PatrolPoints 오브젝트를 추가하세요.");
            }
            else
            {
                Debug.Log($"[KillerAI] 순찰 지점 {patrolPoints.Length}개 설정됨");
            }

            // 초기 상태 설정 (0.1초 후 OnEnterState 호출)
            Debug.Log($"[KillerAI] 초기 상태: {currentState}");
            Invoke(nameof(DelayedStartPatrol), 0.1f);
        }

        private void DelayedStartPatrol()
        {
            // NavMesh 확인
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("[KillerAI] DelayedStartPatrol: NavMesh 위에 없음, 다시 스냅 시도");
                SnapToNavMesh();
            }

            Debug.Log($"[KillerAI] 지연 초기화 - isOnNavMesh: {agent.isOnNavMesh}, enabled: {agent.enabled}");
            OnEnterState(currentState);
        }

        /// <summary>
        /// 씬에서 PatrolPoints 오브젝트를 찾아 자동으로 순찰 지점 설정
        /// </summary>
        private void AutoFindPatrolPoints()
        {
            // PatrolPoints 이름의 오브젝트 찾기
            GameObject patrolPointsObj = GameObject.Find("PatrolPoints");
            if (patrolPointsObj == null)
            {
                // Patrol Points 이름도 시도
                patrolPointsObj = GameObject.Find("Patrol Points");
            }

            if (patrolPointsObj != null)
            {
                // 자식 Transform들 중 NavMesh에 도달 가능한 것만 사용
                List<Transform> validPoints = new List<Transform>();
                int childCount = patrolPointsObj.transform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform point = patrolPointsObj.transform.GetChild(i);
                    NavMeshHit hit;

                    // NavMesh 위의 가장 가까운 위치 찾기
                    if (NavMesh.SamplePosition(point.position, out hit, 10f, NavMesh.AllAreas))
                    {
                        // 현재 위치에서 해당 지점까지 경로가 있는지 확인
                        NavMeshPath path = new NavMeshPath();
                        if (agent.CalculatePath(hit.position, path))
                        {
                            if (path.status == NavMeshPathStatus.PathComplete)
                            {
                                validPoints.Add(point);
                                Debug.Log($"[KillerAI] 순찰 지점 {i} ({point.name}) 유효: {hit.position}");
                            }
                            else
                            {
                                Debug.LogWarning($"[KillerAI] 순찰 지점 {i} ({point.name})까지 완전한 경로 없음 (부분 경로: {path.status})");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[KillerAI] 순찰 지점 {i} ({point.name})이 NavMesh 근처에 없음: {point.position}");
                    }
                }

                if (validPoints.Count > 0)
                {
                    patrolPoints = validPoints.ToArray();
                    Debug.Log($"[KillerAI] PatrolPoints에서 유효한 순찰 지점 {validPoints.Count}/{childCount}개 설정됨");
                }
                else
                {
                    Debug.LogWarning("[KillerAI] 유효한 순찰 지점이 없습니다! NavMesh를 확인하세요.");
                }
            }
            else
            {
                Debug.LogWarning("[KillerAI] 씬에서 PatrolPoints 오브젝트를 찾을 수 없습니다.");
            }
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

            // 스턴 상태 체크
            if (isStunned)
            {
                UpdateStunned();
                return;
            }

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

                    // 총 추적 시간 증가
                    totalChaseTime += Time.deltaTime;

                    // 최대 추적 시간 초과 시 잠시 휴식 (순찰로 전환 후 다시 추적)
                    if (totalChaseTime >= maxChaseTime && !isResting)
                    {
                        Debug.Log($"[KillerAI] 최대 추적 시간 {maxChaseTime}초 초과 - 휴식 시작");
                        isResting = true;
                        totalChaseTime = 0f;
                        SetState(AIState.Patrol);
                        StartCoroutine(RestAndResumeChase());
                        break;
                    }

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
                    // 휴식 중이 아닐 때만 이벤트 발생 (재추적 시에는 발생 안함)
                    if (!isResting)
                    {
                        totalChaseTime = 0f; // 새 추적 시작 시 리셋
                        OnPlayerSpotted?.Invoke();
                    }
                    if (chaseMusic != null && !audioSource.isPlaying)
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
            if (patrolPoints.Length == 0)
            {
                Debug.LogWarning("[KillerAI] GoToNextPatrolPoint: 순찰 지점이 없습니다");
                return;
            }
            if (!agent.isOnNavMesh)
            {
                Debug.LogWarning("[KillerAI] GoToNextPatrolPoint: NavMesh 위에 없습니다");
                return;
            }

            Vector3 targetPosition = patrolPoints[currentPatrolIndex].position;

            // NavMesh 위의 가장 가까운 위치 찾기
            NavMeshHit hit;
            Vector3 destination;
            if (NavMesh.SamplePosition(targetPosition, out hit, 10f, NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            else
            {
                Debug.LogWarning($"[KillerAI] 순찰 지점 {currentPatrolIndex}이 NavMesh 근처에 없습니다: {targetPosition}");
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                return;
            }

            agent.isStopped = false;
            bool pathSet = agent.SetDestination(destination);
            Debug.Log($"[KillerAI] 순찰 지점으로 이동: {currentPatrolIndex} -> {destination} (원본: {targetPosition}), 경로 설정: {pathSet}");
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

            // 플레이어 위치에서 가장 가까운 NavMesh 위치 찾기
            Vector3 targetPosition = player.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, 20f, NavMesh.AllAreas))
            {
                // 경로가 유효한지 확인
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    agent.SetDestination(hit.position);
                }
                else
                {
                    // 완전한 경로가 없으면 부분 경로라도 사용
                    if (path.status == NavMeshPathStatus.PathPartial && path.corners.Length > 0)
                    {
                        agent.SetDestination(path.corners[path.corners.Length - 1]);
                    }
                }
            }
            else
            {
                // NavMesh에서 찾을 수 없으면 직접 플레이어 위치로 시도
                agent.SetDestination(player.position);
            }
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

            // 거리 체크 (앉기 상태 고려)
            float effectiveViewDist = GetEffectiveViewDistance();
            if (distanceToPlayer > effectiveViewDist)
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
        /// 즉시 추적 상태로 전환 (외부 호출용)
        /// </summary>
        public void StartChasing()
        {
            if (player == null)
            {
                // 플레이어 찾기
                var vrPlayer = FindObjectOfType<VRPlayer>();
                if (vrPlayer != null)
                {
                    player = vrPlayer.transform;
                }
                else
                {
                    var pcPlayer = FindObjectOfType<PCPlayerController>();
                    if (pcPlayer != null)
                    {
                        player = pcPlayer.transform;
                    }
                }
            }

            if (player != null)
            {
                lastKnownPosition = player.position;
                SetState(AIState.Chase);
                Debug.Log("[KillerAI] 추적 시작!");
            }
            else
            {
                Debug.LogWarning("[KillerAI] 플레이어를 찾을 수 없어 추적을 시작할 수 없습니다.");
            }
        }

        /// <summary>
        /// 순찰 상태로 전환 (외부 호출용)
        /// </summary>
        public void StartPatrolling()
        {
            SetState(AIState.Patrol);
            Debug.Log("[KillerAI] 순찰 시작!");
        }

        /// <summary>
        /// 소리 감지
        /// </summary>
        public void HearNoise(Vector3 noisePosition)
        {
            // 스턴 상태에서는 소리 무시
            if (isStunned) return;

            float distance = Vector3.Distance(transform.position, noisePosition);

            // 앉기 상태 고려한 청각 범위
            float effectiveHearing = GetEffectiveHearingRange();

            if (distance <= effectiveHearing)
            {
                lastNoisePosition = noisePosition;
                lastNoiseTime = Time.time;
                hasNoiseToInvestigate = true;

                Debug.Log($"[KillerAI] 소리 감지! 거리: {distance:F1}m (범위: {effectiveHearing:F1}m)");

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

            // KillerCatchSequence 이벤트 발생 (흔들기 + 빨간화면 + YOU DIED)
            // KillerCatchSequence가 있으면 시퀀스 완료 후 GameOverUI 표시
            // KillerCatchSequence가 없으면 바로 GameOver 호출
            OnPlayerCaught?.Invoke();

            // KillerCatchSequence가 없는 경우에만 직접 GameOver 호출
            var catchSequence = GetComponent<KillerCatchSequence>();
            if (catchSequence == null)
            {
                if (HorrorGameManager.Instance != null)
                {
                    HorrorGameManager.Instance.GameOver("살인마에게 잡혔습니다!");
                }
            }
        }

        /// <summary>
        /// 추적 휴식 후 재추적
        /// </summary>
        private IEnumerator RestAndResumeChase()
        {
            Debug.Log($"[KillerAI] {chaseRestTime}초 동안 휴식 후 재추적 시작");

            // 추적 음악 중지
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // 휴식 시간 대기
            yield return new WaitForSeconds(chaseRestTime);

            // 휴식 종료
            isResting = false;

            // 플레이어가 보이면 다시 추적
            if (CanSeePlayer())
            {
                Debug.Log("[KillerAI] 휴식 종료 - 플레이어 발견, 재추적!");
                lastKnownPosition = player.position;
                SetState(AIState.Chase);
            }
            else
            {
                Debug.Log("[KillerAI] 휴식 종료 - 플레이어 안보임, 순찰 계속");
                // 순찰 상태 유지
            }
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

        /// <summary>
        /// 손전등에 의한 스턴
        /// </summary>
        public void StunByFlashlight()
        {
            if (!canBeStunned) return;
            if (isStunned) return;

            // 쿨다운 체크
            if (Time.time - lastStunTime < stunCooldown) return;

            Debug.Log("[KillerAI] 손전등에 스턴됨!");

            isStunned = true;
            stunEndTime = Time.time + stunDuration;
            lastStunTime = Time.time;

            // 이동 정지
            if (agent != null)
            {
                agent.isStopped = true;
            }

            // 스턴 사운드
            if (stunSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(stunSound);
            }

            // 스턴 이벤트
            OnStunned?.Invoke();

            // 애니메이터에 스턴 알림
            var animator = GetComponent<KillerAnimator>();
            if (animator != null)
            {
                animator.SetStunned(true);
            }
        }

        /// <summary>
        /// 스턴 상태 업데이트
        /// </summary>
        private void UpdateStunned()
        {
            if (Time.time >= stunEndTime)
            {
                EndStun();
            }
        }

        /// <summary>
        /// 스턴 종료
        /// </summary>
        private void EndStun()
        {
            Debug.Log("[KillerAI] 스턴 종료");

            isStunned = false;

            // 이동 재개
            if (agent != null)
            {
                agent.isStopped = false;
            }

            // 애니메이터에 스턴 해제 알림
            var animator = GetComponent<KillerAnimator>();
            if (animator != null)
            {
                animator.SetStunned(false);
            }

            // 어그로 해제 (순찰 상태로 복귀)
            if (loseAggroAfterStun)
            {
                SetState(AIState.Patrol);
                OnPlayerLost?.Invoke();
                Debug.Log("[KillerAI] 어그로 해제, 순찰로 복귀");
            }
            else
            {
                // 어그로 유지 시 마지막 위치 조사
                if (currentState == AIState.Chase)
                {
                    SetState(AIState.Investigate);
                }
            }
        }

        /// <summary>
        /// 플레이어가 앉아있는지 확인
        /// </summary>
        private bool IsPlayerCrouching()
        {
            if (player == null) return false;

            // PC 플레이어 체크
            var pcPlayer = player.GetComponent<PCPlayerController>();
            if (pcPlayer != null)
            {
                return pcPlayer.IsCrouching;
            }

            // VR 플레이어 체크 (머리 높이로 판단)
            var vrPlayer = player.GetComponent<VRPlayer>();
            if (vrPlayer != null)
            {
                return vrPlayer.IsCrouching;
            }

            return false;
        }

        /// <summary>
        /// 현재 감지 거리 반환 (앉기 상태 고려)
        /// </summary>
        public float GetEffectiveViewDistance()
        {
            if (IsPlayerCrouching())
            {
                return viewDistance * crouchDetectionMultiplier;
            }
            return viewDistance;
        }

        /// <summary>
        /// 현재 청각 범위 반환 (앉기 상태 고려)
        /// </summary>
        public float GetEffectiveHearingRange()
        {
            if (IsPlayerCrouching())
            {
                return hearingRange * crouchHearingMultiplier;
            }
            return hearingRange;
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
