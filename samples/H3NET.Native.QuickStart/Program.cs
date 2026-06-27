// SPDX-License-Identifier: Apache-2.0

// H3NET.Native QuickStart
//
// This sample consumes the H3NET.Native package via a PackageReference resolved from
// the local feed (see nuget.config). All angular values below are in DEGREES.

using H3NET.Native;

Console.WriteLine("H3NET.Native QuickStart");
Console.WriteLine("=======================");
Console.WriteLine();

// 1. Index a coordinate (near the Ferry Building, San Francisco) at resolution 9.
var sanFrancisco = new LatLng(LatitudeDegrees: 37.7752, LongitudeDegrees: -122.4188);
var cell = H3Index.FromLatLng(sanFrancisco, resolution: 9);

Console.WriteLine($"Input coordinate : {sanFrancisco.LatitudeDegrees}, {sanFrancisco.LongitudeDegrees} (deg)");
Console.WriteLine($"Cell (16-hex)    : {cell}");
Console.WriteLine($"Resolution       : {cell.Resolution}");
Console.WriteLine($"IsPentagon       : {cell.IsPentagon}");

// 2. Round-trip the cell back to its center coordinate.
var center = cell.ToLatLng();
Console.WriteLine($"Cell center      : {center.LatitudeDegrees:F6}, {center.LongitudeDegrees:F6} (deg)");
Console.WriteLine();

// 3. Cell boundary vertices (degrees).
IReadOnlyList<LatLng> boundary = cell.GetBoundary();
Console.WriteLine($"Boundary vertices: {boundary.Count}");
if (boundary.Count > 0)
{
    LatLng first = boundary[0];
    Console.WriteLine($"  vertex[0]      : {first.LatitudeDegrees:F6}, {first.LongitudeDegrees:F6} (deg)");
}
Console.WriteLine();

// 4. Grid disk: the cell plus its k=1 ring (7 cells for a hexagon).
H3Index[] disk = cell.GridDisk(k: 1);
Console.WriteLine($"GridDisk(1)      : {disk.Length} cells");
foreach (H3Index neighbor in disk)
{
    Console.WriteLine($"  {neighbor}");
}
Console.WriteLine();

// 5. Polygon to cells: fill a small bounding-box polygon around the input point.
//    Rings are open (do not repeat the first vertex); winding order is irrelevant.
var bbox = new GeoPolygon(
    exterior:
    [
        new LatLng(37.770, -122.430),
        new LatLng(37.770, -122.410),
        new LatLng(37.785, -122.410),
        new LatLng(37.785, -122.430),
    ]);

H3Index[] covering = H3Polygon.ToCells(bbox, resolution: 9);
Console.WriteLine($"Polygon -> cells : {covering.Length} cells at res 9 covering the bbox");
Console.WriteLine();

Console.WriteLine("Done. Native libh3 was loaded from the H3NET.Native package's runtimes/ assets.");
