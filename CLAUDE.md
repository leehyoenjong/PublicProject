# CLAUDE.md — PublicProject (Unity 공용 프레임워크)

## 프로젝트 개요

- **목적**: 새 프로젝트에 넣어 사용하는 공용 프레임워크/템플릿
- **Unity 버전**: 6000.3.9f1 (Unity 6)
- **렌더 파이프라인**: URP 2D (17.3.0)
- **Input System**: New Input System (1.18.0)
- **스테이지**: Framework (재사용 목적 — SOLID 전면 적용)

## SOLID 원칙

재사용 프레임워크이므로 모든 SOLID 원칙을 적용한다.

- **SRP**: 클래스 하나에 역할 하나. God Class 금지.
- **OCP**: 전략 패턴, 확장 시스템에 적용. 수정 없이 확장 가능하게.
- **DIP**: Interfaces/Core/Mono 분리로 의존성 역전.
- **ISP**: 인터페이스는 작게 분리. 불필요한 메서드 강제 금지.
- **LSP**: 상속 시 부모의 계약을 깨지 않는다.

## 폴더 구조

```
Assets/
├── 01_Scenes/
├── 02_Scripts/
│   ├── 01_Interfaces/     # 인터페이스, 추상 클래스
│   ├── 02_Core/           # 순수 C# 로직 (MonoBehaviour 의존 없음)
│   ├── 03_Mono/           # MonoBehaviour 구현체
│   └── 04_Utils/          # 유틸리티, 확장 메서드
├── 03_Prefabs/
├── 04_Materials/
├── 05_Sprites/
├── 06_Animations/
├── 07_Audio/
├── 08_UI/
├── 09_ScriptableObjects/
└── Settings/
```

## 네이밍 규칙

| 대상 | 규칙 | 예시 |
|---|---|---|
| 폴더 | `번호_이름` | `01_Scripts`, `03_Prefabs` |
| 클래스/인터페이스 | `PascalCase` | `PlayerController`, `IScore` |
| 멤버 변수 | `_camelCase` | `_health`, `_moveSpeed` |
| 스태틱 변수 | `PascalCase` | `MaxCount`, `Instance` |
| 지역 변수 | `camelCase` | `count`, `targetPos` |
| 상수 | `UPPER_SNAKE` | `MAX_HEALTH`, `TILE_SIZE` |
| 메서드 | `PascalCase` | `TakeDamage()`, `GetScore()` |
| 프로퍼티 | `PascalCase` | `Health`, `IsAlive` |

- 쉬운 영어 단어 사용 (어린아이도 이해 가능한 수준)

## 로그 규칙

| 상황 | 레벨 | 예시 |
|---|---|---|
| 기능 진입/종료 | `Debug.Log` | `"[ScoreSystem] Init started"` |
| 중요 상태 변경 | `Debug.Log` | `"[Player] Health changed: 100 -> 80"` |
| 예외/에러 | `Debug.LogError` | `"[Save] File not found: path"` |
| 경고 | `Debug.LogWarning` | `"[Pool] Object limit reached"` |

포맷: `[시스템명] 메시지`

## 안티패턴 (금지)

- 빈 catch 블록 — 에러가 숨겨져서 디버깅 불가
- God Class — 하나의 클래스에 모든 로직 집중
- 매직 넘버 — 상수로 분리할 것
- public 남용 — 불필요한 외부 노출 금지
- Update()에 로직 집중 — 매 프레임 무거운 로직 금지
- string 비교 — enum이나 상수 사용

## 프레임워크 설계 원칙

- 다른 프로젝트에 복사하여 바로 사용 가능해야 한다.
- 외부 의존성 최소화. Unity 기본 패키지만 사용한다.
- 시스템 간 결합도를 낮춘다. 인터페이스로 소통한다.
- 각 시스템은 독립적으로 사용/제거 가능해야 한다.

## 검증 규칙

- 코드 수정 후 컴파일 통과를 확인해야 다음 작업으로 넘어간다.
- 새 기능 추가 후 Log로 동작을 확인해야 완료로 간주한다.
