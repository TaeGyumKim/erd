# 적 AI 생성

새로운 적 AI 스크립트를 생성합니다.

## 사용법
`/add-enemy [적이름]`

## 지시사항

`KillerAI`를 참조하여 새 적 AI 스크립트를 생성하세요:

1. **위치**: `Assets/Scripts/Enemy/`
2. **필수 컴포넌트**: `NavMeshAgent`
3. **필수 기능**:
   - 상태 머신 (Patrol, Search, Chase 등)
   - 시야 감지 (`CanSeePlayer`)
   - 청각 감지 (`HearNoise`)

## 참조
- KillerAI.cs: 기본 살인마 AI

사용자 요청: $ARGUMENTS
