// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3NET.Native.Tests.Fixtures;
using Xunit;

namespace H3NET.Native.Tests.Differential;

/// <summary>
/// Differential test: H3Polygon.ToCells coverage must match the h3-py 4.5.0 oracle as
/// an unordered set (order is not guaranteed; the binding strips H3_NULL).
/// </summary>
public sealed class PolygonToCellsTests
{
    private static readonly List<FixtureLoader.PolygonToCellsCase> AllCases =
        FixtureLoader.LoadPolygonToCells().ToList();

    public static IEnumerable<object[]> Cases() =>
        Enumerable.Range(0, AllCases.Count).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(Cases))]
    public void ToCells_MatchesOracle_AsSet(int index)
    {
        var testCase = AllCases[index];
        var exterior = ToRing(testCase.Polygon.Exterior);
        var holes = (testCase.Polygon.Holes ?? [])
            .Select(h => (IReadOnlyList<LatLng>)ToRing(h))
            .ToList();

        var polygon = new GeoPolygon(exterior, holes.Count == 0 ? null : holes);

        var expected = testCase.Cells.Select(s => H3Index.Parse(s).Value).ToHashSet();
        var actual = H3Polygon.ToCells(polygon, testCase.Res).Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }

    // Oracle ring layout is [lat, lng].
    private static List<LatLng> ToRing(IEnumerable<double[]> ring) =>
        ring.Select(p => new LatLng(p[0], p[1])).ToList();
}
