# P9 CardUsePlan 계약

이 문서는 P9에서 추가한 카드 사용 계획 계약의 범위와 후속 단계 경계를 정리한다.

## 목적

P9의 목적은 AI가 단순히 “어떤 카드를 쓸지”만 추천하는 상태에서 벗어나, 다음 정보를 순수 C# 데이터로 표현하고 검증할 수 있게 하는 것이다.

```text
CardUsePlan = actor + card ID + concrete target + allowed parameter
```

이 계약은 Unity 실행 경계와 headless simulator가 같은 plan 정보를 해석하기 위한 기반이다. 카드별로 좋은 target을 고르는 판단은 P10, 사용률과 승률 기반 수치 튜닝은 P11 범위다.

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

P9 headless trace는 state mutation을 하지 않는다. `agile` movement override, `charge` pawn movement, `fire` delayed destruction, `peace_zone` capture cancellation, `portal` teleport/shared uses 적용은 P9 범위가 아니다.

## Unity Adapter 경계

Unity adapter는 AI release 이후 별도 단계에서 진행한다.

예상 책임:

- core `Square`를 Unity `Vector3Int`로 변환
- `PieceAtSquare`를 현재 Unity `Piece`로 재해석하고 color/kind 재검증
- `OrderedSquares` 순서 보존
- invalid/stale plan을 임의 target으로 대체하지 않고 failure code로 반환
- 기존 move-only fallback 유지

`charge`와 `portal`은 현재 Unity executor에서 `GameManager.Instance.PlayerColor`를 caster로 사용한다. P9 core 계약에는 actor/caster 필요 근거로 기록하되, gameplay fix는 별도 이슈와 승인으로 분리한다.

## 제외 범위

P9에서 하지 않는 것:

- 카드별 좋은 target score/heuristic
- target 후보 탐색과 ranking
- 카드 사용 시점 최적화
- 여러 카드 combo plan
- 45종 추가 `AiSupported` 확대
- 카드 효과 state apply
- P7 CSV 컬럼 확장
- 카드 점수, target score, threshold 튜닝
- Unity gameplay bug 수정
- release/tag/DLL pin 교체

## 후속 단계

- P10: 카드별 지능형 타겟팅과 target strategy
- P11: recommended/applied/skipped 분석과 밸런싱 수치 튜닝
