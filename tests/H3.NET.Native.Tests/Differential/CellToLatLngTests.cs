// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential test: H3Index.ToLatLng must match the h3-py 4.5.0 oracle center to
/// within 1e-7 degrees (the two share the same C library).
/// </summary>
public sealed class CellToLatLngTests
{
    private const double ToleranceDegrees = 1e-7;

    public static IEnumerable<object[]> Cases() =>
        FixtureLoader.LoadCellToLatLng()
            .Select(c => new object[] { c.Cell, c.Lat, c.Lng });

    [Theory]
    [MemberData(nameof(Cases))]
    public void ToLatLng_MatchesOracle(string hex, double expectedLat, double expectedLng)
    {
        var cell = H3Index.Parse(hex);
        var center = cell.ToLatLng();

        Assert.Equal(expectedLat, center.LatitudeDegrees, ToleranceDegrees);
        Assert.Equal(expectedLng, center.LongitudeDegrees, ToleranceDegrees);
    }
}
