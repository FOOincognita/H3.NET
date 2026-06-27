// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3NET.Native.Interop;

/// <summary>
/// Owns the head <see cref="NativeLinkedGeoPolygon"/> node consumed by
/// <c>cellsToLinkedMultiPolygon</c> and guarantees its full teardown.
/// </summary>
/// <remarks>
/// Verified ownership semantics (src/h3lib/lib/linkedGeo.c, H3 v4.5.0):
/// the caller allocates and OWNS the head node; the native fill heap-allocates
/// the loops, coordinate nodes, and any subsequent polygon nodes (the
/// <c>next</c> chain). <c>destroyLinkedMultiPolygon</c> frees those children and
/// ZEROES the head (<c>*polygon = {0}</c>) rather than freeing it, and is
/// idempotent. Therefore release must (1) call destroy to free the children,
/// then (2) free the head allocation itself. Inheriting from
/// <see cref="SafeHandle"/> ensures this runs exactly once on both the success
/// and exception paths.
/// </remarks>
internal sealed unsafe class LinkedGeoPolygonHandle : SafeHandle
{
    // CA1419: the parameterless constructor must be at least as visible as the
    // type (internal) so the runtime/marshaller can construct the handle.
    internal LinkedGeoPolygonHandle()
        : base(invalidHandleValue: nint.Zero, ownsHandle: true)
    {
    }

    /// <inheritdoc />
    public override bool IsInvalid => handle == nint.Zero;

    /// <summary>
    /// Gets the caller-owned, zero-initialized head node pointer to hand to
    /// <c>cellsToLinkedMultiPolygon</c> as its <c>out</c> argument.
    /// </summary>
    public NativeLinkedGeoPolygon* Head => (NativeLinkedGeoPolygon*)handle;

    /// <summary>
    /// Allocates a zeroed head node and wraps it in a handle. The native fill
    /// call must target <see cref="Head"/>; disposing the handle tears down the
    /// entire structure regardless of whether the fill succeeded.
    /// </summary>
    /// <returns>A handle owning a freshly allocated, zeroed head node.</returns>
    public static LinkedGeoPolygonHandle Allocate()
    {
        var result = new LinkedGeoPolygonHandle();
        // AllocZeroed so the head is a valid empty linked structure even if the
        // native fill is never invoked or fails before writing anything.
        void* head = NativeMemory.AllocZeroed((nuint)sizeof(NativeLinkedGeoPolygon));
        result.SetHandle((nint)head);
        return result;
    }

    /// <inheritdoc />
    protected override bool ReleaseHandle()
    {
        var head = (NativeLinkedGeoPolygon*)handle;

        // Free native-allocated children and zero the head (idempotent), then
        // free the caller-owned head allocation itself.
        NativeMethods.DestroyLinkedMultiPolygon(head);
        NativeMemory.Free(head);
        return true;
    }
}
