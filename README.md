# VR Horror Escape Game

Unity로 만드는 VR 공포 탈출 게임 (학교 과제용)

---

## 이게 뭔가요?

**살인마를 피해 열쇠를 찾고 탈출하는** VR 공포 게임이에요!

```text
게임 시작 → 맵 탐색 → 열쇠 찾기 → 탈출! (승리)
              ↓
         살인마에게 잡힘 → 게임오버
```

---

## 뭐가 필요한가요?

| 필요한 것 | 설명 |
|-----------|------|
| **Unity 2022.3.21f1** | 게임 만드는 프로그램 |
| **Meta Quest 3** | VR 헤드셋 |
| **USB-C 케이블** | Quest 3와 PC 연결용 |
| **괜찮은 PC** | GTX 1060 이상 그래픽카드 |

---

## 어디서부터 시작하나요?

### 순서대로 따라하세요

1. **[사전 준비](docs/01-사전준비.md)** - 뭐가 필요한지 확인
2. **[Unity 설치](docs/02-Unity-설치.md)** - Unity Hub와 Unity 설치
3. **[프로젝트 열기](docs/03-프로젝트-열기.md)** - 이 프로젝트 열기
4. **[Quest 3 연결](docs/04-Quest3-연결.md)** - VR 연결 설정
5. **[첫 번째 VR 씬](docs/05-첫번째-VR씬.md)** - VR 세계 만들기
6. **[테스트하기](docs/06-테스트하기.md)** - 만든 거 테스트

### 게임 시스템 알아보기

- **[공포 게임 시스템](docs/07-공포게임-시스템.md)** - 플레이어, 살인마, 아이템
- **[VR 스크립트](docs/08-VR-스크립트.md)** - VR 전용 기능들
- **[컨트롤러 가이드](docs/09-컨트롤러-가이드.md)** - Quest 3 조작법
- **[추가 기능](docs/11-추가기능.md)** - 발소리, 심장박동, 메모, 저장 등
- **[맵 제작 가이드](docs/12-맵제작-가이드.md)** - NavMesh, 디자이너용 도구

### 문제가 생기면

- **[문제 해결](docs/10-문제해결.md)** - 자주 묻는 질문

---

## 프로젝트 구조

```text
이 폴더/
├── Assets/
│   ├── Scripts/          ← 코드들
│   │   ├── Player/           - 플레이어 관련
│   │   ├── Enemy/            - 살인마 AI
│   │   ├── Interaction/      - 문, 열쇠, 아이템, 메모
│   │   ├── Game/             - 게임 관리, 목표, 저장
│   │   ├── UI/               - 화면 UI
│   │   ├── VR/               - VR 기능, 편의 설정
│   │   ├── Effects/          - 심장박동, 호흡 효과
│   │   ├── Audio/            - 발소리 시스템
│   │   └── Environment/      - 조명 깜빡임 등
│   ├── Scenes/           ← 게임 장면들
│   └── ...
├── docs/                 ← 문서들
├── README.md             ← 지금 보고 있는 파일
└── CLAUDE.md             ← AI 도우미 설정
```

---

## 빠른 시작 (이미 Unity를 알면)

```bash
# 1. Unity 2022.3.21f1로 프로젝트 열기

# 2. Package Manager에서 확인
# - XR Interaction Toolkit 2.5.2
# - OpenXR 1.9.1
# - XR Hands 1.3.0

# 3. Project Settings > XR Plug-in Management
# - OpenXR 체크
# - Oculus Touch Controller Profile 추가
# - Meta Quest Support 체크

# 4. Quest Link 연결 후 Play
```

---

## 주요 스크립트

### 핵심 시스템

| 스크립트 | 설명 |
|----------|------|
| `VRPlayer` | 플레이어 (스태미나, 숨기) |
| `VRFlashlight` | 손전등 (배터리) |
| `KillerAI` | 살인마 (순찰, 추적) |
| `HorrorGameManager` | 게임 상태 관리 |

### 상호작용

| 스크립트 | 설명 |
|----------|------|
| `Door` | 문 (열쇠로 잠금) |
| `KeyItem` | 열쇠 아이템 |
| `HidingSpot` | 숨는 장소 |
| `ReadableNote` | 읽을 수 있는 메모 |

### 분위기/효과

| 스크립트 | 설명 |
|----------|------|
| `HeartbeatEffect` | 심장박동 효과 |
| `BreathingSystem` | 호흡 효과 |
| `FootstepSystem` | 발소리 시스템 |
| `LightFlicker` | 조명 깜빡임 |

### 진행/저장

| 스크립트 | 설명 |
|----------|------|
| `ObjectiveSystem` | 목표/퀘스트 관리 |
| `CheckpointSystem` | 체크포인트 저장 |
| `RandomEventTrigger` | 랜덤 이벤트 |

---

## 단축키

| 동작 | Windows | Mac |
|------|---------|-----|
| 게임 실행 | `Ctrl + P` | `Cmd + P` |
| 저장 | `Ctrl + S` | `Cmd + S` |
| 되돌리기 | `Ctrl + Z` | `Cmd + Z` |

---

## 도움이 필요하면

- [Unity VR 공식 문서](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.5/manual/index.html)
- [Meta Quest 개발 문서](https://developer.oculus.com/documentation/)
- [문제 해결 가이드](docs/10-문제해결.md)
