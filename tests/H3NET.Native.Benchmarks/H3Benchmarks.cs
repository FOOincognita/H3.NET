// SPDX-License-Identifier: Apache-2.0
using BenchmarkDotNet.Attributes;

namespace H3NET.Native.Benchmarks;

/// <summary>
/// Compares the native P/Invoke binding (H3NET.Native) against the fully-managed
/// pocketken.H3 4.0.0 baseline across three representative workloads:
/// point indexing, gridDisk over a mid-resolution cell, and polygonToCells over
/// a small polygon. The pocketken.H3 methods are intentionally NOT marked as the
/// BenchmarkDotNet baseline: until the real pocketken.H3 calls land (deferred to
/// PR4), they delegate to <see cref="PocketkenPlaceholder"/>, so computing a ratio
/// against them would present fabricated A/B numbers.
///
/// IMPORTANT (supervisor): the pocketken.H3 4.0.0 public API names below could not
/// be verified offline at authoring time. The H3NET.Native side is fully real.
/// The pocketken side uses the ASSUMED API documented in each method and is
/// guarded so the project compiles regardless; see this file's TODOs and the
/// returned notes for the exact calls to verify/uncomment at build time.
/// </summary>
[MemoryDiagnoser]
public class H3Benchmarks
{
    // San Francisco-ish point used for indexing.
    private const double Lat = 37.775938728915946;
    private const double Lng = -122.41795063018799;

    // Mid resolution for indexing and gridDisk.
    private const int Resolution = 9;

    // gridDisk ring size.
    private const int K = 5;

    // A mid-res cell to expand with gridDisk (computed once in setup).
    private H3Index _nativeCell;

    // A small triangular polygon around the test point (degrees).
    private static readonly LatLng[] PolygonExterior =
    [
        new LatLng(37.813318999983238, -122.4089866999972145),
        new LatLng(37.7866302000007224, -122.3805436999997056),
        new LatLng(37.7198061999978478, -122.3544736999993603),
    ];

    private GeoPolygon _nativePolygon = null!;

    [GlobalSetup]
    public void Setup()
    {
        _nativeCell = H3Index.FromLatLng(new LatLng(Lat, Lng), Resolution);
        _nativePolygon = new GeoPolygon(PolygonExterior);
    }

    // ----------------------------------------------------------------------
    // Point indexing: latLng -> cell
    // ----------------------------------------------------------------------

    [BenchmarkCategory("LatLngToCell")]
    // Baseline intentionally dropped until the real pocketken.H3 call lands (PR4);
    // marking a placeholder as Baseline would emit a ratio against fabricated data.
    [Benchmark(Description = "pocketken.H3 LatLngToCell")]
    public ulong PocketkenLatLngToCell()
    {
        // ASSUMED pocketken.H3 4.0.0 API:
        //   pocketken.H3.Algorithms.Hierarchy / H3.Api -> NetTopologySuite Coordinate based.
        //   var index = pocketken.H3.Api.LatLngToCell(
        //       new NetTopologySuite.Geometries.Coordinate(Lng, Lat), Resolution);
        //   return index.Value; // H3Index has implicit ulong / .Value
        // TODO(supervisor): verify the static entry type/method name and the
        // returned index's ulong accessor, then replace the placeholder below.
        return PocketkenPlaceholder.LatLngToCell(Lat, Lng, Resolution);
    }

    [BenchmarkCategory("LatLngToCell")]
    [Benchmark(Description = "H3NET.Native FromLatLng")]
    public ulong NativeLatLngToCell() =>
        H3Index.FromLatLng(new LatLng(Lat, Lng), Resolution).Value;

    // ----------------------------------------------------------------------
    // gridDisk over a mid-res cell
    // ----------------------------------------------------------------------

    [BenchmarkCategory("GridDisk")]
    // Baseline intentionally dropped until the real pocketken.H3 call lands (PR4);
    // marking a placeholder as Baseline would emit a ratio against fabricated data.
    [Benchmark(Description = "pocketken.H3 GridDisk")]
    public int PocketkenGridDisk()
    {
        // ASSUMED pocketken.H3 4.0.0 API:
        //   var origin = new pocketken.H3.H3Index(_nativeCell.Value);
        //   IEnumerable<pocketken.H3.H3Index> disk = origin.GridDisk(K);
        //   return disk.Count();
        // TODO(supervisor): verify H3Index ctor from ulong and the GridDisk
        // extension/method name, then replace the placeholder.
        return PocketkenPlaceholder.GridDisk(_nativeCell.Value, K);
    }

    [BenchmarkCategory("GridDisk")]
    [Benchmark(Description = "H3NET.Native GridDisk")]
    public int NativeGridDisk() => _nativeCell.GridDisk(K).Length;

    // ----------------------------------------------------------------------
    // polygonToCells over a small polygon
    // ----------------------------------------------------------------------

    [BenchmarkCategory("PolygonToCells")]
    // Baseline intentionally dropped until the real pocketken.H3 call lands (PR4);
    // marking a placeholder as Baseline would emit a ratio against fabricated data.
    [Benchmark(Description = "pocketken.H3 PolygonToCells")]
    public int PocketkenPolygonToCells()
    {
        // ASSUMED pocketken.H3 4.0.0 API (NetTopologySuite-based):
        //   var poly = new NetTopologySuite.Geometries.Polygon(... ring from PolygonExterior ...);
        //   IEnumerable<pocketken.H3.H3Index> cells = poly.Fill(Resolution);
        //   return cells.Count();
        // TODO(supervisor): verify the Polygon.Fill(resolution) extension (or
        // pocketken.H3.Api.PolygonToCells) and replace the placeholder.
        return PocketkenPlaceholder.PolygonToCells(PolygonExterior, Resolution);
    }

    [BenchmarkCategory("PolygonToCells")]
    [Benchmark(Description = "H3NET.Native ToCells")]
    public int NativePolygonToCells() => H3Polygon.ToCells(_nativePolygon, Resolution).Length;
}
