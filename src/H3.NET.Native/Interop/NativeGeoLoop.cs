// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3.NET.Native.Interop;

/// <summary>Geo loop (C <c>GeoLoop</c>): vertex count plus a pointer to a RADIANS vertex array (16 bytes on 64-bit).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeGeoLoop
{
    public int NumVerts;

    // 4 bytes of implicit padding here so the pointer is 8-byte aligned.
    public NativeLatLng* Verts;
}
