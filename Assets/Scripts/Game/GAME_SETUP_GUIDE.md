# Run 'Till the End - 게임 설정 가이드

## 빠른 시작

### 1. Scene Builder 열기
Unity 메뉴에서 `Horror Game > Run Till End Scene Builder` 선택

### 2. 새 게임 씬 생성
1. "새 게임 씬 생성" 버튼 클릭
2. 저장 위치 선택 (기본: Assets/Scenes/RunTillEnd_Main.unity)

### 3. 전체 게임 자동 설정
"전체 게임 자동 설정" 버튼 클릭하면 자동으로:
- 게임 매니저 시스템 생성
- 단서 아이템 배치 (USB, 라이터, 보안카드, 열쇠)
- 상호작용 오브젝트 배치 (컴퓨터, 벽 문양, 카드 리더, 탈출문)
- 살인마와 유령 캐릭터 생성
- 숨기 장소 생성

### 4. 환경 구성
Scene Builder의 "환경 오브젝트" 섹션에서:
- Hospital Horror Pack: 바닥, 벽, 천장, 문 등
- Horror Pack: 침대, 테이블, 의자, 휠체어 등

### 5. NavMesh 베이크
1. 환경 오브젝트 배치 완료 후
2. "NavMesh 베이크" 버튼 클릭
3. 또는 Window > AI > Navigation에서 직접 베이크

### 6. 씬 검증
"씬 검증" 버튼으로 필수 컴포넌트 확인

---

## 게임 시스템 설명

### HorrorGameManager
- 게임 상태 관리 (Playing, GameOver, Victory)
- 제한 시간 설정 (기본 600초)
- 탈출에 필요한 열쇠 수 설정

### StoryProgressManager
스토리 진행 단계:
1. **Introduction**: 게임 시작, 도입부 표시
2. **Exploration**: 맵 탐색, 단서 수집
3. **ClueCollection**: 단서 조합
4. **FinalPuzzle**: 탈출 열쇠 획득
5. **Escape**: 탈출 시도

### 단서 아이템
| 아이템 | 용도 |
|--------|------|
| USB | 컴퓨터에서 메시지 확인 |
| 라이터 | 숨겨진 벽 문양 발견 |
| 보안카드 | 잠긴 문 열기 |
| 배터리 | 기계 장치 작동 |
| 기어 | 기계 장치 수리 |
| 탈출 열쇠 | 최종 탈출 |

### 살인마 (KillerAI)
- 상태: Patrol → Search → Chase → Investigate
- NavMesh를 사용한 이동
- 플레이어 시야/소음 감지
- GameSetup.killerDelay 후 활성화

### 유령 (GhostAI)
- 플레이어를 돕는 조력자
- 단서 위치 힌트 제공
- 속삭임으로 정보 전달

---

## 간단 모드 vs 전체 모드

### 간단 모드 (GameSetup.simpleMode = true)
- 단서 조합 없이 아이템만 수집
- 목표: USB, 라이터, 보안카드 수집 → 열쇠 획득 → 탈출

### 전체 모드 (GameSetup.simpleMode = false)
- USB → 컴퓨터에서 메시지 확인
- 라이터 → 벽 문양 발견
- 보안카드 → 잠긴 구역 해제
- 모든 단서 조합 → 탈출 열쇠 획득 가능

---

## 디버그 명령어

Inspector에서 GameSetup 컴포넌트 선택 후 Context Menu:
- `Debug: Collect All Clues` - 모든 단서 즉시 획득
- `Debug: Activate Killer` - 살인마 즉시 활성화
- `Debug: Win Game` - 즉시 승리

---

## VR 테스트

### PC에서 테스트 (Quest Link)
1. Meta Quest 연결
2. Quest Link 활성화
3. Unity Play 모드 실행

### 빌드 테스트
1. Build Settings > Android 선택
2. XR Plugin Management > Oculus 활성화
3. Build and Run

---

## 문제 해결

### NavMesh 관련
- 바닥이 Static으로 설정되어 있는지 확인
- Navigation 창에서 Agent 설정 확인

### VR 컨트롤러 인식 안됨
- XR Plugin Management 설정 확인
- OpenXR 또는 Oculus 활성화

### 스크립트 컴파일 오류
- XR Interaction Toolkit 패키지 설치 확인
- TMPro (TextMesh Pro) 설치 확인
