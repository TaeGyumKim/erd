using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace HorrorGame
{
    /// <summary>
    /// 체크포인트 저장 시스템
    /// 플레이어 진행 상황 저장/불러오기
    ///
    /// 사용법:
    /// 1. 게임 매니저에 추가
    /// 2. 체크포인트 위치에 Checkpoint 트리거 배치
    /// </summary>
    public class CheckpointSystem : MonoBehaviour
    {
        public static CheckpointSystem Instance { get; private set; }

        [Header("Settings")]
        [Tooltip("자동 저장 활성화")]
        public bool autoSaveEnabled = true;

        [Tooltip("자동 저장 간격 (초)")]
        public float autoSaveInterval = 60f;

        [Tooltip("저장 슬롯 이름")]
        public string saveSlotName = "HorrorGame_Save";

        [Header("Save Data")]
        public GameSaveData currentSaveData;

        [Header("Events")]
        public UnityEvent OnCheckpointReached;
        public UnityEvent OnGameSaved;
        public UnityEvent OnGameLoaded;

        [System.Serializable]
        public class GameSaveData
        {
            public string checkpointId;
            public Vector3 playerPosition;
            public Quaternion playerRotation;

            // 플레이어 상태
            public float stamina;
            public float flashlightBattery;
            public List<string> collectedKeys = new List<string>();
            public List<string> collectedItems = new List<string>();

            // 게임 상태
            public int keysCollected;
            public float playTime;
            public List<string> completedObjectives = new List<string>();
            public List<string> readNotes = new List<string>();

            // 월드 상태
            public List<string> openedDoors = new List<string>();
            public List<string> usedItems = new List<string>();

            // 메타 데이터
            public string saveTime;
            public int saveVersion = 1;
        }

        private float autoSaveTimer;
        private string lastCheckpointId;

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

            currentSaveData = new GameSaveData();
        }

        private void Start()
        {
            // 저장된 게임 불러오기 시도
            if (HasSaveData())
            {
                // 자동 로드는 선택적
                // LoadGame();
            }
        }

        private void Update()
        {
            if (autoSaveEnabled)
            {
                autoSaveTimer += Time.deltaTime;
                if (autoSaveTimer >= autoSaveInterval)
                {
                    AutoSave();
                    autoSaveTimer = 0;
                }
            }
        }

        /// <summary>
        /// 체크포인트 도달
        /// </summary>
        public void ReachCheckpoint(string checkpointId, Vector3 position, Quaternion rotation)
        {
            if (checkpointId == lastCheckpointId) return;

            lastCheckpointId = checkpointId;
            currentSaveData.checkpointId = checkpointId;
            currentSaveData.playerPosition = position;
            currentSaveData.playerRotation = rotation;

            SaveGame();
            OnCheckpointReached?.Invoke();

            Debug.Log($"[CheckpointSystem] 체크포인트 도달: {checkpointId}");

            // UI 알림
            if (VRHUD.Instance != null)
            {
                VRHUD.Instance.ShowStatus("체크포인트 저장됨", 2f);
            }
        }

        /// <summary>
        /// 게임 저장
        /// </summary>
        public void SaveGame()
        {
            CollectCurrentGameState();

            currentSaveData.saveTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string json = JsonUtility.ToJson(currentSaveData);
            PlayerPrefs.SetString(saveSlotName, json);
            PlayerPrefs.Save();

            OnGameSaved?.Invoke();
            Debug.Log("[CheckpointSystem] 게임 저장됨");
        }

        /// <summary>
        /// 현재 게임 상태 수집
        /// </summary>
        private void CollectCurrentGameState()
        {
            // 플레이어 상태
            if (VRPlayer.Instance != null)
            {
                currentSaveData.playerPosition = VRPlayer.Instance.transform.position;
                currentSaveData.playerRotation = VRPlayer.Instance.transform.rotation;
                currentSaveData.stamina = VRPlayer.Instance.currentStamina;
            }

            // 손전등 배터리
            var flashlight = FindObjectOfType<VRFlashlight>();
            if (flashlight != null)
            {
                currentSaveData.flashlightBattery = flashlight.BatteryPercent;
            }

            // 인벤토리
            if (PlayerInventory.Instance != null)
            {
                currentSaveData.collectedKeys = new List<string>(PlayerInventory.Instance.keys);
                currentSaveData.keysCollected = currentSaveData.collectedKeys.Count;
            }

            // 게임 매니저
            if (HorrorGameManager.Instance != null)
            {
                currentSaveData.keysCollected = HorrorGameManager.Instance.collectedKeys;
            }

            // 목표 시스템
            if (ObjectiveSystem.Instance != null)
            {
                currentSaveData.completedObjectives.Clear();
                foreach (var obj in ObjectiveSystem.Instance.objectives)
                {
                    if (obj.state == ObjectiveSystem.ObjectiveState.Completed)
                    {
                        currentSaveData.completedObjectives.Add(obj.objectiveId);
                    }
                }
            }

            // 열린 문들
            currentSaveData.openedDoors.Clear();
            var doors = FindObjectsOfType<Door>();
            foreach (var door in doors)
            {
                if (door.IsOpen)
                {
                    currentSaveData.openedDoors.Add(door.gameObject.name);
                }
            }
        }

        /// <summary>
        /// 게임 불러오기
        /// </summary>
        public void LoadGame()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning("[CheckpointSystem] 저장된 데이터 없음");
                return;
            }

            string json = PlayerPrefs.GetString(saveSlotName);
            currentSaveData = JsonUtility.FromJson<GameSaveData>(json);

            ApplyLoadedGameState();

            OnGameLoaded?.Invoke();
            Debug.Log("[CheckpointSystem] 게임 불러오기 완료");
        }

        /// <summary>
        /// 불러온 상태 적용
        /// </summary>
        private void ApplyLoadedGameState()
        {
            // 플레이어 위치
            if (VRPlayer.Instance != null)
            {
                VRPlayer.Instance.transform.position = currentSaveData.playerPosition;
                VRPlayer.Instance.transform.rotation = currentSaveData.playerRotation;
                VRPlayer.Instance.currentStamina = currentSaveData.stamina;
            }

            // 손전등 배터리
            var flashlight = FindObjectOfType<VRFlashlight>();
            if (flashlight != null)
            {
                flashlight.currentBattery = currentSaveData.flashlightBattery * flashlight.maxBattery;
            }

            // 인벤토리 복원
            if (PlayerInventory.Instance != null)
            {
                foreach (var keyId in currentSaveData.collectedKeys)
                {
                    PlayerInventory.Instance.AddKey(keyId);
                }
            }

            // 게임 매니저 복원
            if (HorrorGameManager.Instance != null)
            {
                HorrorGameManager.Instance.collectedKeys = currentSaveData.keysCollected;
            }

            // 목표 복원
            if (ObjectiveSystem.Instance != null)
            {
                foreach (var objId in currentSaveData.completedObjectives)
                {
                    ObjectiveSystem.Instance.CompleteObjective(objId);
                }
            }

            // 문 상태 복원
            var doors = FindObjectsOfType<Door>();
            foreach (var door in doors)
            {
                if (currentSaveData.openedDoors.Contains(door.gameObject.name))
                {
                    door.OpenDoor();
                }
            }
        }

        /// <summary>
        /// 저장 데이터 존재 여부
        /// </summary>
        public bool HasSaveData()
        {
            return PlayerPrefs.HasKey(saveSlotName);
        }

        /// <summary>
        /// 저장 데이터 삭제
        /// </summary>
        public void DeleteSaveData()
        {
            PlayerPrefs.DeleteKey(saveSlotName);
            PlayerPrefs.Save();
            currentSaveData = new GameSaveData();
            Debug.Log("[CheckpointSystem] 저장 데이터 삭제됨");
        }

        /// <summary>
        /// 자동 저장
        /// </summary>
        private void AutoSave()
        {
            if (HorrorGameManager.Instance != null &&
                HorrorGameManager.Instance.currentState != HorrorGameManager.GameState.Playing)
            {
                return; // 게임 중이 아니면 저장 안 함
            }

            SaveGame();
            Debug.Log("[CheckpointSystem] 자동 저장됨");
        }

        /// <summary>
        /// 마지막 체크포인트로 복귀
        /// </summary>
        public void RespawnAtLastCheckpoint()
        {
            if (string.IsNullOrEmpty(currentSaveData.checkpointId))
            {
                Debug.LogWarning("[CheckpointSystem] 체크포인트 없음");
                return;
            }

            // 플레이어 위치 복원
            if (VRPlayer.Instance != null)
            {
                VRPlayer.Instance.transform.position = currentSaveData.playerPosition;
                VRPlayer.Instance.transform.rotation = currentSaveData.playerRotation;
            }

            Debug.Log("[CheckpointSystem] 마지막 체크포인트로 복귀");
        }
    }

    /// <summary>
    /// 체크포인트 트리거
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("체크포인트 ID")]
        public string checkpointId;

        [Tooltip("리스폰 위치 (비워두면 이 오브젝트 위치)")]
        public Transform respawnPoint;

        [Tooltip("한 번만 활성화")]
        public bool activateOnce = true;

        private bool activated;

        private void OnTriggerEnter(Collider other)
        {
            if (activated && activateOnce) return;

            if (other.GetComponent<VRPlayer>() != null)
            {
                Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : transform.position;
                Quaternion spawnRot = respawnPoint != null ? respawnPoint.rotation : transform.rotation;

                CheckpointSystem.Instance?.ReachCheckpoint(checkpointId, spawnPos, spawnRot);
                activated = true;
            }
        }
    }
}
