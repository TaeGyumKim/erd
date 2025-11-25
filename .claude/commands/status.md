# 프로젝트 상태 확인

현재 프로젝트 상태를 확인합니다.

## 사용법
`/status`

## 확인 항목

다음 항목을 확인하고 보고하세요:

1. **스크립트 현황**
   - `Assets/Scripts/` 하위 모든 .cs 파일 목록
   - 각 폴더별 스크립트 수

2. **설정 파일**
   - `Packages/manifest.json` 패키지 목록
   - `ProjectSettings/` 주요 설정

3. **구현 상태**
   - 플레이어 시스템: VRPlayer, VRFlashlight, PlayerInventory
   - 적 AI: KillerAI
   - 인터랙션: Door, HidingSpot, PickupItem, KeyItem, BatteryItem
   - 게임 관리: HorrorGameManager, EscapeZone, JumpScare
   - UI: VRHUD, VRMenuUI

4. **TODO/FIXME 검색**
   - 코드 내 TODO 주석 찾기
