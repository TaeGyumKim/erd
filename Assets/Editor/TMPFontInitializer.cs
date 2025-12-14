// TMP Korean Font Initializer - Auto-fixes Korean font rendering issues
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text;

namespace HorrorGame.Editor
{
    /// <summary>
    /// 에디터 시작 시 TMP 폰트를 자동으로 초기화
    /// </summary>
    public static class TMPFontInitializer
    {
        private const string PREF_KEY = "TMPFontInitialized_v4";

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            Debug.Log("[TMPFontInitializer] OnEditorLoad 호출됨");

            // 이미 초기화되었으면 스킵
            if (EditorPrefs.GetBool(PREF_KEY, false))
            {
                Debug.Log("[TMPFontInitializer] 이미 초기화됨, 스킵. 강제 초기화: Horror Game > Force Initialize TMP Fonts Now");
                return;
            }

            // 지연 실행 (에디터 로드 완료 후)
            EditorApplication.delayCall += InitializeFonts;
        }

        private static void InitializeFonts()
        {
            Debug.Log("[TMPFontInitializer] 폰트 초기화 시작...");

            string[] guids = AssetDatabase.FindAssets("NanumGothic t:TMP_FontAsset");
            int count = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

                if (font != null)
                {
                    InitializeFont(font);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            EditorPrefs.SetBool(PREF_KEY, true);

            Debug.Log($"[TMPFontInitializer] {count}개 폰트 초기화 완료");
        }

        private static void InitializeFont(TMP_FontAsset font)
        {
            Debug.Log($"[TMPFontInitializer] 초기화 중: {font.name}");

            // 폰트 아틀라스 초기화
            font.ClearFontAssetData();

            // 게임에서 사용하는 필수 문자 추가
            string essentialChars = GetEssentialCharacters();
            font.TryAddCharacters(essentialChars, out string missing);

            int added = essentialChars.Length - (string.IsNullOrEmpty(missing) ? 0 : missing.Length);
            Debug.Log($"[TMPFontInitializer] {font.name}: {added}자 추가됨");

            EditorUtility.SetDirty(font);
        }

        private static string GetEssentialCharacters()
        {
            StringBuilder sb = new StringBuilder();

            // ASCII 기본 문자
            for (int i = 32; i <= 126; i++)
            {
                sb.Append((char)i);
            }

            // 한글 자모
            for (int i = 0x3131; i <= 0x3163; i++)
            {
                sb.Append((char)i);
            }

            // 게임에서 사용하는 주요 한글
            sb.Append("끝까지달려라종료시작게임설정옵션");
            sb.Append("계속하기새로시작메인메뉴");
            sb.Append("비밀번호입력확인취소닫기열기저장");
            sb.Append("눈을떠라뜨떠플레이어탈출승리패배사망");
            sb.Append("손전등켜기끄기숨기나가기문열쇠잠금해제");
            sb.Append("살인마유령힌트경고주의위험안전");
            sb.Append("단서발견했습니다아이템획득");
            sb.Append("스태미나체력배터리시간남은");
            sb.Append("찾기위치이동점프달리기걷기멈추기");
            sb.Append("낡은메모지하실의");
            sb.Append("절대잊지말것비번");

            return sb.ToString();
        }

        [MenuItem("Horror Game/Reset Font Initialization Flag")]
        private static void ResetFlag()
        {
            EditorPrefs.DeleteKey(PREF_KEY);
            Debug.Log("[TMPFontInitializer] 초기화 플래그 리셋됨. 에디터 재시작 시 폰트가 다시 초기화됩니다.");
        }

        [MenuItem("Horror Game/Force Initialize TMP Fonts Now")]
        private static void ForceInitialize()
        {
            EditorPrefs.DeleteKey(PREF_KEY);
            InitializeFonts();
        }
    }
}
