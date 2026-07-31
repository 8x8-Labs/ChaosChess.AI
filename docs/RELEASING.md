# ChaosChess.AI 릴리스 절차

이 문서는 ChaosChess.AI core DLL을 Unity에서 고정 소비할 수 있는 GitHub Release 아티팩트로 발행하는 절차를 정리한다.

## 버전 정책

- 버전은 SemVer `vMAJOR.MINOR.PATCH` 형식의 Git tag를 기준으로 한다.
- 첫 정식 DLL 릴리스 후보는 `v0.1.0`이다. P8 단계 번호를 `0.8.0`으로 해석하지 않는다.
- 정식 전 검증이 필요하면 `v0.1.0-rc.1` 같은 prerelease tag를 사용한다.
- `VersionPrefix`, `AssemblyVersion`, `FileVersion`의 기본값은 `Directory.Build.props`에 둔다.
- release build에서는 `InformationalVersion`을 `<version>+<40-char commit sha>`로 주입한다.
- 공유된 tag는 이동하거나 덮어쓰지 않는다. 잘못된 release는 새 patch 또는 새 RC로 정정한다.

현재 repository에는 license가 확정되어 있지 않다. release workflow는 `LICENSE` 파일이 없으면 GitHub Release 생성을 중단한다. 라이선스 문구는 저장소 소유자 승인 없이 임의로 추가하지 않는다.

## 기준 브랜치

P8 hardening 구현은 feature branch에서 진행하고 `develop` PR로 검증한다. 첫 정식 릴리스 tag는 별도 승인 후 `main`에 도달한 commit에 찍는다.

권장 흐름:

```text
feature/release-hardening
  -> develop PR
  -> develop merge
  -> develop to main release PR
  -> annotated tag v0.1.0 on main commit
  -> GitHub Release
```

## Unity 아티팩트 구성

기본 Unity bundle 이름:

```text
ChaosChess.AI-vX.Y.Z-unity.zip
```

zip 내부:

```text
ChaosChess.AI.dll
ChaosChess.AI.pdb
ChaosChess.AI.xml
manifest.json
SHA256SUMS.txt
```

포함하지 않는 항목:

- `ChaosChess.AI.Stockfish.dll`
- `ChaosChess.AI.Simulator`
- Fairy Stockfish 실행 파일
- `variants.ini`
- Unity 프로젝트 파일
- NuGet package

## Manifest와 Checksum

`manifest.json`은 release 아티팩트의 출처와 core DLL 정보를 담는다.

```json
{
  "schemaVersion": 1,
  "name": "ChaosChess.AI",
  "version": "0.1.0",
  "tag": "v0.1.0",
  "commitSha": "<40-char sha>",
  "targetFramework": "netstandard2.1",
  "assembly": "ChaosChess.AI.dll",
  "files": []
}
```

`files`에는 DLL, PDB, XML의 `path`, uppercase `sha256`, `size`를 기록한다. `manifest.json` 자체의 hash는 manifest에 넣지 않고 `SHA256SUMS.txt`에만 기록해 순환 참조를 피한다.

## 로컬 dry-run

```powershell
dotnet restore ChaosChess.AI.sln
dotnet build ChaosChess.AI.sln --configuration Release --no-restore
dotnet test ChaosChess.AI.sln --configuration Release --no-build
.\scripts\package-unity.ps1 -Version 0.1.0 -OutputRoot .\artifacts\release -NoBuild
```

검증 항목:

- `manifest.json`의 DLL hash와 실제 DLL SHA-256 일치
- `SHA256SUMS.txt`에 DLL, PDB, XML, manifest hash 존재
- DLL assembly name이 `ChaosChess.AI`
- target framework가 `netstandard2.1`
- `UnityEngine`, DOTween, `FairyStockfishBridge`, `ChaosChess.AI.Stockfish` 참조 없음

## GitHub Release

`.github/workflows/release.yml`은 `v*` tag push에서만 실행된다.

workflow는 다음을 확인한다.

- tag가 `v`로 시작하는 SemVer인지
- tag target commit이 `origin/main`에서 도달 가능한지
- release가 이미 존재하지 않는지
- `LICENSE`가 존재하는지
- restore/build/test/package가 통과하는지

이 workflow 파일이 merge된 것과 첫 tag를 발행하는 것은 별도 승인 단계다.

## Rollback

Unity pin은 release tag, commit SHA, artifact hash를 기준으로 고정한다. 문제가 생기면 이전 release의 tag, commit, DLL hash로 되돌린다.

이미 공유된 tag는 이동하지 않는다. 잘못된 release는 삭제로 숨기지 않고 새 patch 또는 새 RC로 정정한다.
