// SPDX-License-Identifier: Apache-2.0

using CsCheck;
using Xunit;

namespace H3NET.Native.Tests.Properties;

/// <summary>
/// Boundary invariants: every cell's boundary has between 5 and 10 finite vertices in
/// canonical degree ranges.
/// </summary>
public sealed class BoundaryPropertyTests
{
    private const long Iterations = 150;

    [Fact]
    public void Boundary_VertexCount_InClosedRangeFiveToTen()
    {
        Generators.PointAtResolution.Sample(
            input =>
            {
                var (point, res) = input;
                var cell = H3Index.FromLatLng(point, res);
                var boundary = cell.GetBoundary();

                Assert.InRange(boundary.Count, 5, 10);

                foreach (var v in boundary)
                {
                    Assert.True(double.IsFinite(v.LatitudeDegrees));
                    Assert.True(double.IsFinite(v.LongitudeDegrees));
                    Assert.InRange(v.LatitudeDegrees, -90.0, 90.0);
                    Assert.InRange(v.LongitudeDegrees, -180.0, 180.0);
                }
            },
            iter: Iterations);
    }
}
