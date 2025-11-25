# Claude Code 프로젝트 컨텍스트

## 프로젝트 개요

**프로젝트명**: VR Horror Escape Game
**엔진**: Unity 2022.3.21f1
**타겟 기기**: Meta Quest 3 + PC (Quest Link)
**장르**: VR 공포 탈출 게임

## 게임 컨셉

살인마를 피해 열쇠를 찾고 탈출하는 공포 게임

```
플레이어 시작 → 맵 탐색 → 열쇠 수집 → 살인마 회피/숨기 → 탈출구 도달 → 승리!
                              ↓
                        살인마에게 잡힘 → 게임오버
```

## 코드 컨벤션

### 네임스페이스
- 모든 스크립트는 `HorrorGame` 네임스페이스 사용

### 파일 구조
```
Assets/Scripts/
├── Player/      - 플레이어 (VRPlayer, VRFlashlight, PlayerInventory)
├── Enemy/       - 적 AI (KillerAI)
├── Interaction/ - 상호작용 (Door, HidingSpot, PickupItem, ReadableNote, DoorPeek 등)
├── Game/        - 게임 시스템 (HorrorGameManager, ObjectiveSystem, CheckpointSystem 등)
├── UI/          - UI (VRHUD, VRMenuUI, NoteUI)
├── VR/          - VR 전용 (Quest3Controller, VRComfortSettings 등)
├── Effects/     - 효과 (HeartbeatEffect, BreathingSystem)
├── Audio/       - 오디오 (FootstepSystem)
├── Environment/ - 환경 (LightFlicker)
└── Utility/     - 유틸리티
```

### 싱글톤 패턴
다음 클래스들은 `Instance` 프로퍼티로 접근:
- `VRPlayer.Instance`
- `PlayerInventory.Instance`
- `HorrorGameManager.Instance`
- `HorrorAudioManager.Instance`
- `VRHUD.Instance`
- `VRMenuUI.Instance`
- `ObjectiveSystem.Instance`
- `CheckpointSystem.Instance`
- `VRComfortSettings.Instance`

### 주석
- 한글 주석 사용
- XML 문서 주석(`///`) 권장
- Tooltip 어트리뷰트로 Inspector 설명

## 주요 시스템

### 플레이어
- **스태미나**: 달리기 시 소모, 시간 경과로 회복
- **숨기**: `HidingSpot`에서 숨기 가능, 숨는 동안 적에게 보이지 않음
- **소음**: 걷기/달리기 시 소음 발생, 적이 감지 가능

### 살인마 AI
- **상태**: Patrol → Search → Chase → Investigate
- **감지**: 시야(거리+각도) + 청각(소음 범위)
- **숨은 플레이어**: 감지 불가

### 열쇠 시스템
- `KeyItem.keyId` ↔ `Door.requiredKeyId` 매칭
- `PlayerInventory`에서 열쇠 관리

## VR 패키지

- XR Interaction Toolkit 2.5.2
- OpenXR 1.9.1
- Oculus XR Plugin 4.1.2
- XR Hands 1.3.0

## 슬래시 명령어

- `/create-script` - 새 스크립트 생성
- `/add-interaction` - 인터랙션 오브젝트 생성
- `/add-enemy` - 적 AI 생성
- `/review` - 코드 리뷰
- `/status` - 프로젝트 상태 확인

## 자주 하는 작업

### 새 인터랙션 추가
1. `InteractableObject` 상속
2. `OnSelectEntered` 오버라이드
3. Inspector 필드 추가

### 새 아이템 추가
1. `PickupItem` 상속
2. `itemData` 설정
3. `Collect()` 커스터마이즈

### 새 적 추가
1. `KillerAI` 참조
2. `NavMeshAgent` 필요
3. 상태 머신 구현
