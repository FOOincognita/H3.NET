// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential tests: the PR6 measures surface must match the h3-py 4.5.0 oracle to within
/// <see cref="RelativeTolerance"/> (a cross-platform libm tolerance; see the constant for the
/// per-platform native-build rationale and measured deltas). Mirrors CellToLatLngTests. Covers
/// cell areas (cellArea* == h3.cell_area), edge lengths (edgeLength* == h3.edge_length),
/// average hexagon area / edge length (the four GetHexagon* statics == h3.average_hexagon_area
/// / h3.average_hexagon_edge_length, res 0..15 -- these read native constant tables and are
/// bit-exact across platforms), GetNumCells == h3.get_num_cells, great-circle distances over
/// curated degree pairs (the critical degrees-input check), and the two constant-count
/// collections against the committed res0_cells.csv and pentagons.csv (set equality, 122 / 12
/// per resolution).
/// </summary>
public sealed class MeasuresTests
{
    // Cross-platform floating-point tolerance -- NOT a slack/"sloppy" value.
    //
    // The .NET binding calls the native libh3 bundled for the RUNNING platform
    // (osx-arm64 libh3.dylib, linux-x64 / linux-musl-x64 libh3.so), while the committed
    // oracle fixtures were generated once via h3-py 4.5.0 on macOS. These are DIFFERENT,
    // separately compiled native builds, so their math libraries (libm: sin/cos/atan2/...)
    // differ in the last ULPs. cellArea (spherical excess) and edgeLength (haversine over
    // boundary points) chain many trig ops, amplifying that ULP difference into a small
    // relative spread between binding and oracle.
    //
    // Because the oracle is macOS-generated, osx-arm64 matches it within 1e-9 (CI green on
    // osx-arm64 + local macOS), but Linux exceeds 1e-9: measured MAX relative delta is
    // ~3.4e-9 on cellArea and ~1.4e-8 on edgeLength on linux-x64 (great-circle distance,
    // by contrast, drifts only ~1e-16, and the GetHexagon*Avg lookup-table constants are
    // bit-exact across platforms). 1e-6 sits ~70x above the worst observed Linux drift yet
    // stays far below any gross error -- a wrong unit would be off by 1e3/1e6, a wrong
    // formula by orders of magnitude -- so this still fails loudly on a real binding bug.
    private const double RelativeTolerance = 1e-6;

    private static void AssertRelative(double expected, double actual)
    {
        // Relative comparison handles the wide magnitude range (rads^2 ~ 1e-15 up to m^2
        // ~ 1e12). Falls back to an absolute floor for values near zero. RelativeTolerance
        // is a cross-platform libm tolerance, not slack -- see the constant above.
        double tolerance = System.Math.Max(System.Math.Abs(expected) * RelativeTolerance, 1e-12);
        Assert.Equal(expected, actual, tolerance);
    }

    // ---- cell area ---------------------------------------------------------

    public static IEnumerable<object[]> CellAreaCases() =>
        FixtureLoader.LoadCellArea().Select(c => new object[] { c.Cell, c.Rads2, c.Km2, c.M2 });

    [Theory]
    [MemberData(nameof(CellAreaCases))]
    public void CellArea_MatchesOracle(string hex, double rads2, double km2, double m2)
    {
        var cell = H3Index.Parse(hex);
        AssertRelative(rads2, cell.CellAreaRads2());
        AssertRelative(km2, cell.CellAreaKm2());
        AssertRelative(m2, cell.CellAreaM2());
    }

    // ---- edge length -------------------------------------------------------

    public static IEnumerable<object[]> EdgeLengthCases() =>
        FixtureLoader.LoadEdgeLength().Select(c => new object[] { c.Edge, c.Rads, c.Km, c.M });

