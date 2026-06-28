// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3.NET.Native.Interop;

/// <summary>IJ hexagon coordinates (C <c>CoordIJ</c>, 8 bytes). Included for ABI completeness; unused by the current slice.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeCoordIJ
{
    public int I;
    public int J;
}
