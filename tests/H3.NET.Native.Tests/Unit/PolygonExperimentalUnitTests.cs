// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using Xunit;

namespace H3.NET.Native.Tests.Unit;

/// <summary>
/// Per-member unit tests for the experimental region API
/// <see cref="H3Polygon.ToCellsExperimental(GeoPolygon, int, ContainmentMode)"/>, covering
/// the exact, exception-based contracts the happy-path differential suite
/// (PolygonToCellsExperimentalTests vs the h3-py 4.5.0 oracle) does not exercise:
///
///   * An out-of-domain resolution (-1, 16) is routed through the validate-first guard
///     and surfaces E_RES_DOMAIN (4) as H3DomainException -- before any native call, the
///     same typed exception with the same ErrorCode as the stable ToCells guard.
///   * An out-of-range ContainmentMode cast ((ContainmentMode)99) is NOT validated in C#;
///     the native sizer/fill reject it with E_OPTION_INVALID (15), which the marshaller
///     maps to H3DomainException.
///   * ToCellsExperimental(null) rejects a null polygon with ArgumentNullException.
///   * Each of the four defined ContainmentMode values returns a non-null result for a
///     simple polygon (the externs, the mode->flags threading, and the size-then-fill +
///     StripNull loop all execute).
///   * The valid resolution boundaries (0 and 15) pass the guard and complete the native
///     fill without throwing, mirroring stable ToCells.
///   * ContainmentMode.Center is a native-to-native invariant against the validated stable
///     ToCells: the two covering sets are exactly equal (CONTAINMENT_CENTER is the stable
///     polygonToCells default), tying the experimental path to the stable path with no oracle.
///
/// All assertions are integer/type/set based (no floating point). Every invalid-input
/// case throws the typed exception and never segfaults.
/// </summary>
public sealed class PolygonExperimentalUnitTests
{
    // Pinned libh3 4.5.0 error codes. The validate-first guard raises E_RES_DOMAIN; an
    // out-of-range mode is rejected by native E_OPTION_INVALID.
    private const uint ResDomainErrorCode = 4;
    private const uint OptionInvalidErrorCode = 15;

    // A small, valid hole-free triangle near San Francisco, in degrees, with a clear
    // interior margin so every containment mode produces a deterministic result.
    private static GeoPolygon ValidPolygon() =>
        new(
        [
            new LatLng(37.80, -122.45),
            new LatLng(37.80, -122.40),
            new LatLng(37.75, -122.42),
        ]);

    // ---- Resolution guard --------------------------------------------------

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void ToCellsExperimental_OutOfRangeResolution_ThrowsH3Domain(int resolution)
    {
        // The validate-first guard maps an out-of-range resolution to E_RES_DOMAIN (4)
        // -> H3DomainException before any native call, identical to stable ToCells.
        var ex = Assert.Throws<H3DomainException>(
            () => H3Polygon.ToCellsExperimental(ValidPolygon(), resolution, ContainmentMode.Center));
        Assert.Equal(ResDomainErrorCode, ex.ErrorCode);
    }

    // ---- Out-of-range mode -------------------------------------------------

    [Fact]
    public void ToCellsExperimental_OutOfRangeMode_ThrowsH3Domain()
    {
        // The mode is deliberately NOT validated in C#; the native sizer rejects an
        // out-of-range flags value with E_OPTION_INVALID (15) -> H3DomainException.
        var ex = Assert.Throws<H3DomainException>(
            () => H3Polygon.ToCellsExperimental(ValidPolygon(), 7, (ContainmentMode)99));
        Assert.Equal(OptionInvalidErrorCode, ex.ErrorCode);
    }

    // ---- Null argument contract --------------------------------------------

    [Fact]
    public void ToCellsExperimental_NullPolygon_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => H3Polygon.ToCellsExperimental(null!, 7, ContainmentMode.Center));
        Assert.Equal("polygon", ex.ParamName);
    }

    // ---- Each mode returns a non-null result -------------------------------

    [Theory]
    [InlineData(ContainmentMode.Center)]
    [InlineData(ContainmentMode.Full)]
    [InlineData(ContainmentMode.Overlapping)]
    [InlineData(ContainmentMode.OverlappingBBox)]
    public void ToCellsExperimental_EachMode_ReturnsNonNull(ContainmentMode mode)
    {
        // Every defined mode must thread through flags and complete the native
        // size-then-fill without throwing, returning a (possibly empty) non-null array.
        H3Index[] cells = H3Polygon.ToCellsExperimental(ValidPolygon(), 9, mode);
        Assert.NotNull(cells);
    }

    // ---- Valid resolution boundaries ---------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void ToCellsExperimental_ValidResolutionBoundaries_DoesNotThrow(int resolution)
    {
        // The two extreme in-domain resolutions must pass the validate-first guard and
        // complete the native size-then-fill without throwing, mirroring stable ToCells.
        H3Index[] cells = H3Polygon.ToCellsExperimental(ValidPolygon(), resolution, ContainmentMode.Center);
        Assert.NotNull(cells);
    }

    // ---- Native-to-native invariant: Center == stable ToCells --------------

    [Theory]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(11)]
    public void ToCellsExperimental_CenterMode_EqualsStableToCells_AsSet(int resolution)
    {
        // CONTAINMENT_CENTER is the stable polygonToCells default, so the experimental
        // path with ContainmentMode.Center must produce exactly the same covering set as
        // the validated stable ToCells. Exact unordered-set equality on integer cell ids
        // (no floating point), ties the experimental path to the stable path with no
        // oracle, and is cross-platform safe.
        GeoPolygon polygon = ValidPolygon();

        var stable = H3Polygon.ToCells(polygon, resolution).Select(c => c.Value).ToHashSet();
        var experimental = H3Polygon
            .ToCellsExperimental(polygon, resolution, ContainmentMode.Center)
            .Select(c => c.Value)
            .ToHashSet();

        Assert.Equal(stable, experimental);
    }
}
