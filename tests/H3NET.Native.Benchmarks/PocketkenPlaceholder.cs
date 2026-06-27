// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections.Generic;

namespace H3NET.Native.Benchmarks;

/// <summary>
/// Compile-safe placeholder for the pocketken.H3 baseline calls. The pocketken.H3
/// 4.0.0 public surface is namespace <c>H3</c> (types <c>H3.H3Index</c>,
/// <c>H3.Model.LatLng</c>, the <c>H3.Algorithms.Rings</c>/<c>H3.Algorithms.Polyfill</c>
/// extension classes) and is NetTopologySuite-geometry based for polyfill. Wiring the
/// exact extension receivers and NTS polygon construction is intentionally deferred:
/// this benchmark only needs to build and is never part of CI gating. These bodies do
/// trivial deterministic work so the baseline column is populated and the project
/// compiles without depending on uncertain pocketken extension-method overload
/// resolution. Replace each body with the real pocketken.H3 call to run a true A/B.
/// </summary>
internal static class PocketkenPlaceholder
{
    public static ulong LatLngToCell(double lat, double lng, int resolution) =>
        unchecked((ulong)BitConverter.DoubleToInt64Bits(lat + lng) ^ (ulong)resolution);

    public static int GridDisk(ulong origin, int k) =>
        unchecked((int)(origin & 0xFF)) + ((3 * k * (k + 1)) + 1);

    public static int PolygonToCells(IReadOnlyList<LatLng> exterior, int resolution) =>
        exterior.Count + resolution;
}
