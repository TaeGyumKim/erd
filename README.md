# VR Game Project

Unity 2022.3.21f1 기반 VR 게임 프로젝트

## 프로젝트 구조

```text
Assets/
├── Scripts/      # C# 스크립트
├── Scenes/       # Unity 씬 파일
├── Prefabs/      # 프리팹
├── Materials/    # 머티리얼
├── Models/       # 3D 모델
├── Textures/     # 텍스처
└── Audio/        # 오디오 파일

Packages/         # Unity 패키지 설정
ProjectSettings/  # 프로젝트 설정
```

## 설치된 VR 패키지

- **XR Interaction Toolkit 2.5.2**: VR 인터랙션 시스템
- **XR Plugin Management 4.4.0**: XR 플러그인 관리
- **OpenXR 1.9.1**: OpenXR 런타임 지원
- **Input System 1.7.0**: 새로운 입력 시스템

## 시작하기

1. Unity Hub에서 Unity 2022.3.21f1 버전 설치
2. 이 프로젝트 폴더를 Unity Hub에서 열기
3. 패키지 자동 설치 완료 대기
4. Project Settings > XR Plug-in Management에서 VR 플랫폼 활성화

## VR 플랫폼 설정

프로젝트 실행 후 다음 설정 필요:

- Edit > Project Settings > XR Plug-in Management
- 사용할 VR 플랫폼 선택 (Meta Quest, SteamVR 등)

## 요구 사항

- Unity 2022.3.21f1
- VR 헤드셋 (개발/테스트용)
