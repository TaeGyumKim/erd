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
- Input System 1.7.0

### Input System 설정
- **Active Input Handling**: Both (New Input System + Legacy)
- ProjectSettings > Player > Other Settings > Active Input Handling = Both
- VR 컨트롤러와 XR Interaction을 위해 필요

## 슬래시 명령어

- `/create-script` - 새 스크립트 생성
- `/add-interaction` - 인터랙션 오브젝트 생성
- `/add-enemy` - 적 AI 생성
- `/review` - 코드 리뷰
- `/status` - 프로젝트 상태 확인

## 프로젝트 설정

### Unity Editor 설정

1. **Input System**
   - Edit > Project Settings > Player > Other Settings
   - Active Input Handling: **Both**
   - 이 설정이 `-1`이면 에러 발생

2. **XR 설정**
   - Edit > Project Settings > XR Plug-in Management
   - PC: OpenXR 활성화
   - Android: Oculus 활성화

3. **필수 파일**
   - `ProjectSettings/XRPackageSettings.asset`: JSON 형식으로 유지
   - `Assets/XR/`: XR 로더 및 OpenXR 설정
   - `Assets/XRI/`: XR Interaction 설정
   - 이 파일들은 Git에 커밋해야 VR 설정이 보존됨

### 개발 도구

- **맵 제작 도구**: `Horror Game > 맵 제작 도구` 메뉴
  - NavMesh 설정 및 베이크
  - 살인마 AI 생성 및 순찰 지점 설정
  - 상호작용 오브젝트 빠른 생성
  - 게임 설정 검증

## Git 설정

### 커밋 대상
- ✅ Assets/Scripts/ (모든 스크립트)
- ✅ Assets/Editor/ (에디터 도구)
- ✅ Assets/XR/ (VR 로더 설정)
- ✅ Assets/XRI/ (XR Interaction 설정)
- ✅ ProjectSettings/ (프로젝트 설정)
- ✅ Packages/manifest.json (패키지 의존성)

### 제외 대상
- ❌ Library/ (Unity 캐시)
- ❌ Temp/, Obj/, Builds/ (임시 파일)
- ❌ .vs/, .idea/ (IDE 설정)
- ❌ .claude/settings.local.json (로컬 설정)

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

## 알려진 이슈 및 해결방법

### Input System 에러

**증상**: `ArgumentException: Invalid value of 'activeInputHandler' setting: -1`

**해결**:

```text
ProjectSettings/ProjectSettings.asset 파일에서
activeInputHandler: 2 로 설정
```

### XRPackageSettings JSON 에러

**증상**: `JSON parse error: Invalid value`

**해결**:

```json
ProjectSettings/XRPackageSettings.asset를 JSON 형식으로:
{
    "m_Settings": []
}
```

### NavigationStatic 경고

**증상**: `CS0618: 'StaticEditorFlags.NavigationStatic' is obsolete`

**해결**: `#pragma warning disable CS0618`로 경고 억제 (Unity 2022.3에서는 정상 작동)
