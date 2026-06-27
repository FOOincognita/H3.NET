// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3NET.Native.Tests.Fixtures;
using Xunit;

namespace H3NET.Native.Tests.Differential;

/// <summary>
/// Differential test: H3Index.GridDisk membership must match the h3-py 4.5.0 oracle
/// as an unordered set (H3 does not guarantee ordering; the binding strips H3_NULL).
/// </summary>
public sealed class GridDiskTests
{
    private static readonly List<FixtureLoader.GridDiskCase> AllCases =
        FixtureLoader.LoadGridDisk().ToList();

    public static IEnumerable<object[]> Cases() =>
        Enumerable.Range(0, AllCases.Count).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(Cases))]
    public void GridDisk_MatchesOracle_AsSet(int index)
    {
        var testCase = AllCases[index];
        var origin = H3Index.Parse(testCase.Cell);

        var expected = testCase.Cells.Select(s => H3Index.Parse(s).Value).ToHashSet();
        var actual = origin.GridDisk(testCase.K).Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GridDiskInto_MatchesGridDisk(int index)
    {
        var testCase = AllCases[index];
        var origin = H3Index.Parse(testCase.Cell);
        int k = testCase.K;
        var expected = testCase.Cells.Select(s => H3Index.Parse(s).Value).ToHashSet();

        // Span destination must hold at least the max grid-disk size: 3k(k+1)+1.
        int maxSize = (3 * k * (k + 1)) + 1;
        var destination = new H3Index[maxSize];
        int count = origin.GridDiskInto(k, destination);

        var actual = destination.Take(count).Select(c => c.Value).ToHashSet();
        Assert.Equal(expected, actual);
    }
}
