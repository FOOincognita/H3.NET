// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential test: GetIcosahedronFaces (sorted) must match the h3-py 4.5.0 oracle
/// (icosahedron_faces.ndjson) exactly. Exact integer-set equality. The existing corpus
/// does not cover faces, so this fixture is new.
/// </summary>
public sealed class IcosahedronFacesTests
{
    public static IEnumerable<object[]> Cases() =>
        FixtureLoader.LoadIcosahedronFaces()
            .Select(c => new object[] { c.Cell, c.Faces.ToArray() });

    [Theory]
    [MemberData(nameof(Cases))]
    public void GetIcosahedronFaces_MatchesOracle(string hex, int[] expectedSortedFaces)
    {
        var actual = H3Index.Parse(hex).GetIcosahedronFaces();

        // The oracle stores faces sorted ascending; sort the binding output to compare
        // as sets (the native fill order is unspecified).
        var sorted = actual.OrderBy(f => f).ToArray();
        Assert.Equal(expectedSortedFaces, sorted);
    }

    [Fact]
    public void Corpus_IsNonEmpty()
    {
        Assert.NotEmpty(Cases());
    }
}
