using UnityEngine;
using UnityEngine.AI;

namespace HorrorGame
{
    /// <summary>
    /// 살인마 애니메이션 컨트롤러
    /// KillerAI와 함께 사용하여 TG_Hero 캐릭터의 애니메이션을 제어합니다.
    ///
    /// 사용법:
    /// 1. Killer 오브젝트에 이 스크립트 추가
    /// 2. Animator 컴포넌트가 필요함 (자동 탐색됨)
    /// 3. KillerAnimatorController를 Animator에 연결
    /// </summary>
    public class KillerAnimator : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("애니메이터 (자동 탐색됨)")]
        public Animator animator;

        [Tooltip("KillerAI (자동 탐색됨)")]
        public KillerAI killerAI;

        [Tooltip("NavMeshAgent (자동 탐색됨)")]
        public NavMeshAgent agent;

        [Header("Animation Settings")]
        [Tooltip("이동 속도 스무딩")]
        public float speedSmoothTime = 0.1f;

        [Tooltip("걷기/달리기 구분 속도")]
        public float runSpeedThreshold = 3f;

        [Tooltip("회전 스무딩")]
        public float turnSmoothTime = 0.1f;

        [Header("State-Based Animation")]
        [Tooltip("순찰 시 애니메이션 속도 배율")]
        public float patrolAnimSpeed = 0.8f;

        [Tooltip("수색 시 애니메이션 속도 배율")]
        public float searchAnimSpeed = 1.0f;

        [Tooltip("추적 시 애니메이션 속도 배율")]
        public float chaseAnimSpeed = 1.8f;

        [Tooltip("조사 시 애니메이션 속도 배율")]
        public float investigateAnimSpeed = 0.9f;

        [Header("Running Animation")]
        [Tooltip("뛰기 시 보폭 증가 (Root Motion용)")]
        public float runningStrideMultiplier = 1.3f;

        [Tooltip("뛰기 판정 속도 임계값")]
        public float runningThreshold = 3.5f;

        [Header("Root Motion")]
        [Tooltip("Root Motion 사용 여부 - Walk 애니메이션에 Root Motion이 없으면 비활성화")]
        public bool useRootMotion = false;

        [Tooltip("Root Motion 속도 배율")]
        public float rootMotionSpeedMultiplier = 1.0f;

        // 애니메이션 파라미터 해시 (성능 최적화)
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int IsSearchingHash = Animator.StringToHash("IsSearching");
        private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
        private static readonly int AlertTriggerHash = Animator.StringToHash("Alert");
        private static readonly int CatchTriggerHash = Animator.StringToHash("Catch");
        private static readonly int NormalizedSpeedHash = Animator.StringToHash("NormalizedSpeed");

        private float currentSpeed;
        private float speedVelocity;
        private float currentTurnSpeed;
        private float turnVelocity;
        private Vector3 lastPosition;
        private KillerAI.AIState lastState;
        private bool wasMoving;
        private Vector3 rootMotionDelta;
        private bool isRootMotionEnabled;

        private void Awake()
        {
            // 컴포넌트 자동 탐색
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (killerAI == null)
            {
                killerAI = GetComponent<KillerAI>();
            }

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }

        private void Start()
        {
            if (animator == null)
            {
                Debug.LogWarning("[KillerAnimator] Animator를 찾을 수 없습니다!");
            }

            lastPosition = transform.position;

            if (killerAI != null)
            {
                lastState = killerAI.currentState;
                // KillerAI 이벤트 구독
                killerAI.OnPlayerSpotted.AddListener(OnPlayerSpotted);
                killerAI.OnPlayerCaught.AddListener(OnPlayerCaught);
            }

            // Root Motion 설정
            SetupRootMotion();
        }

        /// <summary>
        /// Root Motion 초기 설정
        /// </summary>
        private void SetupRootMotion()
        {
            if (animator != null)
            {
                animator.applyRootMotion = useRootMotion;
                isRootMotionEnabled = useRootMotion;
            }

            if (agent != null)
            {
                if (useRootMotion)
                {
                    // Root Motion 사용 시: NavMeshAgent가 위치를 제어하지 않음
                    agent.updatePosition = false;
                    agent.updateRotation = true;
                    Debug.Log("[KillerAnimator] Root Motion 활성화됨 - NavMeshAgent 위치 제어 비활성화");
                }
                else
                {
                    // Root Motion 미사용 시: NavMeshAgent가 직접 이동 제어
                    agent.updatePosition = true;
                    agent.updateRotation = true;
                    Debug.Log("[KillerAnimator] Root Motion 비활성화됨 - NavMeshAgent가 이동 제어");
                }
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (killerAI != null)
            {
                killerAI.OnPlayerSpotted.RemoveListener(OnPlayerSpotted);
                killerAI.OnPlayerCaught.RemoveListener(OnPlayerCaught);
            }
        }

        private void Update()
        {
            UpdateAnimation();
            UpdateStateBasedSettings();
            SyncAgentPosition();
        }

        /// <summary>
        /// Root Motion 콜백 - 애니메이션에서 이동 데이터를 받아옴
        /// </summary>
        private void OnAnimatorMove()
        {
            if (!isRootMotionEnabled || animator == null) return;

            // 애니메이션의 Root Motion 델타 저장 (Y축 무시 - 공중에 뜨는 것 방지)
            rootMotionDelta = animator.deltaPosition * rootMotionSpeedMultiplier;
            rootMotionDelta.y = 0f; // Y축 이동 제거

            // NavMeshAgent의 목표 속도에 맞게 Root Motion 스케일링
            if (agent != null && agent.enabled && agent.hasPath)
            {
                // NavMeshAgent가 원하는 속도와 Root Motion 속도의 비율
                float desiredSpeed = agent.desiredVelocity.magnitude;
                float animationSpeed = rootMotionDelta.magnitude / Time.deltaTime;

                if (animationSpeed > 0.01f)
                {
                    // 애니메이션 속도를 NavMeshAgent 목표 속도에 맞춤
                    float speedRatio = desiredSpeed / animationSpeed;
                    rootMotionDelta *= speedRatio;
                }

                // Root Motion 방향을 NavMeshAgent 목표 방향으로 조정
                if (agent.desiredVelocity.sqrMagnitude > 0.01f)
                {
                    Vector3 direction = agent.desiredVelocity.normalized;
                    direction.y = 0f; // 수평 방향만
                    float magnitude = rootMotionDelta.magnitude;
                    rootMotionDelta = direction * magnitude;
                }
            }

            // 위치 적용 (XZ만)
            transform.position += rootMotionDelta;
        }

        /// <summary>
        /// NavMeshAgent 위치를 실제 Transform 위치와 동기화
        /// </summary>
        private void SyncAgentPosition()
        {
            if (!isRootMotionEnabled || agent == null || !agent.enabled) return;

            // NavMesh 위에 위치 고정 (공중에 뜨는 것 방지)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
            {
                // Y 위치만 NavMesh에 맞춤
                Vector3 correctedPos = transform.position;
                correctedPos.y = hit.position.y;
                transform.position = correctedPos;
            }

            // NavMeshAgent의 내부 위치를 실제 Transform 위치로 동기화
            agent.nextPosition = transform.position;
        }

        /// <summary>
        /// 애니메이션 파라미터 업데이트
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator == null) return;

            // 이동 속도 계산 (스무딩 적용)
            float targetSpeed = 0f;
            if (agent != null && agent.enabled)
            {
                targetSpeed = agent.velocity.magnitude;
            }
            else
            {
                // NavMeshAgent가 없으면 위치 변화로 속도 계산
                float deltaDistance = Vector3.Distance(transform.position, lastPosition);
                targetSpeed = deltaDistance / Time.deltaTime;
                lastPosition = transform.position;
            }

            currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, speedSmoothTime);

            // Speed 파라미터 설정
            SetFloatSafe(SpeedHash, currentSpeed);

            // 정규화된 속도 계산 - 걷기 속도(patrolSpeed) 기준으로 정규화
            // patrolSpeed에서 NormalizedSpeed = 0.4 (BlendTree의 Walk 위치)
            // chaseSpeed에서 NormalizedSpeed = 1.0 (BlendTree의 Run 위치)
            float patrolSpeed = killerAI != null ? killerAI.patrolSpeed : 2f;
            float chaseSpeed = killerAI != null ? killerAI.chaseSpeed : 4.5f;

            float normalizedSpeed;
            if (currentSpeed <= patrolSpeed)
            {
                // 0 ~ patrolSpeed → 0 ~ 0.4
                normalizedSpeed = (currentSpeed / patrolSpeed) * 0.4f;
            }
            else
            {
                // patrolSpeed ~ chaseSpeed → 0.4 ~ 1.0
                float t = (currentSpeed - patrolSpeed) / (chaseSpeed - patrolSpeed);
                normalizedSpeed = 0.4f + t * 0.6f;
            }
            normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

            SetFloatSafe(NormalizedSpeedHash, normalizedSpeed);

            // 이동 상태 파라미터 설정
            bool isMoving = currentSpeed > 0.1f;
            bool isRunning = currentSpeed > runSpeedThreshold;
            bool isChasing = killerAI != null && killerAI.currentState == KillerAI.AIState.Chase;

            SetBoolSafe(IsWalkingHash, isMoving && !isRunning);
            SetBoolSafe(IsRunningHash, isRunning || isChasing);

            // 이동 시작/정지 감지
            if (isMoving != wasMoving)
            {
                OnMovementStateChanged(isMoving);
                wasMoving = isMoving;
            }

            // AI 상태에 따른 추가 파라미터
            UpdateAIStateParameters();
        }

        /// <summary>
        /// AI 상태에 따른 파라미터 업데이트
        /// </summary>
        private void UpdateAIStateParameters()
        {
            if (killerAI == null) return;

            var currentState = killerAI.currentState;

            // 추적 상태
            bool isChasing = currentState == KillerAI.AIState.Chase;
            SetBoolSafe(IsChasingHash, isChasing);

            // 수색 상태
            bool isSearching = currentState == KillerAI.AIState.Search ||
                               currentState == KillerAI.AIState.Investigate;
            SetBoolSafe(IsSearchingHash, isSearching);

            // 상태 변경 감지
            if (currentState != lastState)
            {
                OnAIStateChanged(lastState, currentState);
                lastState = currentState;
            }
        }

        /// <summary>
        /// 상태 기반 애니메이션 설정 업데이트
        /// </summary>
        private void UpdateStateBasedSettings()
        {
            if (animator == null || killerAI == null) return;

            // 상태에 따른 애니메이션 속도 조절
            float targetAnimSpeed = 1f;
            switch (killerAI.currentState)
            {
                case KillerAI.AIState.Patrol:
                    targetAnimSpeed = patrolAnimSpeed;
                    break;
                case KillerAI.AIState.Search:
                    targetAnimSpeed = searchAnimSpeed;
                    break;
                case KillerAI.AIState.Chase:
                    targetAnimSpeed = chaseAnimSpeed;
                    break;
                case KillerAI.AIState.Investigate:
                    targetAnimSpeed = investigateAnimSpeed;
                    break;
            }

            // 이동 중이 아니면 기본 속도
            if (currentSpeed < 0.1f)
            {
                targetAnimSpeed = 1f;
            }

            animator.speed = Mathf.Lerp(animator.speed, targetAnimSpeed, Time.deltaTime * 5f);
        }

        /// <summary>
        /// AI 상태 변경 시 호출
        /// </summary>
        private void OnAIStateChanged(KillerAI.AIState from, KillerAI.AIState to)
        {
            Debug.Log($"[KillerAnimator] 상태 변경: {from} → {to}");

            // 추적 시작 시 Alert 트리거
            if (to == KillerAI.AIState.Chase && from != KillerAI.AIState.Chase)
            {
                TriggerAnimationSafe(AlertTriggerHash);
            }
        }

        /// <summary>
        /// 이동 상태 변경 시 호출
        /// </summary>
        private void OnMovementStateChanged(bool isMoving)
        {
            // 필요시 이동 시작/정지 애니메이션 처리
        }

        /// <summary>
        /// 플레이어 발견 시 이벤트 핸들러
        /// </summary>
        private void OnPlayerSpotted()
        {
            TriggerAnimationSafe(AlertTriggerHash);
        }

        /// <summary>
        /// 플레이어 잡았을 때 이벤트 핸들러
        /// </summary>
        private void OnPlayerCaught()
        {
            TriggerAttack();
        }

        /// <summary>
        /// Float 파라미터 안전하게 설정
        /// </summary>
        private void SetFloatSafe(int hash, float value)
        {
            if (animator == null) return;
            animator.SetFloat(hash, value);
        }

        /// <summary>
        /// Bool 파라미터 안전하게 설정
        /// </summary>
        private void SetBoolSafe(int hash, bool value)
        {
            if (animator == null) return;
            animator.SetBool(hash, value);
        }

        /// <summary>
        /// 트리거 파라미터 안전하게 설정
        /// </summary>
        private void TriggerAnimationSafe(int hash)
        {
            if (animator == null) return;
            animator.SetTrigger(hash);
        }

        /// <summary>
        /// 공격 애니메이션 트리거
        /// </summary>
        public void TriggerAttack()
        {
            TriggerAnimationSafe(AttackTriggerHash);
        }

        /// <summary>
        /// 잡기 애니메이션 트리거
        /// </summary>
        public void TriggerCatch()
        {
            TriggerAnimationSafe(CatchTriggerHash);
            Debug.Log("[KillerAnimator] 잡기 애니메이션 트리거");
        }

        /// <summary>
        /// 특정 애니메이션 트리거
        /// </summary>
        public void TriggerAnimation(string triggerName)
        {
            if (animator == null) return;
            animator.SetTrigger(triggerName);
        }

        /// <summary>
        /// 애니메이션 속도 배율 설정
        /// </summary>
        public void SetAnimationSpeed(float speed)
        {
            if (animator == null) return;
            animator.speed = speed;
        }

        /// <summary>
        /// Root Motion 활성화/비활성화
        /// </summary>
        public void SetRootMotion(bool enabled)
        {
            useRootMotion = enabled;
            isRootMotionEnabled = enabled;

            if (animator != null)
            {
                animator.applyRootMotion = enabled;
            }

            if (agent != null)
            {
                agent.updatePosition = !enabled;
            }

            Debug.Log($"[KillerAnimator] Root Motion {(enabled ? "활성화" : "비활성화")}");
        }

        /// <summary>
        /// 현재 애니메이션 상태 정보
        /// </summary>
        public AnimatorStateInfo GetCurrentStateInfo(int layerIndex = 0)
        {
            if (animator == null) return default;
            return animator.GetCurrentAnimatorStateInfo(layerIndex);
        }

        /// <summary>
        /// 특정 애니메이션이 재생 중인지 확인
        /// </summary>
        public bool IsPlayingAnimation(string stateName, int layerIndex = 0)
        {
            if (animator == null) return false;
            return animator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 컴포넌트 자동 탐색
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                    animator = GetComponentInChildren<Animator>();
            }

            if (killerAI == null)
            {
                killerAI = GetComponent<KillerAI>();
            }

            if (agent == null)
            {
                agent = GetComponent<NavMeshAgent>();
            }
        }
#endif
    }
}
