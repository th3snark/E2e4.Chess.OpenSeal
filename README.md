# E2e4.Chess.OpenSeal

A C# implementation of the FICS (Free Internet Chess Server) timeseal/openseal encoder: the cipher that lets a
chess client tell the server when it sent each line, so the server can compensate for network lag.

```csharp
string wire = OpenSeal.Encode("seek 5 0");   // wire text, ends "\x80\n"; send it as ISO-8859-1
```

Targets `net45`, `netstandard2.0`, `netstandard2.1` and `net10.0`. Install from NuGet: `E2e4.Chess.OpenSeal`.

## Licence

GNU General Public License, version 2 or (at your option) any later version. This code is derived from
`timeseal/openseal.c` by Marcello Mamino, distributed with FatICS; see [NOTICE](NOTICE) and [REFERENCES.md](REFERENCES.md).
An application that includes this package is distributed under the GPL.

## Layout

- `src/E2e4.Chess.OpenSeal` - the library.
- `tests/E2e4.Chess.OpenSeal.Test` - unit tests, including golden vectors from a port of `crypt()` in `openseal.c`.
- `native/MyOpenSeal` - a Win32 (WinSock) C port of openseal, kept as a reference. It needs MSBuild with a Visual Studio C++ toolset.

## Use with FicsClientLibrary

[MyChess](https://github.com/th3snark/MyChess)'s `FicsClientLibrary` takes an `ITimesealCodec`; the adapter is one line:

```csharp
sealed class OpenSealCodec : ITimesealCodec
{
    public string Encode(string line) => OpenSeal.Encode(line);
}
```

## Build and test

```
dotnet test OpenSeal.slnx
```
