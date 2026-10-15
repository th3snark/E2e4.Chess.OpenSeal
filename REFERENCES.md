# Upstream reference

| What | Upstream | Pinned to | Licence |
|---|---|---|---|
| FatICS `timeseal/openseal.c` | <https://github.com/gbtami/FatICS> (an import of <https://bitbucket.org/wmahan/fatics>) | commit `577f5ccf62352de721f3f7a57632059150fe7506` (2014-04-13) | GPL v2 or later (Marcello Mamino) |

```
git clone https://github.com/gbtami/FatICS && git -C FatICS checkout 577f5ccf62352de721f3f7a57632059150fe7506
```

If the protocol logic in `src/` changes, check whether `native/MyOpenSeal` needs the equivalent change.
This repository was split out of <https://github.com/th3snark/E2e4.Chess>, and its history up to the split is preserved.
