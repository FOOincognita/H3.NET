# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Initial scaffolding of H3NET.Native, a thin idiomatic P/Invoke binding over
Uber H3 v4.5.0 for .NET 10+. No versions have been released yet.

### Added

- Repository bootstrap: solution and project structure targeting `net10.0` and
  `net8.0`.
- Project governance: `README`, `CONTRIBUTING`, `CODE_OF_CONDUCT`, `SECURITY`,
  and this changelog.
- Native build skeleton: Uber H3 pinned as a git submodule at tag `v4.5.0`
  (`external/h3`) with a per-RID native build script producing
  `runtimes/<rid>/native/libh3.{so,dylib}` for `linux-x64`, `linux-musl-x64`,
  `osx-x64`, and `osx-arm64`.
- Vertical slice: an initial ~5-function public API exercising the full
  native-build, packaging, and P/Invoke interop path end to end (degrees-based
  lat/lng public surface).
- Test fixture tooling: pinned Python venv with `h3>=4` for generating reference
  fixtures.

### Planned

- Continuous integration workflow (build, test, pack) and a CI status badge.
- Incremental fill-in of the full Uber H3 API surface (~70 functions).
- Published documentation site at https://FOOincognita.github.io/H3.NET/.
- First tagged release.

[Unreleased]: https://github.com/FOOincognita/H3.NET/commits/main
