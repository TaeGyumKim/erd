using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 목표/퀘스트 시스템
    /// 게임 목표 추적 및 UI 표시
    ///
    /// 사용법:
    /// 1. 게임 매니저에 추가
    /// 2. 목표 추가 후 CompleteObjective()로 완료 처리
    /// </summary>
    public class ObjectiveSystem : MonoBehaviour
    {
        public static ObjectiveSystem Instance { get; private set; }

        [Header("Objectives")]
        public List<Objective> objectives = new List<Objective>();

        [Header("Settings")]
        [Tooltip("완료된 목표 자동 숨김")]
        public bool hideCompletedObjectives = false;

        [Tooltip("새 목표 알림 시간")]
        public float newObjectiveNotifyDuration = 3f;

        [Header("Events")]
        public UnityEvent<Objective> OnObjectiveAdded;
        public UnityEvent<Objective> OnObjectiveCompleted;
        public UnityEvent<Objective> OnObjectiveFailed;
        public UnityEvent OnAllObjectivesCompleted;

        [System.Serializable]
        public class Objective
        {
            public string objectiveId;
            public string title;
            [TextArea(2, 4)]
            public string description;
            public ObjectiveState state = ObjectiveState.Inactive;
            public bool isOptional = false;
            public bool isHidden = false;

            [Header("Progress (Optional)")]
            public int currentProgress = 0;
            public int targetProgress = 1;

            [Header("Audio")]
            public AudioClip completionSound;
        }

        public enum ObjectiveState
        {
            Inactive,   // 아직 시작 안 함
            Active,     // 진행 중
            Completed,  // 완료
            Failed      // 실패
        }

        private AudioSource audioSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// 새 목표 추가
        /// </summary>
        public void AddObjective(string id, string title, string description, bool isOptional = false, int targetProgress = 1)
        {
            // 이미 있으면 무시
            if (GetObjective(id) != null) return;

            var objective = new Objective
            {
                objectiveId = id,
                title = title,
                description = description,
                state = ObjectiveState.Active,
                isOptional = isOptional,
                targetProgress = targetProgress
            };

            objectives.Add(objective);
            OnObjectiveAdded?.Invoke(objective);

            Debug.Log($"[ObjectiveSystem] 새 목표: {title}");

            // UI에 알림
            NotifyNewObjective(objective);
        }

        /// <summary>
        /// 목표 활성화
        /// </summary>
        public void ActivateObjective(string id)
        {
            var objective = GetObjective(id);
            if (objective != null && objective.state == ObjectiveState.Inactive)
            {
                objective.state = ObjectiveState.Active;
                OnObjectiveAdded?.Invoke(objective);
                NotifyNewObjective(objective);
            }
        }

        /// <summary>
        /// 목표 완료
        /// </summary>
        public void CompleteObjective(string id)
        {
            var objective = GetObjective(id);
            if (objective == null) return;
            if (objective.state == ObjectiveState.Completed) return;

            objective.state = ObjectiveState.Completed;
            objective.currentProgress = objective.targetProgress;

            // 사운드
            if (objective.completionSound != null)
            {
                audioSource.PlayOneShot(objective.completionSound);
            }

            OnObjectiveCompleted?.Invoke(objective);
            Debug.Log($"[ObjectiveSystem] 목표 완료: {objective.title}");

            // 모든 필수 목표 완료 체크
            if (AreAllRequiredObjectivesCompleted())
            {
                OnAllObjectivesCompleted?.Invoke();
                Debug.Log("[ObjectiveSystem] 모든 필수 목표 완료!");
            }
        }

        /// <summary>
        /// 목표 진행도 업데이트
        /// </summary>
        public void UpdateObjectiveProgress(string id, int progress)
        {
            var objective = GetObjective(id);
            if (objective == null) return;
            if (objective.state != ObjectiveState.Active) return;

            objective.currentProgress = Mathf.Min(progress, objective.targetProgress);

            // 목표 달성 시 완료
            if (objective.currentProgress >= objective.targetProgress)
            {
                CompleteObjective(id);
            }
        }

        /// <summary>
        /// 목표 진행도 증가
        /// </summary>
        public void IncrementObjectiveProgress(string id, int amount = 1)
        {
            var objective = GetObjective(id);
            if (objective == null) return;

            UpdateObjectiveProgress(id, objective.currentProgress + amount);
        }

        /// <summary>
        /// 목표 실패
        /// </summary>
        public void FailObjective(string id)
        {
            var objective = GetObjective(id);
            if (objective == null) return;

            objective.state = ObjectiveState.Failed;
            OnObjectiveFailed?.Invoke(objective);

            Debug.Log($"[ObjectiveSystem] 목표 실패: {objective.title}");
        }

        /// <summary>
        /// 목표 가져오기
        /// </summary>
        public Objective GetObjective(string id)
        {
            return objectives.Find(o => o.objectiveId == id);
        }

        /// <summary>
        /// 활성 목표들 가져오기
        /// </summary>
        public List<Objective> GetActiveObjectives()
        {
            return objectives.FindAll(o => o.state == ObjectiveState.Active && !o.isHidden);
        }

        /// <summary>
        /// 모든 필수 목표 완료 여부
        /// </summary>
        public bool AreAllRequiredObjectivesCompleted()
        {
            foreach (var obj in objectives)
            {
                if (!obj.isOptional && obj.state == ObjectiveState.Active)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 목표 상태 확인
        /// </summary>
        public bool IsObjectiveCompleted(string id)
        {
            var objective = GetObjective(id);
            return objective != null && objective.state == ObjectiveState.Completed;
        }

        /// <summary>
        /// 목표 진행률 가져오기 (0~1)
        /// </summary>
        public float GetObjectiveProgress(string id)
        {
            var objective = GetObjective(id);
            if (objective == null) return 0;

            return (float)objective.currentProgress / objective.targetProgress;
        }

        private void NotifyNewObjective(Objective objective)
        {
            // VRHUD에 알림
            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus($"새 목표: {objective.title}", newObjectiveNotifyDuration);
            }
        }

        /// <summary>
        /// 목표 초기화 (게임 재시작 시)
        /// </summary>
        public void ResetAllObjectives()
        {
            foreach (var obj in objectives)
            {
                obj.state = ObjectiveState.Inactive;
                obj.currentProgress = 0;
            }
        }

        /// <summary>
        /// 기본 공포 게임 목표 설정
        /// </summary>
        public void SetupDefaultHorrorObjectives(int keyCount)
        {
            AddObjective("find_keys", $"열쇠 찾기 (0/{keyCount})", "탈출에 필요한 열쇠를 모두 찾으세요", false, keyCount);
            AddObjective("escape", "탈출하기", "출구를 찾아 탈출하세요");
            AddObjective("survive", "생존하기", "살인마에게 잡히지 마세요");

            // 선택 목표
            AddObjective("find_notes", "메모 수집", "숨겨진 메모들을 찾으세요", true, 5);
            AddObjective("no_flashlight", "어둠 속에서", "손전등 없이 탈출하세요", true);
        }
    }
}
