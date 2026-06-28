// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Edge;

/// <summary>
/// End-to-end decompose/recompose round-trips: a valid cell broken into
/// (resolution, base cell, digits[1..res]) via the inspection surface must rebuild to
/// the identical cell through Construct, across all 16 resolutions for the SF sample
/// point and for the res-0 pentagons.
/// </summary>
public sealed class ConstructCellRoundTripTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    public void SamplePoint_DecomposeRecompose_AtEveryResolution(int resolution)
    {
        var point = new LatLng(37.775938728915946, -122.41795063018799);
        var cell = H3Index.FromLatLng(point, resolution);

        var rebuilt = DecomposeRecompose(cell);
        Assert.Equal(cell.Value, rebuilt.Value);
    }

    public static IEnumerable<object[]> Res0Pentagons() =>
        FixtureLoader.LoadPentagons().Where(p => p.Res == 0).Select(p => new object[] { p.Cell });

    [Theory]
    [MemberData(nameof(Res0Pentagons))]
    public void Res0Pentagon_DecomposeRecompose(string hex)
    {
        var cell = H3Index.Parse(hex);
        var rebuilt = DecomposeRecompose(cell);
        Assert.Equal(cell.Value, rebuilt.Value);
    }

    private static H3Index DecomposeRecompose(H3Index cell)
    {
        int res = cell.Resolution;
        int baseCell = cell.BaseCellNumber;
        var digits = new int[res];
        for (int r = 1; r <= res; r++)
        {
            digits[r - 1] = cell.GetIndexDigit(r);
        }

        return H3Index.Construct(res, baseCell, digits);
    }
}
