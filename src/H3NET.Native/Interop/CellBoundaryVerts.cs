// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace H3NET.Native.Interop;

/// <summary>
/// Fixed inline buffer of <see cref="NativeLayout.MaxCellBoundaryVerts"/> contiguous
/// <see cref="NativeLatLng"/> elements. InlineArray lays the elements out back-to-back
/// with the element's own alignment, reproducing the C array <c>LatLng verts[10]</c>.
/// The synthesized indexer / span support gives access to individual vertices.
/// </summary>
[InlineArray(NativeLayout.MaxCellBoundaryVerts)]
internal struct CellBoundaryVerts
{
    // Single element field; InlineArray multiplies it by the length attribute.
    private NativeLatLng _element0;
}
