// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3.NET.Native.Interop;

/// <summary>Polygon node in a linked geo structure (C <c>LinkedGeoPolygon</c>). The head is caller-owned; see <see cref="LinkedGeoPolygonHandle"/>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeLinkedGeoPolygon
{
    public NativeLinkedGeoLoop* First;
    public NativeLinkedGeoLoop* Last;
    public NativeLinkedGeoPolygon* Next;
}
