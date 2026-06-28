// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using H3.NET.Native.Interop;

namespace H3.NET.Native;

/// <summary>
/// Local IJ coordinates of a cell relative to an anchoring origin cell.
/// </summary>
/// <param name="I">The I component of the local coordinate.</param>
/// <param name="J">The J component of the local coordinate.</param>
/// <remarks>
/// Local IJ coordinates are only defined within a small region around an origin and
/// are <b>not</b> globally meaningful: the same <see cref="CoordIJ"/> resolves to
/// different cells under different origins. The IJ-to-cell mapping is also not
/// guaranteed invertible near pentagons or the antimeridian.
/// </remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct CoordIJ(int I, int J)
{
    /// <summary>Converts this coordinate into the interop struct used at the native boundary.</summary>
    /// <returns>The equivalent <see cref="NativeCoordIJ"/>.</returns>
    internal NativeCoordIJ ToNative() => new() { I = I, J = J };

    /// <summary>Converts an interop coordinate struct into a public <see cref="CoordIJ"/>.</summary>
    /// <param name="native">The interop coordinate.</param>
    /// <returns>The equivalent <see cref="CoordIJ"/>.</returns>
    internal static CoordIJ FromNative(in NativeCoordIJ native) => new(native.I, native.J);
}
