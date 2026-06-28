// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Differential;

/// <summary>
/// Managed-vs-native string parity. The shipped managed Parse/ToString fast path and the
/// native-backed FromString/ToCanonicalString must agree on every valid corpus cell's
/// INPUT, while the zero-padding divergence on OUTPUT (native unpadded vs managed
/// 16-char) is pinned exactly.
/// </summary>
public sealed class StringConversionTests
{
    public static IEnumerable<object[]> Cells() =>
        FixtureLoader.LoadCellToLatLng().Select(c => c.Cell)
            .Concat(FixtureLoader.LoadRes0Cells())
            .Concat(FixtureLoader.LoadPentagons().Select(p => p.Cell))
            .Distinct(System.StringComparer.Ordinal)
            .Select(hex => new object[] { hex });

    [Theory]
    [MemberData(nameof(Cells))]
    public void FromString_AgreesWithManagedParse_OnInput(string hex)
    {
        // Managed and native parsers must decode the same INPUT to the same ulong.
        Assert.Equal(H3Index.Parse(hex).Value, H3Index.FromString(hex).Value);
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void Canonical_EqualsManagedToString_StrippedOfLeadingZeros(string hex)
    {
        var cell = H3Index.Parse(hex);

        // Locked parity: ToCanonicalString() == ToString().TrimStart('0')
        //              == Value.ToString("x"). This pins native-unpadded vs managed-padded.
        Assert.Equal(cell.ToString().TrimStart('0'), cell.ToCanonicalString());
        Assert.Equal(cell.Value.ToString("x", CultureInfo.InvariantCulture), cell.ToCanonicalString());
    }

    [Theory]
    [MemberData(nameof(Cells))]
    public void BothForms_RoundTrip_BackToCell(string hex)
    {
        var cell = H3Index.Parse(hex);

        // Native canonical -> native parse -> same cell.
        Assert.Equal(cell, H3Index.FromString(cell.ToCanonicalString()));

        // Managed padded -> managed parse -> same cell.
        Assert.Equal(cell, H3Index.Parse(cell.ToString()));
    }
}
