using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace HorrorGame.Editor
{
    /// <summary>
    /// KillerFootstep 컴포넌트에 발소리 에셋을 자동으로 설정하는 에디터 도구
    /// </summary>
    public class KillerFootstepSetup : EditorWindow
    {
        private const string FOOTSTEP_ASSET_PATH = "Assets/Footstep(Concrete & Wood)";

        [MenuItem("Horror Game/Setup Killer Footsteps")]
        public static void ShowWindow()
        {
            GetWindow<KillerFootstepSetup>("Killer Footstep Setup");
        }

        private void OnGUI()
        {
            GUILayout.Label("살인마 발소리 설정", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "이 도구는 씬의 KillerFootstep 컴포넌트에 발소리 에셋을 자동으로 설정합니다.\n" +
                "에셋 경로: " + FOOTSTEP_ASSET_PATH,
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("씬의 모든 살인마에 발소리 설정", GUILayout.Height(30)))
            {
                SetupAllKillerFootsteps();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("선택된 오브젝트에 발소리 설정", GUILayout.Height(30)))
            {
                SetupSelectedFootsteps();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (GUILayout.Button("KillerFootstep 컴포넌트 추가 (선택된 오브젝트)", GUILayout.Height(25)))
            {
                AddFootstepComponent();
            }
        }

        private static void SetupAllKillerFootsteps()
        {
            var footsteps = FindObjectsByType<KillerFootstep>(FindObjectsSortMode.None);

            if (footsteps.Length == 0)
            {
                // KillerAI가 있는 오브젝트에 KillerFootstep 추가
                var killers = FindObjectsByType<KillerAI>(FindObjectsSortMode.None);
                foreach (var killer in killers)
                {
                    if (killer.GetComponent<KillerFootstep>() == null)
                    {
                        Undo.AddComponent<KillerFootstep>(killer.gameObject);
                        Debug.Log($"[KillerFootstepSetup] KillerFootstep 추가됨: {killer.name}");
                    }
                }
                footsteps = FindObjectsByType<KillerFootstep>(FindObjectsSortMode.None);
            }

            int setupCount = 0;
            foreach (var footstep in footsteps)
            {
                if (SetupFootstepClips(footstep))
                {
                    setupCount++;
                }
            }

            Debug.Log($"[KillerFootstepSetup] {setupCount}개의 KillerFootstep 설정 완료");
            EditorUtility.DisplayDialog("설정 완료", $"{setupCount}개의 살인마에 발소리가 설정되었습니다.", "확인");
        }

        private static void SetupSelectedFootsteps()
        {
            var selected = Selection.gameObjects;
            int setupCount = 0;

            foreach (var go in selected)
            {
                var footstep = go.GetComponent<KillerFootstep>();
                if (footstep == null)
                {
                    // KillerAI가 있으면 KillerFootstep 추가
                    if (go.GetComponent<KillerAI>() != null)
                    {
                        footstep = Undo.AddComponent<KillerFootstep>(go);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (SetupFootstepClips(footstep))
                {
                    setupCount++;
                }
            }

            if (setupCount > 0)
            {
                Debug.Log($"[KillerFootstepSetup] {setupCount}개의 KillerFootstep 설정 완료");
                EditorUtility.DisplayDialog("설정 완료", $"{setupCount}개의 오브젝트에 발소리가 설정되었습니다.", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("알림", "설정할 KillerFootstep 또는 KillerAI가 없습니다.", "확인");
            }
        }

        private static void AddFootstepComponent()
        {
            var selected = Selection.gameObjects;
            int addedCount = 0;

            foreach (var go in selected)
            {
                if (go.GetComponent<KillerFootstep>() == null)
                {
                    var footstep = Undo.AddComponent<KillerFootstep>(go);
                    SetupFootstepClips(footstep);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                Debug.Log($"[KillerFootstepSetup] {addedCount}개의 KillerFootstep 컴포넌트 추가됨");
            }
        }

        private static bool SetupFootstepClips(KillerFootstep footstep)
        {
            bool modified = false;

            // One Shots 폴더에서 발소리 로드 (Loop 대신 개별 발소리 사용 - 겹침 방지)
            var oneShotClips = LoadAudioClips($"{FOOTSTEP_ASSET_PATH}/Footstep  One Shots/concrete");
            if (oneShotClips.Length > 0)
            {
                Undo.RecordObject(footstep, "Setup Footsteps");
                // 같은 클립을 걷기/뛰기 모두에 사용 (볼륨과 간격으로 구분)
                footstep.walkFootsteps = oneShotClips;
                footstep.runFootsteps = oneShotClips;
                modified = true;
                Debug.Log($"[KillerFootstepSetup] {footstep.name}: One Shots 발소리 {oneShotClips.Length}개 설정");
            }
            else
            {
                Debug.LogWarning($"[KillerFootstepSetup] One Shots 발소리를 찾을 수 없습니다");
            }

            if (modified)
            {
                EditorUtility.SetDirty(footstep);
            }

            return modified;
        }

        private static AudioClip[] LoadAudioClips(string folderPath)
        {
            var clips = new List<AudioClip>();

            // 폴더 내의 모든 오디오 파일 검색
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            return clips.OrderBy(c => c.name).ToArray();
        }

        // 메뉴에서 바로 설정
        [MenuItem("CONTEXT/KillerAI/Add Footstep System")]
        private static void AddFootstepFromContext(MenuCommand command)
        {
            var killerAI = command.context as KillerAI;
            if (killerAI == null) return;

            var footstep = killerAI.GetComponent<KillerFootstep>();
            if (footstep == null)
            {
                footstep = Undo.AddComponent<KillerFootstep>(killerAI.gameObject);
            }

            SetupFootstepClips(footstep);
        }

        [MenuItem("CONTEXT/KillerFootstep/Auto Setup Footstep Clips")]
        private static void SetupFootstepFromContext(MenuCommand command)
        {
            var footstep = command.context as KillerFootstep;
            if (footstep == null) return;

            SetupFootstepClips(footstep);
        }
    }
}
