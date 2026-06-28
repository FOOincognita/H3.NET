// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Differential test: BaseCellNumber, IsResClassIII, and the full GetIndexDigit vector
/// must match the h3-py 4.5.0 oracle (index_digits.ndjson) exactly. Integer/boolean
/// equality, no tolerance.
/// </summary>
public sealed class IndexInspectionTests
{
    public static IEnumerable<object[]> Cases() =>
        FixtureLoader.LoadIndexDigits()
            .Select(c => new object[] { c.Cell, c.Res, c.BaseCell, c.IsClassIii, c.Digits.ToArray() });

    [Theory]
    [MemberData(nameof(Cases))]
    public void Inspection_MatchesOracle(string hex, int res, int baseCell, bool isClassIii, int[] digits)
    {
        var cell = H3Index.Parse(hex);

        Assert.Equal(res, cell.Resolution);
        Assert.Equal(baseCell, cell.BaseCellNumber);
        Assert.Equal(isClassIii, cell.IsResClassIII);

        // digits[r] is the stored digit at resolution r+1 (1-indexed in the API).
        for (int r = 1; r <= res; r++)
        {
            Assert.Equal(digits[r - 1], cell.GetIndexDigit(r));
        }
    }

    [Fact]
    public void Corpus_IsNonEmpty()
    {
        Assert.NotEmpty(Cases());
    }
}
