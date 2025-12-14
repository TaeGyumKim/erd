# Claude Code 프로젝트 컨텍스트

## 프로젝트 개요

**프로젝트명**: Run 'Till the End (끝까지 달려라)
**엔진**: Unity 2022.3.21f1
**타겟 기기**: Meta Quest 3 + PC (Quest Link)
**장르**: VR 공포 탈출 게임
**전체 완성도**: 78% (2024-11 기준)

## 게임 컨셉

살인마를 피해 열쇠를 찾고 탈출하는 공포 게임

```
플레이어 시작 → 맵 탐색 → 열쇠 수집 → 살인마 회피/숨기 → 탈출구 도달 → 승리!
                              ↓
                        살인마에게 잡힘 → 게임오버
```

## 스토리 배경

- **장소**: 오래된 저택/실험실 - 연쇄 살인 사건과 초자연적 실험이 있었던 곳
- **살인마**: 과거 저택의 소유자 또는 연구자, 끔찍한 일을 저지름
- **플레이어**: 피실험자로 희생될 뻔했으나 가까스로 목숨을 구함, 방에서 깨어나 탈출 시도
- **유령**: 과거 희생자, 플레이어에게 힌트 제공

## 스토리 진행 단계

```
1. Introduction (도입)
   └─ 플레이어가 방에서 깨어남, YES 클릭 후 미션 시작

2. Exploration (탐색) - 30초 후 살인마 활성화
   └─ 단서 아이템 탐색 시작

3. ClueCollection (단서 수집)
   └─ USB → 라이터 → 보안카드 → 벽 문양 발견

4. FinalPuzzle (최종 퍼즐)
   └─ 단서 조합 → 열쇠 위치 파악

5. Escape (탈출)
   └─ 열쇠로 문 열기 → "이제 자유다" → 엔딩
```

## 단서 아이템 시스템

| 아이템 | 용도 | 구현 스크립트 |
|--------|------|---------------|
| USB 키 | 단말기에 꽂으면 메시지 재생 | `ComputerTerminal.cs` |
| 라이터 | 벽 문양 드러냄 (UV 효과) | `HiddenWallSymbol.cs` |
| 보안 카드 | 카드 리더로 잠금 해제 | `CardReader.cs` |
| 배터리 | 유령 기계 작동 | `BatteryItem.cs` |
| 기어 | 유령 기계 작동 | `ClueItem.cs` |
| 열쇠 | 최종 탈출문 열기 | `KeyItem.cs`, `ExitDoor.cs` |

## 등장인물

| 캐릭터 | 역할 | 구현 스크립트 | 완성도 |
|--------|------|---------------|--------|
| 플레이어 | 도망자/피실험자 | `VRPlayer.cs` | 95% |
| 살인마 | 추격자/저택 소유자 | `KillerAI.cs` | 95% |
| 유령 | 조수/힌트 제공 | `GhostAI.cs` | 50% |

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

## Model Context Protocol (MCP) 서버

Unity MCP 서버를 통해 AI가 Unity 에디터와 직접 상호작용할 수 있습니다.

### 설정

`.claude/settings.json`에 Unity MCP 서버가 설정되어 있습니다:

```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": [
        "E:/code/erd/Library/PackageCache/com.gamelovers.mcp-unity@0d46436568/Server~/build/index.js"
      ]
    }
  }
}
```

### 기능

- 🎮 **Unity 에디터 제어**: 씬, 게임오브젝트, 컴포넌트 관리
- 📁 **에셋 관리**: 프로젝트 파일 검색 및 조작
- 🔍 **코드 분석**: Unity의 컴파일러를 활용한 코드 분석
- 🎬 **씬 관리**: Hierarchy 검색 및 씬 조작
- 🐛 **디버깅**: 런타임 디버깅 지원

### 사용 방법

1. Unity 에디터 열기
2. Claude Code에서 MCP 서버가 자동으로 연결됨
3. AI가 Unity 명령을 실행할 수 있음

### 참고 자료

