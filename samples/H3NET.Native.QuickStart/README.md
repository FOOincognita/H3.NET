<!-- SPDX-License-Identifier: Apache-2.0 -->
# H3NET.Native QuickStart

A minimal console app that consumes **H3NET.Native** the way a real external
consumer would: via a `PackageReference` to the packed `.nupkg`, resolved from a
**local feed** (`artifacts/feed`). This is the path that proves the native `libh3`
asset is delivered by the **package** (its `runtimes/{rid}/native/` payload), with
no help from the repository's `eng/` targets.

This sample is deliberately **not** part of `H3.NET.slnx`. It opts out of the
repo's Central Package Management and is restored/built on its own.

## How it works

- `nuget.config` here clears inherited sources and adds `nuget.org` plus the local
  feed at `../../artifacts/feed`.
- The project sets `ManagePackageVersionsCentrally=false` and references the package
  with a floating prerelease version `*-*`, so restore picks up whatever build of
  `H3NET.Native` is currently in the local feed.
- It does **not** import `eng/H3NET.Native.targets`. The native library must arrive
  through the package alone.

## Run it

From the **repository root**:

```bash
# 1. Pack the binding into the local feed the sample restores from.
dotnet pack src/H3NET.Native/H3NET.Native.csproj -c Release -o artifacts/feed

# 2. Run the sample. Restore resolves H3NET.Native from the local feed
#    (this directory's nuget.config), and the package supplies native libh3.
dotnet run --project samples/H3NET.Native.QuickStart -c Release
```

Expected output: the indexed cell for `(37.7752, -122.4188)` at resolution 9, its
center round-trip, boundary vertex count and first vertex, the 7-cell `GridDisk(1)`,
and the cell count from filling a small bounding-box polygon. All values are in
**degrees**.

## Troubleshooting

- **`H3NET.Native` not found on restore**: ensure step 1 actually produced a
  `.nupkg` under `artifacts/feed`. If you re-pack, you may need to clear the NuGet
  HTTP/global cache entry for the floating version (`dotnet nuget locals all --clear`)
  so the new package is picked up.
- **`DllNotFoundException` for `libh3`**: the pack did not include a
  `runtimes/{your-rid}/native/libh3.*` entry for your host RID. Confirm the native
  library was built into `runtimes/` before packing.
