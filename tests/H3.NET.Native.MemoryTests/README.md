<!-- SPDX-License-Identifier: Apache-2.0 -->
# H3.NET.Native.MemoryTests

Two complementary leak gates for the native binding.

## Managed soak (`SoakTests.cs`)

xUnit v3 tests (all traited `[Trait("Category", "Soak")]`) that drive the binding in
tight loops and assert that managed heap and process working set (RSS) stay **bounded**
across a long run. They cover:

- the hot scalar paths (`FromLatLng` / `ToLatLng` / `GetBoundary` / `GridDisk`);
- the native **heap-owning** success path, `H3Polygon.FromCells`, which allocates the
  `LinkedGeoPolygon` via a `SafeHandle` and tears it down (`cellsToLinkedMultiPolygon`
  + `destroyLinkedMultiPolygon`);
- the **exception** path of that heap-owning code (invalid input), proving the
  `SafeHandle` releases when the native call throws, that it always raises a typed
  `H3Exception`, and that it never segfaults.

This is the managed gate: it proves bounded RSS including the native heap-ownership and
exception cleanup paths. It is intentionally robust to GC/allocator noise (see below),
so it catches *unbounded monotonic growth*, not byte-level leaks.

### Tuning

Iteration count comes from the `H3_SOAK_ITERS` env var (default `200000`, a few seconds
locally). CI can raise it for a longer soak:

```sh
H3_SOAK_ITERS=5000000 dotnet test --filter "Category=Soak"
```

## Native valgrind harness (`native-harness/`)

The pure-C program under `native-harness/` (built with its own `CMakeLists.txt`) is the
**authoritative** byte-level leak gate, run under valgrind in CI (Linux only). It is not
part of the .NET build and is excluded from this project's compilation. See
`native-harness/README.md`.
