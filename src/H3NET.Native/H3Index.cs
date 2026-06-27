// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using H3NET.Native.Interop;

namespace H3NET.Native;

/// <summary>
/// A 64-bit H3 cell index. This is the primary handle for an H3 cell; all
/// public coordinates exposed through its members are in <b>degrees</b>.
/// </summary>
/// <param name="Value">The raw 64-bit H3 index value.</param>
public readonly partial record struct H3Index(ulong Value)
{
    /// <summary>Gets the sentinel "null" index (raw value <c>0</c>), representing an invalid or absent cell.</summary>
    public static H3Index Null => default;

    /// <summary>Gets a value indicating whether this is the <see cref="Null"/> sentinel (raw value <c>0</c>).</summary>
    public bool IsNull => Value == 0;

    /// <summary>Gets a value indicating whether the native library considers this a valid H3 cell.</summary>
    public bool IsValidCell => NativeMethods.IsValidCell(Value) != 0;

    /// <summary>Gets a value indicating whether this is a valid H3 cell. Alias for <see cref="IsValidCell"/>.</summary>
    public bool IsValid => IsValidCell;

    /// <summary>
    /// Gets the resolution (0-15) of this cell.
    /// </summary>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    public int Resolution
    {
        get
        {
            EnsureValidCell();
            return NativeMethods.GetResolution(Value);
        }
    }

    /// <summary>
    /// Gets a value indicating whether this cell is one of the twelve pentagons at its resolution.
    /// </summary>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    public bool IsPentagon
    {
        get
        {
            EnsureValidCell();
            return NativeMethods.IsPentagon(Value) != 0;
        }
    }

    /// <summary>
    /// Converts a geographic coordinate to the containing H3 cell at the given resolution.
    /// </summary>
    /// <param name="latLng">The coordinate, in degrees.</param>
    /// <param name="resolution">The target H3 resolution (0-15).</param>
    /// <returns>The H3 cell containing <paramref name="latLng"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The coordinate components are outside their valid ranges.</exception>
    /// <exception cref="H3DomainException">The resolution or coordinate was rejected by the native library.</exception>
    public static H3Index FromLatLng(LatLng latLng, int resolution)
    {
        latLng.Validate();
        var native = latLng.ToNative();
        H3ErrorMarshaller.ThrowIfError(NativeMethods.LatLngToCell(native, resolution, out ulong cell));
        return new H3Index(cell);
    }

    /// <summary>
    /// Returns the geographic center of this cell.
    /// </summary>
    /// <returns>The cell center, in degrees.</returns>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    public LatLng ToLatLng()
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToLatLng(Value, out NativeLatLng native));
        return LatLng.FromNative(native);
    }

    /// <summary>
    /// Returns the boundary vertices of this cell in counter-clockwise order.
    /// </summary>
    /// <returns>The boundary vertices, in degrees (5 vertices for pentagons, 6 for hexagons, more when edges cross icosahedron faces).</returns>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    public IReadOnlyList<LatLng> GetBoundary()
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToBoundary(Value, out CellBoundary boundary));

        int count = boundary.NumVerts;
        var result = new LatLng[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = LatLng.FromNative(boundary.Verts[i]);
        }

        return result;
    }

    /// <summary>
    /// Returns all cells within grid distance <paramref name="k"/> of this cell
    /// (the cell itself plus its <paramref name="k"/>-ring neighborhood).
    /// </summary>
    /// <param name="k">The grid distance (number of rings); must be non-negative.</param>
    /// <returns>The cells in the k-ring, with null padding slots removed. Order is not guaranteed.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> is negative, or so large that the result would exceed <see cref="Array.MaxLength"/>.</exception>
    /// <exception cref="H3Exception">The native operation failed.</exception>
    public unsafe H3Index[] GridDisk(int k)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(k);

        H3ErrorMarshaller.ThrowIfError(NativeMethods.MaxGridDiskSize(k, out long maxSize));
        if (maxSize <= 0)
        {
            return [];
        }

        // Upstream maxGridDiskSize clamps very large k to the cell count at res 15
        // (~5.7e11) without erroring, which would overflow any .NET array allocation.
        if (maxSize > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(k),
                k,
                $"k={k} would require {maxSize} cells, exceeding the maximum array length.");
        }

        var buffer = new ulong[maxSize];
        fixed (ulong* ptr = buffer)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.GridDisk(Value, k, ptr));
        }

        int count = 0;
        for (long i = 0; i < maxSize; i++)
        {
            if (buffer[i] != 0)
            {
                count++;
            }
        }

        var result = new H3Index[count];
        int next = 0;
        for (long i = 0; i < maxSize; i++)
        {
            if (buffer[i] != 0)
            {
                result[next++] = new H3Index(buffer[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with the cells within grid distance
    /// <paramref name="k"/> of this cell, packing non-null cells to the front.
    /// </summary>
    /// <param name="k">The grid distance (number of rings); must be non-negative.</param>
    /// <param name="destination">
    /// The destination span. Its length must be at least the maximum grid-disk size
    /// for <paramref name="k"/> (see the upstream <c>maxGridDiskSize</c>).
    /// </param>
    /// <returns>The number of non-null cells written to the front of <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> is negative, so large that the result would exceed <see cref="Array.MaxLength"/>, or <paramref name="destination"/> is too small.</exception>
    /// <exception cref="H3Exception">The native operation failed.</exception>
    public unsafe int GridDiskInto(int k, Span<H3Index> destination)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(k);

        H3ErrorMarshaller.ThrowIfError(NativeMethods.MaxGridDiskSize(k, out long maxSize));

        // Upstream maxGridDiskSize clamps very large k to the cell count at res 15
        // (~5.7e11) without erroring, which no .NET span could ever satisfy.
        if (maxSize > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(k),
                k,
                $"k={k} would require {maxSize} cells, exceeding the maximum array length.");
        }

        if (destination.Length < maxSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Length,
                $"Destination span must hold at least {maxSize} elements for k={k}.");
        }

        if (maxSize <= 0)
        {
            return 0;
        }

        // Write directly into the caller's span: H3Index is blittable and layout-compatible
        // with ulong, so the native fill needs no intermediate buffer.
        fixed (H3Index* ptr = destination)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.GridDisk(Value, k, (ulong*)ptr));
        }

        // Compact the H3_NULL (0) padding holes in place. The write index never
        // outruns the read index, so no live entry is clobbered.
        int count = 0;
        for (long i = 0; i < maxSize; i++)
        {
            if (destination[(int)i].Value != 0)
            {
                destination[count++] = destination[(int)i];
            }
        }

        return count;
    }

    /// <summary>Returns the lowercase, zero-padded 16-digit hexadecimal representation of this index.</summary>
    /// <returns>The 16-character hexadecimal index string.</returns>
    public override string ToString() => Value.ToString("x16", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses an H3 index from its hexadecimal string representation.
    /// </summary>
    /// <param name="value">The hexadecimal index string (with or without a leading "0x").</param>
    /// <returns>The parsed <see cref="H3Index"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException"><paramref name="value"/> is not valid hexadecimal.</exception>
    public static H3Index Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!TryParse(value, out H3Index result))
        {
            throw new FormatException($"'{value}' is not a valid hexadecimal H3 index.");
        }

        return result;
    }

    /// <summary>
    /// Attempts to parse an H3 index from its hexadecimal string representation.
    /// </summary>
    /// <param name="value">The hexadecimal index string (with or without a leading "0x").</param>
    /// <param name="result">When this method returns <see langword="true"/>, the parsed index; otherwise <see cref="Null"/>.</param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out H3Index result)
    {
        result = Null;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        ReadOnlySpan<char> span = value;
        if (span.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            span = span[2..];
        }

        if (!ulong.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong parsed))
        {
            return false;
        }

        result = new H3Index(parsed);
        return true;
    }

    private void EnsureValidCell()
    {
        if (!IsValidCell)
        {
            throw new H3InvalidCellException(
                (uint)H3ErrorCode.CellInvalid,
                $"0x{Value:x16} is not a valid H3 cell.");
        }
    }
}
