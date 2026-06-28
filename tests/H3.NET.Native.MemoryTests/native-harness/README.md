<!-- SPDX-License-Identifier: Apache-2.0 -->

# Native valgrind leak harness

`leakcheck.c` is a small, dependency-free C program that exercises the exact
native call sequences the `H3.NET.Native` binding uses against `libh3`, in a
loop, with the same buffer-sizing and ownership rules.

## What it proves

Running the harness under valgrind proves that the **native usage pattern** the
managed binding relies on is leak-free. It covers:

- `latLngToCell` / `cellToLatLng` / `cellToBoundary` — caller-owned, fixed-size
  output structs (no heap).
- `maxGridDiskSize` + `gridDisk` — size-then-allocate-exactly; the caller mallocs
  and frees the output buffer; `H3_NULL` padding slots are tolerated.
- `maxPolygonToCellsSize` + `polygonToCells` — building a `GeoPolygon` from a
  heap-allocated `GeoLoop` (no holes), sizing and allocating the output, then
  freeing both. `flags` is `0` as required by the non-experimental API.
- `cellsToLinkedMultiPolygon` + `destroyLinkedMultiPolygon` — the heap-owning
  path. The **caller** owns the head `LinkedGeoPolygon` (here on the stack);
  `cellsToLinkedMultiPolygon` heap-allocates the loops, coordinate nodes, and any
  subsequent polygon nodes; `destroyLinkedMultiPolygon` frees those children and
  zeroes the head (it does not free the head). The harness also calls destroy
  twice to prove idempotency (no double-free).

Every `H3Error` is checked; the process exits non-zero on the first failure.
`main` takes an optional iteration count (default `50`).

## How CI invokes valgrind (Linux only)

```sh
cmake -B build -S . -DH3_BUILD_DIR=../../../external/h3/build
cmake --build build

valgrind --leak-check=full --show-leak-kinds=all --errors-for-leak-kinds=all \
         --error-exitcode=1 ./build/leakcheck 100
```

`--error-exitcode=1` makes valgrind return non-zero on any leak or memory error,
failing the CI memory job. The harness's own non-zero exit on any `H3Error`
provides a second guard.

## Why it is Linux-only

`valgrind` is not supported on modern macOS (no working arm64 / recent Darwin
port), so the leak-checking step runs only in the Linux CI job. The harness
still **compiles and links** locally on macOS against the already-built
`libh3.dylib`, which is the local smoke test; the actual leak verification
happens in Linux CI.

## Local build/run (macOS, smoke test only)

```sh
cc -std=c11 -Wall -Wextra \
  -I../../../external/h3/build/src/h3lib/include \
  -I../../../external/h3/src/h3lib/include \
  leakcheck.c \
  -L../../../external/h3/build/lib -lh3 \
  -Wl,-rpath,../../../external/h3/build/lib -lm -o leakcheck
./leakcheck 100   # prints "OK: ..." and exits 0
```
