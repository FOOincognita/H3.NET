// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3.NET.Native.Interop;

/// <summary>IJ hexagon coordinates (C <c>CoordIJ</c>, 8 bytes), used to marshal the local IJ APIs.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeCoordIJ
{
    public int I;
    public int J;
}
