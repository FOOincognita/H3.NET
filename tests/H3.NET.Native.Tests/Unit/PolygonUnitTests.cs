// SPDX-License-Identifier: Apache-2.0

using System;
using Xunit;

namespace H3.NET.Native.Tests.Unit;

/// <summary>
/// Per-member unit tests for the region facade <see cref="H3Polygon"/>, covering the
/// exact, exception-based contracts that the happy-path differential suites
/// (PolygonToCellsTests vs the h3-py 4.5.0 oracle, CellsToPolygonRoundTripTests'
/// native-to-native invariants) do not exercise:
///
///   * ToCells routes an out-of-domain resolution (-1, 16) through the validate-first
///     guard and surfaces E_RES_DOMAIN (4) as H3DomainException -- the same typed
///     exception with the same ErrorCode the native path would raise, asserted at the
///     exact boundaries, so the guard is indistinguishable from native E_RES_DOMAIN.
///   * ToCells(null) and FromCells(null) reject null arguments with ArgumentNullException.
///   * FromCells over an empty or all-H3_NULL input returns an empty list (the H3_NULL
///     sentinel is filtered before any native call, so no polygons are produced and no
///     native pointer is ever dereferenced -- proving the strip happens pre-call).
///   * ToCells at the valid resolution boundaries (0 and 15) does not throw.
///
/// All assertions are integer/type/set based (no floating point), per the PR6
/// cross-platform tolerance lesson. Every invalid-input case throws the typed exception
/// and never segfaults.
/// </summary>
public sealed class PolygonUnitTests
{
    // Pinned libh3 4.5.0 error code: E_RES_DOMAIN. The validate-first guard at the top
    // of ToCells raises the identical code the native maxPolygonToCellsSize path would.
    private const uint ResDomainErrorCode = 4;

    // A small, valid hole-free triangle near San Francisco, in degrees. Vertex exactness
    // is irrelevant for the guard / null tests: each fails before any native fill. The
    // boundary sanity test fills it at res 0 and 15.
    private static GeoPolygon ValidPolygon() =>
        new(
        [
            new LatLng(37.80, -122.45),
            new LatLng(37.80, -122.40),
            new LatLng(37.75, -122.42),
        ]);

    // ---- ToCells resolution guard ------------------------------------------

    [Theory]
    [InlineData(-1)]
    [InlineData(16)]
    public void ToCells_OutOfRangeResolution_ThrowsH3Domain(int resolution)
    {
        // The validate-first guard at the top of ToCells maps an out-of-range
        // resolution to E_RES_DOMAIN (4) -> H3DomainException, before any native call,
        // and pins the same ErrorCode the native path surfaces.
        var ex = Assert.Throws<H3DomainException>(() => H3Polygon.ToCells(ValidPolygon(), resolution));
        Assert.Equal(ResDomainErrorCode, ex.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void ToCells_ValidResolutionBoundaries_DoesNotThrow(int resolution)
    {
        // The two extreme in-domain resolutions must pass the guard and complete the
        // native size-then-fill without throwing.
        H3Index[] cells = H3Polygon.ToCells(ValidPolygon(), resolution);
        Assert.NotNull(cells);
    }

    // ---- Null argument contracts -------------------------------------------

    [Fact]
    public void ToCells_NullPolygon_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => H3Polygon.ToCells(null!, 7));
        Assert.Equal("polygon", ex.ParamName);
    }

    [Fact]
    public void FromCells_Null_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => H3Polygon.FromCells(null!));
        Assert.Equal("cells", ex.ParamName);
    }

    // ---- FromCells empty / all-null input ----------------------------------

    [Fact]
    public void FromCells_Empty_ReturnsEmptyList()
    {
        Assert.Empty(H3Polygon.FromCells(Array.Empty<H3Index>()));
    }

    [Fact]
    public void FromCells_AllNullSentinels_ReturnsEmptyList()
    {
        // Two H3_NULL sentinels are stripped before the native call, leaving no cells
        // and thus no polygons: this proves the H3_NULL strip precedes any native
        // dereference (no segfault on a would-be-empty native set).
        H3Index[] cells = [H3Index.Null, H3Index.Null];
        Assert.Empty(H3Polygon.FromCells(cells));
    }
}
