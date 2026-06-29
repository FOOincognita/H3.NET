// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential test: the PR5 vertex surface must match the h3-py 4.5.0 oracle
/// (vertex.ndjson). Covers every oracle-backed projection: cell_to_vertex (the indexed
/// vertex for each (origin, vertex_num)), cell_to_vertexes (as an unordered set, H3_NULL
/// stripped, pentagons giving 5 and hexagons 6), vertex_to_latlng (degrees, matched to
/// 1e-9), and is_valid_vertex over a mix of real vertexes and junk. The GetVertexesInto
/// overload is checked against the array overload across the corpus.
/// </summary>
public sealed class VertexTests
{
    // Vertex lat/lng must match the oracle to ~1e-9; matches CellToBoundaryTests.
    private const double ToleranceDegrees = 1e-9;

    // Junk raw values that the oracle reports as NOT valid vertexes.
    private static readonly ulong[] JunkVertices =
    [
        0x0UL,
        0x1UL,
        0xdeadbeefUL,
        0xffffffffffffffffUL,
        0x7fffffffffffffffUL,
    ];

    // Cached corpus indexed positionally; theory rows carry only an int index so xUnit v3
    // can serialize the theory data (complex objects are not natively serializable).
    private static readonly List<FixtureLoader.VertexCase> AllOrigins =
        FixtureLoader.LoadVertex().ToList();

    public static IEnumerable<object[]> OriginCases() =>
        Enumerable.Range(0, AllOrigins.Count).Select(i => new object[] { i });

    // One row per (origin index, vertex_num) so cell_to_vertex is pinned per vertex.
    public static IEnumerable<object[]> VertexCases() =>
        Enumerable.Range(0, AllOrigins.Count)
            .SelectMany(i => AllOrigins[i].Vertexes.Select(v => new object[] { i, v.VertexNum }));

    public static IEnumerable<object[]> ValidVertexCases() =>
        AllOrigins.SelectMany(c => c.Vertexes).Select(v => new object[] { v.Vertex, true });

    public static IEnumerable<object[]> JunkVertexCases() =>
        JunkVertices.Select(v => new object[] { v.ToString("x16", System.Globalization.CultureInfo.InvariantCulture), false });

    // ---- cell_to_vertex ----------------------------------------------------

    [Theory]
    [MemberData(nameof(VertexCases))]
    public void GetVertex_MatchesOracle(int originIndex, int vertexNum)
    {
        var oracle = AllOrigins[originIndex];
        var detail = oracle.Vertexes.First(v => v.VertexNum == vertexNum);
        var cell = H3Index.Parse(oracle.Origin);

        var vertex = cell.GetVertex(vertexNum);

        Assert.Equal(H3Index.Parse(detail.Vertex).Value, vertex.Value);
    }

    // ---- cell_to_vertexes (unordered set, H3_NULL stripped) ---------------

    [Theory]
    [MemberData(nameof(OriginCases))]
    public void GetVertexes_MatchesOracleSet(int index)
    {
        var oracle = AllOrigins[index];
        var origin = H3Index.Parse(oracle.Origin);

        var expected = oracle.Vertexes.Select(v => H3Index.Parse(v.Vertex).Value).ToHashSet();
        var actual = origin.GetVertexes().Select(v => v.Value).ToHashSet();

        // Pentagons give 5 (H3_NULL stripped), hexagons 6; order is unspecified.
        Assert.Equal(oracle.IsPentagon ? 5 : 6, expected.Count);
        Assert.Equal(oracle.NumVertexes, expected.Count);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(OriginCases))]
    public void GetVertexesInto_MatchesGetVertexes(int index)
    {
        var oracle = AllOrigins[index];
        var origin = H3Index.Parse(oracle.Origin);
        var expected = origin.GetVertexes();

        var destination = new H3Vertex[6];
        int count = origin.GetVertexesInto(destination);

        Assert.Equal(expected.Length, count);
        Assert.Equal(expected, destination[..count]);
    }

    // ---- vertex_to_latlng --------------------------------------------------

    [Theory]
    [MemberData(nameof(VertexCases))]
    public void ToLatLng_MatchesOracle(int originIndex, int vertexNum)
    {
        var oracle = AllOrigins[originIndex];
        var detail = oracle.Vertexes.First(v => v.VertexNum == vertexNum);
        var vertex = new H3Vertex(H3Index.Parse(detail.Vertex).Value);

        var point = vertex.ToLatLng();

        Assert.Equal(detail.Lat, point.LatitudeDegrees, ToleranceDegrees);
        Assert.Equal(detail.Lng, point.LongitudeDegrees, ToleranceDegrees);
    }

    // ---- is_valid_vertex (real vertexes + junk) ----------------------------

    [Theory]
    [MemberData(nameof(ValidVertexCases))]
    [MemberData(nameof(JunkVertexCases))]
    public void IsValid_MatchesOracle(string hex, bool expected)
    {
        var vertex = new H3Vertex(H3Index.Parse(hex).Value);
        Assert.Equal(expected, vertex.IsValid());
        Assert.Equal(expected, H3Vertex.IsValid(vertex.Value));
    }

    [Fact]
    public void Corpus_IsNonEmpty()
    {
        Assert.NotEmpty(OriginCases());
        Assert.NotEmpty(VertexCases());
        // The corpus must include at least one pentagon so the 5-vertex strip is exercised.
        Assert.Contains(AllOrigins, c => c.IsPentagon);
    }
}
