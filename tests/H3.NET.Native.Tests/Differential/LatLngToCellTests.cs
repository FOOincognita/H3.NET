// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential test: H3Index.FromLatLng must match the h3-py 4.5.0 oracle exactly
/// (cells are compared as raw ulong; no tolerance applies to the index itself).
/// </summary>
public sealed class LatLngToCellTests
{
    public static IEnumerable<object[]> Cases() =>
        FixtureLoader.LoadLatLngToCell()
            .Select(c => new object[] { c.Lat, c.Lng, c.Res, c.Cell });

    [Theory]
    [MemberData(nameof(Cases))]
    public void FromLatLng_MatchesOracle(double lat, double lng, int res, string expectedHex)
    {
        var expected = H3Index.Parse(expectedHex);
        var actual = H3Index.FromLatLng(new LatLng(lat, lng), res);

        Assert.Equal(expected.Value, actual.Value);
    }
}
