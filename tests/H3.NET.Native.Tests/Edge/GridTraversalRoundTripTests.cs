// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace H3.NET.Native.Tests.Edge;

/// <summary>
/// End-to-end grid-traversal round-trips driven from the San Francisco sample point
/// across resolutions: the CellToLocalIJ &lt;-&gt; LocalIJToCell inverse, GridPathCells
/// endpoint-inclusive ordering with a length that matches GridDistance, and the
/// GridRing &lt;-&gt; GridDisk shell relationship.
///
/// CellToLocalIJ / GridPathCells / GridDistance are PARTIAL: at the coarsest
/// resolutions even San Francisco's small neighborhood spans pentagon distortion, where
/// the native library legitimately fails with E_FAILED. The round-trip assertions
/// therefore guard the forward call and only assert the inverse where the forward map is
/// defined -- mirroring the binding's "faithful raw-native-channel" contract. GridRing /
/// GridDisk are total (they never fail on distortion in 4.5.0), so the shell relationship
/// is asserted unconditionally at every resolution.
/// </summary>
public sealed class GridTraversalRoundTripTests
{
    private static readonly LatLng SamplePoint = new(37.775938728915946, -122.41795063018799);

    public static IEnumerable<object[]> AllResolutions() =>
        Enumerable.Range(0, 16).Select(r => new object[] { r });

    [Theory]
    [MemberData(nameof(AllResolutions))]
    public void SamplePoint_LocalIJ_RoundTripsOverDisk(int resolution)
    {
        var origin = H3Index.FromLatLng(SamplePoint, resolution);

        foreach (var target in origin.GridDisk(2))
        {
            CoordIJ ij;
            try
            {
                ij = origin.CellToLocalIJ(target);
            }
            catch (H3Exception)
            {
                continue; // pentagon distortion at this origin: local IJ undefined.
            }

            var back = origin.LocalIJToCell(ij);
            Assert.Equal(target.Value, back.Value);
        }
    }

    [Theory]
    [MemberData(nameof(AllResolutions))]
    public void SamplePoint_GridPath_IsEndpointInclusive_AndLengthIsDistancePlusOne(int resolution)
    {
        var origin = H3Index.FromLatLng(SamplePoint, resolution);

        foreach (var end in origin.GridRing(3))
        {
            H3Index[] path;
            try
            {
                path = origin.GridPathCells(end);
            }
            catch (H3Exception)
            {
                continue; // pentagon / antimeridian: no clean path between these cells.
            }

            Assert.Equal(origin.Value, path[0].Value);
            Assert.Equal(end.Value, path[^1].Value);
            Assert.Equal(origin.GridDistance(end) + 1, path.Length);
        }
    }

    [Theory]
    [MemberData(nameof(AllResolutions))]
    public void SamplePoint_GridRing_IsExactlyTheDiskShell(int resolution)
    {
        var origin = H3Index.FromLatLng(SamplePoint, resolution);

        for (int k = 1; k <= 3; k++)
        {
            var disk = origin.GridDisk(k).Select(c => c.Value).ToHashSet();
            var inner = origin.GridDisk(k - 1).Select(c => c.Value).ToHashSet();
            var expectedShell = disk.Except(inner).ToHashSet();

            var ring = origin.GridRing(k).Select(c => c.Value).ToHashSet();
            Assert.Equal(expectedShell, ring);
        }
    }
}
