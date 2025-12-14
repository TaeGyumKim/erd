using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text;

namespace HorrorGame.Editor
{
    /// <summary>
    /// TextMesh Pro 한글 폰트 아틀라스 수정 도구
    /// 한글 기본 문자 세트를 폰트에 추가하고 아틀라스를 초기화
    /// </summary>
    public class TMPKoreanFontFixer : EditorWindow
    {
        private TMP_FontAsset targetFont;
        private bool includeBasicLatin = true;
        private bool includeKoreanSyllables = true;
        private bool includeKoreanJamo = true;
        private bool includeNumbers = true;
        private bool includePunctuation = true;

        [MenuItem("Horror Game/TMP 한글 폰트 수정")]
        public static void ShowWindow()
        {
            GetWindow<TMPKoreanFontFixer>("TMP 한글 폰트 수정");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("TextMesh Pro 한글 폰트 수정 도구", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "한글 문자가 깨지는 문제를 해결합니다.\n" +
                "1. 폰트 아틀라스를 초기화하고\n" +
                "2. 한글 기본 문자 세트를 추가합니다.",
                MessageType.Info);

            EditorGUILayout.Space();

            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
                "대상 폰트", targetFont, typeof(TMP_FontAsset), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("포함할 문자 세트:", EditorStyles.boldLabel);

            includeBasicLatin = EditorGUILayout.Toggle("기본 라틴 (A-Z, a-z)", includeBasicLatin);
            includeNumbers = EditorGUILayout.Toggle("숫자 (0-9)", includeNumbers);
            includePunctuation = EditorGUILayout.Toggle("구두점 (!?.,등)", includePunctuation);
            includeKoreanJamo = EditorGUILayout.Toggle("한글 자모 (ㄱ-ㅎ, ㅏ-ㅣ)", includeKoreanJamo);
            includeKoreanSyllables = EditorGUILayout.Toggle("한글 음절 (가-힣) - 11,172자", includeKoreanSyllables);

            EditorGUILayout.Space();

            if (includeKoreanSyllables)
            {
                EditorGUILayout.HelpBox(
                    "한글 전체 음절(11,172자)을 포함하면 아틀라스 크기가 커집니다.\n" +
                    "Dynamic 모드에서는 필요한 문자만 런타임에 추가됩니다.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(targetFont == null);

            if (GUILayout.Button("폰트 아틀라스 초기화 및 문자 세트 업데이트", GUILayout.Height(40)))
            {
                UpdateFontCharacterSet(targetFont);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("폰트 정보 확인"))
            {
                ShowFontInfo(targetFont);
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("빠른 수정:", EditorStyles.boldLabel);

            if (GUILayout.Button("모든 NanumGothic 폰트 수정", GUILayout.Height(30)))
            {
                FixAllNanumGothicFonts();
            }
        }

        private string BuildCharacterSet()
        {
            StringBuilder sb = new StringBuilder();

            // 기본 라틴
            if (includeBasicLatin)
            {
                for (char c = 'A'; c <= 'Z'; c++) sb.Append(c);
                for (char c = 'a'; c <= 'z'; c++) sb.Append(c);
            }

            // 숫자
            if (includeNumbers)
            {
                for (char c = '0'; c <= '9'; c++) sb.Append(c);
            }

            // 구두점
            if (includePunctuation)
            {
                sb.Append(" !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~");
            }

            // 한글 자모
            if (includeKoreanJamo)
            {
                // 한글 호환 자모 (ㄱ-ㅎ, ㅏ-ㅣ): U+3130 - U+318F
                for (int i = 0x3131; i <= 0x3163; i++)
                {
                    sb.Append((char)i);
                }
            }

            // 한글 음절
            if (includeKoreanSyllables)
            {
                // 한글 음절 전체 (가-힣): U+AC00 - U+D7A3
                for (int i = 0xAC00; i <= 0xD7A3; i++)
                {
                    sb.Append((char)i);
                }
            }

            return sb.ToString();
        }

        private void UpdateFontCharacterSet(TMP_FontAsset font)
        {
            if (font == null)
            {
                Debug.LogError("[TMPKoreanFontFixer] 폰트가 선택되지 않았습니다.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(font);
            Debug.Log($"[TMPKoreanFontFixer] 폰트 수정 시작: {path}");

            // Dynamic 모드 확인
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                Debug.LogWarning($"[TMPKoreanFontFixer] {font.name}은 Static 모드입니다. Dynamic 모드로 변경합니다.");
            }

            // 폰트 아틀라스 초기화
            font.ClearFontAssetData();
            Debug.Log($"[TMPKoreanFontFixer] 폰트 아틀라스 초기화 완료");

            // 문자 세트 생성
            string characterSet = BuildCharacterSet();
            Debug.Log($"[TMPKoreanFontFixer] 문자 세트 크기: {characterSet.Length}자");

            // 문자 추가 시도
            bool success = font.TryAddCharacters(characterSet, out string missingCharacters);

            if (success)
            {
                Debug.Log($"[TMPKoreanFontFixer] 모든 문자가 성공적으로 추가되었습니다.");
            }
            else
            {
                int missing = string.IsNullOrEmpty(missingCharacters) ? 0 : missingCharacters.Length;
                int added = characterSet.Length - missing;
                Debug.Log($"[TMPKoreanFontFixer] {added}자 추가됨, {missing}자 누락됨");

                if (missing > 0 && missing < 100)
                {
                    Debug.LogWarning($"[TMPKoreanFontFixer] 누락된 문자: {missingCharacters}");
                }
            }

            // 에셋 저장
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TMPKoreanFontFixer] 폰트 저장 완료: {font.name}");
            EditorUtility.DisplayDialog("완료",
                $"폰트 '{font.name}'이 업데이트되었습니다.\n" +
                $"추가된 문자: {characterSet.Length}자\n\n" +
                "Unity를 재시작하거나 씬을 다시 로드하면 변경사항이 적용됩니다.",
                "확인");
        }

        private void ShowFontInfo(TMP_FontAsset font)
        {
            if (font == null) return;

            StringBuilder info = new StringBuilder();
            info.AppendLine($"폰트 이름: {font.name}");
            info.AppendLine($"소스 폰트: {font.sourceFontFile?.name ?? "없음"}");
            info.AppendLine($"아틀라스 모드: {font.atlasPopulationMode}");
            info.AppendLine($"아틀라스 크기: {font.atlasWidth} x {font.atlasHeight}");
            info.AppendLine($"등록된 문자 수: {font.characterTable?.Count ?? 0}");
            info.AppendLine($"등록된 글리프 수: {font.glyphTable?.Count ?? 0}");
            info.AppendLine($"Multi Atlas 사용: {font.isMultiAtlasTexturesEnabled}");

            // 특정 문자 확인
            info.AppendLine("\n--- 문제 문자 확인 ---");
            char[] testChars = { '끝', '까', '려', '라', '료', '가', '나', '다' };
            foreach (char c in testChars)
            {
                bool hasChar = font.HasCharacter(c);
                info.AppendLine($"'{c}' (U+{((int)c):X4}): {(hasChar ? "있음" : "없음")}");
            }

            Debug.Log(info.ToString());
            EditorUtility.DisplayDialog("폰트 정보", info.ToString(), "확인");
        }

        private void FixAllNanumGothicFonts()
        {
            string[] guids = AssetDatabase.FindAssets("NanumGothic t:TMP_FontAsset");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("알림", "NanumGothic 폰트를 찾을 수 없습니다.", "확인");
                return;
            }

            bool confirm = EditorUtility.DisplayDialog("확인",
                $"{guids.Length}개의 NanumGothic 폰트를 수정하시겠습니까?\n\n" +
                "이 작업은 폰트 아틀라스를 초기화하고 한글 문자 세트를 추가합니다.",
                "수정", "취소");

            if (!confirm) return;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

                if (font != null)
                {
                    Debug.Log($"[TMPKoreanFontFixer] 수정 중: {font.name}");

                    // 폰트 초기화 및 기본 문자 추가
                    font.ClearFontAssetData();

                    // 기본 문자 세트 (게임에서 사용하는 주요 문자들)
                    string basicSet = BuildCharacterSet();
                    font.TryAddCharacters(basicSet, out _);

                    EditorUtility.SetDirty(font);
                }
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("완료",
                $"{guids.Length}개의 NanumGothic 폰트가 수정되었습니다.\n\n" +
                "Unity를 재시작하거나 씬을 다시 로드하면 변경사항이 적용됩니다.",
                "확인");
        }
    }
}
