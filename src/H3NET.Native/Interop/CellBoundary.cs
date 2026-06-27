// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3NET.Native.Interop;

/// <summary>
/// Cell boundary (C <c>CellBoundary</c>). Total size MUST be 168 bytes:
/// 4 (NumVerts) + 4 (implicit padding to 8-byte-align the LatLng array)
/// + 10 * 16 (Verts) = 168. The 4-byte padding after NumVerts is inserted
/// automatically because <see cref="NativeLatLng"/> has 8-byte alignment.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CellBoundary
{
    public int NumVerts;
    public CellBoundaryVerts Verts;
}
