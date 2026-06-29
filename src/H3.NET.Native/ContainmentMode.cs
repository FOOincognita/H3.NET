// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;

namespace H3.NET.Native;

/// <summary>
/// Containment predicate that selects which H3 cells of a given resolution are
/// returned when filling a <see cref="GeoPolygon"/> via
/// <see cref="H3Polygon.ToCellsExperimental(GeoPolygon, int, ContainmentMode)"/>.
/// </summary>
/// <remarks>
/// This API is experimental and may change in any future minor H3 version. The
/// backing type is <see langword="uint"/> to match the C <c>uint32_t</c> flags
/// argument; it maps 1:1 to the upstream C <c>ContainmentMode</c> constants
/// (<c>CONTAINMENT_CENTER</c>, <c>CONTAINMENT_FULL</c>,
/// <c>CONTAINMENT_OVERLAPPING</c>, <c>CONTAINMENT_OVERLAPPING_BBOX</c>), minus the
/// <c>CONTAINMENT_INVALID</c> sentinel. Passing a value outside the defined range
/// is rejected by the native library with <see cref="H3DomainException"/>.
/// </remarks>
[Experimental("H3NET0001")]
public enum ContainmentMode : uint
{
    /// <summary>Cells whose center falls within the polygon.</summary>
    Center = 0,

    /// <summary>Cells that are fully contained within the polygon.</summary>
    Full = 1,

    /// <summary>Cells that overlap the polygon (their boundary intersects it or they are contained).</summary>
    Overlapping = 2,

    /// <summary>
    /// Cells that overlap the bounding box of the polygon. This is a faster, looser
    /// approximation of <see cref="Overlapping"/> that may include extra cells.
    /// </summary>
    OverlappingBBox = 3,
}
