// SPDX-License-Identifier: Apache-2.0

using System.Linq;
using CsCheck;
using Xunit;

namespace H3.NET.Native.Tests.Properties;

/// <summary>
/// Vertex invariants for the PR5 surface, sampled over random cells via
/// <see cref="Generators.PointAtResolution"/>: GetVertexes yields 6 vertices for a hexagon
/// / 5 for a pentagon, each valid with a finite, in-range ToLatLng; the indexed GetVertex
/// reproduces exactly that set (set equality of GetVertexes() vs {GetVertex(n)}); and
/// GetVertexesInto agrees with the array overload. No exact vertex-&gt;cell inverse exists,
/// so the geometric check is a finite/in-range sanity invariant, not an equality.
/// </summary>
public sealed class VertexPropertyTests
{
    private const long Iterations = 150;

    [Fact]
    public void GetVertexes_CountIsSixForHexagonFiveForPentagon_AllValidAndFiniteInRange()
    {
        Generators.PointAtResolution.Sample(
            input =>
            {
                var (point, res) = input;
                var origin = H3Index.FromLatLng(point, res);

                var vertexes = origin.GetVertexes();

                Assert.Equal(origin.IsPentagon ? 5 : 6, vertexes.Length);
                Assert.All(vertexes, v =>
                {
                    Assert.True(v.IsValid());
                    var ll = v.ToLatLng();
                    Assert.True(double.IsFinite(ll.LatitudeDegrees));
                    Assert.True(double.IsFinite(ll.LongitudeDegrees));
                    Assert.InRange(ll.LatitudeDegrees, -90.0, 90.0);
                    Assert.InRange(ll.LongitudeDegrees, -180.0, 180.0);
                });
            },
            iter: Iterations);
    }

    [Fact]
    public void GetVertexes_EqualsIndexedGetVertexSet()
    {
        Generators.PointAtResolution.Sample(
            input =>
            {
                var (point, res) = input;
                var origin = H3Index.FromLatLng(point, res);

                var bulk = origin.GetVertexes().Select(v => v.Value).ToHashSet();

                // Every vertex from the bulk API must be reachable via some GetVertex(n) for
                // n in 0..count-1, and vice versa: the two surfaces describe the same set.
                int count = bulk.Count;
                var indexed = Enumerable.Range(0, count)
                    .Select(n => origin.GetVertex(n).Value)
                    .ToHashSet();

                Assert.Equal(bulk, indexed);
            },
            iter: Iterations);
    }

    [Fact]
    public void GetVertexesInto_MatchesArrayOverload()
    {
        Generators.PointAtResolution.Sample(
            input =>
            {
                var (point, res) = input;
                var origin = H3Index.FromLatLng(point, res);
                var expected = origin.GetVertexes();

                var destination = new H3Vertex[6];
                int count = origin.GetVertexesInto(destination);

                Assert.Equal(expected.Length, count);
                Assert.Equal(expected, destination[..count]);
            },
            iter: Iterations);
    }

    [Fact]
    public void EveryVertexToLatLng_IsFiniteAndInCanonicalRange()
    {
        // No exact inverse maps an isolated vertex back to a unique cell, so this is a
        // sanity invariant: each vertex projects to a finite, canonical-range coordinate
        // (vertices are shared with neighbors and lie on the cell boundary).
        Generators.PointAtResolution.Sample(
            input =>
            {
                var (point, res) = input;
                var origin = H3Index.FromLatLng(point, res);

                foreach (var vertex in origin.GetVertexes())
                {
                    var ll = vertex.ToLatLng();
                    Assert.True(double.IsFinite(ll.LatitudeDegrees));
                    Assert.True(double.IsFinite(ll.LongitudeDegrees));
                    Assert.InRange(ll.LatitudeDegrees, -90.0, 90.0);
                    Assert.InRange(ll.LongitudeDegrees, -180.0, 180.0);
                }
            },
            iter: Iterations);
    }
}
