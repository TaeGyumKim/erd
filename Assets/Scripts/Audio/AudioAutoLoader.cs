using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HorrorGame
{
    /// <summary>
    /// 게임 시작 시 자동으로 사운드를 로드하고 할당하는 컴포넌트
    /// Resources 폴더 또는 에셋 경로에서 사운드를 찾아 할당
    /// </summary>
    public class AudioAutoLoader : MonoBehaviour
    {
        [Header("Auto Load Settings")]
        [Tooltip("시작 시 자동으로 사운드 로드")]
        public bool autoLoadOnStart = true;

        [Tooltip("발소리 로드")]
        public bool loadFootsteps = true;

        [Tooltip("로드된 발소리 수")]
        [SerializeField] private int loadedFootstepsCount;

        private void Start()
        {
            if (autoLoadOnStart)
            {
                LoadAndAssignSounds();
            }
        }

        /// <summary>
        /// 사운드 로드 및 할당
        /// </summary>
        public void LoadAndAssignSounds()
        {
            Debug.Log("[AudioAutoLoader] 사운드 자동 로드 시작...");

            if (loadFootsteps)
            {
                LoadFootstepSounds();
            }

            LoadHorrorSounds();
            LoadAmbientSounds();

            Debug.Log("[AudioAutoLoader] 사운드 자동 로드 완료!");
        }

        private void LoadFootstepSounds()
        {
            var soundManager = HorrorSoundManager.Instance;
            if (soundManager == null)
            {
                Debug.LogWarning("[AudioAutoLoader] HorrorSoundManager가 없습니다.");
                return;
            }

            // 발소리 경로들
            string[] footstepPaths = new string[]
            {
                "Footstep(Concrete & Wood)/Footstep  One Shots/concrete",
                "Footstep(Concrete & Wood)/Footstep  One Shots/wood"
            };

            string[] runPaths = new string[]
            {
                "Footstep(Concrete & Wood)/Footstep Loops/concrete/running",
                "Footstep(Concrete & Wood)/Footstep Loops/wood/running"
            };

            string[] slowPaths = new string[]
            {
                "Footstep(Concrete & Wood)/Footstep Loops/concrete/slowsteps",
                "Footstep(Concrete & Wood)/Footstep Loops/wood/slowsteps"
            };

            // 걷기 발소리
            if (soundManager.footstepSounds == null || soundManager.footstepSounds.Length == 0)
            {
                var footsteps = LoadAudioClipsFromPaths(footstepPaths);
                if (footsteps.Count > 0)
                {
                    soundManager.footstepSounds = footsteps.Take(15).ToArray();
                    Debug.Log($"[AudioAutoLoader] 걷기 발소리 {soundManager.footstepSounds.Length}개 로드");
                }
            }

            // 달리기 발소리
            if (soundManager.runFootstepSounds == null || soundManager.runFootstepSounds.Length == 0)
            {
                var runFootsteps = LoadAudioClipsFromPaths(runPaths);
                if (runFootsteps.Count > 0)
                {
                    soundManager.runFootstepSounds = runFootsteps.ToArray();
                    Debug.Log($"[AudioAutoLoader] 달리기 발소리 {soundManager.runFootstepSounds.Length}개 로드");
                }
            }

            // 웅크리기 발소리
            if (soundManager.crouchFootstepSounds == null || soundManager.crouchFootstepSounds.Length == 0)
            {
                var slowFootsteps = LoadAudioClipsFromPaths(slowPaths);
                if (slowFootsteps.Count > 0)
                {
                    soundManager.crouchFootstepSounds = slowFootsteps.ToArray();
                    Debug.Log($"[AudioAutoLoader] 웅크리기 발소리 {soundManager.crouchFootstepSounds.Length}개 로드");
                }
            }

            loadedFootstepsCount = (soundManager.footstepSounds?.Length ?? 0) +
                                   (soundManager.runFootstepSounds?.Length ?? 0) +
                                   (soundManager.crouchFootstepSounds?.Length ?? 0);

            // KillerFootstep에도 할당
            AssignKillerFootsteps();
        }

        private void AssignKillerFootsteps()
        {
            var killerFootsteps = FindObjectsOfType<KillerFootstep>();
            foreach (var kf in killerFootsteps)
            {
                if (kf.walkFootsteps == null || kf.walkFootsteps.Length == 0)
                {
                    var footsteps = LoadAudioClipsFromPaths(new[] { "Footstep(Concrete & Wood)/Footstep  One Shots/concrete" });
                    if (footsteps.Count > 0)
                    {
                        kf.walkFootsteps = footsteps.Take(5).ToArray();
                    }
                }

                if (kf.runFootsteps == null || kf.runFootsteps.Length == 0)
                {
                    var runFootsteps = LoadAudioClipsFromPaths(new[] { "Footstep(Concrete & Wood)/Footstep Loops/concrete/running" });
                    if (runFootsteps.Count > 0)
                    {
                        kf.runFootsteps = runFootsteps.ToArray();
                    }
                }

                Debug.Log($"[AudioAutoLoader] {kf.gameObject.name} 발소리 할당 완료");
            }
        }

        private void LoadHorrorSounds()
        {
            var soundManager = HorrorSoundManager.Instance;
            if (soundManager == null) return;

            // Resources 폴더에서 호러 사운드 로드 시도
            string[] horrorPaths = new string[]
            {
                "Audio/Horror",
                "Sounds/Horror",
                "Free Sound Effects Pack"
            };

            // 심장박동
            if (soundManager.heartbeatSound == null)
            {
                soundManager.heartbeatSound = FindAudioClipByName("heartbeat", "heart", "beat", "pulse");
                if (soundManager.heartbeatSound != null)
                {
                    Debug.Log($"[AudioAutoLoader] 심장박동 사운드 로드: {soundManager.heartbeatSound.name}");
                }
            }

            // 점프스케어
            if (soundManager.jumpScareSound == null)
            {
                soundManager.jumpScareSound = FindAudioClipByName("scare", "jump", "stinger", "shock");
                if (soundManager.jumpScareSound != null)
                {
                    Debug.Log($"[AudioAutoLoader] 점프스케어 사운드 로드: {soundManager.jumpScareSound.name}");
                }
            }
        }

        private void LoadAmbientSounds()
        {
            var soundManager = HorrorSoundManager.Instance;
            if (soundManager == null) return;

            // Free Horror Starter Pack에서 배경음 로드
            string[] ambientPaths = new string[]
            {
                "Free Horror Starter Pack",
                "Audio/Ambient",
                "Audio/Music"
            };

            var ambientClips = LoadAudioClipsFromPaths(ambientPaths);

            foreach (var clip in ambientClips)
            {
                string name = clip.name.ToLower();

                // 평상시 배경음
                if (soundManager.normalAmbient == null &&
                    (name.Contains("ambient") || name.Contains("calm") || name.Contains("peaceful") || name.Contains("atmosphere")))
                {
                    soundManager.normalAmbient = clip;
                    Debug.Log($"[AudioAutoLoader] 평상시 배경음: {clip.name}");
                }
                // 긴장 배경음
                else if (soundManager.tensionAmbient == null &&
                    (name.Contains("tension") || name.Contains("suspense") || name.Contains("tense") || name.Contains("scary")))
                {
                    soundManager.tensionAmbient = clip;
                    Debug.Log($"[AudioAutoLoader] 긴장 배경음: {clip.name}");
                }
                // 추격 배경음
                else if (soundManager.chaseAmbient == null &&
                    (name.Contains("chase") || name.Contains("danger") || name.Contains("action") || name.Contains("intense")))
                {
                    soundManager.chaseAmbient = clip;
                    Debug.Log($"[AudioAutoLoader] 추격 배경음: {clip.name}");
                }
            }
        }

        private List<AudioClip> LoadAudioClipsFromPaths(string[] relativePaths)
        {
            List<AudioClip> clips = new List<AudioClip>();

#if UNITY_EDITOR
            foreach (string relativePath in relativePaths)
            {
                string fullPath = $"Assets/{relativePath}";
                if (!System.IO.Directory.Exists(fullPath)) continue;

                string[] extensions = new[] { "*.wav", "*.mp3", "*.ogg", "*.aiff" };

                foreach (var ext in extensions)
                {
                    try
                    {
                        string[] files = System.IO.Directory.GetFiles(fullPath, ext, System.IO.SearchOption.AllDirectories);
                        foreach (string file in files)
                        {
                            string assetPath = file.Replace("\\", "/");
                            AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                            if (clip != null && !clips.Contains(clip))
                            {
                                clips.Add(clip);
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[AudioAutoLoader] 경로 검색 실패: {fullPath} - {e.Message}");
                    }
                }
            }
#endif

            // Resources 폴더에서도 로드 시도
            foreach (string path in relativePaths)
            {
                var resourceClips = Resources.LoadAll<AudioClip>(path);
                foreach (var clip in resourceClips)
                {
                    if (!clips.Contains(clip))
                    {
                        clips.Add(clip);
                    }
                }
            }

            return clips;
        }

        private AudioClip FindAudioClipByName(params string[] keywords)
        {
#if UNITY_EDITOR
            // 프로젝트 전체에서 AudioClip 검색
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();

                foreach (string keyword in keywords)
                {
                    if (fileName.Contains(keyword.ToLower()))
                    {
                        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    }
                }
            }
#endif
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Load Sounds Now")]
        public void LoadSoundsNow()
        {
            LoadAndAssignSounds();
        }
#endif
    }
}
