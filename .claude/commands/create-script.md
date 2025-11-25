# Unity C# 스크립트 생성

새로운 Unity C# 스크립트를 생성합니다.

## 사용법
`/create-script [스크립트이름] [폴더경로]`

## 지시사항

다음 규칙을 따라 Unity C# 스크립트를 생성하세요:

1. **네임스페이스**: `HorrorGame` 사용
2. **파일 위치**: `Assets/Scripts/` 하위 폴더
3. **코드 스타일**:
   - XML 문서 주석 포함
   - SerializeField 사용 권장
   - 한글 주석 포함

## 템플릿

```csharp
using UnityEngine;

namespace HorrorGame
{
    /// <summary>
    /// [스크립트 설명]
    /// </summary>
    public class [클래스명] : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("설명")]
        [SerializeField] private float exampleValue = 1f;

        private void Awake()
        {
        }

        private void Start()
        {
        }

        private void Update()
        {
        }
    }
}
```

사용자가 요청한 스크립트: $ARGUMENTS
