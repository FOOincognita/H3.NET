// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential tests for the PR3 grid-traversal / localIJ surface (GridRing,
/// GridPathCells, GridDistance, CellToLocalIJ, LocalIJToCell, GridDiskDistances and
/// every *Into overload) against the h3-py 4.5.0 oracle. Ring/disk results assert
/// unordered set equality (H3 does not guarantee ordering; the binding strips H3_NULL);
/// paths assert exact ORDERED, endpoint-inclusive equality; distances and IJ assert
/// exact scalar equality. Cases are addressed by index to keep MemberData serializable.
/// </summary>
public sealed class GridTraversalTests
{
    private static readonly List<FixtureLoader.GridRingCase> RingCases =
        FixtureLoader.LoadGridRing().ToList();

    private static readonly List<FixtureLoader.GridPathCase> PathCases =
        FixtureLoader.LoadGridPath().ToList();

    private static readonly List<FixtureLoader.GridDistanceCase> DistanceCases =
        FixtureLoader.LoadGridDistance().ToList();

    private static readonly List<FixtureLoader.LocalIjCase> LocalIjCases =
        FixtureLoader.LoadLocalIj().ToList();

    private static readonly List<FixtureLoader.GridDiskDistancesCase> DiskDistancesCases =
        FixtureLoader.LoadGridDiskDistances().ToList();

    public static IEnumerable<object[]> Rings() =>
        Enumerable.Range(0, RingCases.Count).Select(i => new object[] { i });

    public static IEnumerable<object[]> Paths() =>
        Enumerable.Range(0, PathCases.Count).Select(i => new object[] { i });

    public static IEnumerable<object[]> Distances() =>
        Enumerable.Range(0, DistanceCases.Count).Select(i => new object[] { i });

    public static IEnumerable<object[]> LocalIjs() =>
        Enumerable.Range(0, LocalIjCases.Count).Select(i => new object[] { i });

    public static IEnumerable<object[]> DiskDistances() =>
        Enumerable.Range(0, DiskDistancesCases.Count).Select(i => new object[] { i });

    // ---- GridRing ----------------------------------------------------------

    [Theory]
    [MemberData(nameof(Rings))]
    public void GridRing_MatchesOracle_AsSet(int index)
    {
        var testCase = RingCases[index];
        var origin = H3Index.Parse(testCase.Cell);

        var expected = testCase.Cells.Select(s => H3Index.Parse(s).Value).ToHashSet();
        var actual = origin.GridRing(testCase.K).Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Rings))]
    public void GridRingInto_MatchesGridRing(int index)
    {
        var testCase = RingCases[index];
        var origin = H3Index.Parse(testCase.Cell);
        int k = testCase.K;
        var expected = testCase.Cells.Select(s => H3Index.Parse(s).Value).ToHashSet();

        // maxGridRingSize is 6k (k>0) or 1 (k==0); size the span to the binding's helper.
        int maxSize = k == 0 ? 1 : 6 * k;
        var destination = new H3Index[maxSize];
        int count = origin.GridRingInto(k, destination);

        var actual = destination.Take(count).Select(c => c.Value).ToHashSet();
        Assert.Equal(expected, actual);
    }

    // ---- GridPathCells -----------------------------------------------------

    [Theory]
    [MemberData(nameof(Paths))]
    public void GridPathCells_MatchesOracle_Ordered_EndpointInclusive(int index)
    {
        var testCase = PathCases[index];
        var start = H3Index.Parse(testCase.Start);
        var end = H3Index.Parse(testCase.End);

        var expected = testCase.Path.Select(s => H3Index.Parse(s).Value).ToArray();
        var actual = start.GridPathCells(end).Select(c => c.Value).ToArray();

        // Ordered, endpoint-inclusive: exact sequence equality, not a set.
        Assert.Equal(expected, actual);
        Assert.Equal(start.Value, actual[0]);
        Assert.Equal(end.Value, actual[^1]);
    }

    [Theory]
    [MemberData(nameof(Paths))]
    public void GridPathCellsInto_MatchesGridPathCells(int index)
    {
        var testCase = PathCases[index];
        var start = H3Index.Parse(testCase.Start);
        var end = H3Index.Parse(testCase.End);

        var expected = start.GridPathCells(end).Select(c => c.Value).ToArray();

        var destination = new H3Index[expected.Length];
        int count = start.GridPathCellsInto(end, destination);

        Assert.Equal(expected.Length, count);
        Assert.Equal(expected, destination.Take(count).Select(c => c.Value).ToArray());
    }

    // ---- GridDistance ------------------------------------------------------

    [Theory]
    [MemberData(nameof(Distances))]
    public void GridDistance_MatchesOracle(int index)
    {
        var testCase = DistanceCases[index];
        var origin = H3Index.Parse(testCase.Origin);
        var other = H3Index.Parse(testCase.Other);

        Assert.Equal(testCase.Distance, origin.GridDistance(other));
    }

    // ---- CellToLocalIJ / LocalIJToCell -------------------------------------

    [Theory]
    [MemberData(nameof(LocalIjs))]
    public void CellToLocalIJ_MatchesOracle(int index)
    {
        var testCase = LocalIjCases[index];
        var origin = H3Index.Parse(testCase.Origin);
        var target = H3Index.Parse(testCase.Target);

        var ij = origin.CellToLocalIJ(target);
        Assert.Equal(testCase.I, ij.I);
        Assert.Equal(testCase.J, ij.J);
    }

    [Theory]
    [MemberData(nameof(LocalIjs))]
    public void LocalIJToCell_FromOracleIJ_ReproducesTarget(int index)
    {
        var testCase = LocalIjCases[index];
        var origin = H3Index.Parse(testCase.Origin);
        var target = H3Index.Parse(testCase.Target);

        var cell = origin.LocalIJToCell(new CoordIJ(testCase.I, testCase.J));
        Assert.Equal(target.Value, cell.Value);
    }

    // ---- GridDiskDistances -------------------------------------------------

    [Theory]
    [MemberData(nameof(DiskDistances))]
    public void GridDiskDistances_MatchesOracle_AsPairSet(int index)
    {
        var testCase = DiskDistancesCases[index];
        var origin = H3Index.Parse(testCase.Cell);

        var expected = testCase.Cells
            .Zip(testCase.Distances, (c, d) => (H3Index.Parse(c).Value, d))
            .ToHashSet();

        var (cells, distances) = origin.GridDiskDistances(testCase.K);
        var actual = cells.Zip(distances, (c, d) => (c.Value, d)).ToHashSet();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(DiskDistances))]
    public void GridDiskDistancesInto_MatchesArrayOverload_Lockstep(int index)
    {
        var testCase = DiskDistancesCases[index];
        var origin = H3Index.Parse(testCase.Cell);
        int k = testCase.K;

        var (expectedCells, expectedDistances) = origin.GridDiskDistances(k);
        var expected = expectedCells
            .Zip(expectedDistances, (c, d) => (c.Value, d))
            .ToHashSet();

        // Span destinations must hold at least the max grid-disk size: 3k(k+1)+1.
        int maxSize = (3 * k * (k + 1)) + 1;
        var cells = new H3Index[maxSize];
        var distances = new int[maxSize];
        int count = origin.GridDiskDistancesInto(k, cells, distances);

        var actual = cells.Take(count)
            .Zip(distances.Take(count), (c, d) => (c.Value, d))
            .ToHashSet();

        Assert.Equal(expected, actual);
    }
}
