// SPDX-License-Identifier: Apache-2.0

namespace H3NET.Native.Interop;

/// <summary>ABI layout constants shared by the interop structs.</summary>
internal static class NativeLayout
{
    // MAX_CELL_BNDRY_VERTS in the C header: worst case is a pentagon, 5 original
    // verts + 5 edge crossings.
    public const int MaxCellBoundaryVerts = 10;
}
