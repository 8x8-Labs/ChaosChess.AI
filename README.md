# ChaosChess.AI

`ChaosChess.AI`는 Chaos Chess의 AI 의사결정 로직을 Unity에서 분리해 개발하고 테스트하기 위한 순수 C# 라이브러리입니다.

이 저장소의 현재 범위는 [`Chaos-Chess-v2` #268](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/268)의 P0 스캐폴딩과 서브 이슈 [`#271`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/271)입니다. 실제 AI 판단 로직은 후속 이슈에서 구현합니다.

## 프로젝트 구성

```text
ChaosChess.AI/
├── src/
│   └── ChaosChess.AI/          # netstandard2.1 라이브러리
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
- 무작위성과 외부 체스 엔진은 후속 단계에서 주입 가능한 인터페이스 경계로 연결합니다.
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

## 로컬 검증

```shell
dotnet restore ChaosChess.AI.sln
dotnet build ChaosChess.AI.sln --configuration Release --no-restore
dotnet test ChaosChess.AI.sln --configuration Release --no-build
```
