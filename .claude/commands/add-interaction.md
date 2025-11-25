# 인터랙션 오브젝트 생성

새로운 상호작용 가능한 오브젝트 스크립트를 생성합니다.

## 사용법
`/add-interaction [오브젝트이름]`

## 지시사항

`InteractableObject`를 상속받는 새 스크립트를 생성하세요:

1. **상속**: `InteractableObject` 또는 `XRSimpleInteractable`
2. **위치**: `Assets/Scripts/Interaction/`
3. **필수 요소**:
   - `OnSelectEntered` 오버라이드
   - Inspector 설정 가능한 필드
   - UnityEvent 이벤트

## 기존 인터랙션 참조
- Door.cs: 문 열기/닫기, 잠금
- HidingSpot.cs: 숨는 장소
- PickupItem.cs: 줍는 아이템

사용자 요청: $ARGUMENTS
