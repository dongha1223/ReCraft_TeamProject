# Climbing the 100th Floor — Claude Guide

## 프로젝트 개요
- **장르:** 2D 로그라이크(스컬 모작)
- **엔진:** Unity 6000.3.10f1, Universal Render Pipeline (2D)
- **플랫폼:** Windows (StandaloneWindows64)

## 기술 스택
- Unity New Input System (`InputSystem_Actions.inputactions` 존재)
- URP 2D Renderer (`Assets/Settings/Renderer2D.asset`)
- MCPForUnity — MCP 서버로 Claude가 Unity 에디터를 직접 제어

## 반드시 지켜야 할 점
- 한글 사용 금지(주석 설명은 괜찮음)
- OOP 기반 설계
- 계획을 말하고 승인 받은 후에 작업
- 최적화를 고려한 코드 작성

## 폴더 구조 규칙
```
Assets/
  Scenes/         # 씬 파일
  Scripts/        # 게임 스크립트 (C#)
    Player/       # 플레이어 관련
    Floor/        # 층/플랫폼 관련
    UI/           # UI 관련
    Core/         # GameManager, 공통 시스템
  Prefabs/        # 프리팹
  Sprites/        # 스프라이트/텍스처
  Audio/          # 사운드
  Settings/       # URP 설정 (건드리지 말 것)
  MCPForUnity/    # MCP 플러그인 (건드리지 말 것)
```

## 코딩 컨벤션
- 네임스페이스: '2D_Roguelike'
- 클래스명: PascalCase
- 변수명: camelCase, private 필드는 `_camelCase`
- SerializeField 사용 (public 필드 지양)
- 주석: 한국어 허용

## 작업 방식
- 스크립트 생성/수정 후 반드시 `read_console`로 컴파일 오류 확인
- 씬 수정 후 반드시 save
- MCPForUnity로 Unity 에디터 직접 제어 (씬 조작, 스크립트 생성 등)