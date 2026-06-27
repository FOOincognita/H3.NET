// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace H3NET.Native.Tests.Differential;

/// <summary>
/// Correctness tests for <see cref="H3Polygon.FromCells"/>, the most intricate pointer
/// walk in the binding (the <c>LinkedGeoPolygon</c> -> <c>LinkedGeoLoop</c> ->
/// <c>LinkedLatLng</c> chains behind a SafeHandle). No fixture data is needed: the
/// oracle is the documented H3 invariant that round-tripping a compact, single-resolution
/// cell set through <c>cellsToLinkedMultiPolygon</c> and back through center-containment
/// <c>polygonToCells</c> reproduces the original set exactly. Each structural case below
/// exercises a distinct branch of the walk: exterior-only loops, an interior hole, and the
/// <c>Next</c> link between disjoint polygons.
/// </summary>
public sealed class CellsToPolygonRoundTripTests
{
    private const int Resolution = 7;

    // Well-separated mid-latitude points, away from pentagon base cells and the
    // poles/antimeridian, so the round trip stays exact and geometry is unambiguous.
    private static readonly LatLng SanFrancisco = new(37.7749, -122.4194);
    private static readonly LatLng NewYork = new(40.7128, -74.0060);

    [Fact]
    public void FromCells_ExteriorOnly_RoundTripsToSameSet()
    {
        var origin = H3Index.FromLatLng(SanFrancisco, Resolution);
        Assert.False(origin.IsPentagon);

        // A k=2 disk is a single contiguous, hole-free region: one exterior loop only.
        var cells = origin.GridDisk(2);
        var expected = cells.Select(c => c.Value).ToHashSet();

        var polygons = H3Polygon.FromCells(cells);
        Assert.Single(polygons);
        Assert.Empty(polygons[0].Holes);
        AssertVerticesInDegreeRange(polygons);

        var actual = RoundTrip(polygons);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FromCells_WithHole_RoundTripsWithoutFillingHole()
    {
        var origin = H3Index.FromLatLng(SanFrancisco, Resolution);
        Assert.False(origin.IsPentagon);

        // The six neighbors of a hexagon (k=1 disk minus its center) form a ring whose
        // interior is the omitted center cell: a single polygon with exactly one hole.
        var ring = origin.GridDisk(1).Where(c => c != origin).ToArray();
        Assert.Equal(6, ring.Length);
        var expected = ring.Select(c => c.Value).ToHashSet();

        var polygons = H3Polygon.FromCells(ring);
        Assert.Single(polygons);
        Assert.Single(polygons[0].Holes);
        AssertVerticesInDegreeRange(polygons);

        // Center containment must keep the center cell out of the round trip: its center
        // falls inside the hole, not the filled region.
        var actual = RoundTrip(polygons);
        Assert.Equal(expected, actual);
        Assert.DoesNotContain(origin.Value, actual);
    }

    [Fact]
    public void FromCells_DisjointCells_ProduceSeparatePolygons()
    {
        var west = H3Index.FromLatLng(SanFrancisco, Resolution);
        var east = H3Index.FromLatLng(NewYork, Resolution);
        Assert.False(west.IsPentagon);
        Assert.False(east.IsPentagon);
        Assert.NotEqual(west, east);

        H3Index[] cells = [west, east];
        var expected = cells.Select(c => c.Value).ToHashSet();

        // Two far-apart cells force the LinkedGeoPolygon Next walk to yield two nodes.
        var polygons = H3Polygon.FromCells(cells);
        Assert.Equal(2, polygons.Count);
        AssertVerticesInDegreeRange(polygons);

        var actual = RoundTrip(polygons);
        Assert.Equal(expected, actual);
    }

    // Fills every returned polygon at the source resolution and unions the cells, so a
    // multi-polygon result round-trips as the union of its parts (order-insensitive).
    private static HashSet<ulong> RoundTrip(IReadOnlyList<GeoPolygon> polygons) =>
        polygons
            .SelectMany(p => H3Polygon.ToCells(p, Resolution))
            .Select(c => c.Value)
            .ToHashSet();

    // Pins the radians->degrees conversion and the lat/lng ordering of the walk: every
    // vertex of every loop must be finite and within geographic degree ranges.
    private static void AssertVerticesInDegreeRange(IReadOnlyList<GeoPolygon> polygons)
    {
        foreach (GeoPolygon polygon in polygons)
        {
            AssertRingInRange(polygon.Exterior);
            foreach (IReadOnlyList<LatLng> hole in polygon.Holes)
            {
                AssertRingInRange(hole);
            }
        }
    }

    private static void AssertRingInRange(IReadOnlyList<LatLng> ring)
    {
        Assert.NotEmpty(ring);
        foreach (LatLng vertex in ring)
        {
            Assert.True(double.IsFinite(vertex.LatitudeDegrees));
            Assert.True(double.IsFinite(vertex.LongitudeDegrees));
            Assert.InRange(vertex.LatitudeDegrees, -90.0, 90.0);
            Assert.InRange(vertex.LongitudeDegrees, -180.0, 180.0);
        }
    }
}
