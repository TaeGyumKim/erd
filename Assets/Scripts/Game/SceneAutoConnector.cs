using UnityEngine;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 씬 오브젝트들을 자동으로 연결해주는 초기화 스크립트
    /// KillerAI의 순찰 지점, GhostAI의 힌트 타겟을 자동 연결
    /// </summary>
    public class SceneAutoConnector : MonoBehaviour
    {
        [Header("Auto Connect Settings")]
        [Tooltip("자동 연결 활성화")]
        public bool autoConnectOnStart = true;

        [Header("References (선택적 - 자동 탐색됨)")]
        public KillerAI killer;
        public GhostAI ghost;

        private void Start()
        {
            if (autoConnectOnStart)
            {
                ConnectAll();
            }
        }

        /// <summary>
        /// 모든 연결 수행
        /// </summary>
        public void ConnectAll()
        {
            ConnectKillerPatrolPoints();
            ConnectGhostHintTargets();
        }

        /// <summary>
        /// KillerAI에 순찰 지점 연결
        /// </summary>
        public void ConnectKillerPatrolPoints()
        {
            // Killer 찾기
            if (killer == null)
            {
                killer = FindObjectOfType<KillerAI>();
            }

            if (killer == null)
            {
                Debug.LogWarning("[SceneAutoConnector] KillerAI를 찾을 수 없습니다");
                return;
            }

            // 이미 할당되어 있으면 스킵
            if (killer.patrolPoints != null && killer.patrolPoints.Length > 0)
            {
                Debug.Log("[SceneAutoConnector] KillerAI 순찰 지점이 이미 할당되어 있습니다");
                return;
            }

            // 순찰 지점 부모 찾기
            var patrolParent = GameObject.Find("--- PATROL POINTS ---");
            if (patrolParent == null)
            {
                Debug.LogWarning("[SceneAutoConnector] '--- PATROL POINTS ---' 오브젝트를 찾을 수 없습니다");
                return;
            }

            // 순찰 지점 수집
            var points = new List<Transform>();
            foreach (Transform child in patrolParent.transform)
            {
                if (child.name.StartsWith("PatrolPoint"))
                {
                    points.Add(child);
                }
            }

            if (points.Count > 0)
            {
                killer.patrolPoints = points.ToArray();
                Debug.Log($"[SceneAutoConnector] KillerAI에 {points.Count}개의 순찰 지점 연결 완료");
            }
        }

        /// <summary>
        /// GhostAI에 힌트 타겟 연결
        /// </summary>
        public void ConnectGhostHintTargets()
        {
            // Ghost 찾기
            if (ghost == null)
            {
                ghost = FindObjectOfType<GhostAI>();
            }

            if (ghost == null)
            {
                Debug.LogWarning("[SceneAutoConnector] GhostAI를 찾을 수 없습니다");
                return;
            }

            // 이미 할당되어 있으면 스킵
            if (ghost.hintTargets != null && ghost.hintTargets.Length > 0)
            {
                Debug.Log("[SceneAutoConnector] GhostAI 힌트 타겟이 이미 할당되어 있습니다");
                return;
            }

            // 힌트 지점 부모 찾기
            var hintParent = GameObject.Find("--- GHOST HINT POINTS ---");
            if (hintParent == null)
            {
                Debug.LogWarning("[SceneAutoConnector] '--- GHOST HINT POINTS ---' 오브젝트를 찾을 수 없습니다");
                return;
            }

            // 힌트 지점 수집
            var targets = new List<Transform>();
            foreach (Transform child in hintParent.transform)
            {
                if (child.name.StartsWith("HintPoint"))
                {
                    targets.Add(child);
                }
            }

            if (targets.Count > 0)
            {
                ghost.hintTargets = targets.ToArray();
                Debug.Log($"[SceneAutoConnector] GhostAI에 {targets.Count}개의 힌트 타겟 연결 완료");
            }
        }
    }
}
