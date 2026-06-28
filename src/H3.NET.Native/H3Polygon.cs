// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using H3.NET.Native.Interop;

namespace H3.NET.Native;

/// <summary>
/// Region operations that convert between <see cref="GeoPolygon"/> shapes and sets of
/// H3 cells. All coordinates are in <b>degrees</b>.
/// </summary>
public static class H3Polygon
{
    // The non-experimental polygonToCells always uses CONTAINMENT_CENTER and ignores
    // the other containment modes, so 0 is the only flags value with well-defined
    // behavior. The native validator only rejects bits outside the containment-mode
    // mask (or a mode >= CONTAINMENT_INVALID) with E_OPTION_INVALID; modes 1-3 are
    // accepted but silently ignored, which is why we always pass 0.
    private const uint NoFlags = 0;

    /// <summary>
    /// Returns the set of H3 cells of the given resolution whose centers fall within
    /// <paramref name="polygon"/>.
    /// </summary>
    /// <param name="polygon">The region to fill, in degrees.</param>
    /// <param name="resolution">The target H3 resolution (0-15).</param>
    /// <returns>The covering cells, with null padding slots removed. Order is not guaranteed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="polygon"/> is <see langword="null"/>.</exception>
    /// <exception cref="H3DomainException">The resolution was rejected by the native library.</exception>
    /// <exception cref="H3Exception">The native operation otherwise failed.</exception>
    public static unsafe H3Index[] ToCells(GeoPolygon polygon, int resolution)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        // All vertex arrays and the holes array must stay pinned across BOTH native
        // calls (size then fill), so every pin is collected here and freed once in
        // the single finally below.
        int holeCount = polygon.Holes.Count;
        var pins = new List<GCHandle>(2 + holeCount);
        try
        {
            var geoPolygon = new NativeGeoPolygon
            {
                GeoLoop = PinLoop(polygon.Exterior, pins),
                NumHoles = holeCount,
                Holes = PinHoles(polygon.Holes, pins),
            };

            return FillCells(&geoPolygon, resolution);
        }
        finally
        {
            foreach (GCHandle pin in pins)
            {
                pin.Free();
            }
        }
    }

    // Pins a single ring's radians vertices and returns a native loop pointing at
    // them. The pin is appended to <paramref name="pins"/> for the caller to free.
    private static unsafe NativeGeoLoop PinLoop(IReadOnlyList<LatLng> ring, List<GCHandle> pins)
    {
        NativeLatLng[] verts = ToNativeVerts(ring);
        var pin = GCHandle.Alloc(verts, GCHandleType.Pinned);
        pins.Add(pin);
        return new NativeGeoLoop
        {
            NumVerts = verts.Length,
            Verts = (NativeLatLng*)pin.AddrOfPinnedObject(),
        };
    }

    // Pins every hole ring plus the holes loop array itself, returning the array
    // pointer (or null when there are no holes). All pins are appended to the list.
    private static unsafe NativeGeoLoop* PinHoles(
        IReadOnlyList<IReadOnlyList<LatLng>> holes, List<GCHandle> pins)
    {
        int holeCount = holes.Count;
        if (holeCount == 0)
        {
            return null;
        }

        var holeLoops = new NativeGeoLoop[holeCount];
        for (int i = 0; i < holeCount; i++)
        {
            holeLoops[i] = PinLoop(holes[i], pins);
        }

        var holeLoopsPin = GCHandle.Alloc(holeLoops, GCHandleType.Pinned);
        pins.Add(holeLoopsPin);
        return (NativeGeoLoop*)holeLoopsPin.AddrOfPinnedObject();
    }

    // Runs the two-call size-then-fill protocol against a fully pinned native
    // polygon. Must be invoked while every vertex array and the holes array remain
    // pinned by the caller.
    private static unsafe H3Index[] FillCells(NativeGeoPolygon* geoPolygon, int resolution)
    {
        H3ErrorMarshaller.ThrowIfError(
            NativeMethods.MaxPolygonToCellsSize(geoPolygon, resolution, NoFlags, out long maxSize));

        if (maxSize <= 0)
        {
            return [];
        }

        var buffer = new ulong[maxSize];
        fixed (ulong* outCells = buffer)
        {
            H3ErrorMarshaller.ThrowIfError(
                NativeMethods.PolygonToCells(geoPolygon, resolution, NoFlags, outCells));
        }

        return StripNull(buffer);
    }

    /// <summary>
    /// Merges a set of H3 cells into one or more polygons describing their outline,
    /// including holes.
    /// </summary>
    /// <param name="cells">The cells to merge. The <see cref="H3Index.Null"/> sentinel is ignored.</param>
    /// <returns>The resulting polygons, with vertices in degrees. The first loop of each polygon is the exterior; any subsequent loops are holes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cells"/> is <see langword="null"/>.</exception>
    /// <exception cref="H3Exception">The native operation failed.</exception>
    public static unsafe IReadOnlyList<GeoPolygon> FromCells(IEnumerable<H3Index> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);

        var rawCells = new List<ulong>();
        foreach (H3Index cell in cells)
        {
            if (!cell.IsNull)
            {
                rawCells.Add(cell.Value);
            }
        }

        var result = new List<GeoPolygon>();
        if (rawCells.Count == 0)
        {
            return result;
        }

        ulong[] cellArray = [.. rawCells];

        using var handle = LinkedGeoPolygonHandle.Allocate();
        fixed (ulong* setPtr = cellArray)
        {
            H3ErrorMarshaller.ThrowIfError(
                NativeMethods.CellsToLinkedMultiPolygon(setPtr, cellArray.Length, handle.Head));
        }

        for (NativeLinkedGeoPolygon* polygon = handle.Head; polygon != null; polygon = polygon->Next)
        {
            result.Add(ConvertPolygon(polygon));
        }

        return result;
    }

    /// <summary>
    /// Converts a single native linked-geo-polygon node (one exterior loop followed by
    /// zero or more hole loops) into a managed <see cref="GeoPolygon"/>, in degrees.
    /// </summary>
    private static unsafe GeoPolygon ConvertPolygon(NativeLinkedGeoPolygon* polygon)
    {
        IReadOnlyList<LatLng>? exterior = null;
        var holes = new List<IReadOnlyList<LatLng>>();

        for (NativeLinkedGeoLoop* loop = polygon->First; loop != null; loop = loop->Next)
        {
            var ring = new List<LatLng>();
            for (NativeLinkedLatLng* coord = loop->First; coord != null; coord = coord->Next)
            {
                ring.Add(LatLng.FromNative(coord->Vertex));
            }

            if (exterior is null)
            {
                exterior = ring;
            }
            else
            {
                holes.Add(ring);
            }
        }

        // normalizeMultiPolygon guarantees every emitted node has at least an
        // exterior loop; a null here means that contract was broken upstream, so
        // surface it instead of fabricating a degenerate empty exterior.
        if (exterior is null)
        {
            throw new InvalidOperationException(
                "H3 returned a polygon node with no loops, violating the cellsToLinkedMultiPolygon contract.");
        }

        return new GeoPolygon(exterior, holes);
    }

    private static NativeLatLng[] ToNativeVerts(IReadOnlyList<LatLng> ring)
    {
        var verts = new NativeLatLng[ring.Count];
        for (int i = 0; i < ring.Count; i++)
        {
            LatLng vertex = ring[i];
            vertex.Validate();
            verts[i] = vertex.ToNative();
        }

        return verts;
    }

    private static H3Index[] StripNull(ulong[] buffer)
    {
        int count = 0;
        foreach (ulong value in buffer)
        {
            if (value != 0)
            {
                count++;
            }
        }

        var result = new H3Index[count];
        int next = 0;
        foreach (ulong value in buffer)
        {
            if (value != 0)
            {
                result[next++] = new H3Index(value);
            }
        }

        return result;
    }
}
