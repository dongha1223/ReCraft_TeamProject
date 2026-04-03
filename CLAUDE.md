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
- **UI는 UIToolkit 우선** (UXML/USS로 레이아웃·스타일 정의, 스크립트는 참조만)
- **스크립트 내에서 UI 요소를 코드로 생성하지 말 것** (가독성 저해, AddComponent/new GameObject 등 금지)
- UIToolkit으로 불가한 경우에만 Canvas 방식 사용하되 반드시 사용자에게 먼저 설명
- **⚠ 런타임 스프라이트/텍스처 생성 주의** (`new Texture2D`, `Sprite.Create` 등 런타임 에셋 생성은 Inspector 관리 불가·최적화 저해 우려가 있으므로 사용 전 반드시 사용자에게 경고할 것. 스프라이트가 필요한 경우 에셋을 직접 준비하는 방식 권장)

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
  UI/             # UIToolkit 파일 (.uxml, .uss, PanelSettings)
  Settings/       # URP 설정 (건드리지 말 것)
  MCPForUnity/    # MCP 플러그인 (건드리지 말 것)
```

## 코딩 컨벤션
- 네임스페이스: '2D_Roguelike'
- 클래스명: PascalCase
- 변수명: camelCase, private 필드는 `_camelCase`
- SerializeField 사용 (public 필드 지양)
- 주석: 한국어 허용

## 좌우 방향 전환 규칙
- **`SpriteRenderer.flipX` 사용 금지**
- 방향 전환은 반드시 `transform.localScale.x`의 부호를 바꾸는 방식으로 구현
- 아래 패턴을 표준으로 사용:
  ```csharp
  private void Flip(float dirX)
  {
      Vector3 scale = transform.localScale;
      scale.x = dirX > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
      transform.localScale = scale;
  }
  ```
- 이 방식을 사용하면 자식 오브젝트(공격 판정, 스폰 포인트 등)가 자동으로 함께 미러링됨
- 단, 자식 오브젝트에 X 오프셋이 있을 경우 반드시 미러링 여부를 확인할 것

## 작업 방식
- 스크립트 생성/수정 후 반드시 `read_console`로 컴파일 오류 확인
- 씬 수정 후 반드시 save
- MCPForUnity로 Unity 에디터 직접 제어 (씬 조작, 스크립트 생성 등)