- [Unity MCP Server (CoplayDev)](https://github.com/CoplayDev/unity-mcp)
- [Model Context Protocol 공식 문서](https://modelcontextprotocol.io/)

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

## 구현 현황 (2024-11 기준)

### 시스템별 완성도

| 시스템 | 완성도 | 주요 스크립트 | 상태 |
|--------|--------|---------------|------|
| 플레이어 | 95% | `VRPlayer.cs`, `PlayerInventory.cs`, `VRFlashlight.cs` | 완성 |
| 살인마 AI | 95% | `KillerAI.cs` (468줄) | 완성 |
| 게임 관리 | 90% | `HorrorGameManager.cs`, `GameSetup.cs` | 완성 |
| 스토리 진행 | 90% | `StoryProgressManager.cs` (414줄) | 완성 |
| 상호작용 | 85% | `Door.cs`, `HidingSpot.cs`, `ExitDoor.cs` 등 12개 | 완성 |
| 오디오 | 90% | `HorrorSoundManager.cs` (486줄) | 완성 |
| UI | 85% | `VRHUD.cs` (378줄), `StoryUI.cs` | 완성 |
| VR 시스템 | 75% | `Quest3Controller.cs`, `VRComfortSettings.cs` | 보완 필요 |
| 유령 AI | 50% | `GhostAI.cs` | 미완성 |
| 도입/엔딩 연출 | 40% | - | 미구현 |

### 핵심 스크립트 요약

```
Assets/Scripts/
├── Player/
│   ├── VRPlayer.cs (242줄) - 스태미나, 숨기, 소음, 상태 관리
│   ├── PlayerInventory.cs (200줄) - 아이템/열쇠 관리
│   └── VRFlashlight.cs (214줄) - 손전등, 배터리 시스템
├── Enemy/
│   ├── KillerAI.cs (468줄) - Patrol→Search→Chase→Investigate 상태 머신
│   └── GhostAI.cs (100줄+) - 유령 AI [미완성]
├── Game/
│   ├── HorrorGameManager.cs (294줄) - 게임 상태, 열쇠, 타이머
│   ├── StoryProgressManager.cs (414줄) - 5단계 스토리 진행
│   ├── GameSetup.cs (210줄) - 난이도, 초기화
│   └── ObjectiveSystem.cs (283줄) - 목표 추적
├── Interaction/
│   ├── InteractableObject.cs (132줄) - 기본 클래스
│   ├── Door.cs (221줄) - 문 열기/잠금
│   ├── HidingSpot.cs (192줄) - 숨기 장소
│   ├── ExitDoor.cs (357줄) - 탈출문
│   ├── ComputerTerminal.cs - USB 읽기
│   ├── HiddenWallSymbol.cs - 라이터로 문양 발견
│   ├── CardReader.cs - 보안카드 인식
│   └── ClueItem.cs - 단서 아이템
├── Audio/
│   └── HorrorSoundManager.cs (486줄) - 배경음 3단계, 발소리, 속삭임, 심장박동
├── UI/
│   ├── VRHUD.cs (378줄) - 스태미나, 배터리, 열쇠, 시간
│   └── StoryUI.cs - 스토리 메시지
└── Editor/
    └── RunTillEndSceneBuilder.cs (926줄) - 씬 빌더 도구
```

## 외부 에셋

### 핵심 에셋 (게임에 필수)

| 에셋 | 용도 | 경로 |
|------|------|------|
| Asset pack for horror game | 병원 소품 (침대, 휠체어, 의자, 문, 열쇠 등) | `Assets/Asset pack for horror game/` |
| HospitalHorrorPack | 병원 모듈식 맵 (벽, 바닥, 천장, 문) | `Assets/Dnk_Dev/HospitalHorrorPack/` |
| Common/Mask | 살인마 마스크 모델 | `Assets/Common/Mask/` |
| Character | 캐릭터 모델, Sentis AI 얼굴 변형 | `Assets/Character/` |

### 보조 에셋

| 에셋 | 용도 | 경로 |
|------|------|------|
| StylizedHandPaintedDungeon | 던전/지하실 환경 | `Assets/StylizedHandPaintedDungeon(Free)/` |
| Mega Fantasy Props Pack | 판타지 소품 (배럴, 상자, 가구) | `Assets/Mega Fantasy Props Pack/` |
| PSX Crates and Barrels | 레트로 스타일 상자/배럴 | `Assets/McSteeg/` |
| MinesAndCaveSet | 광산/동굴 환경 | `Assets/LoafbrrAssets/MInesAndCaveSet/` |

### 애니메이션

```
Assets/Animations/
├── Idle/idle_anim.fbx
├── Walk/Walk_anim.fbx, Walk_anim_extended.fbx
├── Poses/PosesAnim.fbx
├── TPose.fbx
└── WalkInPlace.fbx
```

## 다음 작업 (우선순위순)

### 1. 유령(GhostAI) 완성 [높음]
- [ ] 힌트 제공 로직 구현
- [ ] 플레이어 접근 시 메시지 표시 ("도와달라", "그를 멈춰달라")
- [ ] Speaking, Guiding 상태 완성
- [ ] 유령 음성/속삭임 연동

### 2. 도입 시퀀스 [중간]
- [ ] 방에서 깨어나는 연출
- [ ] "YES" 버튼 클릭 UI
- [ ] 살인마 등장 타이밍 연출
- [ ] 초기 안내 메시지

### 3. 유령 기계 시스템 [중간]
- [ ] 기계 오브젝트 생성
- [ ] 배터리 + 기어 조합 시 작동
- [ ] 작동 후 유령 시각/사운드 강화

### 4. 엔딩 연출 [낮음]
- [ ] "이제 자유다" 텍스트 표시
- [ ] 유령 실루엣 등장 (선택)
- [ ] 조명 페이드 아웃
- [ ] 크레딧 또는 메인 메뉴 복귀

### 5. 추가 씬 [낮음]
- [ ] 메인 메뉴 씬
- [ ] 게임 오버 씬
- [ ] 승리 씬

## 에디터 도구

### RunTillEndSceneBuilder (Assets/Editor/)
`Horror Game > Run Till End Scene Builder` 메뉴로 접근

- **씬 생성**: 새 게임 씬 자동 설정
- **환경 배치**: 3가지 에셋 팩에서 오브젝트 배치
- **단서 아이템**: USB, 라이터, 보안카드 등 자동 생성
- **캐릭터 배치**: 살인마, 유령 배치
- **NavMesh 베이크**: AI 경로 자동 설정
- **씬 검증**: 필수 요소 확인

### HorrorGameMapTools (Assets/Editor/)
`Horror Game > 맵 제작 도구` 메뉴로 접근

- NavMesh 설정 및 베이크
- 살인마 AI 순찰 지점 설정
- 상호작용 오브젝트 빠른 생성
- 게임 설정 검증

## 테스트 체크리스트

### 핵심 기능
- [ ] 플레이어 이동/달리기/스태미나
- [ ] 숨기 장소에서 숨기
- [ ] 살인마 순찰/추격/감지
- [ ] 단서 아이템 수집 (USB, 라이터, 보안카드, 열쇠)
- [ ] USB 단말기 읽기
- [ ] 라이터로 벽 문양 발견
- [ ] 보안 카드로 잠금 해제
- [ ] 탈출문 열기

### VR 기능
- [ ] 컨트롤러 입력
- [ ] 손 추적
- [ ] 손전등 토글
- [ ] 아이템 잡기/놓기
- [ ] HUD 표시

### 오디오
- [ ] 배경음 3단계 전환
- [ ] 발소리
- [ ] 속삭임
- [ ] 심장 박동

## 개발 지침 및 해결된 이슈

### 문(Door) 시스템

#### 문 구조
- `Door - Room1` (부모): Door 컴포넌트가 있는 빈 오브젝트, 회전 축 역할
- `Door_01_C` (자식): 실제 문 메시와 Collider

#### 문 피벗(경첩) 설정
문이 중앙에서 회전하는 문제 해결:
1. `Door_01_C`의 **localPosition**을 문 너비 절반만큼 이동 (예: X = 0.5)
2. 부모 `Door - Room1`이 회전하면 자식이 경첩을 기준으로 회전함
3. Door.cs의 `pivotOffset` 옵션도 사용 가능 (런타임에만 적용)

```
// MCP로 설정 예시
update_component: Door - Room1/Door_01_C, Transform, localPosition: {x: 0.5, y: 0, z: 0}
```

#### 열쇠로 문 열기
- PCPlayerController에서 `GetComponentInParent<Door>()` 사용 필수
- 레이캐스트가 자식 메시(`Door_01_C`)에 히트하므로 부모에서 Door 컴포넌트 찾아야 함
- 잠긴 문 + 맞는 열쇠 = 문 제거 (`Destroy(door.gameObject)`)

### PC 플레이어 충돌 감지
- `CharacterController`는 `OnCollisionEnter` 대신 `OnControllerColliderHit` 사용
- Door.cs의 `OnTriggerEnter`/`OnCollisionEnter`는 CharacterController와 작동 안함
- PCPlayerController.cs에 `OnControllerColliderHit` 메서드로 문 충돌 감지 구현됨

### TextMesh Pro 한글 폰트
- NanumGothic SDF.asset 사용
- `m_AtlasPopulationMode: 1` (Dynamic) 설정 필요
- `m_SourceFontFile` 참조 필요
- 일부 한글 문자가 깨지면 Font Asset Creator로 한글 범위(AC00-D7AF) 재생성 필요

### KillerAI 제자리 걸음 문제
- Inspector에서 `patrolPoints` 배열에 순찰 지점 추가 필요
- 순찰 지점이 없으면 경고 로그: `[KillerAI] 순찰 지점(patrolPoints)이 설정되지 않았습니다!`

### KillerAnimator 파라미터 경고
- Animator에 없는 파라미터 설정 시 경고 발생
- `CheckAnimatorParameters()`로 파라미터 존재 여부 캐싱
- `SetFloatSafe()`, `SetBoolSafe()`, `TriggerAnimationSafe()` 메서드로 안전하게 설정

### IntroSequence PC 모드
- YES 버튼 클릭을 위해 `Cursor.lockState = CursorLockMode.None` 필요
- 도입 완료 후 `Cursor.lockState = CursorLockMode.Locked`로 복원

## 자주 사용하는 MCP 명령어

```
# GameObject 선택
select_gameobject: objectName: "Door - Room1"

# 컴포넌트 업데이트
update_component: objectPath: "Door - Room1", componentName: "HorrorGame.Door", componentData: {...}

# Transform 위치 변경
update_component: objectPath: "Door - Room1/Door_01_C", componentName: "Transform", componentData: {"localPosition": {"x": 0.5, "y": 0, "z": 0}}

# 스크립트 재컴파일
recompile_scripts

# 콘솔 로그 확인
get_console_logs: limit: 30, includeStackTrace: false
```

## VR 비밀번호 키패드 시스템

### 시스템 구조

```
┌─────────────────────────────────────────────────────────────┐
│                     비밀번호 시스템 흐름                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  PasswordBookTrigger (book - Room3)                         │
│  ├─ Awake: 랜덤 4자리 비밀번호 생성                           │
│  ├─ Start: ReadableNote.noteContent에 비밀번호 표시           │
│  └─ Start: PasswordPanel.SetPassword() 호출                 │
│                                                             │
│  PasswordPanel (Panel_Wood - Room5)                         │
│  ├─ Interact() → ShowPasswordInput()                        │
│  └─ VRPasswordKeypad.Instance.Open(title, length, callback, transform) │
│                                                             │
│  VRPasswordKeypad (World Space Canvas)                      │
│  ├─ Open(): 타겟 오브젝트 앞에 키패드 배치                     │
│  ├─ PositionNearTarget(): 타겟과 플레이어 사이에 배치          │
│  ├─ OnNumberClick(): 숫자 입력 및 DisplayText 업데이트        │
│  └─ SubmitPassword(): 콜백 호출 후 닫기                       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 주요 파일

| 파일 | 역할 |
|------|------|
| `VRPasswordKeypad.cs` | World Space Canvas 키패드 UI, 싱글톤 |
| `PasswordPanel.cs` | 비밀번호로 열리는 패널/문 컴포넌트 |
| `PasswordChest.cs` | 비밀번호로 열리는 상자 컴포넌트 |
| `PasswordBookTrigger.cs` | 책에서 랜덤 비밀번호 생성 및 배포 |
| `GamePopupUI.cs` | 2D UI 폴백 (VR 키패드 없을 때) |
| `PasswordKeypadSetup.cs` | 에디터 도구 (키패드 생성) |

### 씬에 필요한 오브젝트

1. **VRPasswordKeypad** - `Horror Game > VR 비밀번호 키패드 생성` 메뉴로 생성
2. **book - Room3** - PasswordBookTrigger + ReadableNote 컴포넌트
3. **Panel_Wood - Room5** - PasswordPanel 컴포넌트

### ⚠️ 중요: 실패 케이스 및 주의사항

#### 1. VRPasswordKeypad가 안 보이는 문제
**원인**: `positionNearTarget=false` 또는 고정 위치 사용 시 플레이어가 볼 수 없는 위치에 배치
**해결**:
- `positionNearTarget = true` 설정
- `Open()` 호출 시 반드시 `transform` 파라미터 전달
```csharp
// ✅ 올바른 호출
VRPasswordKeypad.Instance.Open(title, length, callback, transform);

// ❌ 잘못된 호출 (키패드가 고정 위치에 표시)
VRPasswordKeypad.Instance.Open(title, length, callback);
```

#### 2. 키패드가 한 번만 열리는 문제
**원인**: `if (isOpen) return;`으로 이미 열린 상태에서 재호출 차단
**해결**: `if (isOpen) { Close(); }` 로 변경하여 닫고 다시 열기

#### 3. 2D UI(GamePopupUI)가 계속 뜨는 문제
**원인**: VRPasswordKeypad.Instance가 null (씬에 없음)
**해결**:
- 씬에 VRPasswordKeypad 오브젝트 존재 확인
- `Horror Game > VR 비밀번호 키패드 생성` 메뉴로 생성

#### 4. PC에서 키패드 버튼 클릭 안됨
**원인**: PCPlayerController에서 UI Button 클릭 처리 누락
**해결**: `TryClickUIButton()` 메서드 추가
```csharp
// PCPlayerController.cs
private bool TryClickUIButton()
{
    // 레이캐스트로 Button 컴포넌트 찾기
    Button button = hit.collider.GetComponent<Button>();
    if (button == null)
        button = hit.collider.GetComponentInParent<Button>();

    if (button != null && button.interactable)
    {
        button.onClick.Invoke();
        return true;
    }
    return false;
}
```

#### 5. 비밀번호가 항상 "1234"인 문제
**원인**: PasswordBookTrigger가 씬에 없거나 PasswordPanel을 찾지 못함
**해결**:
- book - Room3에 PasswordBookTrigger 컴포넌트 추가
- PasswordBookTrigger.Start()에서 `FindObjectOfType<PasswordPanel>()` 자동 검색
- Panel_Wood - Room5의 correctPassword를 빈 문자열로 설정 (PasswordBookTrigger가 설정)

#### 6. World Space Canvas vs Screen Space Canvas 혼동
| 구분 | World Space Canvas | Screen Space Canvas |
|------|-------------------|---------------------|
| 용도 | VR 키패드 | 2D HUD, 메뉴 |
| 위치 | 3D 공간에 배치 | 화면에 고정 |
| 상호작용 | XR Ray Interactor | 마우스/터치 |
| 컴포넌트 | TrackedDeviceGraphicRaycaster | GraphicRaycaster |
| 예시 | VRPasswordKeypad | GamePopupUI |

### PC 테스트 방법

1. **양손 레이 시스템**
   - 레이가 2개 표시됨 (왼손: 빨강, 오른손: 초록)
   - Tab키로 활성 손 전환
   - 1키: 왼손 레이 조정 모드
   - 2키: 오른손 레이 조정 모드
   - R키: 레이 방향 리셋

2. **상호작용**
   - E키 또는 좌클릭: 상호작용/버튼 클릭
   - 레이가 버튼을 가리키면 Hover 효과

### 디버그 로그 확인

```
[PasswordBookTrigger] 비밀번호 패널에 비밀번호 설정: 7293
[PasswordPanel] Panel_Wood - Room5 VR 비밀번호 키패드 표시 (타겟 전달)
[VRPasswordKeypad] 키패드 열림: 비밀번호를 입력하세요, 4자리, 타겟: Panel_Wood - Room5
[VRPasswordKeypad] 타겟 앞에 위치: (-13.2, 1.8, 5.3), 타겟: Panel_Wood - Room5
[VRPasswordKeypad] 디스플레이 업데이트: 7 _ _ _
[VRPasswordKeypad] 비밀번호 제출: 7293
[PasswordPanel] Panel_Wood - Room5 비밀번호 정답!
```

## 싱글톤 추가 목록

```csharp
// UI 관련
VRPasswordKeypad.Instance  // VR 비밀번호 키패드
GamePopupUI.Instance       // 2D 팝업 UI
```

## 테스트 체크리스트 (비밀번호 시스템)

- [ ] book - Room3 읽으면 랜덤 비밀번호 표시
- [ ] Panel_Wood - Room5 상호작용 시 VR 키패드 표시
- [ ] 키패드가 패널과 플레이어 사이에 배치
- [ ] 숫자 입력 시 DisplayText에 숫자 표시 (마스킹 X)
- [ ] 올바른 비밀번호 입력 시 패널 사라짐
- [ ] 틀린 비밀번호 입력 시 오류 메시지
- [ ] PC에서 E키/클릭으로 키패드 버튼 작동
