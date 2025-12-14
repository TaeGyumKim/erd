using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace HorrorGame
{
    /// <summary>
    /// 호러 게임 사운드 자동 할당 도구
    /// 1. Unity Asset Store에서 무료 사운드 팩 다운로드 안내
    /// 2. 프로젝트 내 사운드를 각 컴포넌트에 자동 할당
    /// </summary>
    public class SoundAssignmentTool : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showDownloadGuide = true;
        private bool showAssignmentStatus = true;
        private bool showFootstepSetup = false;

        // 사운드 경로
        private const string AUDIO_ROOT = "Assets/Audio";
        private const string FOOTSTEP_ROOT = "Assets/Footstep(Concrete & Wood)";

        [MenuItem("Horror Game/사운드 할당 도구", false, 300)]
        public static void ShowWindow()
        {
            var window = GetWindow<SoundAssignmentTool>("사운드 할당 도구");
            window.minSize = new Vector2(500, 600);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("🔊 호러 게임 사운드 할당 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 다운로드 가이드
            showDownloadGuide = EditorGUILayout.Foldout(showDownloadGuide, "📥 무료 사운드 에셋 다운로드 가이드", true);
            if (showDownloadGuide)
            {
                DrawDownloadGuide();
            }

            EditorGUILayout.Space();

            // 할당 현황
            showAssignmentStatus = EditorGUILayout.Foldout(showAssignmentStatus, "📊 사운드 할당 현황", true);
            if (showAssignmentStatus)
            {
                DrawAssignmentStatus();
            }

            EditorGUILayout.Space();

            // 발소리 설정
            showFootstepSetup = EditorGUILayout.Foldout(showFootstepSetup, "👣 발소리 설정", true);
            if (showFootstepSetup)
            {
                DrawFootstepSetup();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 액션 버튼
            EditorGUILayout.LabelField("⚡ 빠른 설정", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📁 Audio 폴더 생성", GUILayout.Height(30)))
            {
                CreateAudioFolders();
            }
            if (GUILayout.Button("🔄 사운드 자동 할당", GUILayout.Height(30)))
            {
                AutoAssignSounds();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("👣 발소리 할당", GUILayout.Height(30)))
            {
                AssignFootstepSounds();
            }
            if (GUILayout.Button("🎵 HorrorSoundManager 생성", GUILayout.Height(30)))
            {
                CreateHorrorSoundManager();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawDownloadGuide()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Unity Asset Store에서 무료 에셋 다운로드:", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawAssetStoreLink("Free Horror Starter Pack",
                "https://assetstore.unity.com/packages/audio/music/free-horror-starter-pack-211340",
                "배경음악 12트랙 (공포 분위기, 긴장, 추격)");

            DrawAssetStoreLink("Free Sound Effects Pack",
                "https://assetstore.unity.com/packages/audio/sound-fx/free-sound-effects-pack-155776",
                "폭발, 몬스터, 유령 등 51개 효과음");

            DrawAssetStoreLink("Free UI Click Sound Pack",
                "https://assetstore.unity.com/packages/audio/sound-fx/free-ui-click-sound-pack-244644",
                "UI 클릭 사운드");

            DrawAssetStoreLink("UI SFX Free Pack",
                "https://assetstore.unity.com/packages/audio/sound-fx/ui-sfx-free-pack-245925",
                "UI 효과음 패키지");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("외부 무료 사운드 사이트:", EditorStyles.boldLabel);

            DrawExternalLink("Pixabay", "https://pixabay.com/sound-effects/",
                "무료 효과음 (심장박동, 호흡, 호러)");
            DrawExternalLink("Mixkit", "https://mixkit.co/free-sound-effects/horror/",
                "무료 호러 효과음");
            DrawExternalLink("Freesound", "https://freesound.org/",
                "커뮤니티 무료 효과음 (CC 라이선스)");

            EditorGUILayout.EndVertical();
        }

        private void DrawAssetStoreLink(string name, string url, string description)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"• {name}", GUILayout.Width(200));
            if (GUILayout.Button("Asset Store에서 열기", GUILayout.Width(130)))
            {
                Application.OpenURL(url);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"   {description}", EditorStyles.miniLabel);
        }

        private void DrawExternalLink(string name, string url, string description)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"• {name}", GUILayout.Width(100));
            if (GUILayout.Button("열기", GUILayout.Width(50)))
            {
                Application.OpenURL(url);
            }
            EditorGUILayout.LabelField(description);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssignmentStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var soundManager = FindObjectOfType<HorrorSoundManager>();

            if (soundManager == null)
            {
                EditorGUILayout.HelpBox("HorrorSoundManager가 씬에 없습니다.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("HorrorSoundManager 할당 현황:", EditorStyles.boldLabel);

                DrawClipStatus("평상시 배경음", soundManager.normalAmbient);
                DrawClipStatus("긴장 배경음", soundManager.tensionAmbient);
                DrawClipStatus("추격 배경음", soundManager.chaseAmbient);
                DrawClipStatus("심장박동", soundManager.heartbeatSound);
                DrawClipStatus("점프스케어", soundManager.jumpScareSound);
                DrawClipStatus("아이템 획득", soundManager.itemPickupSound);
                DrawClipStatus("목표 완료", soundManager.objectiveCompleteSound);
                DrawClipStatus("경고음", soundManager.warningSound);

                DrawArrayStatus("발소리", soundManager.footstepSounds);
                DrawArrayStatus("달리기 발소리", soundManager.runFootstepSounds);
                DrawArrayStatus("속삭임", soundManager.whisperSounds);
                DrawArrayStatus("문 삐걱", soundManager.creekyDoorSounds);
                DrawArrayStatus("호흡", soundManager.breathingSounds);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawClipStatus(string name, AudioClip clip)
        {
            EditorGUILayout.BeginHorizontal();
            string status = clip != null ? "✅" : "❌";
            EditorGUILayout.LabelField($"{status} {name}", GUILayout.Width(200));
            if (clip != null)
            {
                EditorGUILayout.LabelField(clip.name, EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("미할당", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawArrayStatus(string name, AudioClip[] clips)
        {
            EditorGUILayout.BeginHorizontal();
            int count = clips != null ? clips.Length : 0;
            string status = count > 0 ? "✅" : "❌";
            EditorGUILayout.LabelField($"{status} {name}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"{count}개", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFootstepSetup()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool hasFootsteps = Directory.Exists(FOOTSTEP_ROOT);

            if (hasFootsteps)
            {
                EditorGUILayout.HelpBox("✅ Footstep(Concrete & Wood) 에셋이 있습니다!", MessageType.Info);

                // 발소리 파일 개수 표시
                int concreteCount = CountAudioFiles(Path.Combine(FOOTSTEP_ROOT, "Footstep  One Shots/concrete"));
                int woodCount = CountAudioFiles(Path.Combine(FOOTSTEP_ROOT, "Footstep  One Shots/wood"));
                int runCount = CountAudioFiles(Path.Combine(FOOTSTEP_ROOT, "Footstep Loops/concrete/running"));

                EditorGUILayout.LabelField($"콘크리트 발소리: {concreteCount}개");
                EditorGUILayout.LabelField($"나무 발소리: {woodCount}개");
                EditorGUILayout.LabelField($"달리기 발소리: {runCount}개");
            }
            else
            {
                EditorGUILayout.HelpBox("Footstep 에셋이 없습니다. Asset Store에서 다운로드하세요.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private int CountAudioFiles(string path)
        {
            if (!Directory.Exists(path)) return 0;
            return Directory.GetFiles(path, "*.wav").Length + Directory.GetFiles(path, "*.mp3").Length;
        }

        [MenuItem("Horror Game/Setup/Create Audio Folders", false, 301)]
        public static void CreateAudioFolders()
        {
            string[] folders = new string[]
            {
                "Assets/Audio",
                "Assets/Audio/Ambient",
                "Assets/Audio/Footsteps",
                "Assets/Audio/Horror",
                "Assets/Audio/UI",
                "Assets/Audio/Voice",
                "Assets/Audio/Music"
            };

            foreach (string folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                    string newFolder = Path.GetFileName(folder);
                    AssetDatabase.CreateFolder(parent, newFolder);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("[SoundAssignmentTool] Audio 폴더 구조 생성 완료!");
        }

        [MenuItem("Horror Game/Setup/Auto Assign Sounds", false, 302)]
        public static void AutoAssignSounds()
        {
            var soundManager = FindObjectOfType<HorrorSoundManager>();
            if (soundManager == null)
            {
                Debug.LogWarning("[SoundAssignmentTool] HorrorSoundManager를 찾을 수 없습니다. 먼저 생성하세요.");
                return;
            }

            int assigned = 0;

            // 발소리 할당
            assigned += AssignFootstepsToManager(soundManager);

            // Audio 폴더에서 사운드 검색 및 할당
            assigned += AssignAmbientSounds(soundManager);
            assigned += AssignHorrorSounds(soundManager);
            assigned += AssignUISounds(soundManager);

            EditorUtility.SetDirty(soundManager);
            Debug.Log($"[SoundAssignmentTool] {assigned}개 사운드 할당 완료!");
        }

        private static int AssignFootstepsToManager(HorrorSoundManager manager)
        {
            int count = 0;

            // 콘크리트 발소리
            var concreteFootsteps = LoadAudioClips("Assets/Footstep(Concrete & Wood)/Footstep  One Shots/concrete");
            if (concreteFootsteps.Length > 0 && (manager.footstepSounds == null || manager.footstepSounds.Length == 0))
            {
                manager.footstepSounds = concreteFootsteps.Take(10).ToArray();
                count += manager.footstepSounds.Length;
            }

            // 달리기 발소리
            var runningFootsteps = LoadAudioClips("Assets/Footstep(Concrete & Wood)/Footstep Loops/concrete/running");
            if (runningFootsteps.Length > 0 && (manager.runFootstepSounds == null || manager.runFootstepSounds.Length == 0))
            {
                manager.runFootstepSounds = runningFootsteps;
                count += manager.runFootstepSounds.Length;
            }

            // 느린 발소리 (웅크리기용)
            var slowFootsteps = LoadAudioClips("Assets/Footstep(Concrete & Wood)/Footstep Loops/concrete/slowsteps");
            if (slowFootsteps.Length > 0 && (manager.crouchFootstepSounds == null || manager.crouchFootstepSounds.Length == 0))
            {
                manager.crouchFootstepSounds = slowFootsteps;
                count += manager.crouchFootstepSounds.Length;
            }

            return count;
        }

        private static int AssignAmbientSounds(HorrorSoundManager manager)
        {
            int count = 0;

            // Free Horror Starter Pack 에서 찾기
            string[] ambientPaths = new string[]
            {
                "Assets/Free Horror Starter Pack",
                "Assets/Audio/Ambient",
                "Assets/Audio/Music"
            };

            foreach (string basePath in ambientPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                var clips = LoadAudioClipsRecursive(basePath);

                foreach (var clip in clips)
                {
                    string name = clip.name.ToLower();

                    // 평상시 배경음
                    if (manager.normalAmbient == null &&
                        (name.Contains("ambient") || name.Contains("calm") || name.Contains("peaceful") || name.Contains("normal")))
                    {
                        manager.normalAmbient = clip;
                        count++;
                    }
                    // 긴장 배경음
                    else if (manager.tensionAmbient == null &&
                        (name.Contains("tension") || name.Contains("suspense") || name.Contains("tense") || name.Contains("scary")))
                    {
                        manager.tensionAmbient = clip;
                        count++;
                    }
                    // 추격 배경음
                    else if (manager.chaseAmbient == null &&
                        (name.Contains("chase") || name.Contains("danger") || name.Contains("action") || name.Contains("intense")))
                    {
                        manager.chaseAmbient = clip;
                        count++;
                    }
                }
            }

            return count;
        }

        private static int AssignHorrorSounds(HorrorSoundManager manager)
        {
            int count = 0;

            string[] horrorPaths = new string[]
            {
                "Assets/Free Sound Effects Pack",
                "Assets/Audio/Horror",
                "Assets/Audio"
            };

            List<AudioClip> whispers = new List<AudioClip>();
            List<AudioClip> doorSounds = new List<AudioClip>();
            List<AudioClip> breathingSounds = new List<AudioClip>();

            foreach (string basePath in horrorPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                var clips = LoadAudioClipsRecursive(basePath);

                foreach (var clip in clips)
                {
                    string name = clip.name.ToLower();

                    // 심장박동
                    if (manager.heartbeatSound == null &&
                        (name.Contains("heart") || name.Contains("beat") || name.Contains("pulse")))
                    {
                        manager.heartbeatSound = clip;
                        count++;
                    }
                    // 점프스케어
                    else if (manager.jumpScareSound == null &&
                        (name.Contains("scare") || name.Contains("jump") || name.Contains("stinger") || name.Contains("shock")))
                    {
                        manager.jumpScareSound = clip;
                        count++;
                    }
                    // 속삭임
                    else if (name.Contains("whisper") || name.Contains("ghost") || name.Contains("voice"))
                    {
                        whispers.Add(clip);
                    }
                    // 문소리
                    else if (name.Contains("door") || name.Contains("creak") || name.Contains("squeak"))
                    {
                        doorSounds.Add(clip);
                    }
                    // 호흡
                    else if (name.Contains("breath") || name.Contains("breathing"))
                    {
                        breathingSounds.Add(clip);
                    }
                }
            }

            if (whispers.Count > 0 && (manager.whisperSounds == null || manager.whisperSounds.Length == 0))
            {
                manager.whisperSounds = whispers.ToArray();
                count += whispers.Count;
            }
            if (doorSounds.Count > 0 && (manager.creekyDoorSounds == null || manager.creekyDoorSounds.Length == 0))
            {
                manager.creekyDoorSounds = doorSounds.ToArray();
                count += doorSounds.Count;
            }
            if (breathingSounds.Count > 0 && (manager.breathingSounds == null || manager.breathingSounds.Length == 0))
            {
                manager.breathingSounds = breathingSounds.ToArray();
                count += breathingSounds.Count;
            }

            return count;
        }

        private static int AssignUISounds(HorrorSoundManager manager)
        {
            int count = 0;

            string[] uiPaths = new string[]
            {
                "Assets/Free UI Click Sound Pack",
                "Assets/UI SFX Free Pack",
                "Assets/Audio/UI"
            };

            foreach (string basePath in uiPaths)
            {
                if (!Directory.Exists(basePath)) continue;

                var clips = LoadAudioClipsRecursive(basePath);

                foreach (var clip in clips)
                {
                    string name = clip.name.ToLower();

                    // 아이템 획득
                    if (manager.itemPickupSound == null &&
                        (name.Contains("pickup") || name.Contains("collect") || name.Contains("item") || name.Contains("get")))
                    {
                        manager.itemPickupSound = clip;
                        count++;
                    }
                    // 목표 완료
                    else if (manager.objectiveCompleteSound == null &&
                        (name.Contains("complete") || name.Contains("success") || name.Contains("win") || name.Contains("done")))
                    {
                        manager.objectiveCompleteSound = clip;
                        count++;
                    }
                    // 경고
                    else if (manager.warningSound == null &&
                        (name.Contains("warning") || name.Contains("alert") || name.Contains("alarm")))
                    {
                        manager.warningSound = clip;
                        count++;
                    }
                    // 타이머
                    else if (manager.timerTickSound == null &&
                        (name.Contains("tick") || name.Contains("clock") || name.Contains("timer")))
                    {
                        manager.timerTickSound = clip;
                        count++;
                    }
                }
            }

            return count;
        }

        [MenuItem("Horror Game/Setup/Assign Footstep Sounds", false, 303)]
        public static void AssignFootstepSounds()
        {
            // HorrorSoundManager에 할당
            var soundManager = FindObjectOfType<HorrorSoundManager>();
            if (soundManager != null)
            {
                int count = AssignFootstepsToManager(soundManager);
                EditorUtility.SetDirty(soundManager);
                Debug.Log($"[SoundAssignmentTool] HorrorSoundManager에 {count}개 발소리 할당");
            }

            // KillerFootstep에 할당
            var killerFootsteps = FindObjectsOfType<KillerFootstep>();
            foreach (var kf in killerFootsteps)
            {
                if (kf.walkFootsteps == null || kf.walkFootsteps.Length == 0)
                {
                    kf.walkFootsteps = LoadAudioClips("Assets/Footstep(Concrete & Wood)/Footstep  One Shots/concrete").Take(5).ToArray();
                }
                if (kf.runFootsteps == null || kf.runFootsteps.Length == 0)
                {
                    kf.runFootsteps = LoadAudioClips("Assets/Footstep(Concrete & Wood)/Footstep Loops/concrete/running");
                }
                EditorUtility.SetDirty(kf);
                Debug.Log($"[SoundAssignmentTool] {kf.gameObject.name}에 발소리 할당");
            }

            // FootstepSystem에 할당
            var footstepSystems = FindObjectsOfType<FootstepSystem>();
            foreach (var fs in footstepSystems)
            {
                if (fs.defaultFootsteps == null || fs.defaultFootsteps.Length == 0)
                {
                    fs.defaultFootsteps = LoadAudioClips("Assets/Footstep(Concrete & Wood)/Footstep  One Shots/concrete").Take(8).ToArray();
                }
                EditorUtility.SetDirty(fs);
                Debug.Log($"[SoundAssignmentTool] {fs.gameObject.name} FootstepSystem에 발소리 할당");
            }
        }

        [MenuItem("Horror Game/Setup/Create HorrorSoundManager", false, 304)]
        public static void CreateHorrorSoundManager()
        {
            var existing = FindObjectOfType<HorrorSoundManager>();
            if (existing != null)
            {
                Debug.Log("[SoundAssignmentTool] HorrorSoundManager가 이미 존재합니다.");
                Selection.activeGameObject = existing.gameObject;

                // AudioAutoLoader 추가
                if (existing.GetComponent<AudioAutoLoader>() == null)
                {
                    existing.gameObject.AddComponent<AudioAutoLoader>();
                    Debug.Log("[SoundAssignmentTool] AudioAutoLoader 추가됨");
                }
                return;
            }

            GameObject managerObj = new GameObject("HorrorSoundManager");
            var manager = managerObj.AddComponent<HorrorSoundManager>();
            managerObj.AddComponent<AudioAutoLoader>();

            // 기본 설정
            manager.footstepInterval = 0.5f;
            manager.runFootstepInterval = 0.3f;
            manager.whisperIntervalRange = new Vector2(30f, 90f);

            Undo.RegisterCreatedObjectUndo(managerObj, "Create HorrorSoundManager");
            Selection.activeGameObject = managerObj;

            Debug.Log("[SoundAssignmentTool] HorrorSoundManager 생성 완료!");

            // 자동 할당
            AutoAssignSounds();
        }

        [MenuItem("Horror Game/Setup/Add AudioAutoLoader", false, 305)]
        public static void AddAudioAutoLoader()
        {
            var soundManager = FindObjectOfType<HorrorSoundManager>();
            if (soundManager == null)
            {
                Debug.LogWarning("[SoundAssignmentTool] HorrorSoundManager를 먼저 생성하세요.");
                return;
            }

            var autoLoader = soundManager.GetComponent<AudioAutoLoader>();
            if (autoLoader == null)
            {
                autoLoader = soundManager.gameObject.AddComponent<AudioAutoLoader>();
                autoLoader.autoLoadOnStart = true;
                autoLoader.loadFootsteps = true;
                EditorUtility.SetDirty(soundManager.gameObject);
                Debug.Log("[SoundAssignmentTool] AudioAutoLoader 추가 완료!");
            }
            else
            {
                Debug.Log("[SoundAssignmentTool] AudioAutoLoader가 이미 있습니다.");
            }

            // 즉시 로드 실행
            autoLoader.LoadAndAssignSounds();
            EditorUtility.SetDirty(soundManager);
        }

        private static AudioClip[] LoadAudioClips(string path)
        {
            if (!Directory.Exists(path)) return new AudioClip[0];

            List<AudioClip> clips = new List<AudioClip>();
            string[] extensions = new[] { "*.wav", "*.mp3", "*.ogg", "*.aiff" };

            foreach (var ext in extensions)
            {
                string[] files = Directory.GetFiles(path, ext);
                foreach (string file in files)
                {
                    string assetPath = file.Replace("\\", "/");
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip != null)
                    {
                        clips.Add(clip);
                    }
                }
            }

            return clips.ToArray();
        }

        private static AudioClip[] LoadAudioClipsRecursive(string path)
        {
            if (!Directory.Exists(path)) return new AudioClip[0];

            List<AudioClip> clips = new List<AudioClip>();
            string[] extensions = new[] { "*.wav", "*.mp3", "*.ogg", "*.aiff" };

            foreach (var ext in extensions)
            {
                string[] files = Directory.GetFiles(path, ext, SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    string assetPath = file.Replace("\\", "/");
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                    if (clip != null)
                    {
                        clips.Add(clip);
                    }
                }
            }

            return clips.ToArray();
        }
    }
}
