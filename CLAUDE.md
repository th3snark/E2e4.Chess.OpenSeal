# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`E2e4.Chess.OpenSeal`: a small C# NuGet library implementing the FICS timeseal/openseal encoder (and a decoder), derived from `openseal.c` / `openseal_decoder.c` by Marcello Mamino (FatICS). It is GPL-2.0-or-later; keep `NOTICE` and `REFERENCES.md` accurate when changing derived logic. It is a fixed-key XOR/shuffle that carries a client timestamp, not real cryptography.

## Commands

```
dotnet test OpenSeal.slnx                                   # build + all tests (CI runs this with --configuration Release)
dotnet test tests/E2e4.Chess.OpenSeal.Test --filter "FullyQualifiedName~OpenSealWireFormatUnitTest"   # one test class
dotnet pack src/E2e4.Chess.OpenSeal/E2e4.Chess.OpenSeal.csproj --configuration Release
```

Releases: push a `vX.Y.Z` tag; `publish.yml` tests, packs with `-p:Version=<tag minus v>`, and pushes to nuget.org via Trusted Publishing. The csproj intentionally has no fixed `Version`.

## Architecture

- `src/E2e4.Chess.OpenSeal/OpenSeal.cs`: the whole public API (`Encode`, `Decode`, `TryDecode`). Wire format: `text + \x18 + timestamp + \x19`, padded with `'1'` to a multiple of 12, each 12-block shuffled (three symmetric swaps; self-inverse), then `((c|0x80) ^ Key[n % keyLen]) - 32`, then a `\x80` key-offset char and `\n`. Strings are Latin-1; chars above U+00FF become `?`. `TryDecode` is deliberately slightly stricter than the C decoder (see its remarks).
- `Helpers/TimeHelper.cs` (internal): converts a Windows file time (0 = now) into the `ssssuuu` timestamp (last 4 digits of seconds + 3 of milliseconds). The `systemTime` parameter exists so tests can be deterministic.
- `Polyfills.cs`: internal `NotNullWhen` for older targets.
- `native/MyOpenSeal`: Win32 C reference port, not part of the .NET build (needs MSBuild with C++ toolset).
- Tests (xunit, net10.0 only) use golden byte vectors from a port of `crypt()` in `OpenSealTestData.cs`; changes to the algorithm must keep them passing.

## Build constraints

- Library multi-targets `net45;netstandard2.0;netstandard2.1;net10.0`, so avoid APIs missing from net45/netstandard2.0 (use `Substring`, not ranges; add polyfills only as internal types).
- `Directory.Build.props` enables `Nullable`, `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` (style violations in `.editorconfig` break the build). NuGet versions are centrally managed in `Directory.Packages.props`.
