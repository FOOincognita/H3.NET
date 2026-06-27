// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3NET.Native.Interop;

/// <summary>Coordinate node in a linked geo structure (C <c>LinkedLatLng</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeLinkedLatLng
{
    public NativeLatLng Vertex;
    public NativeLinkedLatLng* Next;
}
