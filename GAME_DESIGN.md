# Run 'Till the End - 게임 기획서

## 맵 구조

```
        [Room 5] - 탈출구
            |
        [Room 2] - 복도 (중앙)
       /    |    \
[Room 3] [Room 4] [Room 1] - 시작점
```

## 게임 플로우

### (1) Room1 - 시작
- **시작 위치**: Room1 침대
- **목표**: 책상 위 포크(열쇠) 획득 → Room1 문 열기
- **오브젝트**:
  - `fork key - Room1`: keyId = "key_room1"
  - `Door - Room1`: requiredKeyId = "key_room1", isLocked = true

### (2) Room2 - 복도
- **목표**: 배럴 위 열쇠 획득 → Room3 문 열기
- **오브젝트**:
  - `key - Room2`: keyId = "key_room3" (Room3 문을 여는 열쇠)
  - `Door - Room3`: requiredKeyId = "key_room3", isLocked = true
- **특징**:
  - 숨는 공간 있음
  - Room5 입구(탈출구) 보임

### (3) Room3 - 책 이벤트
- **목표**: 책상 위 책 확인 → 스토리 팝업창
- **오브젝트**:
  - `book - Room3`: 상호작용 시 팝업창 표시
- **팝업 내용**: Room4 상자 비밀번호 힌트 포함

### (4) Room3 - 살인마 등장
- **트리거**: 팝업창 닫힘
- **이벤트**:
  - `Lighting - Room3` 내 Light 3개 모두 켜짐
  - 창문 뒤에 살인마(ghost 1) 나타남
- **선택 추가**:
  - 휠체어 넘어짐 → 큰 소리 (들킴)
  - 살인마 고함 소리
  - 호러 브금 시작

### (5) Room2 - 숨기
- **이벤트**: 창문 뒤에서 뛰어가는 유령(ghost 2) 보임
- **목표**: 숨기 장소에 들어가기
- **선택 추가**:
  - 제한시간 안에 못 숨으면 살인마에게 잡힘

### (6) Room2 - 대기
- **필수**: Room4에서 문 열고 나오는 유령(ghost 3)
- **선택 추가**:
  - 살인마가 Room2를 한바퀴 돌아봄
  - 살인마 고함 소리, 호러 브금 끝 (점점 작아짐)
  - 브금 끝나기 전에 숨기 장소에서 나오면 잡힘
- **참고**: Room4 문은 열린 상태로 유지 (열쇠 없음)

### (7) Room4 - 상자
- **목표**: Room4 상자 열기 → 오브젝트(미정) 획득
- **오브젝트**:
  - `chest - Room4`: 비밀번호 상자 (힌트는 (3) 팝업)
- **선택 추가**:
  - 비밀번호 입력 필요

### (8) Room5 - 탈출
- **목표**: Room4 상자 오브젝트로 Room5 문 열기 → 탈출
- **오브젝트**:
  - `Panel_Wood - Room5`: 탈출문 (위로 열림)
- **선택 추가**:
  - 문이 천천히 위로 열림
  - 유령 뛰어오는 소리 (멀리서)
  - 유령 고함 소리

### (9) 탈출 성공
- 탈출 성공 팝업창 표시

---

## 오브젝트 목록

### 열쇠 & 문 연결

| 열쇠 | keyId | 문 | requiredKeyId | 상태 |
|------|-------|-----|---------------|------|
| fork key - Room1 | key_room1 | Door - Room1 | key_room1 | ✅ 설정됨 |
| key - Room2 | key_room3 | Door - Room3 | key_room3 | ✅ 설정됨 |
| EscapeKey (chest) | key_room5 | Panel_Wood - Room5 | key_room5 | ✅ 설정됨 |

### 상호작용 오브젝트

| 오브젝트 | 타입 | 기능 | 상태 |
|----------|------|------|------|
| book - Room3 | ReadableNote | 팝업창 표시 (비번 힌트: 1234) | ✅ 설정됨 |
| chest - Room4 | PasswordChest | 비밀번호(1234) 입력 후 EscapeKey 제공 | ✅ 설정됨 |
| Lighting - Room3 | LightingEventTrigger | 이벤트 시 켜짐 | ✅ 설정됨 |
| HidingSpot - Room2 | HidingSpot | 숨기 장소 | ✅ 설정됨 |

### 유령 & 살인마

| 오브젝트 | 역할 | 등장 시점 |
|----------|------|-----------|
| ghost 1 | 살인마 (창문 뒤) | (4) Room3 팝업 후 |
| ghost 2 | 유령 (뛰어감) | (5) Room2 |
| ghost 3 | 유령 (Room4에서 나옴) | (6) Room2 |

### 숨기 장소

| 위치 | 용도 |
|------|------|
| Room2 숨는 공간 | (5)~(6) 살인마 피하기 |

---

## 구현 우선순위

### 1단계 - 기본 진행 (필수)

- [x] fork key - Room1 → Door - Room1 연결
- [x] key - Room2 → Door - Room3 연결
- [x] chest - Room4 아이템 → Panel_Wood - Room5 연결
- [x] Door - Room4 열린 상태로 설정

### 2단계 - 상호작용

- [x] book - Room3 팝업창 시스템
- [x] chest - Room4 비밀번호 시스템 (비밀번호: 1234)
- [x] 숨기 장소 설정 (HidingSpot - Room2)

### 3단계 - 이벤트 연출

- [x] Lighting - Room3 켜짐 이벤트 (LightingEventTrigger 추가)
- [ ] book - Room3 OnNoteClosed → Lighting 연결 (Inspector에서 설정 필요)
- [x] ghost 1/2/3 등장 스크립트 (GhostAppearanceManager 추가)
- [ ] GhostAppearanceManager에 ghost 오브젝트 연결 (Inspector에서 설정 필요)
- [ ] 살인마 순찰 경로

### 4단계 - 사운드 & 연출
- [ ] 호러 브금 시작/종료
- [ ] 살인마 고함 소리
- [ ] 유령 발소리
- [ ] Panel_Wood - Room5 위로 열리는 애니메이션
