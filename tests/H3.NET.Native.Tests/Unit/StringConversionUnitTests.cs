// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using H3.NET.Native.Tests.Fixtures;
using Xunit;

namespace H3.NET.Native.Tests.Unit;

/// <summary>
/// Per-function unit tests for the native-backed string conversion members
/// (FromString via stringToH3, ToCanonicalString via h3ToString). The managed
/// Parse/ToString fast path is covered elsewhere; here the focus is the native
/// channel's error/argument behavior and the canonical (unpadded) format.
/// </summary>
public sealed class StringConversionUnitTests
{
    public static IEnumerable<object[]> ValidCells() =>
        FixtureLoader.LoadRes0Cells()
            .Concat(FixtureLoader.LoadPentagons().Select(p => p.Cell))
            .Concat(FixtureLoader.LoadIndexDigits().Select(c => c.Cell))
            .Distinct(System.StringComparer.Ordinal)
            .Select(hex => new object[] { hex });

    [Fact]
    public void FromString_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => H3Index.FromString(null!));
    }

    [Theory]
    [InlineData("not-hex")]
    [InlineData("zzzz")]
    [InlineData("")]
    public void FromString_WithUnparseableString_ThrowsH3Exception(string value)
    {
        // The native sscanf path surfaces E_FAILED, mapped to the base H3Exception.
        Assert.Throws<H3Exception>(() => H3Index.FromString(value));
    }

    [Theory]
    [MemberData(nameof(ValidCells))]
    public void FromString_OnValidHex_EqualsManagedParse(string hex)
    {
        Assert.Equal(H3Index.Parse(hex).Value, H3Index.FromString(hex).Value);
    }

    [Theory]
    [MemberData(nameof(ValidCells))]
    public void ToCanonicalString_IsLowercase_Unpadded_AndRoundTrips(string hex)
    {
        var cell = H3Index.Parse(hex);
        string canonical = cell.ToCanonicalString();

        // Native h3ToString uses sprintf("%PRIx64"): lowercase, variable-length (not
        // zero-padded). It equals the ulong's unpadded hex form.
        Assert.Equal(cell.Value.ToString("x", CultureInfo.InvariantCulture), canonical);
        Assert.Equal(canonical.ToLowerInvariant(), canonical);
        Assert.DoesNotContain('-', canonical);

        // Round-trips back to the same cell through the native parser.
        Assert.Equal(cell.Value, H3Index.FromString(canonical).Value);
    }

    [Fact]
    public void ToCanonicalString_IsNotZeroPadded_ForCorpusCells()
    {
        // At least one corpus cell must demonstrate the unpadded divergence from the
        // 16-char ToString fast path, otherwise the canonical contract is untested.
        bool anyUnpadded = FixtureLoader.LoadRes0Cells()
            .Concat(FixtureLoader.LoadPentagons().Select(p => p.Cell))
            .Select(H3Index.Parse)
            .Any(c => c.ToCanonicalString().Length < 16);

        // Every res-0/pentagon cell has top mode bits set, yielding 15-char canonical
        // forms; this guards the corpus from silently losing the divergence.
        Assert.True(anyUnpadded);
    }

    [Fact]
    public void TryFromString_WithNull_ReturnsFalse()
    {
        Assert.False(H3Index.TryFromString(null, out var result));
        Assert.Equal(H3Index.Null, result);
    }

    [Fact]
    public void TryFromString_WithGarbage_ReturnsFalse()
    {
        Assert.False(H3Index.TryFromString("not-hex", out var result));
        Assert.Equal(H3Index.Null, result);
    }
}
