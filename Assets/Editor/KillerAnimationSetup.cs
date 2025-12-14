using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 킬러 애니메이션 설정 도구
    /// BlendTree에 Walk/Run 애니메이션을 자동으로 설정합니다.
    /// </summary>
    public class KillerAnimationSetup : EditorWindow
    {
        private const string CONTROLLER_PATH = "Assets/Character/KillerAnimatorController.controller";

        // 애니메이션 경로
        private const string WALK_ANIM_PATH = "Assets/Animations/Walk/Walk_anim.fbx";
        private const string RUN_ANIM_PATH = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx";
        private const string IDLE_ANIM_PATH = "Assets/Animations/Idle/idle_anim.fbx";

        [MenuItem("Horror Game/Animation/Killer BlendTree Setup Window")]
        public static void ShowWindow()
        {
            GetWindow<KillerAnimationSetup>("킬러 애니메이션 설정");
        }

        [MenuItem("Horror Game/Animation/Setup Killer BlendTree")]
        public static void AutoSetupBlendTree()
        {
            SetupLocomotionBlendTree();
        }

        private void OnGUI()
        {
            GUILayout.Label("킬러 애니메이션 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "이 도구는 킬러의 Locomotion BlendTree를 자동으로 설정합니다.\n\n" +
                "- Idle (속도 0)\n" +
                "- Walk (속도 0.5)\n" +
                "- Run (속도 1.0)\n\n" +
                "필요한 파라미터: NormalizedSpeed, Attack, Alert, Catch",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("BlendTree 자동 설정", GUILayout.Height(40)))
            {
                SetupLocomotionBlendTree();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("필수 파라미터 추가", GUILayout.Height(30)))
            {
                EnsureAnimatorParameters();
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Animator Controller 선택", GUILayout.Height(30)))
            {
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
                if (controller != null)
                {
                    Selection.activeObject = controller;
                    EditorGUIUtility.PingObject(controller);
                }
            }
        }

        private static void SetupLocomotionBlendTree()
        {
            // Animator Controller 로드
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogError($"[KillerAnimationSetup] Animator Controller를 찾을 수 없습니다: {CONTROLLER_PATH}");
                return;
            }

            // 애니메이션 클립 로드
            AnimationClip idleClip = LoadAnimationClip(IDLE_ANIM_PATH);
            AnimationClip walkClip = LoadAnimationClip(WALK_ANIM_PATH);
            AnimationClip runClip = LoadAnimationClip(RUN_ANIM_PATH);

            if (walkClip == null || runClip == null)
            {
                Debug.LogError("[KillerAnimationSetup] Walk 또는 Run 애니메이션을 찾을 수 없습니다!");
                return;
            }

            // Base Layer의 State Machine 가져오기
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // Locomotion 상태 찾기
            AnimatorState locomotionState = null;
            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name == "Locomotion")
                {
                    locomotionState = state.state;
                    break;
                }
            }

            if (locomotionState == null)
            {
                Debug.LogError("[KillerAnimationSetup] Locomotion 상태를 찾을 수 없습니다!");
                return;
            }

            // 새 BlendTree 생성
            BlendTree blendTree = new BlendTree();
            blendTree.name = "Locomotion BlendTree";
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "NormalizedSpeed";
            blendTree.useAutomaticThresholds = false;

            // 애니메이션 추가
            // Idle (선택적)
            if (idleClip != null)
            {
                blendTree.AddChild(idleClip, 0f);
            }
            else
            {
                // Idle이 없으면 Walk를 0에도 추가 (정지 상태)
                blendTree.AddChild(walkClip, 0f);
            }

            // Walk
            blendTree.AddChild(walkClip, 0.5f);

            // Run
            blendTree.AddChild(runClip, 1f);

            // BlendTree를 에셋에 추가
            AssetDatabase.AddObjectToAsset(blendTree, controller);

            // Locomotion 상태에 BlendTree 설정
            locomotionState.motion = blendTree;

            // Speed Parameter로 애니메이션 속도 조절 활성화
            locomotionState.speedParameterActive = true;
            locomotionState.speedParameter = "NormalizedSpeed";

            // 저장
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[KillerAnimationSetup] BlendTree 설정 완료!");
            Debug.Log($"  - Idle: {(idleClip != null ? idleClip.name : "Walk (대체)")}");
            Debug.Log($"  - Walk: {walkClip.name}");
            Debug.Log($"  - Run: {runClip.name}");

            EditorUtility.DisplayDialog("완료",
                "킬러 Locomotion BlendTree가 설정되었습니다!\n\n" +
                "- 속도 0: Idle/정지\n" +
                "- 속도 0.5: Walk\n" +
                "- 속도 1.0: Run",
                "확인");
        }

        private static AnimationClip LoadAnimationClip(string path)
        {
            // FBX에서 애니메이션 클립 로드
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
                {
                    return clip;
                }
            }

            // 직접 로드 시도
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        /// <summary>
        /// Animator Controller에 필수 파라미터 추가
        /// </summary>
        [MenuItem("Horror Game/Animation/Ensure Killer Animator Parameters")]
        private static void EnsureAnimatorParameters()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            if (controller == null)
            {
                Debug.LogError($"[KillerAnimationSetup] Animator Controller를 찾을 수 없습니다: {CONTROLLER_PATH}");
                return;
            }

            // 필수 파라미터 목록
            var floatParams = new string[] { "Speed", "NormalizedSpeed" };
            var boolParams = new string[] { "IsChasing", "IsWalking", "IsRunning", "IsSearching" };
            var triggerParams = new string[] { "Attack", "Alert", "Catch" };

            int addedCount = 0;

            // Float 파라미터
            foreach (var param in floatParams)
            {
                if (!HasParameter(controller, param))
                {
                    controller.AddParameter(param, AnimatorControllerParameterType.Float);
                    addedCount++;
                    Debug.Log($"[KillerAnimationSetup] Float 파라미터 추가: {param}");
                }
            }

            // Bool 파라미터
            foreach (var param in boolParams)
            {
                if (!HasParameter(controller, param))
                {
                    controller.AddParameter(param, AnimatorControllerParameterType.Bool);
                    addedCount++;
                    Debug.Log($"[KillerAnimationSetup] Bool 파라미터 추가: {param}");
                }
            }

            // Trigger 파라미터
            foreach (var param in triggerParams)
            {
                if (!HasParameter(controller, param))
                {
                    controller.AddParameter(param, AnimatorControllerParameterType.Trigger);
                    addedCount++;
                    Debug.Log($"[KillerAnimationSetup] Trigger 파라미터 추가: {param}");
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                Debug.Log($"[KillerAnimationSetup] {addedCount}개의 파라미터가 추가되었습니다.");
                EditorUtility.DisplayDialog("완료", $"{addedCount}개의 파라미터가 추가되었습니다.", "확인");
            }
            else
            {
                Debug.Log("[KillerAnimationSetup] 모든 필수 파라미터가 이미 존재합니다.");
                EditorUtility.DisplayDialog("확인", "모든 필수 파라미터가 이미 존재합니다.", "확인");
            }
        }

        /// <summary>
        /// 파라미터 존재 여부 확인
        /// </summary>
        private static bool HasParameter(AnimatorController controller, string paramName)
        {
            foreach (var param in controller.parameters)
            {
                if (param.name == paramName)
                    return true;
            }
            return false;
        }
    }
}
