# VR Game Project

Unity 2022.3.21f1 기반 VR 게임 프로젝트 (학교 과제용)

---

## 목차

1. [Unity 설치하기](#1-unity-설치하기)
2. [프로젝트 열기](#2-프로젝트-열기)
3. [VR 설정하기](#3-vr-설정하기)
4. [프로젝트 구조 설명](#4-프로젝트-구조-설명)
5. [자주 발생하는 문제 해결](#5-자주-발생하는-문제-해결)

---

## 1. Unity 설치하기

### Step 1: Unity Hub 다운로드

Unity Hub는 Unity 버전들을 관리하는 프로그램입니다.

**Windows:**

1. 웹 브라우저에서 `https://unity.com/kr/download` 접속
2. "Download Unity Hub" 버튼 클릭
3. 다운로드된 `UnityHubSetup.exe` 실행
4. 설치 마법사 따라서 "다음" 계속 클릭
5. 설치 완료 후 Unity Hub 실행

**Mac:**

1. 웹 브라우저에서 `https://unity.com/kr/download` 접속
2. "Download Unity Hub" 버튼 클릭
3. 다운로드된 `UnityHubSetup.dmg` 실행
4. Unity Hub 아이콘을 Applications 폴더로 드래그
5. Applications에서 Unity Hub 실행

### Step 2: Unity 계정 만들기 (처음인 경우)

1. Unity Hub 실행
2. 좌측 상단 사람 아이콘 클릭
3. "Create account" 클릭
4. 이메일, 비밀번호 입력 후 계정 생성
5. 이메일 인증 완료
6. Unity Hub에서 로그인

### Step 3: Unity 라이선스 활성화

1. Unity Hub 좌측 상단 톱니바퀴(설정) 클릭
2. "Licenses" 탭 클릭
3. "Add" 버튼 클릭
4. "Get a free personal license" 선택
5. "Agree and get personal edition license" 클릭

### Step 4: Unity 2022.3.21f1 설치

**중요: 반드시 이 버전을 설치해야 합니다!**

1. Unity Hub 좌측 메뉴에서 "Installs" 클릭
2. 우측 상단 "Install Editor" 버튼 클릭
3. "Archive" 탭 클릭
4. "download archive" 링크 클릭 (웹페이지 열림)
5. 웹페이지에서 "Unity 2022.X" 선택
6. `2022.3.21f1` 찾아서 "Unity Hub" 버튼 클릭
7. Unity Hub가 열리면 다음 모듈 체크:
   - ✅ Android Build Support
   - ✅ Android SDK & NDK Tools
   - ✅ OpenJDK
8. "Install" 클릭
9. 설치 완료까지 대기 (30분~1시간 소요)

---

## 2. 프로젝트 열기

### Step 1: 프로젝트 폴더 열기

1. Unity Hub 실행
2. 좌측 메뉴에서 "Projects" 클릭
3. 우측 상단 "Open" 버튼 클릭
4. 이 프로젝트 폴더 선택 (이 README.md가 있는 폴더)
5. "Open" 또는 "폴더 선택" 클릭

### Step 2: 첫 실행 시 패키지 설치 대기

처음 열 때 시간이 오래 걸립니다! (5~15분)

1. Unity 에디터가 열리면 하단에 진행 바가 표시됨
2. "Importing Assets" 또는 "Compiling Scripts" 메시지가 사라질 때까지 대기
3. 절대로 중간에 Unity를 끄지 마세요!

---

## 3. VR 설정하기

### Step 1: XR Plugin Management 설정

1. 상단 메뉴에서 `Edit` 클릭
2. `Project Settings...` 클릭
3. 좌측 목록에서 `XR Plug-in Management` 클릭
4. 처음이면 "Install XR Plugin Management" 버튼 클릭 후 대기

### Step 2: PC VR 설정 (SteamVR, Oculus Link 등)

Windows/Mac 탭에서:

1. `XR Plug-in Management` 선택된 상태 확인
2. ✅ `OpenXR` 체크
3. 좌측에서 `OpenXR` 하위 메뉴 클릭
4. `Interaction Profiles`에서 `+` 버튼 클릭
5. 사용할 컨트롤러 선택:
   - Meta Quest: `Oculus Touch Controller Profile`
   - HTC Vive: `HTC Vive Controller Profile`
   - Valve Index: `Valve Index Controller Profile`

### Step 3: Meta Quest 독립 실행형 설정 (선택사항)

Quest에서 직접 실행하려면:

1. Project Settings 창에서 상단 Android 탭(로봇 아이콘) 클릭
2. ✅ `Oculus` 체크
3. 좌측에서 `Oculus` 하위 메뉴 클릭
4. `Target Devices`에서 사용할 기기 선택

### Step 4: Input System 설정

1. 상단 메뉴 `Edit` → `Project Settings...`
2. 좌측에서 `Player` 클릭
3. `Other Settings` 섹션 찾기 (스크롤 내리기)
4. `Active Input Handling`을 `Both`로 변경
5. Unity 재시작 팝업이 뜨면 "Yes" 클릭

---

## 4. 프로젝트 구조 설명

```text
Assets/                    ← 게임에 들어가는 모든 파일
├── Scripts/              ← C# 코드 파일 (.cs)
├── Scenes/               ← 게임 씬/레벨 파일 (.unity)
├── Prefabs/              ← 재사용 가능한 게임 오브젝트
├── Materials/            ← 색상/질감 설정 파일
├── Models/               ← 3D 모델 파일 (.fbx, .obj)
├── Textures/             ← 이미지 파일 (.png, .jpg)
└── Audio/                ← 소리 파일 (.mp3, .wav)

Packages/                  ← Unity 패키지 설정 (건드리지 마세요)
ProjectSettings/           ← 프로젝트 설정 (건드리지 마세요)
```

### 설치된 VR 패키지

| 패키지 이름 | 용도 |
|-------------|------|
| XR Interaction Toolkit | VR에서 물건 잡기, 버튼 누르기 등 |
| OpenXR | 다양한 VR 기기 지원 |
| Input System | 컨트롤러 입력 처리 |

---

## 5. 자주 발생하는 문제 해결

### "Unity 버전이 다릅니다" 경고

→ Unity Hub에서 정확히 `2022.3.21f1` 버전을 설치했는지 확인

### 프로젝트 열 때 에러가 많이 뜸

→ 정상입니다! 하단 Console 창의 에러가 사라질 때까지 기다리세요

### VR 헤드셋이 인식되지 않음

**Windows:**

1. SteamVR 또는 Oculus 앱이 실행 중인지 확인
2. 헤드셋이 PC에 연결되어 있는지 확인
3. Unity에서 `Edit` → `Project Settings` → `XR Plug-in Management`에서 OpenXR 체크 확인

**Mac:**

- Mac은 대부분의 VR 헤드셋을 지원하지 않습니다
- Quest Link나 SteamVR은 Windows에서만 작동합니다
- Mac에서는 시뮬레이터로 테스트하거나 Android로 빌드하세요

### Console에 빨간 에러가 계속 남아있음

1. 상단 메뉴 `Assets` → `Reimport All` 클릭
2. 시간이 오래 걸리니 기다리기

### 스크립트 수정 후 변경사항이 적용 안됨

1. Unity 에디터 클릭해서 포커스 주기
2. 자동으로 컴파일됨 (하단에 로딩 표시)
3. 안되면 `Ctrl+R` (Windows) 또는 `Cmd+R` (Mac)

---

## 유용한 단축키

| 동작 | Windows | Mac |
|------|---------|-----|
| 게임 실행/정지 | `Ctrl + P` | `Cmd + P` |
| 저장 | `Ctrl + S` | `Cmd + S` |
| 실행 취소 | `Ctrl + Z` | `Cmd + Z` |
| 씬 뷰에서 이동 | 우클릭 + WASD | 우클릭 + WASD |
| 오브젝트에 포커스 | `F` | `F` |

---

## 도움이 필요하면

- [Unity 공식 문서](https://docs.unity3d.com/)
- [Unity Learn (무료 강의)](https://learn.unity.com/)
- [XR Interaction Toolkit 문서](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.5/manual/index.html)
