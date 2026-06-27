// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3NET.Native.Interop;

// Hand-written P/Invoke surface, 1:1 with the H3 v4.5.0 C ABI.
//
// - [LibraryImport("h3")] resolves to libh3.dylib (macOS) / libh3.so (linux),
//   copied next to consumer output by the repo's native-copy targets.
// - H3 exports BARE C names; EntryPoint pins the exact symbol while the C#
//   method name stays PascalCase to satisfy the repo analyzer rules.
// - H3Index = ulong, H3Error = uint (modeled as H3ErrorCode).
// - Inspection helpers return a bare C int (NOT H3Error); callers must validate
//   inputs first because those entry points do not report errors.
// - No [SuppressGCTransition] anywhere (deferred per design).
internal static unsafe partial class NativeMethods
{
    // ---- Core indexing -----------------------------------------------------

    [LibraryImport("h3", EntryPoint = "latLngToCell")]
    internal static partial H3ErrorCode LatLngToCell(in NativeLatLng g, int res, out ulong outCell);

    [LibraryImport("h3", EntryPoint = "cellToLatLng")]
    internal static partial H3ErrorCode CellToLatLng(ulong cell, out NativeLatLng g);

    [LibraryImport("h3", EntryPoint = "cellToBoundary")]
    internal static partial H3ErrorCode CellToBoundary(ulong cell, out CellBoundary boundary);

    // ---- Grid traversal ----------------------------------------------------

    [LibraryImport("h3", EntryPoint = "maxGridDiskSize")]
    internal static partial H3ErrorCode MaxGridDiskSize(int k, out long size);

    // out buffer length must be >= maxGridDiskSize(k); unused slots are H3_NULL.
    [LibraryImport("h3", EntryPoint = "gridDisk")]
    internal static partial H3ErrorCode GridDisk(ulong origin, int k, ulong* outCells);

    // ---- Region (polygon) operations ---------------------------------------

    [LibraryImport("h3", EntryPoint = "maxPolygonToCellsSize")]
    internal static partial H3ErrorCode MaxPolygonToCellsSize(
        NativeGeoPolygon* geoPolygon, int res, uint flags, out long size);

    // Non-experimental polygonToCells always uses CONTAINMENT_CENTER and ignores the
    // other containment modes, so callers pass flags == 0 (the only value with well-
    // defined behavior). The validator only rejects bits outside the containment-mode
    // mask (or a mode >= CONTAINMENT_INVALID) with OptionInvalid; modes 1-3 are
    // accepted but ignored. out buffer length must be >= maxPolygonToCellsSize; unused
    // slots are H3_NULL.
    [LibraryImport("h3", EntryPoint = "polygonToCells")]
    internal static partial H3ErrorCode PolygonToCells(
        NativeGeoPolygon* geoPolygon, int res, uint flags, ulong* outCells);

    // ---- Linked geo (multi-polygon) ----------------------------------------

    // Caller allocates and owns the head 'out' node; native fills it and heap-
    // allocates the children. Pair with DestroyLinkedMultiPolygon. See
    // LinkedGeoPolygonHandle for the verified ownership/cleanup semantics.
    [LibraryImport("h3", EntryPoint = "cellsToLinkedMultiPolygon")]
    internal static partial H3ErrorCode CellsToLinkedMultiPolygon(
        ulong* h3Set, int numHexes, NativeLinkedGeoPolygon* outPolygon);

    // Frees all loops/coords and every polygon node EXCEPT the head, which it
    // zeroes (*polygon = {0}). Idempotent. Does NOT free the head allocation.
    [LibraryImport("h3", EntryPoint = "destroyLinkedMultiPolygon")]
    internal static partial void DestroyLinkedMultiPolygon(NativeLinkedGeoPolygon* polygon);

    // ---- Error description -------------------------------------------------

    // Returns a pointer to a static C string; the public layer converts it via
    // Marshal.PtrToStringUTF8. Never freed by the caller.
    [LibraryImport("h3", EntryPoint = "describeH3Error")]
    internal static partial nint DescribeH3Error(H3ErrorCode err);

    // ---- Inspection (bare int returns; validate inputs first) --------------

    [LibraryImport("h3", EntryPoint = "getResolution")]
    internal static partial int GetResolution(ulong cell);

    [LibraryImport("h3", EntryPoint = "isValidCell")]
    internal static partial int IsValidCell(ulong cell);

    [LibraryImport("h3", EntryPoint = "isPentagon")]
    internal static partial int IsPentagon(ulong cell);

    [LibraryImport("h3", EntryPoint = "isResClassIII")]
    internal static partial int IsResClassIII(ulong cell);

    [LibraryImport("h3", EntryPoint = "getBaseCellNumber")]
    internal static partial int GetBaseCellNumber(ulong cell);

    // ---- Corpus helpers ----------------------------------------------------

    [LibraryImport("h3", EntryPoint = "res0CellCount")]
    internal static partial int Res0CellCount();

    // out length must be >= res0CellCount().
    [LibraryImport("h3", EntryPoint = "getRes0Cells")]
    internal static partial H3ErrorCode GetRes0Cells(ulong* outCells);

    [LibraryImport("h3", EntryPoint = "pentagonCount")]
    internal static partial int PentagonCount();

    // out length must be >= pentagonCount().
    [LibraryImport("h3", EntryPoint = "getPentagons")]
    internal static partial H3ErrorCode GetPentagons(int res, ulong* outCells);
}
