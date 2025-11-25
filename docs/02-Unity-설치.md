# Unity 설치하기

Unity는 게임을 만드는 프로그램이에요. VR 게임도 Unity로 만들 수 있어요!

---

## Unity Hub란?

Unity Hub는 여러 버전의 Unity를 관리하는 프로그램이에요.
Unity를 직접 설치하는 게 아니라, Unity Hub를 먼저 설치하고 그 안에서 Unity를 설치해요.

왜 이렇게 하냐면:
- Unity 버전이 엄청 많아요 (2019, 2020, 2021, 2022...)
- 프로젝트마다 다른 버전을 써야 할 수 있어요
- Hub가 이걸 편하게 관리해줘요

---

## Unity Hub 설치하기

### Windows에서 설치하기

**1단계: 다운로드 페이지 열기**

웹 브라우저에서 열어주세요:
```
https://unity.com/kr/download
```

**2단계: 다운로드**

"Download Unity Hub" 버튼을 클릭하세요.

**3단계: 설치하기**

1. 다운로드된 `UnityHubSetup.exe`를 더블클릭
2. "동의함" 체크하고 "설치" 클릭
3. 끝나면 "마침" 클릭

**4단계: Unity Hub 실행**

바탕화면에 생긴 Unity Hub 아이콘을 더블클릭!

---

### Mac에서 설치하기

**1단계: 다운로드**

```
https://unity.com/kr/download
```
에서 "Download Unity Hub" 클릭

**2단계: 설치하기**

1. 다운로드된 `UnityHubSetup.dmg` 더블클릭
2. Unity Hub 아이콘을 Applications 폴더로 드래그
3. Applications에서 Unity Hub 실행

---

## Unity 계정 만들기

Unity Hub를 처음 실행하면 로그인하라고 해요.

### 계정이 없다면

1. "Create account" 또는 "계정 만들기" 클릭
2. 이메일, 비밀번호 입력
3. 이메일로 온 인증 메일 확인
4. 완료!

### 이미 계정이 있다면

그냥 로그인하면 돼요.

---

## 무료 라이선스 받기

Unity를 사용하려면 라이선스가 필요해요. 학생/개인은 무료예요!

**라이선스 받는 법:**

1. Unity Hub 왼쪽 아래 **톱니바퀴(설정)** 클릭
2. **"Licenses"** (라이선스) 클릭
3. **"Add"** (추가) 버튼 클릭
4. **"Get a free personal license"** 선택
5. 끝!

---

## Unity 2022.3.21f1 설치하기

### 왜 이 버전이어야 할까요?

이 프로젝트는 Unity 2022.3.21f1로 만들어졌어요.
다른 버전을 쓰면 오류가 날 수 있어요!

> **LTS**란? Long Term Support의 약자로, 오래 지원해주는 안정적인 버전이에요.

### 설치 순서

**1단계: Install Editor 클릭**

Unity Hub 왼쪽 메뉴에서 **"Installs"** 클릭
오른쪽 위 **"Install Editor"** 버튼 클릭

**2단계: Archive에서 찾기**

1. 맨 위 탭에서 **"Archive"** 클릭
2. **"download archive"** 파란 글자 클릭
3. 웹 브라우저가 열려요

**3단계: 정확한 버전 찾기**

1. 페이지에서 **"Unity 2022.X"** 섹션 찾기
2. **`2022.3.21f1`** 찾기
3. 옆에 있는 **"Unity Hub"** 버튼 클릭
4. Unity Hub가 열리면서 설치 화면이 나와요

**4단계: 필수 모듈 선택**

설치할 때 이것들을 **반드시** 체크하세요:

- [x] **Android Build Support** - Quest용 빌드에 필요
- [x] **Android SDK & NDK Tools** - 같이 체크됨
- [x] **OpenJDK** - 같이 체크됨

다른 건 체크 안 해도 돼요.

**5단계: 설치 시작**

**"Install"** 버튼을 클릭하고 기다리세요.

- 용량: 약 5~10GB
- 시간: 30분 ~ 1시간 (인터넷 속도에 따라)
- 중간에 끄지 마세요!

---

## 설치 확인하기

설치가 끝나면 Unity Hub > Installs에서 확인할 수 있어요:

```
Unity 2022.3.21f1
  - Android Build Support
```

이렇게 보이면 성공!

---

## 다음 단계

Unity 설치가 끝났어요! 이제 프로젝트를 열어봅시다.

[다음: 프로젝트 열기 >](03-프로젝트-열기.md)

---

## 자주 묻는 질문

### Q: 설치가 너무 오래 걸려요
인터넷 속도에 따라 다르지만, 보통 30분~1시간이에요.
다른 다운로드 프로그램을 잠시 끄면 더 빨라요.

### Q: 용량이 부족하대요
Unity와 필요한 도구들이 약 15GB 정도 필요해요.
불필요한 파일을 정리하거나 다른 드라이브에 설치하세요.

### Q: Archive에서 버전을 못 찾겠어요
페이지에서 `Ctrl+F` (Mac: `Cmd+F`)로 "2022.3.21"을 검색해보세요.
