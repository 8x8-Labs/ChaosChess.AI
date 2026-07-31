# Unity 소비 절차

Chaos-Chess-v2는 승인된 ChaosChess.AI GitHub Release 아티팩트만 사용한다. 로컬 build output을 Unity 프로젝트에 직접 복사하지 않는다.

## 적용 전 확인

AI Release가 성공한 뒤 다음 값을 확정한다.

- release version
- tag
- target commit SHA
- release URL
- artifact name
- `manifest.json`
- `SHA256SUMS.txt`
- `ChaosChess.AI.dll` SHA-256

## 적용 흐름

```text
GitHub Release
  -> exact artifact download
  -> SHA256SUMS.txt 검증
  -> manifest.json 검증
  -> DLL hash 확인
  -> Unity Plugins DLL/PDB 교체
  -> ChaosChessAiVersion 상수 갱신
  -> Unity Editor compile 확인
```

Unity에서 갱신할 후보 파일:

```text
ChaosChess_v2/Assets/Plugins/ChaosChess.AI/ChaosChess.AI.dll
ChaosChess_v2/Assets/Plugins/ChaosChess.AI/ChaosChess.AI.pdb
ChaosChess_v2/Assets/Script/AIIntegration/ChaosChessAiVersion.cs
```

`.meta` 파일은 읽거나 수정하지 않는다. 기존 `.meta`는 그대로 유지한다.

## Version Pin

`ChaosChessAiVersion.cs`에는 현재 repository, commit SHA, DLL SHA-256이 있다. P8 Unity pin 갱신에서는 다음 값을 추가하는 방향을 검토한다.

- semantic version
- tag
- release URL
- artifact name
- manifest schema version

Unity 런타임에서 최신 release를 조회하지 않는다. 게임 실행 중 자동 업데이트도 하지 않는다.

## 검증

에이전트 검증:

- DLL SHA-256과 manifest 일치
- `ChaosChessAiVersion` 값과 release 값 일치
- 금지 dependency 없음
- 관련 Unity `.cs` 정적 검토
- unrelated Unity 파일 변경 없음
- `.meta` 변경 없음

사용자 검증:

1. Unity 6000.0.68f1에서 프로젝트 열기
2. DLL import 후 compile 완료 확인
3. Console compile error 확인
4. P6 AI 통합 테스트 씬 또는 기존 수동 흐름 확인

에이전트는 Unity Play Mode에 들어가지 않는다.
