# H3NET.Native

[![NuGet](https://img.shields.io/nuget/v/H3NET.Native.svg)](https://www.nuget.org/packages/H3NET.Native/)
[![License: Apache 2.0](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
<!-- CI build-status badge intentionally omitted until the CI workflow lands in a later PR. -->

A thin, idiomatic P/Invoke binding over [Uber H3](https://h3geo.org) v4.5.0 for .NET 10+. H3NET.Native exposes the H3 hexagonal hierarchical geospatial indexing system through a clean, managed .NET API. The native `libh3` is built from a pinned upstream source revision and bundled directly in the NuGet package, so consumers need no C compiler, CMake, or other native toolchain to use it.

## Package and repository naming

The repository is named **H3.NET**, but the published NuGet **PackageId is `H3NET.Native`**. The id `H3.NET` was already taken on nuget.org by an unrelated package, so the binding is distributed as `H3NET.Native`. Use `H3NET.Native` in all `dotnet add package` and `PackageReference` declarations.

## Status

Early preview / scaffold in progress. The repository currently contains the project skeleton and an approximately five-function vertical slice that exercises the full native-build, packaging, and interop path end to end. The complete H3 API (roughly seventy functions) is being filled in incrementally over subsequent PRs. Treat the public surface as unstable until the first tagged release.

## Installation

```sh
dotnet add package H3NET.Native
```

The bundled native assets are delivered through NuGet's `runtimes/{rid}/native/` convention. This means H3NET.Native must be consumed as a **`PackageReference`** (from nuget.org or another feed) so the native libraries propagate to your build and publish output. A bare **`ProjectReference`** to the source project does **not** carry the bundled native libraries, and your application will fail to load `libh3` at runtime.

## Supported platforms

| Aspect | Support |
| --- | --- |
| Target frameworks | `net10.0`, `net8.0` |
| Runtime identifiers | `linux-x64`, `linux-musl-x64`, `osx-x64`, `osx-arm64` |

There is currently **no Windows support** and **no Native AOT requirement or support**; both are out of scope for the current scaffold and may be considered later.

## Quickstart

The following snippet illustrates the **target** public API for the current vertical slice. All latitude and longitude values are in **degrees** (radians are internal only), matching `h3-py` and `h3-go`. Method names and shapes may change while the binding is in preview.

```csharp
using H3NET.Native;

var cell = H3Index.FromLatLng(new LatLng(37.7752, -122.4188), resolution: 9);
Console.WriteLine(cell);                 // H3 index

var center = cell.ToLatLng();            // back to LatLng (degrees)

foreach (var neighbor in cell.GridDisk(1))
    Console.WriteLine(neighbor);
```

## Versioning

The library follows [Semantic Versioning](https://semver.org). Versions are derived from annotated git tags prefixed with `v` (for example `v0.1.0`) via [MinVer](https://github.com/adamralph/minver). The library version is **independent of** the bundled Uber H3 version but tracks it: the binding currently bundles and targets Uber H3 **v4.5.0**.

## Links

- API documentation: https://FOOincognita.github.io/H3.NET/ (available once docs publish)
- Uber H3: https://h3geo.org
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
- [Code of conduct](CODE_OF_CONDUCT.md)
- [Changelog](CHANGELOG.md)

## License

Licensed under the [Apache License, Version 2.0](LICENSE). Copyright 2026 FOOincognita.

Uber H3 is a separate project, also licensed under Apache-2.0, copyright Uber Technologies, Inc. See https://github.com/uber/h3.
