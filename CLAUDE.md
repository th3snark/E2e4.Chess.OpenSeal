# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`E2e4.Chess.OpenSeal`: a small C# NuGet library implementing the FICS timeseal/openseal encoder (and a decoder), derived from `openseal.c` / `openseal_decoder.c` by Marcello Mamino (FatICS). It is GPL-2.0-or-later; keep `NOTICE` and `REFERENCES.md` accurate when changing derived logic. It is a fixed-key XOR/shuffle that carries a client timestamp, not real cryptography.

## Commands

```
dotnet test E2e4.Chess.OpenSeal.slnx                                   # build + all tests (CI runs this with --configuration Release)
dotnet test tests/E2e4.Chess.OpenSeal.Test --filter "FullyQualifiedName~OpenSealWireFormatUnitTest"   # one test class
dotnet pack src/E2e4.Chess.OpenSeal/E2e4.Chess.OpenSeal.csproj --configuration Release
```

Releases: push a `vX.Y.Z` tag; `publish.yml` tests, packs with `-p:Version=<tag minus v>`, and pushes to nuget.org via Trusted Publishing. The csproj intentionally has no fixed `Version`.

## Architecture

- `src/E2e4.Chess.OpenSeal/OpenSealCipher.cs`: the whole public API (static class `OpenSealCipher`) (`Encode`, `Decode`, `TryDecode`). Wire format: `text + \x18 + timestamp + \x19`, padded with `'1'` to a multiple of 12, each 12-block shuffled (three symmetric swaps; self-inverse), then `((c|0x80) ^ Key[n % keyLen]) - 32`, then a `\x80` key-offset char and `\n`. Strings are Latin-1; chars above U+00FF become `?`. `TryDecode` is deliberately slightly stricter than the C decoder (see its remarks).
- `Helpers/TimeHelper.cs` (internal): converts a Windows file time (0 = now) into the `ssssuuu` timestamp (last 4 digits of seconds + 3 of milliseconds). The `systemTime` parameter exists so tests can be deterministic; it is reachable only through the internal `OpenSealCipher.Encode(text, systemTime)` overload.
- `Polyfills.cs`: internal `NotNullWhen` for older targets.
- `native/MyOpenSeal`: Win32 C reference port, not part of the .NET build (needs MSBuild with C++ toolset).
- Tests (xunit, net10.0 only) use golden byte vectors from a port of `crypt()` in `OpenSealTestData.cs`; changes to the algorithm must keep them passing.

## Consumer: E2e4.Chess.Fics.Client.Demo

The only consumer is the private `E2e4.Chess.Fics.Client.Demo` app, which references this package as `E2e4.Chess.OpenSeal` (pinned in its `Directory.Packages.props`, currently 2.0.1).

- **Licensing boundary:** this package is GPL; the MIT libraries keep it out. Only the demo references it, via an `OpenSealCodec : ITimesealCodec` adapter (`Encode(line) => OpenSealCipher.Encode(line)`), so only a built demo is GPL. `E2e4.Chess.Fics.Client` takes the codec through `FicsClientOptions.Timeseal` (null = plain lines). Do not change this package in a way that requires those libraries to depend on it.
- **Contract the consumer relies on:** `Encode` returns the full wire text including the `\x80\n` terminator, as a Latin-1 string; `E2e4.Chess.Fics.Client` sends it as ISO-8859-1 bytes (`FicsLineEncoder`) and appends `\n` itself only when no codec is set. Changing the output shape, terminator, or character range breaks it.
- **How it is used:** after connecting, the client sends the hello line `TIMESTAMP|openseal|Running on an operating system|` through the codec (the same string as `Hello` in `OpenSealTestData.cs`), then every outbound line, and answers each server `[G]` ping with the encoded reply `\x02` + `9`. The Fics.Client tests use a fake codec (`SEAL:` + line + `\x80\n`), not this library, so a break here will not show up in their test suite.
- **Release flow:** a tag here publishes to nuget.org; then bump the version in the demo's `Directory.Packages.props`.
- `REFERENCES.md` here carries the FatICS commit (`577f5ccf...`) the algorithm derives from; the E2e4.Chess repository's `REFERENCES.md` points at this repo, so keep the two consistent.

## Build constraints

- `assets/icon.png` (and its source `assets/icon.svg`) is generated, not hand-drawn: it is the E2e4.Chess base icon plus this package's badge, built by `Branding/Build-Icons.ps1` in the E2e4.Chess repository. Change the icon there and copy the files from `Branding/dist`.
- Library multi-targets `net45;netstandard2.0;netstandard2.1;net10.0`, so avoid APIs missing from net45/netstandard2.0 (use `Substring`, not ranges; add polyfills only as internal types).
- `Directory.Build.props` enables `Nullable`, `AnalysisLevel latest-recommended`, `GenerateDocumentationFile`, `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` (style violations in `.editorconfig` break the build). NuGet versions are centrally managed in `Directory.Packages.props`.
- `E2e4.Chess.OpenSeal.csproj` sets `NoWarn` CA1510 on purpose: `ArgumentNullException.ThrowIfNull` does not exist on net45/netstandard2.0.
- Line endings are CRLF (`.editorconfig`); Write/Edit tools and `sed -i` can leave LF, which fails `dotnet format whitespace --verify-no-changes` (run in CI).
