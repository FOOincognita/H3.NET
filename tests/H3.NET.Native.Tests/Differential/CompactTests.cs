// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential tests for CompactCells / UncompactCells (and their *Into overloads)
/// against the h3-py 4.5.0 oracle. CompactCells must match the compacted set;
/// UncompactCells must reproduce the original input set. All comparisons are unordered
/// (H3 does not guarantee ordering). Cases are addressed by index for serializability.
/// </summary>
public sealed class CompactTests
{
    private static readonly List<FixtureLoader.CompactCase> AllCases =
        FixtureLoader.LoadCompact().ToList();

    public static IEnumerable<object[]> Cases() =>
        Enumerable.Range(0, AllCases.Count).Select(i => new object[] { i });

    private static H3Index[] ParseAll(IReadOnlyList<string> hex) =>
        hex.Select(H3Index.Parse).ToArray();

    [Theory]
    [MemberData(nameof(Cases))]
    public void CompactCells_MatchesOracle_AsSet(int index)
    {
        var testCase = AllCases[index];
        var input = ParseAll(testCase.Input);

        var expected = testCase.Compacted.Select(s => H3Index.Parse(s).Value).ToHashSet();
        var actual = H3Index.CompactCells(input).Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CompactCellsInto_MatchesArrayForm(int index)
    {
        var testCase = AllCases[index];
        var input = ParseAll(testCase.Input);

        var expected = H3Index.CompactCells(input).Select(c => c.Value).ToHashSet();

        var destination = new H3Index[input.Length];
        int count = H3Index.CompactCellsInto(input, destination);
        var actual = destination.Take(count).Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void UncompactCells_MatchesOriginalInput_AsSet(int index)
    {
        var testCase = AllCases[index];
        var compacted = ParseAll(testCase.Compacted);

        var expected = testCase.Input.Select(s => H3Index.Parse(s).Value).ToHashSet();
        var actual = H3Index.UncompactCells(compacted, testCase.Res)
            .Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void UncompactCellsInto_MatchesArrayForm(int index)
    {
        var testCase = AllCases[index];
        var compacted = ParseAll(testCase.Compacted);

        var expected = H3Index.UncompactCells(compacted, testCase.Res)
            .Select(c => c.Value).ToHashSet();

        long maxOut = H3Index.UncompactCellsSize(compacted, testCase.Res);
        var destination = new H3Index[maxOut];
        int count = H3Index.UncompactCellsInto(compacted, testCase.Res, destination);
        var actual = destination.Take(count).Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }
}
