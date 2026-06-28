// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace H3.NET.Native.Interop;

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

    // Parallel to maxGridDiskSize: gives 6*k cells for the hollow ring (1 at k=0).
    [LibraryImport("h3", EntryPoint = "maxGridRingSize")]
    internal static partial H3ErrorCode MaxGridRingSize(int k, out long size);

    // Pentagon-SAFE wrapper: self-dispatches to gridRingUnsafe and falls back on
    // distortion. out length must be >= maxGridRingSize(k); on pentagon holes some
    // slots stay H3_NULL, so callers strip defensively. Order is not guaranteed.
    [LibraryImport("h3", EntryPoint = "gridRing")]
    internal static partial H3ErrorCode GridRing(ulong origin, int k, ulong* outCells);

    // Size-half of the gridPathCells M4 pair. EXACT length (gridDistance + 1);
    // propagates E_FAILED for far-apart / mismatched-resolution endpoints.
    [LibraryImport("h3", EntryPoint = "gridPathCellsSize")]
    internal static partial H3ErrorCode GridPathCellsSize(ulong start, ulong end, out long size);

    // out length must be == gridPathCellsSize(start, end). Endpoint-inclusive:
    // out[0] == start, out[^1] == end. No H3_NULL padding (exact size).
    [LibraryImport("h3", EntryPoint = "gridPathCells")]
    internal static partial H3ErrorCode GridPathCells(ulong start, ulong end, ulong* outCells);

    [LibraryImport("h3", EntryPoint = "gridDistance")]
    internal static partial H3ErrorCode GridDistance(ulong origin, ulong h3, out long distance);

    // SAFE-dispatching wrapper. distances is int* (NOT int64). Both buffers sized to
    // maxGridDiskSize(k): the cells buffer is the H3_NULL sentinel channel; distances
    // carries no sentinel (origin's distance is legitimately 0).
    [LibraryImport("h3", EntryPoint = "gridDiskDistances")]
    internal static partial H3ErrorCode GridDiskDistances(ulong origin, int k, ulong* outCells, int* distances);

    // mode is reserved (only 0 is defined); the public layer hides it and always passes 0.
    [LibraryImport("h3", EntryPoint = "cellToLocalIj")]
    internal static partial H3ErrorCode CellToLocalIj(ulong origin, ulong h3, uint mode, out NativeCoordIJ outIj);

    // const CoordIJ* ij -> `in` blittable struct marshals as a pointer. mode reserved (pass 0).
    [LibraryImport("h3", EntryPoint = "localIjToCell")]
    internal static partial H3ErrorCode LocalIjToCell(ulong origin, in NativeCoordIJ ij, uint mode, out ulong outCell);

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

    // isValidIndex is the broader cell-OR-edge-OR-vertex validity predicate; like
    // isValidCell it never reports an error and never throws, so callers must NOT
    // validate-first against it.
    [LibraryImport("h3", EntryPoint = "isValidIndex")]
    internal static partial int IsValidIndex(ulong cell);

    // ---- Inspection / string conversion (H3Error channel) ------------------

    // getIndexDigit only bit-extracts the stored digit; it validates 1 <= res <= 15
    // (E_RES_DOMAIN) but never checks cell validity, so do not validate-first.
    [LibraryImport("h3", EntryPoint = "getIndexDigit")]
    internal static partial H3ErrorCode GetIndexDigit(ulong cell, int res, out int digit);

    // constructCell reads digits[0..res-1]; the caller MUST pin a span of exactly
    // res ints. Argument-domain errors: E_RES_DOMAIN, E_BASE_CELL_DOMAIN,
    // E_DIGIT_DOMAIN, E_DELETED_DIGIT.
    [LibraryImport("h3", EntryPoint = "constructCell")]
    internal static partial H3ErrorCode ConstructCell(int res, int baseCellNumber, int* digits, out ulong outCell);

    // Size-half of the getIcosahedronFaces M4 pair: 2 for hexagons, 5 for pentagons.
    [LibraryImport("h3", EntryPoint = "maxFaceCount")]
    internal static partial H3ErrorCode MaxFaceCount(ulong cell, out int count);

    // out length must be >= maxFaceCount(cell); unused slots are INVALID_FACE (-1),
    // NOT H3_NULL, because 0 is a valid icosahedron face.
    [LibraryImport("h3", EntryPoint = "getIcosahedronFaces")]
    internal static partial H3ErrorCode GetIcosahedronFaces(ulong cell, int* outFaces);

    // sscanf("%016" PRIx64): any valid 16-hex string yields the identical ulong as the
    // managed Parse fast path; an unparseable string returns E_FAILED.
    [LibraryImport("h3", EntryPoint = "stringToH3", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial H3ErrorCode StringToH3(string str, out ulong outCell);

    // sprintf("%" PRIx64): emits VARIABLE-length lowercase hex (no zero padding) into
    // the caller's buffer; requires sz >= 17 or E_MEMORY_BOUNDS.
    [LibraryImport("h3", EntryPoint = "h3ToString")]
    internal static partial H3ErrorCode H3ToString(ulong cell, byte* str, nuint sz);

    // ---- Hierarchy (parent/children/compact) -------------------------------

    [LibraryImport("h3", EntryPoint = "cellToParent")]
    internal static partial H3ErrorCode CellToParent(ulong cell, int parentRes, out ulong parent);

    [LibraryImport("h3", EntryPoint = "cellToCenterChild")]
    internal static partial H3ErrorCode CellToCenterChild(ulong cell, int childRes, out ulong child);

    [LibraryImport("h3", EntryPoint = "cellToChildPos")]
    internal static partial H3ErrorCode CellToChildPos(ulong child, int parentRes, out long pos);

    [LibraryImport("h3", EntryPoint = "childPosToCell")]
    internal static partial H3ErrorCode ChildPosToCell(long childPos, ulong parent, int childRes, out ulong child);

    // Size-half of the cellToChildren M4 pair. Exact count; pentagon parents yield
    // fewer than 7^delta children, so the tail of the fill stays H3_NULL.
    [LibraryImport("h3", EntryPoint = "cellToChildrenSize")]
    internal static partial H3ErrorCode CellToChildrenSize(ulong cell, int childRes, out long size);

    // out length must be >= cellToChildrenSize(cell, childRes); unused slots are H3_NULL.
    [LibraryImport("h3", EntryPoint = "cellToChildren")]
    internal static partial H3ErrorCode CellToChildren(ulong cell, int childRes, ulong* outChildren);

    // compactedSet must be sized to numHexes; it is filled front-to-back and the
    // remaining trailing slots stay H3_NULL.
    [LibraryImport("h3", EntryPoint = "compactCells")]
    internal static partial H3ErrorCode CompactCells(ulong* h3Set, ulong* compactedSet, long numHexes);

    // Size-half of the uncompactCells M4 pair. res must be >= the finest resolution
    // present in the set.
    [LibraryImport("h3", EntryPoint = "uncompactCellsSize")]
    internal static partial H3ErrorCode UncompactCellsSize(ulong* compactedSet, long numCompacted, int res, out long size);

    // outSet length must be >= uncompactCellsSize(set, numCompacted, res); unused
    // slots are H3_NULL.
    [LibraryImport("h3", EntryPoint = "uncompactCells")]
    internal static partial H3ErrorCode UncompactCells(ulong* compactedSet, long numCompacted, ulong* outSet, long numOut, int res);

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
