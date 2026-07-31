# ChaosChess.AI

`ChaosChess.AI`는 Chaos Chess의 AI 의사결정 로직을 Unity에서 분리해 개발하고 테스트하기 위한 순수 C# 라이브러리입니다.

이 저장소는 [`Chaos-Chess-v2` #268](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/268)의 단계별 로드맵을 따릅니다. P0 스캐폴딩([`#271`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/271)), P1 경계·도메인([`#272`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/272)), P2 게임 상태 평가기([`#273`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/273)), P3 카드 결정 모듈([`#274`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/274)), P4 이동 후보 필터([`#275`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/275)), P5 미래 상태 시뮬레이터([`#276`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/276))를 완료했고, 현재 P7 헤드리스 밸런싱 시뮬레이터([`#279`](https://github.com/8x8-Labs/Chaos-Chess-v2/issues/279))를 구성하고 있습니다. Unity 연결 코드는 Unity 저장소에서 별도 단계로 관리합니다.

## 프로젝트 구성

```text
ChaosChess.AI/
├── src/
│   ├── ChaosChess.AI/
│   │   ├── Abstractions/       # 외부 엔진·난수 경계
│   │   ├── Decision/           # 카드 사용 결정
│   │   ├── Domain/             # 순수 게임 상태 DTO
│   │   ├── Evaluation/         # 결정론적 게임 상태 평가
│   │   ├── Fen/                # FEN 파서·직렬화
│   │   └── Simulation/         # coarse 미래 상태 시뮬레이션
│   └── ChaosChess.AI.Stockfish/ # Fairy Stockfish UCI 프로세스 어댑터
├── tests/
│   └── ChaosChess.AI.Tests/    # net8.0 xUnit 테스트
├── tools/
│   └── ChaosChess.AI.Simulator/ # 헤드리스 배치 실행·CSV 출력 도구
└── .github/workflows/ci.yml    # restore/build/test CI
```

## 의존성 경계

라이브러리 프로젝트는 다음 원칙을 지킵니다.

- `UnityEngine`, DOTween, `FairyStockfishBridge`를 참조하지 않습니다.
- 런타임 의존성은 `System.*` 범위로 제한합니다.
- 렌더링, Unity 라이프사이클, 파일·네트워크 IO, 비동기 처리와 스레딩은 라이브러리 밖의 어댑터가 담당합니다.
- AI 의사결정은 순수하고 동기적이며 결정론적으로 유지합니다.
- 외부 체스 엔진은 동기식 `IChessEngine`, 무작위성은 `IRandom`으로만 접근합니다.
- 라이브러리는 카드와 수를 **결정**하고 P5부터 coarse 예측 상태를 만들지만, 정확한 게임 상태 적용 책임은 Unity 어댑터에 있습니다.

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

- `IChessEngine`: 현재 상태의 MultiPV 후보, 고정 깊이 판세 평가와 체크 여부를 동기적으로 요청합니다.
- `IRandom`: 구현체를 주입해 같은 시드에서 같은 결정을 재현할 수 있게 합니다.
- `BoardState`: 기물 배치, 현재 차례, 캐슬링, 앙파상, 수 카운터를 보관합니다.
- `GameState`: 보드 상태와 사용 가능한 카드, 활성 타일 효과를 묶습니다.
- `FenParser`: 6필드 FEN을 `BoardState`로 변환하고 다시 직렬화합니다.

`Square`는 Unity 보드와 동일한 0 기반 좌표를 사용합니다. `a1`은 `(0, 0)`, `h8`은 `(7, 7)`입니다.

기물은 실제 정체성인 `PieceKind`와 현재 엔진에 전달할 `FenCode`를 분리합니다. 표준 기물 외에 `a`(Wall), `s`(Amazon), `y`(Chancellor), `z`(KnightRider)를 인식하며, 카드로 변경된 다른 ASCII 문자도 `Unknown` 종류로 원본 코드를 보존합니다. 현행 Unity·Fairy-Stockfish FEN과 동일하게 한 기물의 FEN 코드는 문자 하나입니다.

P1에서는 인터페이스와 데이터 계약만 정의합니다. 실제 Fairy-Stockfish 프로세스/JNI 연결, Unity DTO 매핑, 상태 평가, 카드 판단 및 타일 효과 적용은 수행하지 않습니다.

## P2 게임 상태 평가기

`GameStateEvaluator`는 주입된 `IChessEngine`의 판세 점수를 기본값으로 사용하고, 엔진이 알지 못하는 Chaos Chess의 타일 효과를 보정합니다. 결과는 지정한 진영의 관점에서 `-100`부터 `100` 사이이며 양수는 유리, 음수는 불리, `0`은 중립입니다.

- `BoardScore`: Stockfish centipawn 점수를 `13`으로 나눠 정규화합니다.
- `Threat`: `Mine`과 `Fire` 반경 1 안에서 위협받는 기물 가치를 계산합니다.
- `Advantage`: `Blessing`, `Peace`, `Portal`의 소유권에 따른 이점을 계산합니다.
- `MateIn`: Stockfish의 mate 예측을 대상 진영 관점으로 보존합니다.

기본 총점 계산식은 다음과 같습니다.

```text
TotalScore =
    BoardScore × 1.0
  + Threat × 0.8
  + Advantage × 0.6
```

centipawn 기반 `BoardScore`는 예측 mate보다 낮은 `-89..89`로 제한합니다. 보정까지 반영한 비종료 총점은 `-99..99`로 제한합니다. 탐색 깊이와 가중치는 `EvaluationOptions`로 교체할 수 있으며 기본 탐색 깊이는 `12`입니다. 시간 기반 탐색 대신 고정 깊이를 사용해 같은 엔진 설정과 입력에서 평가를 재현할 수 있게 합니다.

Stockfish가 예측한 mate는 Chaos Chess의 실제 게임 종료가 아닙니다. 유리한 mate는 `+90`, 불리한 mate는 `-90`의 긴급도 점수로 변환하고 원본 `MateIn`을 함께 반환합니다. `±100`은 카드 사용 기회까지 확인한 실제 게임 종료 판정용으로 남겨둡니다.

후속 카드 결정·시뮬레이션 단계는 카드 미사용 상태와 각 카드를 가상 적용한 상태를 각각 P2로 재평가합니다. Fairy Stockfish variant/FEN으로 표현 가능한 특수 기물과 행마는 엔진 평가에 맡기며, 엔진으로 표현할 수 없는 카드 기반 행마는 후속 규칙 시뮬레이터가 처리합니다.

P2에서는 평가 기준과 엔진 계약만 제공합니다. 실제 Fairy Stockfish UCI `score cp`·`score mate` 파싱, 카드 선택, 이동 선택, Mobility·Board Control 평가, 미래 상태 시뮬레이션 및 Unity 연동은 수행하지 않습니다.

## P3 카드 결정

`CardDecisionModule`은 현재 평가 점수와 사용 가능한 카드 목록을 기준으로 이번 턴에 사용할 카드 후보를 결정합니다. 실제 카드 효과 적용, 가상 상태 생성, Unity 상태 변경은 수행하지 않고, 순수한 추천 결과만 반환합니다.

- `EloCardProfile`: 카드 사용 임계값과 턴당 최대 카드 수를 정의합니다.
- `ICardScorer`: 카드별 점수 계산 경계입니다.
- `ConfiguredCardScorer`: 카드 ID별 점수를 우선 적용하고, 없으면 카테고리 점수를 사용합니다.
- `CardDecisionResult`: 선택된 카드 추천 목록과 최종 예상 점수를 반환합니다.

기본 프로파일은 최소 점수 증가량 `1`, 턴당 최대 카드 수 `1`입니다. 기본 카드 점수 계산식은 다음과 같습니다.

```text
baseScore =
    cardId override score
    otherwise category score
    otherwise 0

projectedScore = Clamp(currentScore + baseScore, -99, 99)
effectiveGain = projectedScore - currentScore
```

선택 규칙은 다음과 같습니다.

- `RemainingUses`가 `0`인 카드는 제외합니다.
- `effectiveGain`이 `MinimumScoreGain` 이상인 카드만 선택합니다.
- 가장 높은 `effectiveGain`을 가진 카드를 선택합니다.
- 동점이면 `GameState.AvailableCards` 입력 순서를 유지합니다.
- `MaximumCardsPerTurn`까지 반복하며, 이미 선택한 카드는 같은 결정 루프에서 다시 선택하지 않습니다.

P3에서는 카드 메타데이터 기반 선택 흐름과 ELO 프로파일 임계값만 제공합니다. 카드 적용 전후 상태를 실제로 재평가하는 가상 적용, 카드 효과 시뮬레이션, 이동 선택, Unity DTO 매핑은 후속 단계에서 담당합니다.

## P4 이동 후보 필터

`MoveFilter`는 주입된 `IChessEngine.GetTopMoves()`로 Fairy Stockfish MultiPV 후보를 받고, Chaos Chess 타일 효과 기준으로 실행 불가능한 후보를 제거하거나 점수를 조정해 재정렬합니다. 실제 이동 실행, 타일 효과 소비, 게임 상태 변경은 수행하지 않고 추천 결과만 반환합니다.

- `MoveFilterOptions`: 점수 정규화, 불바다 위험 가중치, 평화 협정·포탈 진입 보너스를 정의합니다.
- `MoveFilterResult`: 추천 후보와 hard filter로 제거된 후보를 분리해 반환합니다.
- `MoveRecommendation`: 엔진 점수, 조정 점수, 최종 점수와 적용 사유를 보관합니다.
- `FilteredMoveCandidate`: 제거된 후보와 제거 사유를 보관합니다.

기본 점수 정규화는 P2와 같은 centipawn `13`분의 1 스케일을 사용합니다. centipawn 후보는 `-89..89`, mate 후보는 `±90`, 조정 후 최종 점수는 비종료 범위인 `-99..99`로 제한합니다. 동일 최종 점수에서는 엔진 MultiPV 입력 순서를 유지합니다.

hard filter 규칙은 다음과 같습니다.

- 잘못된 UCI 후보를 제거합니다.
- 출발 칸에 기물이 없는 후보를 제거합니다.
- 출발 기물 색이 현재 차례와 다른 후보를 제거합니다.
- 점유된 `Peace` 타일로 들어가는 캡처 후보를 제거합니다.
- `Wall`은 FEN 기물 `a/A`로 엔진이 이미 반영한다고 보고 중복 필터하지 않습니다.

soft adjustment 규칙은 다음과 같습니다.

- `Mine`: 룩, 퀸, Amazon, Chancellor, KnightRider가 이동 경로상 지뢰를 통과하면 폭발 반경 1의 기물 손익을 계산합니다.
- `Fire`: 불바다 칸으로 들어가는 기물 가치에 위험 가중치를 적용해 패널티를 줍니다.
- `Blessing`: 승격 가능한 기물이 가호 칸에 들어가면 예상 승격 이득을 보너스로 줍니다.
- `Peace`: 빈 평화 협정 칸 진입은 방어 이득으로 보너스를 줍니다.
- `Portal`: `TileEffectInfo`에 도착지와 공유 사용 횟수가 있을 때만 도착지 기물 가치와 기본 유틸리티를 반영합니다.

포탈 평가를 위해 `TileEffectInfo`는 선택적 `DestinationSquare`, `SharedRemainingUses` 계약을 가집니다. 이 값이 없거나, owner가 없거나, 알 수 없는 효과 타입이면 MoveFilter는 해당 효과를 조정하지 않습니다.

P4에서는 목 엔진 기반 후보 필터와 재정렬만 제공합니다. 실제 Fairy Stockfish UCI MultiPV 구현, Unity DTO 매핑, 포탈 사용 횟수 감소, 가호·불바다의 누적 체류 턴 처리, 2턴 이상 미래 상태 시뮬레이션 및 실제 이동 실행은 후속 단계에서 담당합니다.

## P5 미래 상태 시뮬레이터

`GameSimulator`는 현재 `GameState`에서 짧은 horizon 동안 양측의 평가, 카드 추천, 이동 추천, coarse 이동 적용 결과를 trace로 반환합니다. P5의 기본 horizon은 `2 ply`이며, 현재 `SideToMove`가 한 번 행동하고 다음 진영이 한 번 행동하는 범위입니다. 옵션으로 `0..8` ply를 지정할 수 있고, `0`은 상태 변경 없이 초기 상태를 그대로 반환합니다.

- `SimulationOptions`: horizon ply, MultiPV 후보 수, RNG tie-break 사용 여부와 seed를 정의합니다.
- `SimulationResult`: 초기 상태, 최종 coarse 상태, seed, horizon, 종료 사유, 전체 warning과 ply별 trace를 반환합니다.
- `SimulationStep`: ply index, 행동 진영, 적용 전 평가, 카드 추천 결과, MoveFilter 결과, 선택 이동, 적용 전후 상태, warning을 보관합니다.

각 ply는 다음 순서로 처리합니다.

```text
평가 → 카드 추천 trace 기록 → MoveFilter → 이동 선택
→ UCI 이동 coarse 적용 → 지원 타일 효과 적용/소비
→ remaining turn half-turn tick → 종료 판정 → 다음 진영
```

P5에서 카드 추천은 trace에만 남기고 실제 카드 상태 전이는 수행하지 않습니다. 현재 `CardInfo`에 target, effect parameter, 상태 변경 계약이 없기 때문입니다. 카드 대상과 실제 적용 결과를 표현하는 계약은 후속 Unity 매핑 단계에서 다룹니다.

coarse 이동 적용은 일반 이동, 캡처, 프로모션, 캐슬링 룩 이동, 앙파상 캡처, side-to-move, castling rights, en passant target, halfmove/fullmove counter 갱신을 지원합니다. 전체 합법 수 생성은 하지 않고, `IChessEngine.GetTopMoves()`와 `MoveFilter`가 반환한 후보를 입력으로 사용합니다.

타일 효과는 다음 범위만 상태에 적용합니다.

- `Mine`: 이동 경로상 지뢰 통과 시 반경 1 기물을 제거하고 지뢰를 제거합니다.
- `Peace`: 점유 칸 캡처 진입 후보가 차단되면 이동을 취소하고 효과를 제거합니다.
- `Portal`: owner, destination, shared uses가 있는 경우 반대편으로 이동하고 공유 횟수를 감소시킵니다.
- `Wall`: FEN 기물로 유지하며 타일 효과로 중복 적용하지 않습니다.
- `Fire`, `Blessing`: 현재 DTO로 지연 제거 대상과 체류 상태를 보존할 수 없어 warning만 기록하고 임의 상태 변경은 하지 않습니다.

기본 선택은 결정론적이며 동일 점수에서는 `MoveFilter` 입력 순서를 유지합니다. `UseRandomTieBreak`를 켜면 최상위 동점 후보 그룹에서 `IRandom.NextInt(0, count)`로 균등 선택합니다. 같은 seed, 같은 입력, 같은 fake engine에서는 2 ply trace와 최종 상태가 재현되어야 합니다.

종료 사유는 horizon 도달, 추천 없음, 이동 차단, 킹 제거, 체크메이트, 스테일메이트, 지원하지 않는 효과 발견을 구분합니다. Stockfish 예측 mate 점수 `±90`은 P2와 동일하게 실제 종료 `±100`으로 승격하지 않습니다.

P5에서는 Unity 파일 수정, 실제 Fairy Stockfish 프로세스/JNI 연결, Unity DTO 매핑, 실제 카드 52종 실행 복제, 헤드리스 대량 시뮬레이션, CSV 메트릭 및 DLL 배포를 수행하지 않습니다.

## P7 헤드리스 밸런싱 시뮬레이터

P7은 Unity 없이 `ChaosChess.AI` 의사결정 모듈과 선택적으로 Fairy Stockfish UCI 프로세스를 사용해 AI-vs-AI 배치를 실행하고, 동일 설정과 시드에서 재현 가능한 logical CSV를 생성합니다.

- `ChaosChess.AI.Stockfish`: `IChessEngine` 구현체인 `StockfishProcessEngine`과 UCI `info`/`bestmove` parser를 제공합니다.
- `ChaosChess.AI.Simulator`: console host, batch runner, complete-game runner, player profile, seed derivation, CSV writer를 제공합니다.
- 기본 fake 모드는 외부 엔진 없이 CI와 로컬에서 deterministic CSV를 검증합니다.
- 실엔진 모드는 `--engine`과 `--variant-config`를 명시적으로 받아 실행하며, 두 파일의 SHA-256을 CSV에 기록합니다.

기본 실행 예시:

```shell
dotnet run --project tools/ChaosChess.AI.Simulator -- \
  --games 2 \
  --seed 12345 \
  --max-ply 1 \
  --multipv 1 \
  --output ./artifacts/p7-smoke.csv \
  --overwrite
```

로컬 Unity 프로젝트의 Fairy Stockfish를 사용하는 smoke 예시:

```shell
dotnet run --project tools/ChaosChess.AI.Simulator -- \
  --engine "C:/unity/Chaos-Chess-v2/ChaosChess_v2/Assets/StreamingAssets/fairy-stockfish.exe" \
  --variant-config "C:/unity/Chaos-Chess-v2/ChaosChess_v2/Assets/StreamingAssets/variants.ini" \
  --games 1 \
  --seed 12345 \
  --depth 1 \
  --max-ply 1 \
  --multipv 1 \
  --output ./artifacts/p7-real-smoke.csv \
  --overwrite
```

주요 CLI exit code:

- `0`: 성공
- `2`: 잘못된 CLI 인자 또는 구성 파일 누락
- `3`: output 파일이 이미 있고 `--overwrite`가 없음
- `4`: 엔진 시작, handshake, timeout, invalid output 등 엔진 실패
- `130`: 사용자 취소

CSV는 `p7.logical.v1` 스키마를 사용합니다. 행 순서, game ID, game seed, scenario/profile pairing은 같은 입력에서 재현되어야 하며, wall-clock duration 같은 비결정적 필드는 logical CSV에 포함하지 않습니다.

P7에서도 P5의 카드 계약 공백을 보존합니다. `CardDecisionModule`의 추천 수는 기록하지만 실제 카드 효과 적용과 `RemainingUses` 감소는 수행하지 않으며, CSV에서 `cards_recommended`와 `cards_applied`를 분리합니다. 실제 52종 카드 효과 catalog, target planner, coarse applier는 후속 작업 범위입니다.

## 로컬 검증

```shell
dotnet restore ChaosChess.AI.sln
dotnet build ChaosChess.AI.sln --configuration Release --no-restore
dotnet test ChaosChess.AI.sln --configuration Release --no-build
rg -n "UnityEngine|DOTween|FairyStockfishBridge" src tests tools
git diff --check
```
