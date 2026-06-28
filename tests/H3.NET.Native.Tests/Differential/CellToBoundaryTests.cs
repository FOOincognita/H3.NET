// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential test: H3Index.GetBoundary must match the h3-py 4.5.0 oracle vertex
/// list (same count, same ordering, each vertex within 1e-7 degrees).
/// </summary>
public sealed class CellToBoundaryTests
{
    private const double ToleranceDegrees = 1e-7;

    // Cached corpus indexed positionally; rows carry only an int index so xUnit v3 can
    // serialize the theory data (complex objects are not natively serializable).
    private static readonly List<FixtureLoader.CellToBoundaryCase> AllCases =
        FixtureLoader.LoadCellToBoundary().ToList();

    public static IEnumerable<object[]> Cases() =>
        Enumerable.Range(0, AllCases.Count).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(Cases))]
    public void GetBoundary_MatchesOracle(int index)
    {
        var testCase = AllCases[index];
        var cell = H3Index.Parse(testCase.Cell);
        var boundary = cell.GetBoundary();

        Assert.Equal(testCase.Verts.Count, boundary.Count);

        for (int i = 0; i < testCase.Verts.Count; i++)
        {
            // Oracle vertex layout is [lat, lng].
            Assert.Equal(testCase.Verts[i][0], boundary[i].LatitudeDegrees, ToleranceDegrees);
            Assert.Equal(testCase.Verts[i][1], boundary[i].LongitudeDegrees, ToleranceDegrees);
        }
    }
}
