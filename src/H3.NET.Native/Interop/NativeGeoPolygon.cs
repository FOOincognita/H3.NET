// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3.NET.Native.Interop;

/// <summary>Geo polygon (C <c>GeoPolygon</c>): exterior loop, hole count, and a pointer to a hole-loop array (32 bytes on 64-bit).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeGeoPolygon
{
    public NativeGeoLoop GeoLoop;
    public int NumHoles;

    // 4 bytes of implicit padding here so the pointer is 8-byte aligned.
    public NativeGeoLoop* Holes;
}
