# H3.NET.Native

[![NuGet](https://img.shields.io/nuget/v/H3.NET.Native.svg)](https://www.nuget.org/packages/H3.NET.Native/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![CI](https://github.com/FOOincognita/H3.NET.Native/actions/workflows/ci.yml/badge.svg)](https://github.com/FOOincognita/H3.NET.Native/actions/workflows/ci.yml)

A thin, idiomatic P/Invoke binding over [Uber H3](https://h3geo.org) v4.5.0 for .NET 8 and .NET 10 (`net8.0` / `net10.0`). H3.NET.Native exposes the H3 hexagonal hierarchical geospatial indexing system through a clean, managed .NET API. The native `libh3` is built from a pinned upstream source revision and bundled directly in the NuGet package, so consumers need no C compiler, CMake, or other native toolchain to use it.

## NuGet package id

The published NuGet **PackageId is `H3.NET.Native`**. The shorter id `H3.NET` was already taken on nuget.org by an unrelated package, so the binding is distributed as `H3.NET.Native`. Use `H3.NET.Native` in all `dotnet add package` and `PackageReference` declarations.

## Status

First preview release on the `0.x` line. The full Uber H3 v4.5.0 public surface (roughly seventy functions across inspection, hierarchy, grid traversal, directed edges, vertices, measures/units, and regions) is implemented and validated. As a `0.x` preview the API is largely stable but may still receive minor refinements before `1.0`.

## Installation

```sh
dotnet add package H3.NET.Native
```

The bundled native assets are delivered through NuGet's `runtimes/{rid}/native/` convention. This means H3.NET.Native must be consumed as a **`PackageReference`** (from nuget.org or another feed) so the native libraries propagate to your build and publish output. A bare **`ProjectReference`** to the source project does **not** carry the bundled native libraries, and your application will fail to load `libh3` at runtime.

## Supported platforms

| Aspect | Support |
| --- | --- |
| Target frameworks | `net10.0`, `net8.0` |
| Runtime identifiers | `linux-x64`, `linux-musl-x64`, `osx-arm64` |

There is currently **no Windows support** and **no Native AOT requirement or support**; both are out of scope for this initial release and may be considered later.

## Quickstart

The following snippet illustrates the public API. All latitude and longitude values are in **degrees** (radians are internal only), matching `h3-py` and `h3-go`. On the `0.x` line the API may still see minor refinements before `1.0`.

```csharp
using H3.NET.Native;

var cell = H3Index.FromLatLng(new LatLng(37.7752, -122.4188), resolution: 9);
Console.WriteLine(cell);                 // H3 index

var center = cell.ToLatLng();            // back to LatLng (degrees)

foreach (var neighbor in cell.GridDisk(1))
    Console.WriteLine(neighbor);
```

## Versioning

The library follows [Semantic Versioning](https://semver.org). Versions are derived from annotated git tags prefixed with `v` (for example `v0.1.0`) via [MinVer](https://github.com/adamralph/minver). The library version is **independent of** the bundled Uber H3 version but tracks it: the binding currently bundles and targets Uber H3 **v4.5.0**.

## Correctness and benchmarks

**Correctness** is validated against the reference implementation, not against other managed ports: the differential test corpus is generated from the official **Uber H3 C library** (via `h3-py` ≥ 4, pinned to the bundled v4.5.0), with **h3-go** available as a tiebreaker and a pure-C `valgrind` harness guarding native memory usage.

**Performance** is measured with [BenchmarkDotNet](https://benchmarkdotnet.org). Consistent with the early-preview status, the benchmark project today wires a single comparison:

- **[pocketken.H3](https://github.com/pocketken/H3.net)** — the managed (NetTopologySuite-based) port; answers the practical "how does this compare to the managed library many teams run today?" question. The pocketken side is currently a clearly-marked placeholder so the project compiles; its bodies must be replaced with the real pocketken.H3 calls before the numbers mean anything.

Two further baselines are **planned** to place this binding on the full spectrum:

- **Raw libh3 C** — direct P/Invoke into native libh3 with no idiomatic layer; the speed ceiling, isolating this library's own marshalling overhead.
- **[H3.Standard](https://github.com/entrepreneur-interet-general/H3.Standard)** — the other native P/Invoke binding (v4.0.1); an apples-to-apples binding comparison.

Benchmarks are informational, never gate CI, and their shapes may change while the binding is in preview.

## Links

- API documentation: https://FOOincognita.github.io/H3.NET.Native/
- Uber H3: https://h3geo.org
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [Code of conduct](CODE_OF_CONDUCT.md)
- [Changelog](CHANGELOG.md)

## License

Licensed under the [Apache License, Version 2.0](LICENSE). Copyright 2026 FOOincognita.

Uber H3 is a separate project, also licensed under Apache-2.0, copyright Uber Technologies, Inc. See https://github.com/uber/h3.
