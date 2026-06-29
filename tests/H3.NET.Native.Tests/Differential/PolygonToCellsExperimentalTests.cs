// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential test: H3Polygon.ToCellsExperimental coverage must match the h3-py 4.5.0
/// oracle (h3shape_to_cells_experimental) as an unordered set, for every ContainmentMode.
/// The result is a SET of integer H3 cell ids compared by exact unordered-set equality
/// (no floating point), so the PR6 cross-platform tolerance lesson does not apply. Each
/// fixture record pins one (polygon, resolution, mode) and its full covering set.
/// </summary>
public sealed class PolygonToCellsExperimentalTests
{
    private static readonly List<FixtureLoader.PolygonToCellsExperimentalCase> AllCases =
        FixtureLoader.LoadPolygonToCellsExperimental().ToList();

    public static IEnumerable<object[]> Cases() =>
        Enumerable.Range(0, AllCases.Count).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(Cases))]
    public void ToCellsExperimental_MatchesOracle_AsSet(int index)
    {
        var testCase = AllCases[index];
        var exterior = ToRing(testCase.Polygon.Exterior);
        var holes = (testCase.Polygon.Holes ?? [])
            .Select(h => (IReadOnlyList<LatLng>)ToRing(h))
            .ToList();

        var polygon = new GeoPolygon(exterior, holes.Count == 0 ? null : holes);
        var mode = (ContainmentMode)testCase.Mode;

        var expected = testCase.Cells.Select(s => H3Index.Parse(s).Value).ToHashSet();
        var actual = H3Polygon.ToCellsExperimental(polygon, testCase.Res, mode)
            .Select(c => c.Value)
            .ToHashSet();

        Assert.Equal(expected, actual);
    }

    // Oracle ring layout is [lat, lng].
    private static List<LatLng> ToRing(IEnumerable<double[]> ring) =>
        ring.Select(p => new LatLng(p[0], p[1])).ToList();
}
