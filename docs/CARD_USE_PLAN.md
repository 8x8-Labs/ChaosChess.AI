# P9/P10 CardUsePlan과 지능형 타겟팅 계약

이 문서는 P9에서 추가한 카드 사용 계획 계약과 P10에서 추가한 카드별 지능형 타겟팅 구조의 범위와 후속 단계 경계를 정리한다.

## 목적

P9의 목적은 AI가 단순히 “어떤 카드를 쓸지”만 추천하는 상태에서 벗어나, 다음 정보를 순수 C# 데이터로 표현하고 검증할 수 있게 하는 것이다.

```text
CardUsePlan = actor + card ID + concrete target + allowed parameter
```

이 계약은 Unity 실행 경계와 headless simulator가 같은 plan 정보를 해석하기 위한 기반이다. P10은 이 계약 위에서 대표 5종 카드의 합법 후보를 생성하고 설명 가능한 opportunity score로 best plan을 고른다. 사용률과 승률 기반 수치 튜닝은 P11 범위다.

## 지원 카드 subset

P9 초기 지원 subset은 P8.5에서 승인한 대표 5종으로 제한한다.

| Card ID | Target shape | Target count | 비고 |
|---|---|---:|---|
| `agile` | `PieceAtSquare` | 1 | actor 측 pawn 1개 |
| `charge` | `None` | 0 | target 없는 global 카드 |
| `fire` | `BoardSquare` | 1 | 빈 square 1개 |
| `peace_zone` | `BoardSquare` | 1 | 빈 square 1개 |
| `portal` | `OrderedSquares` | 2 | 서로 다른 빈 square 2개, 순서 보존 |

나머지 45종은 P9에서 AI 지원 대상으로 확장하지 않는다. random, 과거 상태, mini-game, promotion/summon choice 같은 추가 shape는 후속 additive API로 확장한다.

## Target Shape

P9 public API는 대표 5종에 필요한 최소 target shape만 제공한다.

- `None`: target 없음
- `PieceAtSquare`: `Square`와 expected color/kind snapshot
- `BoardSquare`: 단일 board square
- `OrderedSquares`: 입력 순서를 보존하는 square 목록

`PieceAtSquare`는 Unity object ID를 담지 않는다. 현재 snapshot의 square와 예상 기물 정보를 담고, 실행 직전 재검증에서 stale target을 실패로 처리한다.

## Definition과 Plan

`DefaultCardPlanningCatalog`는 대표 5종의 target requirement를 제공한다.

`CardUsePlan`은 현재 `GameState` snapshot에서 생성되어 같은 턴에 즉시 실행되는 단일 카드 계획이다. 여러 카드 combo나 한 카드 적용 후 재평가는 P10 이후 범위다.

카드 효과 수치, 지속시간, shared uses 같은 고정 게임 규칙은 Unity card catalog/definition이 authoritative source다. 대표 5종에서 AI가 선택할 effect parameter는 없으므로 `CardEffectParameters.Empty`로 시작한다.

## Validation

`CardUsePlanValidator`는 plan과 `GameState`를 대조하고 stable validation code와 사람이 읽는 reason을 반환한다.

현재 validation code:

- `Valid`
- `NullGameState`
- `NullPlan`
- `CardNotInHand`
- `CardHasNoRemainingUses`
- `UnsupportedCard`
- `ActorDoesNotMatchSideToMove`
- `TargetKindMismatch`
- `TargetCountMismatch`
- `TargetPieceMissing`
- `TargetPieceColorMismatch`
- `TargetPieceKindMismatch`
- `TargetSquareOccupied`
- `TargetSquareHasTileEffect`
- `DuplicateTargetSquare`

`Square` 범위는 `Square` 생성자에서 이미 보장한다. Unity `BlockedTiles`와 active piece effector 여부는 현재 AI `GameState`에 없으므로 Unity adapter 실행 직전 재검증 범위로 둔다.

## Headless Trace

`CardUsePlanTraceRecorder`는 plan을 적용하지 않고 validator 결과를 trace로 남긴다.

기록 정보:

- plan
- accepted/rejected
- validation code
- validation reason
- optional plan score
- optional plan skip code/reason

P9/P10 headless trace는 state mutation을 하지 않는다. `agile` movement override, `charge` pawn movement, `fire` delayed destruction, `peace_zone` capture cancellation, `portal` teleport/shared uses 적용은 현재 범위가 아니다.

