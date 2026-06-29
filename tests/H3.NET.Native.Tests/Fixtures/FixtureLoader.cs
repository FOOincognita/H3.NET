// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace H3.NET.Native.Tests.Fixtures;

/// <summary>
/// Loads the committed oracle corpus (h3-py 4.5.0) from
/// <c>AppContext.BaseDirectory/Fixtures/data</c>. NDJSON files are read one JSON
/// object per line; CSV files are read line by line. All angular values are degrees.
/// </summary>
internal static class FixtureLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private static string DataDirectory =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "data");

    /// <summary>Resolves the absolute path of a committed fixture file.</summary>
    public static string PathFor(string fileName) => Path.Combine(DataDirectory, fileName);

    // --- Typed case records (nested to satisfy Meziantou MA0048: one public top-level type per file). ---

    /// <summary>One latlng_to_cell.ndjson record.</summary>
    public sealed record LatLngToCellCase(
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lng")] double Lng,
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("cell")] string Cell);

    /// <summary>One cell_to_latlng.ndjson record.</summary>
    public sealed record CellToLatLngCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lng")] double Lng);

    /// <summary>One cell_to_boundary.ndjson record. Each vertex is [lat, lng] degrees.</summary>
    public sealed record CellToBoundaryCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("verts")] IReadOnlyList<double[]> Verts);

    /// <summary>One grid_disk.ndjson record. Cells are a compact, unordered set.</summary>
    public sealed record GridDiskCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("k")] int K,
        [property: JsonPropertyName("cells")] IReadOnlyList<string> Cells);

    /// <summary>One polygon_to_cells.ndjson record.</summary>
    public sealed record PolygonToCellsCase(
        [property: JsonPropertyName("polygon")] PolygonShape Polygon,
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("cells")] IReadOnlyList<string> Cells);

    /// <summary>Polygon geometry inside a polygon_to_cells record; rings are [lat, lng] degree pairs.</summary>
    public sealed record PolygonShape(
        [property: JsonPropertyName("exterior")] IReadOnlyList<double[]> Exterior,
        [property: JsonPropertyName("holes")] IReadOnlyList<double[][]>? Holes);

    /// <summary>
    /// One polygon_to_cells_experimental.ndjson record: a polygon, resolution, the
    /// numeric ContainmentMode ordinal (0=Center, 1=Full, 2=Overlapping,
    /// 3=OverlappingBBox), and the oracle covering-cell set for that mode.
    /// </summary>
    public sealed record PolygonToCellsExperimentalCase(
        [property: JsonPropertyName("polygon")] PolygonShape Polygon,
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("mode")] uint Mode,
        [property: JsonPropertyName("cells")] IReadOnlyList<string> Cells);

    /// <summary>One index_digits.ndjson record. Digits is the stored vector digits[1..res].</summary>
    public sealed record IndexDigitsCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("base_cell")] int BaseCell,
        [property: JsonPropertyName("is_class_iii")] bool IsClassIii,
        [property: JsonPropertyName("digits")] IReadOnlyList<int> Digits);

    /// <summary>One icosahedron_faces.ndjson record. Faces are sorted ascending, each 0-19.</summary>
    public sealed record IcosahedronFacesCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("faces")] IReadOnlyList<int> Faces);

    /// <summary>
    /// One hierarchy.ndjson record: a sample cell plus its chosen parent, center child,
    /// and full sorted children set. Covers hexagons and pentagons.
    /// </summary>
    public sealed record HierarchyCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("parent_res")] int ParentRes,
        [property: JsonPropertyName("parent")] string Parent,
        [property: JsonPropertyName("center_child_res")] int CenterChildRes,
        [property: JsonPropertyName("center_child")] string CenterChild,
        [property: JsonPropertyName("children_res")] int ChildrenRes,
        [property: JsonPropertyName("children")] IReadOnlyList<string> Children);

    /// <summary>One child_pos.ndjson record: a child cell and its cellToChildPos at a coarser parent resolution.</summary>
    public sealed record ChildPosCase(
        [property: JsonPropertyName("child")] string Child,
        [property: JsonPropertyName("parent_res")] int ParentRes,
        [property: JsonPropertyName("pos")] long Pos);

    /// <summary>One compact.ndjson record: a full child set at a resolution and its compacted form.</summary>
    public sealed record CompactCase(
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input,
        [property: JsonPropertyName("compacted")] IReadOnlyList<string> Compacted);

    /// <summary>One grid_ring.ndjson record: the hollow ring at exactly distance k. Cells are a compact, unordered set.</summary>
    public sealed record GridRingCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("k")] int K,
        [property: JsonPropertyName("cells")] IReadOnlyList<string> Cells);

    /// <summary>One grid_path.ndjson record: the ORDERED, endpoint-inclusive path (path[0]==start, path[^1]==end).</summary>
    public sealed record GridPathCase(
        [property: JsonPropertyName("start")] string Start,
        [property: JsonPropertyName("end")] string End,
        [property: JsonPropertyName("path")] IReadOnlyList<string> Path);

    /// <summary>One grid_distance.ndjson record: the grid distance between origin and other.</summary>
    public sealed record GridDistanceCase(
        [property: JsonPropertyName("origin")] string Origin,
        [property: JsonPropertyName("other")] string Other,
        [property: JsonPropertyName("distance")] long Distance);

    /// <summary>One local_ij.ndjson record: the local IJ of target relative to origin (mode 0).</summary>
    public sealed record LocalIjCase(
        [property: JsonPropertyName("origin")] string Origin,
        [property: JsonPropertyName("target")] string Target,
        [property: JsonPropertyName("i")] int I,
        [property: JsonPropertyName("j")] int J);

    /// <summary>One grid_disk_distances.ndjson record: parallel (cell, distance) arrays for the disk at radius k.</summary>
    public sealed record GridDiskDistancesCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("k")] int K,
        [property: JsonPropertyName("cells")] IReadOnlyList<string> Cells,
        [property: JsonPropertyName("distances")] IReadOnlyList<int> Distances);

    /// <summary>One directed_edge.ndjson record: an origin cell and every directed edge originating at it.</summary>
    public sealed record DirectedEdgeCase(
        [property: JsonPropertyName("origin")] string Origin,
        [property: JsonPropertyName("is_pentagon")] bool IsPentagon,
        [property: JsonPropertyName("edges")] IReadOnlyList<DirectedEdgeDetail> Edges);

    /// <summary>One directed edge's full oracle: its index, origin/destination cells, cell pair, reverse, and boundary.</summary>
    public sealed record DirectedEdgeDetail(
        [property: JsonPropertyName("edge")] string Edge,
        [property: JsonPropertyName("origin")] string Origin,
        [property: JsonPropertyName("destination")] string Destination,
        [property: JsonPropertyName("cells")] IReadOnlyList<string> Cells,
        [property: JsonPropertyName("reverse")] string Reverse,
        [property: JsonPropertyName("boundary")] IReadOnlyList<double[]> Boundary);

    /// <summary>One neighbor.ndjson record: an ordered (origin, candidate) pair and the are_neighbor_cells result.</summary>
    public sealed record NeighborCase(
        [property: JsonPropertyName("origin")] string Origin,
        [property: JsonPropertyName("candidate")] string Candidate,
        [property: JsonPropertyName("are_neighbors")] bool AreNeighbors);

    /// <summary>One vertex.ndjson record: an origin cell and every topological vertex it owns.</summary>
    public sealed record VertexCase(
        [property: JsonPropertyName("origin")] string Origin,
        [property: JsonPropertyName("is_pentagon")] bool IsPentagon,
        [property: JsonPropertyName("num_vertexes")] int NumVertexes,
        [property: JsonPropertyName("vertexes")] IReadOnlyList<VertexDetail> Vertexes);

    /// <summary>One vertex's full oracle: its vertexNum, the 16-hex vertex index, and its degrees lat/lng.</summary>
    public sealed record VertexDetail(
        [property: JsonPropertyName("vertex_num")] int VertexNum,
        [property: JsonPropertyName("vertex")] string Vertex,
        [property: JsonPropertyName("lat")] double Lat,
        [property: JsonPropertyName("lng")] double Lng);

    /// <summary>One cell_area.ndjson record: a cell and its area in rads^2 / km^2 / m^2.</summary>
    public sealed record CellAreaCase(
        [property: JsonPropertyName("cell")] string Cell,
        [property: JsonPropertyName("rads2")] double Rads2,
        [property: JsonPropertyName("km2")] double Km2,
        [property: JsonPropertyName("m2")] double M2);

    /// <summary>One edge_length.ndjson record: a directed edge and its length in rads / km / m.</summary>
    public sealed record EdgeLengthCase(
        [property: JsonPropertyName("edge")] string Edge,
        [property: JsonPropertyName("rads")] double Rads,
        [property: JsonPropertyName("km")] double Km,
        [property: JsonPropertyName("m")] double M);

    /// <summary>One hexagon_area_avg.ndjson record: a resolution and its average hexagon area in km^2 / m^2.</summary>
    public sealed record HexagonAreaAvgCase(
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("km2")] double Km2,
        [property: JsonPropertyName("m2")] double M2);

    /// <summary>One hexagon_edge_length_avg.ndjson record: a resolution and its average hexagon edge length in km / m.</summary>
    public sealed record HexagonEdgeLengthAvgCase(
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("km")] double Km,
        [property: JsonPropertyName("m")] double M);

    /// <summary>One num_cells.ndjson record: a resolution and its total cell count.</summary>
    public sealed record NumCellsCase(
        [property: JsonPropertyName("res")] int Res,
        [property: JsonPropertyName("count")] long Count);

    /// <summary>One great_circle_distance.ndjson record: a degree point pair and its distance in rads / km / m.</summary>
    public sealed record GreatCircleDistanceCase(
        [property: JsonPropertyName("a_lat")] double ALat,
        [property: JsonPropertyName("a_lng")] double ALng,
        [property: JsonPropertyName("b_lat")] double BLat,
        [property: JsonPropertyName("b_lng")] double BLng,
        [property: JsonPropertyName("rads")] double Rads,
        [property: JsonPropertyName("km")] double Km,
        [property: JsonPropertyName("m")] double M);

    // --- Loaders ---

    public static IEnumerable<LatLngToCellCase> LoadLatLngToCell() =>
        LoadNdjson<LatLngToCellCase>("latlng_to_cell.ndjson");

    public static IEnumerable<CellToLatLngCase> LoadCellToLatLng() =>
        LoadNdjson<CellToLatLngCase>("cell_to_latlng.ndjson");

    public static IEnumerable<CellToBoundaryCase> LoadCellToBoundary() =>
        LoadNdjson<CellToBoundaryCase>("cell_to_boundary.ndjson");

    public static IEnumerable<GridDiskCase> LoadGridDisk() =>
        LoadNdjson<GridDiskCase>("grid_disk.ndjson");

    public static IEnumerable<PolygonToCellsCase> LoadPolygonToCells() =>
        LoadNdjson<PolygonToCellsCase>("polygon_to_cells.ndjson");

    public static IEnumerable<PolygonToCellsExperimentalCase> LoadPolygonToCellsExperimental() =>
        LoadNdjson<PolygonToCellsExperimentalCase>("polygon_to_cells_experimental.ndjson");

    public static IEnumerable<IndexDigitsCase> LoadIndexDigits() =>
        LoadNdjson<IndexDigitsCase>("index_digits.ndjson");

    public static IEnumerable<IcosahedronFacesCase> LoadIcosahedronFaces() =>
        LoadNdjson<IcosahedronFacesCase>("icosahedron_faces.ndjson");

    public static IEnumerable<HierarchyCase> LoadHierarchy() =>
        LoadNdjson<HierarchyCase>("hierarchy.ndjson");

    public static IEnumerable<ChildPosCase> LoadChildPos() =>
        LoadNdjson<ChildPosCase>("child_pos.ndjson");

    public static IEnumerable<CompactCase> LoadCompact() =>
        LoadNdjson<CompactCase>("compact.ndjson");

    public static IEnumerable<GridRingCase> LoadGridRing() =>
        LoadNdjson<GridRingCase>("grid_ring.ndjson");

    public static IEnumerable<GridPathCase> LoadGridPath() =>
        LoadNdjson<GridPathCase>("grid_path.ndjson");

    public static IEnumerable<GridDistanceCase> LoadGridDistance() =>
        LoadNdjson<GridDistanceCase>("grid_distance.ndjson");

    public static IEnumerable<LocalIjCase> LoadLocalIj() =>
        LoadNdjson<LocalIjCase>("local_ij.ndjson");

    public static IEnumerable<GridDiskDistancesCase> LoadGridDiskDistances() =>
        LoadNdjson<GridDiskDistancesCase>("grid_disk_distances.ndjson");

    public static IEnumerable<DirectedEdgeCase> LoadDirectedEdge() =>
        LoadNdjson<DirectedEdgeCase>("directed_edge.ndjson");

    public static IEnumerable<NeighborCase> LoadNeighbor() =>
        LoadNdjson<NeighborCase>("neighbor.ndjson");

    public static IEnumerable<VertexCase> LoadVertex() =>
        LoadNdjson<VertexCase>("vertex.ndjson");

    public static IEnumerable<CellAreaCase> LoadCellArea() =>
        LoadNdjson<CellAreaCase>("cell_area.ndjson");

    public static IEnumerable<EdgeLengthCase> LoadEdgeLength() =>
        LoadNdjson<EdgeLengthCase>("edge_length.ndjson");

    public static IEnumerable<HexagonAreaAvgCase> LoadHexagonAreaAvg() =>
        LoadNdjson<HexagonAreaAvgCase>("hexagon_area_avg.ndjson");

    public static IEnumerable<HexagonEdgeLengthAvgCase> LoadHexagonEdgeLengthAvg() =>
        LoadNdjson<HexagonEdgeLengthAvgCase>("hexagon_edge_length_avg.ndjson");

    public static IEnumerable<NumCellsCase> LoadNumCells() =>
        LoadNdjson<NumCellsCase>("num_cells.ndjson");

    public static IEnumerable<GreatCircleDistanceCase> LoadGreatCircleDistance() =>
        LoadNdjson<GreatCircleDistanceCase>("great_circle_distance.ndjson");

    /// <summary>Reads res0_cells.csv: one 16-hex cell per line.</summary>
    public static IReadOnlyList<string> LoadRes0Cells()
    {
        var result = new List<string>();
        foreach (string line in File.ReadLines(PathFor("res0_cells.csv")))
        {
            string trimmed = line.Trim();
            if (trimmed.Length != 0)
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    /// <summary>Reads pentagons.csv (header "pentagon,res"): returns (cell, resolution) pairs.</summary>
    public static IReadOnlyList<(string Cell, int Res)> LoadPentagons()
    {
        var result = new List<(string, int)>();
        bool first = true;
        foreach (string line in File.ReadLines(PathFor("pentagons.csv")))
        {
            if (first)
            {
                first = false;
                continue; // header
            }

            string trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            string[] parts = trimmed.Split(',');
            result.Add((parts[0].Trim(), int.Parse(parts[1].Trim(), CultureInfo.InvariantCulture)));
        }

        return result;
    }

    private static IEnumerable<T> LoadNdjson<T>(string fileName)
    {
        foreach (string line in File.ReadLines(PathFor(fileName)))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            T value = JsonSerializer.Deserialize<T>(line, JsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize line in {fileName}.");
            yield return value;
        }
    }
}
