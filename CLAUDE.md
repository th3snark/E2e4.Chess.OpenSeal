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

## Consumer: MyChess (sibling repo `../MyChess`)

`MyChess` (MIT) is the only known consumer; it references this package as `E2e4.Chess.OpenSeal` (pinned in `MyChess/DotNET/Directory.Packages.props`, currently 1.0.3).

- **Licensing boundary:** this package is GPL; MyChess keeps it out of its libraries. Only `E2e4.Chess.Fics.Client.Demo` references it, via an `OpenSealCodec : ITimesealCodec` adapter (`Encode(line) => OpenSealCipher.Encode(line)`), so only a built demo is GPL. `E2e4.Chess.Fics.Client` takes the codec through `FicsClientOptions.Timeseal` (null = plain lines). Do not change this package in a way that requires MyChess libraries to depend on it.
- **Contract MyChess relies on:** `Encode` returns the full wire text including the `\x80\n` terminator, as a Latin-1 string; MyChess sends it as ISO-8859-1 bytes (`FicsLineEncoder`) and appends `\n` itself only when no codec is set. Changing the output shape, terminator, or character range breaks it.
- **How MyChess uses it:** after connecting, it sends the hello line `TIMESTAMP|openseal|Running on an operating system|` through the codec (the same string as `Hello` in `OpenSealTestData.cs`), then every outbound line, and answers each server `[G]` ping with the encoded reply `\x02` + `9`. Its tests use a fake codec (`SEAL:` + line + `\x80\n`), not this library, so a break here will not show up in MyChess's test suite.
- **Release flow:** a tag here publishes to nuget.org; MyChess then bumps the version in its `Directory.Packages.props`.
- `REFERENCES.md` in MyChess points at this repo for the C# openseal port and documents the FatICS commit (`577f5ccf...`) the algorithm derives from; this repo's `REFERENCES.md` carries its own copy, so keep the two consistent.

## Build constraints

- Library multi-targets `net45;netstandard2.0;netstandard2.1;net10.0`, so avoid APIs missing from net45/netstandard2.0 (use `Substring`, not ranges; add polyfills only as internal types).
- `Directory.Build.props` enables `Nullable`, `TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` (style violations in `.editorconfig` break the build). NuGet versions are centrally managed in `Directory.Packages.props`.
