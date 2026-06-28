// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Edge;

/// <summary>
/// End-to-end hierarchy round-trips: parent &lt;-&gt; children consistency, the
/// childPos &lt;-&gt; cell inverse, and compact &lt;-&gt; uncompact identity. Driven from the
/// San Francisco sample point across all 16 resolutions and from the res-0 pentagons
/// (which exercise the fewer-than-7 children / H3_NULL strip).
/// </summary>
public sealed class HierarchyRoundTripTests
{
    private static readonly LatLng SamplePoint = new(37.775938728915946, -122.41795063018799);

    public static IEnumerable<object[]> AllResolutions() =>
        Enumerable.Range(0, 16).Select(r => new object[] { r });

    public static IEnumerable<object[]> Res0Pentagons() =>
        FixtureLoader.LoadPentagons().Where(p => p.Res == 0).Select(p => new object[] { p.Cell });

    // ---- parent <-> children -----------------------------------------------

    [Theory]
    [MemberData(nameof(AllResolutions))]
    public void SamplePoint_ParentOfEveryChild_IsOriginal(int resolution)
    {
        var cell = H3Index.FromLatLng(SamplePoint, resolution);
        AssertParentChildrenConsistency(cell);
    }

    [Theory]
    [MemberData(nameof(Res0Pentagons))]
    public void Res0Pentagon_ParentOfEveryChild_IsOriginal(string hex)
    {
        AssertParentChildrenConsistency(H3Index.Parse(hex));
    }

    private static void AssertParentChildrenConsistency(H3Index cell)
    {
        int res = cell.Resolution;
        if (res >= 15)
        {
            return; // no finer children resolution.
        }

        int childRes = res + 1;
        var children = cell.CellToChildren(childRes);

        // Pentagons yield fewer than the hexagon maximum (the H3_NULL strip is exercised).
        Assert.True(children.Length is 6 or 7, $"Expected 6 or 7 children, got {children.Length}.");

        // Every child's parent at the coarser resolution is the original cell.
        foreach (var child in children)
        {
            Assert.Equal(cell.Value, child.CellToParent(res).Value);
        }

        // The children set contains the center child.
        var centerChild = cell.CellToCenterChild(childRes);
        Assert.Contains(children, c => c.Value == centerChild.Value);
    }

    // ---- childPos <-> cell inverse -----------------------------------------

    [Theory]
    [MemberData(nameof(AllResolutions))]
    public void SamplePoint_ChildPos_RoundTrips(int resolution)
    {
        var parent = H3Index.FromLatLng(SamplePoint, resolution);
        AssertChildPosRoundTrip(parent);
    }

    [Theory]
    [MemberData(nameof(Res0Pentagons))]
    public void Res0Pentagon_ChildPos_RoundTrips(string hex)
    {
        AssertChildPosRoundTrip(H3Index.Parse(hex));
    }

    private static void AssertChildPosRoundTrip(H3Index parent)
    {
        int parentRes = parent.Resolution;
        if (parentRes >= 15)
        {
            return;
        }

        int childRes = parentRes + 1;
        foreach (var child in parent.CellToChildren(childRes))
        {
            long pos = child.CellToChildPos(parentRes);
            var reconstructed = parent.ChildPosToCell(pos, childRes);
            Assert.Equal(child.Value, reconstructed.Value);
        }
    }

    // ---- compact <-> uncompact ---------------------------------------------

    [Theory]
    [MemberData(nameof(AllResolutions))]
    public void SamplePoint_CompactUncompact_IsSetStable(int resolution)
    {
        var cell = H3Index.FromLatLng(SamplePoint, resolution);
        AssertCompactRoundTrip(cell);
    }

    [Theory]
    [MemberData(nameof(Res0Pentagons))]
    public void Res0Pentagon_CompactUncompact_IsSetStable(string hex)
    {
        AssertCompactRoundTrip(H3Index.Parse(hex));
    }

    private static void AssertCompactRoundTrip(H3Index cell)
    {
        int res = cell.Resolution;
        if (res >= 15)
        {
            return;
        }

        int childRes = res + 1;
        var fullChildren = cell.CellToChildren(childRes);

        // The complete children set of one parent compacts to exactly that parent.
        var compacted = H3Index.CompactCells(fullChildren);
        Assert.Single(compacted);
        Assert.Equal(cell.Value, compacted[0].Value);

        // Uncompacting back to the child resolution restores the original set.
        var uncompacted = H3Index.UncompactCells(compacted, childRes);
        Assert.Equal(
            fullChildren.Select(c => c.Value).ToHashSet(),
            uncompacted.Select(c => c.Value).ToHashSet());
    }
}