## P10 지능형 타겟팅

P10 public surface:

- `CardTargetingModule`
- `DefaultCardTargetStrategyCatalog`
- `CardTargetStrategyRegistry`
- `ICardTargetStrategy`
- `CardTargetStrategyContext`
- `CardPlanCandidate`
- `CardPlanScore`
- `CardPlanScoreComponent`
- `CardPlanDecisionResult`
- `CardPlanSkipCode`
- `CardTargetingOptions`

기본 registry는 P9 대표 5종 strategy를 제공한다.

| Card ID | Strategy | 핵심 판단 |
|---|---|---|
| `agile` | `AgileCardTargetStrategy` | actor pawn 중 engine top move source, agile lane 연관, promotion pressure |
| `charge` | `ChargeCardTargetStrategy` | actor pawn 전진 가능 수, promotion 도달, blocked pawn |
| `fire` | `FireCardTargetStrategy` | 상대 engine destination, 인접 위험, 중앙 제어, actor route penalty |
| `peace_zone` | `PeaceZoneCardTargetStrategy` | actor route 보호, threatened actor piece 주변 empty buffer, 중앙 제어 |
| `portal` | `PortalCardTargetStrategy` | actor route endpoint 접근, endpoint 거리, 중앙 접근, 상대 destination 위험 |

점수는 `CardPlanScoreComponent.Code`와 정수 값으로 남긴다. component 합계는 `CardPlanScore.Total`과 반드시 일치해야 하며 생성자에서 검증한다. reason text parsing에 의존하지 않는다.

`CardDecisionModule`의 기존 `Decide(GameState, EvaluationResult, PieceColor)` API는 기존 동작을 유지한다. P10 plan-aware overload는 `CardTargetingModule`과 optional engine top moves를 받아 다음 방식으로 카드와 plan을 함께 비교한다.

```text
combinedGain = existingCardEffectiveGain + selectedPlan.Score.Total
```

plan이 선택되지 않은 카드는 plan-aware decision 후보에서 제외한다.

## P10 점수 한계

P10 opportunity score는 카드 적용 후 Stockfish 점수가 아니다. 현재 `GameState`와 선택적으로 전달된 engine/move observation에서 계산한 deterministic heuristic이다.

명시적 한계:

- 후보마다 Stockfish를 호출하지 않는다.
- full legal move generator를 새로 구현하지 않는다.
- 카드 효과를 `GameState`에 exact/coarse apply하지 않는다.
- `MoveCandidate`는 UCI 한 수와 score만 제공하므로 PV path가 있다고 가정하지 않는다.
- Unity `BlockedTiles`, active piece effector, runtime caster 상태는 core `GameState`에 없다.
- `charge`/`portal` Unity caster 문제는 target score로 숨겨 보정하지 않는다.

`portal`은 legal empty square가 `N`개일 때 ordered pair가 `N * (N - 1)`까지 늘어난다. P10 기본값은 endpoint shortlist `16`개이며 pair scoring은 최대 `16 * 15 = 240`개로 제한한다.

## Unity Adapter 경계

Unity adapter는 AI release 이후 별도 단계에서 진행한다.

예상 책임:

- core `Square`를 Unity `Vector3Int`로 변환
- `PieceAtSquare`를 현재 Unity `Piece`로 재해석하고 color/kind 재검증
- `OrderedSquares` 순서 보존
- invalid/stale plan을 임의 target으로 대체하지 않고 failure code로 반환
- 기존 move-only fallback 유지

`charge`와 `portal`은 현재 Unity executor에서 `GameManager.Instance.PlayerColor`를 caster로 사용한다. P9/P10 core 계약에는 actor/caster 필요 근거로 기록하되, gameplay fix는 별도 이슈와 승인으로 분리한다.

## 제외 범위

P9에서 하지 않는 것:

- 45종 전체 target strategy
- 여러 카드 combo plan
- 45종 추가 `AiSupported` 확대
- 카드 효과 state apply
- P7 CSV 컬럼 확장
- 카드 점수, target score, threshold 시뮬레이션 기반 튜닝
- Unity gameplay bug 수정
- release/tag/DLL pin 교체

## 후속 단계

- P10 Unity 단계: release artifact를 Unity에 pin하고 first-valid target 선택을 core selected plan 소비로 교체
- P11: recommended/applied/skipped 분석과 밸런싱 수치 튜닝
