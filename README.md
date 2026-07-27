# ChaosChess.AI

`ChaosChess.AI`는 Chaos Chess의 AI 의사결정 로직을 Unity에서 분리해 개발하고 테스트하기 위한 순수 C# 라이브러리입니다.

이 저장소는 [`Chaos-Chess-v2` #268](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/268)의 단계별 로드맵을 따릅니다. P0 스캐폴딩([`#271`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/271))을 완료했고, 현재 P1 경계·도메인([`#272`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/272))을 구성하고 있습니다. 실제 AI 평가와 카드·수 선택 로직은 후속 단계에서 구현합니다.

## 프로젝트 구성

```text
ChaosChess.AI/
├── src/
│   └── ChaosChess.AI/
│       ├── Abstractions/       # 외부 엔진·난수 경계
│       ├── Domain/             # 순수 게임 상태 DTO
│       └── Fen/                # FEN 파서·직렬화
├── tests/
│   └── ChaosChess.AI.Tests/    # net8.0 xUnit 테스트
└── .github/workflows/ci.yml    # restore/build/test CI
```

## 의존성 경계

라이브러리 프로젝트는 다음 원칙을 지킵니다.

- `UnityEngine`, DOTween, `FairyStockfishBridge`를 참조하지 않습니다.
- 런타임 의존성은 `System.*` 범위로 제한합니다.
- 렌더링, Unity 라이프사이클, 파일·네트워크 IO, 비동기 처리와 스레딩은 라이브러리 밖의 어댑터가 담당합니다.
- AI 의사결정은 순수하고 동기적이며 결정론적으로 유지합니다.
- 외부 체스 엔진은 동기식 `IChessEngine`, 무작위성은 `IRandom`으로만 접근합니다.
- 라이브러리는 카드와 수를 **결정**하지만 실제 게임 상태에 **적용**하지 않습니다. 적용 책임은 Unity 어댑터에 있습니다.

```text
Unity / Console adapters
        │  domain data and injected interfaces
        ▼
ChaosChess.AI
        │  decisions only
        ▼
Move candidates / card-use plans
```

## P0 범위

- 별도 저장소와 솔루션 구성
- `netstandard2.1` 라이브러리 프로젝트 구성
- 단위 테스트 실행 골격 구성
- GitHub Actions 기반 CI 구성
- 라이브러리 역할과 의존성 경계 문서화

P0에서는 AI 평가, 카드 채점, MoveFilter, 미래 시뮬레이션, Stockfish 연동, Unity 코드 변경 및 CD를 수행하지 않습니다.

## P1 경계·도메인

P1은 AI가 Unity 없이 게임 상태를 읽고 테스트할 수 있도록 공용 계약을 정의합니다.

- `IChessEngine`: 현재 상태의 MultiPV 후보와 체크 여부를 동기적으로 요청합니다.
- `IRandom`: 구현체를 주입해 같은 시드에서 같은 결정을 재현할 수 있게 합니다.
- `BoardState`: 기물 배치, 현재 차례, 캐슬링, 앙파상, 수 카운터를 보관합니다.
- `GameState`: 보드 상태와 사용 가능한 카드, 활성 타일 효과를 묶습니다.
- `FenParser`: 6필드 FEN을 `BoardState`로 변환하고 다시 직렬화합니다.

`Square`는 Unity 보드와 동일한 0 기반 좌표를 사용합니다. `a1`은 `(0, 0)`, `h8`은 `(7, 7)`입니다.

기물은 실제 정체성인 `PieceKind`와 현재 엔진에 전달할 `FenCode`를 분리합니다. 표준 기물 외에 `a`(Wall), `s`(Amazon), `y`(Chancellor), `z`(KnightRider)를 인식하며, 카드로 변경된 다른 ASCII 문자도 `Unknown` 종류로 원본 코드를 보존합니다. 현행 Unity·Fairy-Stockfish FEN과 동일하게 한 기물의 FEN 코드는 문자 하나입니다.

P1에서는 인터페이스와 데이터 계약만 정의합니다. 실제 Fairy-Stockfish 프로세스/JNI 연결, Unity DTO 매핑, 상태 평가, 카드 판단 및 타일 효과 적용은 수행하지 않습니다.

## 로컬 검증

```shell
dotnet restore ChaosChess.AI.sln
dotnet build ChaosChess.AI.sln --configuration Release --no-restore
dotnet test ChaosChess.AI.sln --configuration Release --no-build
```
