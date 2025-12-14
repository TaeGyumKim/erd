using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text;

namespace HorrorGame.Editor
{
    /// <summary>
    /// TextMesh Pro 한글 폰트 자동 수정
    /// 메뉴에서 직접 실행 가능
    /// </summary>
    public static class TMPKoreanFontAutoFix
    {
        [MenuItem("Horror Game/Fix TMP Korean Fonts")]
        public static void FixAllNanumGothicFonts()
        {
            string[] guids = AssetDatabase.FindAssets("NanumGothic t:TMP_FontAsset");

            if (guids.Length == 0)
            {
                Debug.LogWarning("[TMPKoreanFontAutoFix] NanumGothic 폰트를 찾을 수 없습니다.");
                return;
            }

            Debug.Log($"[TMPKoreanFontAutoFix] {guids.Length}개의 NanumGothic 폰트 수정 시작...");

            // 게임에서 사용하는 한글 문자 세트
            string characterSet = BuildGameCharacterSet();
            Debug.Log($"[TMPKoreanFontAutoFix] 문자 세트 크기: {characterSet.Length}자");

            int fixedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

                if (font != null)
                {
                    Debug.Log($"[TMPKoreanFontAutoFix] 수정 중: {font.name} ({path})");

                    // 폰트 초기화
                    font.ClearFontAssetData();
                    Debug.Log($"[TMPKoreanFontAutoFix] {font.name} 아틀라스 초기화 완료");

                    // 문자 추가
                    bool success = font.TryAddCharacters(characterSet, out string missing);
                    int added = characterSet.Length - (string.IsNullOrEmpty(missing) ? 0 : missing.Length);
                    Debug.Log($"[TMPKoreanFontAutoFix] {font.name}: {added}자 추가됨");

                    EditorUtility.SetDirty(font);
                    fixedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TMPKoreanFontAutoFix] 완료! {fixedCount}개의 폰트가 수정되었습니다.");
        }

        /// <summary>
        /// 게임에서 사용하는 문자 세트 생성
        /// </summary>
        private static string BuildGameCharacterSet()
        {
            StringBuilder sb = new StringBuilder();

            // 기본 ASCII (공백, 숫자, 알파벳, 구두점)
            for (int i = 32; i <= 126; i++)
            {
                sb.Append((char)i);
            }

            // 한글 자모 (ㄱ-ㅎ, ㅏ-ㅣ)
            for (int i = 0x3131; i <= 0x3163; i++)
            {
                sb.Append((char)i);
            }

            // 게임에서 사용하는 주요 한글 (수동 추가)
            sb.Append("끝까지달려라종료시작게임설정옵션계속하기새로시작메인메뉴");
            sb.Append("비밀번호입력확인취소닫기열기저장불러오기");
            sb.Append("눈을떠라플레이어탈출승리패배사망");
            sb.Append("손전등켜기끄기숨기나가기문열쇠잠금해제");
            sb.Append("살인마유령힌트경고주의위험안전");
            sb.Append("단서발견했습니다아이템획득");
            sb.Append("스태미나체력배터리시간남은");
            sb.Append("찾기위치이동점프달리기걷기멈추기");

            // 한글 전체 음절 (가-힣) - Dynamic 모드에서 필요한 문자만 로드됨
            for (int i = 0xAC00; i <= 0xD7A3; i++)
            {
                sb.Append((char)i);
            }

            return sb.ToString();
        }

        [MenuItem("Horror Game/Check TMP Font Characters")]
        public static void CheckFontCharacters()
        {
            string[] guids = AssetDatabase.FindAssets("NanumGothic SDF t:TMP_FontAsset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

                if (font != null && font.name == "NanumGothic SDF")
                {
                    Debug.Log($"=== {font.name} 폰트 정보 ===");
                    Debug.Log($"아틀라스 모드: {font.atlasPopulationMode}");
                    Debug.Log($"아틀라스 크기: {font.atlasWidth} x {font.atlasHeight}");
                    Debug.Log($"등록된 문자 수: {font.characterTable?.Count ?? 0}");
                    Debug.Log($"등록된 글리프 수: {font.glyphTable?.Count ?? 0}");
                    Debug.Log($"Multi Atlas: {font.isMultiAtlasTexturesEnabled}");

                    // 문제가 되는 문자 확인
                    char[] testChars = { '끝', '까', '려', '라', '료', '가', '나', '다', '종', '시', '작' };
                    StringBuilder result = new StringBuilder();
                    foreach (char c in testChars)
                    {
                        bool has = font.HasCharacter(c);
                        result.Append($"'{c}':{(has ? "O" : "X")} ");
                    }
                    Debug.Log($"문자 확인: {result}");
                }
            }
        }
    }
}
