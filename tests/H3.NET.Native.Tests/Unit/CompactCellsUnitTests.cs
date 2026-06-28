// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Unit;

/// <summary>
/// Unit tests for CompactCells / CompactCellsInto. The headline case is the
/// dirtied-destination regression: native compactCells writes its result strictly
/// front-to-back and never H3_NULL-pads the unused tail, so the *Into overload must
/// pre-clear the scanned window. Passing a destination span pre-filled with non-zero
/// sentinel garbage and a compactible input must still return the true compacted
/// count and contents -- not the stale tail miscounted as real cells.
/// </summary>
public sealed class CompactCellsUnitTests
{
    /// <summary>
    /// The seven children of a single res-1 parent. They compact to exactly the one
    /// parent, leaving every trailing destination slot unwritten by the native call --
    /// the precise condition that exposes a non-zeroed tail.
    /// </summary>
    private static H3Index[] SevenChildrenOfOneParent()
    {
        H3Index parent = H3Index.Parse(FixtureLoader.LoadRes0Cells()[0]).CellToChildren(1)[0];
        return parent.CellToChildren(2);
    }

    [Fact]
    public void CompactCellsInto_WithDirtyDestination_ReturnsTrueCompactedCountAndContents()
    {
        H3Index[] children = SevenChildrenOfOneParent();
        H3Index[] expected = H3Index.CompactCells(children);

        // Pre-fill the destination with non-zero sentinel garbage. Before the fix, the
        // native call leaves the tail untouched and the strip loop counts these stale
        // values as real cells.
        var destination = new H3Index[children.Length];
        Array.Fill(destination, new H3Index(0xFFFFFFFFFFFFFFFFUL));

        int count = H3Index.CompactCellsInto(children, destination);

        Assert.Equal(expected.Length, count);
        Assert.True(count < children.Length, "Children of one parent must compact strictly smaller.");
        Assert.Equal(
            expected.OrderBy(c => c.Value),
            destination[..count].ToArray().OrderBy(c => c.Value));
    }

    [Fact]
    public void CompactCellsInto_ReusedBufferAcrossCalls_DoesNotLeakStaleCells()
    {
        H3Index[] children = SevenChildrenOfOneParent();
        H3Index[] expected = H3Index.CompactCells(children);

        // Reuse one buffer across calls -- the documented purpose of the *Into pattern.
        var destination = new H3Index[children.Length];

        int first = H3Index.CompactCellsInto(children, destination);
        int second = H3Index.CompactCellsInto(children, destination);

        Assert.Equal(expected.Length, first);
        Assert.Equal(first, second);
        Assert.Equal(
            destination[..first].ToArray().OrderBy(c => c.Value),
            destination[..second].ToArray().OrderBy(c => c.Value));
    }

    [Fact]
    public void CompactCellsInto_MatchesArrayOverload()
    {
        H3Index[] children = SevenChildrenOfOneParent();
        H3Index[] expected = H3Index.CompactCells(children);

        var destination = new H3Index[children.Length];
        int count = H3Index.CompactCellsInto(children, destination);

        Assert.Equal(
            expected.OrderBy(c => c.Value),
            destination[..count].ToArray().OrderBy(c => c.Value));
    }

    [Fact]
    public void CompactCells_RoundTripsThroughUncompact()
    {
        H3Index[] children = SevenChildrenOfOneParent();
        H3Index[] compacted = H3Index.CompactCells(children);
        H3Index[] uncompacted = H3Index.UncompactCells(compacted, 2);

        Assert.Equal(
            children.OrderBy(c => c.Value),
            uncompacted.OrderBy(c => c.Value));
    }

    [Fact]
    public void CompactCellsInto_TooSmallDestination_Throws()
    {
        H3Index[] children = SevenChildrenOfOneParent();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => H3Index.CompactCellsInto(children, new H3Index[children.Length - 1]));
    }

    [Fact]
    public void CompactCellsInto_EmptyInput_ReturnsZero()
    {
        Assert.Equal(0, H3Index.CompactCellsInto(ReadOnlySpan<H3Index>.Empty, Span<H3Index>.Empty));
    }
}
