// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3.NET.Native.Interop;

// Blittable, 1:1 with the H3 v4.5.0 C ABI. LayoutKind.Sequential matches the C
// compiler's default field ordering and natural 64-bit alignment used by libh3.

/// <summary>Latitude/longitude pair, in RADIANS at the ABI boundary (C <c>LatLng</c>, 16 bytes).</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeLatLng
{
    public double Lat;
    public double Lng;
}
