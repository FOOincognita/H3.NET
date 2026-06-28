// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential tests for the hierarchy surface (CellToParent, CellToCenterChild,
/// CellToChildPos, ChildPosToCell, CellToChildren / CellToChildrenInto) against the
/// h3-py 4.5.0 oracle. Scalar results assert exact equality; children sets assert
/// unordered HashSet equality (H3 does not guarantee ordering; the binding strips
/// H3_NULL). Cases are addressed by index to keep MemberData serializable.
/// </summary>
public sealed class HierarchyTests
{
    private static readonly List<FixtureLoader.HierarchyCase> AllCases =
        FixtureLoader.LoadHierarchy().ToList();

    private static readonly List<FixtureLoader.ChildPosCase> AllChildPosCases =
        FixtureLoader.LoadChildPos().ToList();

    public static IEnumerable<object[]> Cases() =>
        Enumerable.Range(0, AllCases.Count).Select(i => new object[] { i });

    public static IEnumerable<object[]> ChildPosCases() =>
        Enumerable.Range(0, AllChildPosCases.Count).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(Cases))]
    public void CellToParent_MatchesOracle(int index)
    {
        var testCase = AllCases[index];
        var cell = H3Index.Parse(testCase.Cell);
        var expected = H3Index.Parse(testCase.Parent);

        Assert.Equal(expected.Value, cell.CellToParent(testCase.ParentRes).Value);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CellToCenterChild_MatchesOracle(int index)
    {
        var testCase = AllCases[index];
        var cell = H3Index.Parse(testCase.Cell);
        var expected = H3Index.Parse(testCase.CenterChild);

        Assert.Equal(expected.Value, cell.CellToCenterChild(testCase.CenterChildRes).Value);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CellToChildren_MatchesOracle_AsSet(int index)
    {
        var testCase = AllCases[index];
        var cell = H3Index.Parse(testCase.Cell);

        var expected = testCase.Children.Select(s => H3Index.Parse(s).Value).ToHashSet();
        var actual = cell.CellToChildren(testCase.ChildrenRes).Select(c => c.Value).ToHashSet();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CellToChildrenInto_MatchesCellToChildren(int index)
    {
        var testCase = AllCases[index];
        var cell = H3Index.Parse(testCase.Cell);
        int childRes = testCase.ChildrenRes;

        var expected = cell.CellToChildren(childRes).Select(c => c.Value).ToHashSet();

        // Size the span to the upstream cellToChildrenSize (max, before the H3_NULL strip).
        long maxSize = cell.CellToChildrenSize(childRes);
        var destination = new H3Index[maxSize];
        int count = cell.CellToChildrenInto(childRes, destination);

        var actual = destination.Take(count).Select(c => c.Value).ToHashSet();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ChildPosCases))]
    public void CellToChildPos_MatchesOracle(int index)
    {
        var testCase = AllChildPosCases[index];
        var child = H3Index.Parse(testCase.Child);

        Assert.Equal(testCase.Pos, child.CellToChildPos(testCase.ParentRes));
    }

    [Theory]
    [MemberData(nameof(ChildPosCases))]
    public void ChildPosToCell_ReproducesOriginalChild(int index)
    {
        var testCase = AllChildPosCases[index];
        var child = H3Index.Parse(testCase.Child);
        int childRes = child.Resolution;
        var parent = child.CellToParent(testCase.ParentRes);

        // Inverse of cellToChildPos: parent.ChildPosToCell(pos, childRes) == child.
        var roundTripped = parent.ChildPosToCell(testCase.Pos, childRes);
        Assert.Equal(child.Value, roundTripped.Value);
    }
}
