// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3NET.Native.Interop;

/// <summary>Loop node in a linked geo structure (C <c>LinkedGeoLoop</c>).</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NativeLinkedGeoLoop
{
    public NativeLinkedLatLng* First;
    public NativeLinkedLatLng* Last;
    public NativeLinkedGeoLoop* Next;
}