    [Theory]
    [MemberData(nameof(EdgeLengthCases))]
    public void EdgeLength_MatchesOracle(string hex, double rads, double km, double m)
    {
        var edge = new H3DirectedEdge(H3Index.Parse(hex).Value);
        AssertRelative(rads, edge.EdgeLengthRads());
        AssertRelative(km, edge.EdgeLengthKm());
        AssertRelative(m, edge.EdgeLengthM());
    }

    // ---- average hexagon area / edge length --------------------------------

    public static IEnumerable<object[]> HexagonAreaAvgCases() =>
        FixtureLoader.LoadHexagonAreaAvg().Select(c => new object[] { c.Res, c.Km2, c.M2 });

    [Theory]
    [MemberData(nameof(HexagonAreaAvgCases))]
    public void GetHexagonAreaAvg_MatchesOracle(int res, double km2, double m2)
    {
        AssertRelative(km2, H3Index.GetHexagonAreaAvgKm2(res));
        AssertRelative(m2, H3Index.GetHexagonAreaAvgM2(res));
    }

    public static IEnumerable<object[]> HexagonEdgeLengthAvgCases() =>
        FixtureLoader.LoadHexagonEdgeLengthAvg().Select(c => new object[] { c.Res, c.Km, c.M });

    [Theory]
    [MemberData(nameof(HexagonEdgeLengthAvgCases))]
    public void GetHexagonEdgeLengthAvg_MatchesOracle(int res, double km, double m)
    {
        AssertRelative(km, H3Index.GetHexagonEdgeLengthAvgKm(res));
        AssertRelative(m, H3Index.GetHexagonEdgeLengthAvgM(res));
    }

    // ---- num cells ---------------------------------------------------------

    public static IEnumerable<object[]> NumCellsCases() =>
        FixtureLoader.LoadNumCells().Select(c => new object[] { c.Res, c.Count });

    [Theory]
    [MemberData(nameof(NumCellsCases))]
    public void GetNumCells_MatchesOracle(int res, long count)
    {
        Assert.Equal(count, H3Index.GetNumCells(res));
    }

    // ---- great-circle distance (the degrees-input check) -------------------

    public static IEnumerable<object[]> GreatCircleDistanceCases() =>
        FixtureLoader.LoadGreatCircleDistance()
            .Select(c => new object[] { c.ALat, c.ALng, c.BLat, c.BLng, c.Rads, c.Km, c.M });

    [Theory]
    [MemberData(nameof(GreatCircleDistanceCases))]
    public void GreatCircleDistance_MatchesOracle(
        double aLat, double aLng, double bLat, double bLng, double rads, double km, double m)
    {
        var a = new LatLng(aLat, aLng);
        var b = new LatLng(bLat, bLng);

        AssertRelative(rads, LatLng.GreatCircleDistanceRads(a, b));
        AssertRelative(km, LatLng.GreatCircleDistanceKm(a, b));
        AssertRelative(m, LatLng.GreatCircleDistanceM(a, b));
    }

    // ---- res-0 cells / pentagons (committed CSV oracle) --------------------

    [Fact]
    public void GetRes0Cells_MatchesOracleSet()
    {
        var expected = FixtureLoader.LoadRes0Cells()
            .Select(h => H3Index.Parse(h).Value)
            .ToHashSet();

        var actual = H3Index.GetRes0Cells().Select(c => c.Value).ToHashSet();

        Assert.Equal(122, expected.Count);
        Assert.Equal(expected, actual);
    }

    public static IEnumerable<object[]> ResolutionCases() =>
        Enumerable.Range(0, 16).Select(r => new object[] { r });

    [Theory]
    [MemberData(nameof(ResolutionCases))]
    public void GetPentagons_MatchesOracleSet(int resolution)
    {
        var expected = FixtureLoader.LoadPentagons()
            .Where(p => p.Res == resolution)
            .Select(p => H3Index.Parse(p.Cell).Value)
            .ToHashSet();

        var actual = H3Index.GetPentagons(resolution).Select(c => c.Value).ToHashSet();

        Assert.Equal(12, expected.Count);
        Assert.Equal(expected, actual);
    }
}
