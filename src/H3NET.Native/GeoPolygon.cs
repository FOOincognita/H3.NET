// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;

namespace H3NET.Native;

/// <summary>
/// A polygon defined by an exterior ring and zero or more interior holes, with all
/// vertices expressed in <b>degrees</b>.
/// </summary>
/// <remarks>
/// Rings are open: the first and last vertices should not be repeated; H3 implicitly
/// closes each loop. Winding order does not affect H3 region operations.
/// </remarks>
public sealed class GeoPolygon
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeoPolygon"/> class.
    /// </summary>
    /// <param name="exterior">The exterior ring vertices, in degrees.</param>
    /// <param name="holes">The interior hole rings, each a list of vertices in degrees; may be <see langword="null"/> or empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="exterior"/> is <see langword="null"/>.</exception>
    public GeoPolygon(IReadOnlyList<LatLng> exterior, IReadOnlyList<IReadOnlyList<LatLng>>? holes = null)
    {
        ArgumentNullException.ThrowIfNull(exterior);

        Exterior = exterior;
        Holes = holes ?? [];

        if (Holes.Any(hole => hole is null))
        {
            throw new ArgumentException("Hole rings must not be null.", nameof(holes));
        }
    }

    /// <summary>Gets the exterior ring vertices, in degrees.</summary>
    public IReadOnlyList<LatLng> Exterior { get; }

    /// <summary>Gets the interior hole rings, each a list of vertices in degrees.</summary>
    public IReadOnlyList<IReadOnlyList<LatLng>> Holes { get; }
}
