# Quest 3 연결 설정

Unity에서 Quest 3를 인식하도록 설정해봅시다!

---

## 전체 과정 미리보기

```
1. XR Plugin Management 설치
        ↓
2. OpenXR 켜기
        ↓
3. Meta Quest 지원 켜기
        ↓
4. Input System 설정
        ↓
5. 샘플 설치
```

하나씩 따라가면 돼요!

---

## Step 1: XR Plugin Management 설치

XR Plugin Management는 VR 기기들을 관리하는 도구예요.

### 설치하기

1. Unity 상단 메뉴: **Edit** > **Project Settings** 클릭

   ![Project Settings 위치](/docs/images/project-settings-menu.png)

2. 왼쪽 목록에서 **XR Plug-in Management** 클릭

3. 처음이면 **"Install XR Plugin Management"** 버튼이 보여요
   - 이 버튼을 클릭하세요
   - 설치될 때까지 기다리기 (1~2분)

4. 설치가 끝나면 설정 화면이 나타나요

---

## Step 2: OpenXR 켜기

OpenXR은 여러 VR 기기를 지원하는 표준이에요.
Quest 3도 OpenXR로 연결해요.

### Windows 탭에서 설정하기

> **중요!** 탭이 두 개 있어요 (Windows, Android). 지금은 **Windows** 탭에서 해요.

1. **XR Plug-in Management** 화면에서
2. 위에 **Windows 아이콘 탭**이 선택되어 있는지 확인
3. **OpenXR** 체크박스를 클릭해서 **켜기(체크)**

### 컨트롤러 프로필 추가하기

OpenXR을 켜면 왼쪽에 **"OpenXR"** 메뉴가 생겨요.

1. 왼쪽에서 **OpenXR** 클릭
2. **Interaction Profiles** 섹션 찾기
3. **"+" 버튼** 클릭
4. 목록에서 **"Oculus Touch Controller Profile"** 선택

> Quest 3 컨트롤러를 "Oculus Touch"라고 불러요 (옛날 이름이에요)

---

## Step 3: Meta Quest 지원 켜기

### OpenXR Features 설정

1. 같은 **OpenXR** 설정 화면에서
2. **OpenXR Feature Groups** 섹션 찾기
3. 다음 항목들을 **체크**:

| 항목 | 설명 |
|------|------|
| **Meta Quest Support** | Quest 3 지원 (필수!) |
| **Hand Tracking Subsystem** | 손 추적 기능 (선택) |

> Hand Tracking은 컨트롤러 없이 손만으로 조작하는 기능이에요.
> 나중에 써보고 싶으면 체크해두세요.

---

## Step 4: Input System 설정

Unity에는 입력을 처리하는 방식이 2가지 있어요:
- **Old Input System** (옛날 방식)
- **New Input System** (새 방식)

VR은 둘 다 필요해서 **"Both"** 로 설정해야 해요.

### 설정하기

1. **Edit** > **Project Settings** (이미 열려있으면 그대로)
2. 왼쪽에서 **Player** 클릭
3. 스크롤해서 **Other Settings** 섹션 찾기
4. 더 스크롤해서 **Active Input Handling** 찾기
5. 드롭다운을 클릭해서 **"Both"** 선택

### Unity 재시작

설정을 바꾸면 이런 창이 나와요:
```
"Unity 에디터를 재시작해야 합니다"
```

**"Yes"** 를 클릭하세요. Unity가 자동으로 껐다 켜져요.

---

## Step 5: 필수 샘플 설치

XR Interaction Toolkit의 샘플(예제)을 설치하면 편해요.

### Package Manager 열기

1. Unity 상단 메뉴: **Window** > **Package Manager**
2. Package Manager 창이 열려요

### XR Interaction Toolkit 찾기

1. 왼쪽 위 드롭다운이 **"Packages: In Project"** 인지 확인
2. 목록에서 **"XR Interaction Toolkit"** 찾아서 클릭
3. 없으면 드롭다운을 **"Unity Registry"** 로 바꾸고 찾기

### 샘플 설치하기

1. 오른쪽에 패키지 정보가 나와요
2. **"Samples"** 탭 클릭 (버전 정보 아래에 있어요)
3. 다음 항목들을 **"Import"** 클릭:

| 샘플 이름 | 설명 | 필수? |
|-----------|------|-------|
| **Starter Assets** | 기본 VR 설정 프리팹들 | 필수! |
| **XR Device Simulator** | 헤드셋 없이 테스트 | 강력 추천 |

> Import 버튼을 누르면 프로젝트에 샘플이 추가돼요.

---

## 설정 완료 확인

모든 설정이 제대로 됐는지 확인해볼까요?

### 체크리스트

- [ ] XR Plug-in Management에서 **OpenXR** 체크됨
- [ ] OpenXR에서 **Oculus Touch Controller Profile** 추가됨
- [ ] OpenXR에서 **Meta Quest Support** 체크됨
- [ ] Player > Other Settings에서 **Active Input Handling**이 **Both**
- [ ] XR Interaction Toolkit의 **Starter Assets** 설치됨

전부 체크됐으면 성공!

---

## 다음 단계

Quest 3 연결 준비가 끝났어요! 이제 첫 번째 VR 씬을 만들어봅시다.

[다음: 첫 번째 VR 씬 만들기 >](05-첫번째-VR씬.md)

---

## 자주 묻는 질문

### Q: OpenXR이 목록에 없어요
XR Plug-in Management가 제대로 설치 안 됐을 수 있어요.
Project Settings > XR Plug-in Management에서 "Install" 버튼을 다시 확인하세요.

### Q: Meta Quest Support가 없어요
Unity 버전이 너무 낮거나 OpenXR 패키지가 오래됐을 수 있어요.
Package Manager에서 OpenXR을 최신 버전으로 업데이트해보세요.

### Q: "Both"로 바꿨는데 재시작 창이 안 떠요
이미 "Both"로 설정되어 있었을 거예요. 문제없이 진행하면 돼요.
