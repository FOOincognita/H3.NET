// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace H3NET.Native.Tests.Fixtures;

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
