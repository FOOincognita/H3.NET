// SPDX-License-Identifier: Apache-2.0

using CsCheck;

namespace H3NET.Native.Tests.Properties;

/// <summary>
/// Shared CsCheck generators for H3 property tests. Latitude is bounded to
/// [-89.9, 89.9] and longitude to [-179.9, 179.9] to stay strictly inside the
/// validated domain (the binding rejects exact-pole/antimeridian only via the
/// differential corpus, not these properties), and resolution to [0, 15].
/// </summary>
internal static class Generators
{
    public static readonly Gen<double> Latitude = Gen.Double[-89.9, 89.9];

    public static readonly Gen<double> Longitude = Gen.Double[-179.9, 179.9];

    public static readonly Gen<int> Resolution = Gen.Int[0, 15];

    public static readonly Gen<LatLng> LatLngGen =
        Latitude.Select(Longitude, (lat, lng) => new LatLng(lat, lng));

    public static readonly Gen<(LatLng Point, int Resolution)> PointAtResolution =
        LatLngGen.Select(Resolution, (p, r) => (p, r));
}
