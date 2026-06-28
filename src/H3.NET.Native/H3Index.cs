// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using H3.NET.Native.Interop;

namespace H3.NET.Native;

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
    /// Gets a value indicating whether the native library considers this a valid H3
    /// index of any kind: a cell, a directed edge, or a vertex. This is broader than
    /// <see cref="IsValidCell"/>, which is true only for cells; it returns
    /// <see langword="true"/> for valid edge and vertex indexes even though the typed
    /// edge and vertex APIs are introduced in later releases.
    /// </summary>
    public bool IsValidIndex => NativeMethods.IsValidIndex(Value) != 0;

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
    /// Gets the base cell number (0-121) of this cell.
    /// </summary>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    public int BaseCellNumber
    {
        get
        {
            EnsureValidCell();
            return NativeMethods.GetBaseCellNumber(Value);
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
    /// Gets a value indicating whether this cell is a Class III cell, that is, one
    /// whose resolution is odd. Class III cells are rotated and slightly distorted
    /// relative to Class II (even-resolution) cells.
    /// </summary>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    public bool IsResClassIII
    {
        get
        {
            EnsureValidCell();
            return NativeMethods.IsResClassIII(Value) != 0;
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

    /// <summary>
    /// Returns the stored indexing digit (0-7) at the given resolution.
    /// </summary>
    /// <param name="resolution">
    /// The 1-indexed resolution (1-15) of the digit to read. Resolution 0 is the base
    /// cell, not a digit. The resolution may exceed this cell's own resolution, in
    /// which case the stored digit (7 for a valid cell) is returned.
    /// </param>
    /// <returns>The indexing digit (0-7) at <paramref name="resolution"/>.</returns>
    /// <exception cref="H3DomainException"><paramref name="resolution"/> is outside the range 1-15.</exception>
    public int GetIndexDigit(int resolution)
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.GetIndexDigit(Value, resolution, out int digit));
        return digit;
    }

    /// <summary>
    /// Constructs an H3 cell from its base cell number and per-resolution indexing digits.
    /// </summary>
    /// <param name="resolution">The target resolution (0-15).</param>
    /// <param name="baseCellNumber">The base cell number (0-121).</param>
    /// <param name="digits">
    /// The indexing digits (each 0-6), one per resolution. Its length must equal
    /// <paramref name="resolution"/>.
    /// </param>
    /// <returns>The constructed <see cref="H3Index"/>.</returns>
    /// <exception cref="ArgumentException">The length of <paramref name="digits"/> does not equal <paramref name="resolution"/>.</exception>
    /// <exception cref="H3DomainException">The resolution, base cell number, or a digit was rejected by the native library (including a pentagon base cell with a leading K-axis digit).</exception>
    public static H3Index Construct(int resolution, int baseCellNumber, ReadOnlySpan<int> digits)
    {
        if (digits.Length != resolution)
        {
            throw new ArgumentException(
                $"digits length ({digits.Length}) must equal resolution ({resolution}).",
                nameof(digits));
        }

        unsafe
        {
            fixed (int* p = digits)
            {
                H3ErrorMarshaller.ThrowIfError(NativeMethods.ConstructCell(resolution, baseCellNumber, p, out ulong cell));
                return new H3Index(cell);
            }
        }
    }

    /// <summary>
    /// Returns the icosahedron face numbers (0-19) that this cell intersects.
    /// </summary>
    /// <returns>The distinct face numbers the cell touches (1-2 for hexagons, 5 for pentagons).</returns>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    /// <exception cref="H3Exception">The native operation failed.</exception>
    public int[] GetIcosahedronFaces()
    {
        EnsureValidCell();

        int maxCount = MaxFaceCount();
        var buffer = new int[maxCount];
        unsafe
        {
            fixed (int* ptr = buffer)
            {
                H3ErrorMarshaller.ThrowIfError(NativeMethods.GetIcosahedronFaces(Value, ptr));
            }
        }

        int count = 0;
        for (int i = 0; i < maxCount; i++)
        {
            if (buffer[i] != InvalidFace)
            {
                count++;
            }
        }

        var result = new int[count];
        int next = 0;
        for (int i = 0; i < maxCount; i++)
        {
            if (buffer[i] != InvalidFace)
            {
                result[next++] = buffer[i];
            }
        }

        return result;
    }

    /// <summary>
    /// Fills <paramref name="destination"/> with the icosahedron face numbers (0-19)
    /// that this cell intersects, packing the valid faces to the front.
    /// </summary>
    /// <param name="destination">
    /// The destination span. Its length must be at least the maximum face count for
    /// this cell (2 for hexagons, 5 for pentagons).
    /// </param>
    /// <returns>The number of distinct faces written to the front of <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small.</exception>
    /// <exception cref="H3InvalidCellException">This is not a valid H3 cell.</exception>
    /// <exception cref="H3Exception">The native operation failed.</exception>
    public int GetIcosahedronFacesInto(Span<int> destination)
    {
        EnsureValidCell();

        int maxCount = MaxFaceCount();
        if (destination.Length < maxCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Length,
                $"Destination span must hold at least {maxCount} elements for this cell.");
        }

        unsafe
        {
            fixed (int* ptr = destination)
            {
                H3ErrorMarshaller.ThrowIfError(NativeMethods.GetIcosahedronFaces(Value, ptr));
            }
        }

        // Compact the INVALID_FACE (-1) padding holes in place; 0 is a valid face.
        int count = 0;
        for (int i = 0; i < maxCount; i++)
        {
            if (destination[i] != InvalidFace)
            {
                destination[count++] = destination[i];
            }
        }

        return count;
    }

    /// <summary>
    /// Parses an H3 index from its hexadecimal string representation using the native
    /// library. Unlike <see cref="Parse(string)"/>, an unparseable string surfaces as
    /// an <see cref="H3Exception"/> rather than a <see cref="FormatException"/>.
    /// </summary>
    /// <param name="value">The hexadecimal index string.</param>
    /// <returns>The parsed <see cref="H3Index"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="H3Exception"><paramref name="value"/> could not be parsed by the native library.</exception>
    public static H3Index FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        H3ErrorMarshaller.ThrowIfError(NativeMethods.StringToH3(value, out ulong cell));
        return new H3Index(cell);
    }

    /// <summary>
    /// Attempts to parse an H3 index from its hexadecimal string representation using
    /// the native library.
    /// </summary>
    /// <param name="value">The hexadecimal index string.</param>
    /// <param name="result">When this method returns <see langword="true"/>, the parsed index; otherwise <see cref="Null"/>.</param>
    /// <returns><see langword="true"/> if the native library parsed the string; otherwise <see langword="false"/>.</returns>
    public static bool TryFromString([NotNullWhen(true)] string? value, out H3Index result)
    {
        result = Null;
        if (value is null)
        {
            return false;
        }

        if (NativeMethods.StringToH3(value, out ulong cell) != H3ErrorCode.Success)
        {
            return false;
        }

        result = new H3Index(cell);
        return true;
    }

    /// <summary>Returns the lowercase, zero-padded 16-digit hexadecimal representation of this index.</summary>
    /// <returns>The 16-character hexadecimal index string.</returns>
    public override string ToString() => Value.ToString("x16", CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the native library's canonical hexadecimal representation of this index.
    /// Unlike <see cref="ToString()"/>, this is <b>not</b> zero-padded, so it is a
    /// variable-length lowercase hex string (the two forms differ only by leading zeros).
    /// </summary>
    /// <returns>The canonical (unpadded) lowercase hexadecimal index string.</returns>
    /// <exception cref="H3Exception">The native operation failed.</exception>
    public string ToCanonicalString()
    {
        Span<byte> buf = stackalloc byte[17];
        unsafe
        {
            fixed (byte* p = buf)
            {
                H3ErrorMarshaller.ThrowIfError(NativeMethods.H3ToString(Value, p, (nuint)buf.Length));
            }
        }

        int len = buf.IndexOf((byte)0);
        return Encoding.ASCII.GetString(buf[..(len < 0 ? buf.Length : len)]);
    }

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

    #region Hierarchy

    /// <summary>
    /// Returns the parent cell of this cell at the given coarser resolution.
    /// </summary>
    /// <param name="parentRes">The target parent resolution (0-15); must be less than or equal to this cell's resolution.</param>
    /// <returns>The parent <see cref="H3Index"/> at <paramref name="parentRes"/>.</returns>
    /// <exception cref="H3DomainException"><paramref name="parentRes"/> is outside the range 0-15.</exception>
    /// <exception cref="H3Exception"><paramref name="parentRes"/> is finer than this cell's resolution, or the native operation failed.</exception>
    public H3Index CellToParent(int parentRes)
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToParent(Value, parentRes, out ulong parent));
        return new H3Index(parent);
    }

    /// <summary>
    /// Returns the center child cell of this cell at the given finer resolution.
    /// </summary>
    /// <param name="childRes">The target child resolution (0-15); must be greater than or equal to this cell's resolution.</param>
    /// <returns>The center child <see cref="H3Index"/> at <paramref name="childRes"/>.</returns>
    /// <exception cref="H3DomainException"><paramref name="childRes"/> is outside the range 0-15.</exception>
    /// <exception cref="H3Exception"><paramref name="childRes"/> is coarser than this cell's resolution, or the native operation failed.</exception>
    public H3Index CellToCenterChild(int childRes)
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToCenterChild(Value, childRes, out ulong child));
        return new H3Index(child);
    }

    /// <summary>
    /// Returns the position of this cell within an ordered traversal of all children
    /// of its parent at the given coarser resolution. Inverse of <see cref="ChildPosToCell"/>.
    /// </summary>
    /// <param name="parentRes">The parent resolution (0-15); must be less than or equal to this cell's resolution.</param>
    /// <returns>The zero-based child position of this cell within its parent at <paramref name="parentRes"/>.</returns>
    /// <exception cref="H3DomainException"><paramref name="parentRes"/> is outside the range 0-15.</exception>
    /// <exception cref="H3Exception"><paramref name="parentRes"/> is finer than this cell's resolution, or the native operation failed.</exception>
    public long CellToChildPos(int parentRes)
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToChildPos(Value, parentRes, out long pos));
        return pos;
    }

    /// <summary>
    /// Returns the child cell of this (parent) cell at the given child position and
    /// resolution. Inverse of <see cref="CellToChildPos"/>.
    /// </summary>
    /// <param name="childPos">The zero-based child position, in <c>[0, cellToChildrenSize)</c>.</param>
    /// <param name="childRes">The child resolution (0-15); must be greater than or equal to this cell's resolution.</param>
    /// <returns>The child <see cref="H3Index"/> at <paramref name="childPos"/> and <paramref name="childRes"/>.</returns>
    /// <exception cref="H3DomainException"><paramref name="childRes"/> is outside the range 0-15, or <paramref name="childPos"/> is outside the valid range.</exception>
    /// <exception cref="H3Exception"><paramref name="childRes"/> is coarser than this cell's resolution, or the native operation failed.</exception>
    public H3Index ChildPosToCell(long childPos, int childRes)
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.ChildPosToCell(childPos, Value, childRes, out ulong child));
        return new H3Index(child);
    }

    /// <summary>
    /// Returns all child cells of this cell at the given finer resolution.
    /// </summary>
    /// <param name="childRes">The target child resolution (0-15); must be greater than or equal to this cell's resolution.</param>
    /// <returns>The child cells, with null padding slots removed (pentagon parents yield fewer than the hexagon maximum). Order is not guaranteed.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="childRes"/> implies a result so large that it would exceed <see cref="Array.MaxLength"/>.</exception>
    /// <exception cref="H3DomainException"><paramref name="childRes"/> is outside the range 0-15.</exception>
    /// <exception cref="H3Exception"><paramref name="childRes"/> is coarser than this cell's resolution, or the native operation failed.</exception>
    public unsafe H3Index[] CellToChildren(int childRes)
    {
        long maxSize = CellToChildrenSize(childRes);
        if (maxSize <= 0)
        {
            return [];
        }

        if (maxSize > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(childRes),
                childRes,
                $"childRes={childRes} would require {maxSize} cells, exceeding the maximum array length.");
        }

        var buffer = new ulong[maxSize];
        fixed (ulong* ptr = buffer)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToChildren(Value, childRes, ptr));
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
    /// Fills <paramref name="destination"/> with the child cells of this cell at the
    /// given finer resolution, packing non-null cells to the front.
    /// </summary>
    /// <param name="childRes">The target child resolution (0-15); must be greater than or equal to this cell's resolution.</param>
    /// <param name="destination">
    /// The destination span. Its length must be at least the maximum child count for
    /// <paramref name="childRes"/> (see the upstream <c>cellToChildrenSize</c>).
    /// </param>
    /// <returns>The number of non-null child cells written to the front of <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="childRes"/> implies a result that would exceed <see cref="Array.MaxLength"/>, or <paramref name="destination"/> is too small.</exception>
    /// <exception cref="H3DomainException"><paramref name="childRes"/> is outside the range 0-15.</exception>
    /// <exception cref="H3Exception"><paramref name="childRes"/> is coarser than this cell's resolution, or the native operation failed.</exception>
    public unsafe int CellToChildrenInto(int childRes, Span<H3Index> destination)
    {
        long maxSize = CellToChildrenSize(childRes);

        if (maxSize > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(childRes),
                childRes,
                $"childRes={childRes} would require {maxSize} cells, exceeding the maximum array length.");
        }

        if (destination.Length < maxSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Length,
                $"Destination span must hold at least {maxSize} elements for childRes={childRes}.");
        }

        if (maxSize <= 0)
        {
            return 0;
        }

        // Write directly into the caller's span: H3Index is blittable and layout-compatible
        // with ulong, so the native fill needs no intermediate buffer.
        fixed (H3Index* ptr = destination)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToChildren(Value, childRes, (ulong*)ptr));
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

    /// <summary>
    /// Compacts a set of cells into the smaller, mixed-resolution set of cells that
    /// covers the same area. Inverse of <see cref="UncompactCells(ReadOnlySpan{H3Index}, int)"/>.
    /// </summary>
    /// <param name="cells">The cells to compact. All must be the same resolution and duplicate-free.</param>
    /// <returns>The compacted, mixed-resolution set of cells. Order is not guaranteed.</returns>
    /// <exception cref="H3Exception">The input contains mixed resolutions or duplicates, or the native operation failed.</exception>
    public static unsafe H3Index[] CompactCells(ReadOnlySpan<H3Index> cells)
    {
        if (cells.Length == 0)
        {
            return [];
        }

        // `new ulong[]` is CLR zero-initialized, so the trailing slots the native
        // compactCells leaves untouched read as H3_NULL(0) and the strip below is valid.
        // A future switch to a non-cleared buffer (e.g. ArrayPool) MUST pre-clear it.
        var buffer = new ulong[cells.Length];
        fixed (H3Index* inPtr = cells)
        fixed (ulong* outPtr = buffer)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.CompactCells((ulong*)inPtr, outPtr, cells.Length));
        }

        int count = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != 0)
            {
                count++;
            }
        }

        var result = new H3Index[count];
        int next = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != 0)
            {
                result[next++] = new H3Index(buffer[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Compacts a set of cells into <paramref name="destination"/>, packing non-null
    /// cells to the front. Inverse of <see cref="UncompactCellsInto"/>.
    /// </summary>
    /// <param name="cells">The cells to compact. All must be the same resolution and duplicate-free.</param>
    /// <param name="destination">The destination span. Its length must be at least <paramref name="cells"/>.Length (compaction never grows the set).</param>
    /// <returns>The number of cells written to the front of <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="destination"/> is too small.</exception>
    /// <exception cref="H3Exception">The input contains mixed resolutions or duplicates, or the native operation failed.</exception>
    public static unsafe int CompactCellsInto(ReadOnlySpan<H3Index> cells, Span<H3Index> destination)
    {
        if (destination.Length < cells.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Length,
                $"Destination span must hold at least {cells.Length} elements.");
        }

        if (cells.Length == 0)
        {
            return 0;
        }

        // The native compactCells writes its result strictly front-to-back and leaves the
        // trailing slots untouched -- it does NOT H3_NULL-pad the unused tail. The array
        // overload survives this only because `new ulong[]` is CLR zero-initialized; a
        // caller-supplied span may carry stale data. Pre-clear the scanned window so the
        // unwritten tail reads as H3_NULL(0) and the strip below counts only real cells.
        destination[..cells.Length].Clear();

        fixed (H3Index* inPtr = cells)
        fixed (H3Index* outPtr = destination)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.CompactCells((ulong*)inPtr, (ulong*)outPtr, cells.Length));
        }

        // Compact the H3_NULL (0) padding holes in place. The native body fills the
        // output front-to-back, so the write index never outruns the read index.
        int count = 0;
        for (int i = 0; i < cells.Length; i++)
        {
            if (destination[i].Value != 0)
            {
                destination[count++] = destination[i];
            }
        }

        return count;
    }

    /// <summary>
    /// Uncompacts a mixed-resolution set of cells to the equivalent set at the given
    /// uniform resolution. Inverse of <see cref="CompactCells"/>.
    /// </summary>
    /// <param name="cells">The compacted, mixed-resolution cells.</param>
    /// <param name="res">The target uniform resolution (0-15); must be greater than or equal to the finest resolution present in <paramref name="cells"/>.</param>
    /// <returns>The uncompacted set of cells, all at <paramref name="res"/>. Order is not guaranteed.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="res"/> implies a result so large that it would exceed <see cref="Array.MaxLength"/>.</exception>
    /// <exception cref="H3DomainException"><paramref name="res"/> is outside the range 0-15.</exception>
    /// <exception cref="H3Exception"><paramref name="res"/> is finer than the input requires, or the native operation failed.</exception>
    public static unsafe H3Index[] UncompactCells(ReadOnlySpan<H3Index> cells, int res)
    {
        if (cells.Length == 0)
        {
            return [];
        }

        long maxOut = UncompactCellsSize(cells, res);
        if (maxOut <= 0)
        {
            return [];
        }

        if (maxOut > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(res),
                res,
                $"res={res} would require {maxOut} cells, exceeding the maximum array length.");
        }

        var buffer = new ulong[maxOut];
        fixed (H3Index* inPtr = cells)
        fixed (ulong* outPtr = buffer)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.UncompactCells((ulong*)inPtr, cells.Length, outPtr, maxOut, res));
        }

        int count = 0;
        for (long i = 0; i < maxOut; i++)
        {
            if (buffer[i] != 0)
            {
                count++;
            }
        }

        var result = new H3Index[count];
        int next = 0;
        for (long i = 0; i < maxOut; i++)
        {
            if (buffer[i] != 0)
            {
                result[next++] = new H3Index(buffer[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// Uncompacts a mixed-resolution set of cells into <paramref name="destination"/>
    /// at the given uniform resolution, packing non-null cells to the front. Inverse
    /// of <see cref="CompactCellsInto"/>.
    /// </summary>
    /// <param name="cells">The compacted, mixed-resolution cells.</param>
    /// <param name="res">The target uniform resolution (0-15); must be greater than or equal to the finest resolution present in <paramref name="cells"/>.</param>
    /// <param name="destination">
    /// The destination span. Its length must be at least the uncompacted size for
    /// <paramref name="res"/> (see the upstream <c>uncompactCellsSize</c>).
    /// </param>
    /// <returns>The number of cells written to the front of <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="res"/> implies a result that would exceed <see cref="Array.MaxLength"/>, or <paramref name="destination"/> is too small.</exception>
    /// <exception cref="H3DomainException"><paramref name="res"/> is outside the range 0-15.</exception>
    /// <exception cref="H3Exception"><paramref name="res"/> is finer than the input requires, or the native operation failed.</exception>
    public static unsafe int UncompactCellsInto(ReadOnlySpan<H3Index> cells, int res, Span<H3Index> destination)
    {
        if (cells.Length == 0)
        {
            return 0;
        }

        long maxOut = UncompactCellsSize(cells, res);

        if (maxOut > Array.MaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(res),
                res,
                $"res={res} would require {maxOut} cells, exceeding the maximum array length.");
        }

        if (destination.Length < maxOut)
        {
            throw new ArgumentOutOfRangeException(
                nameof(destination),
                destination.Length,
                $"Destination span must hold at least {maxOut} elements for res={res}.");
        }

        if (maxOut <= 0)
        {
            return 0;
        }

        fixed (H3Index* inPtr = cells)
        fixed (H3Index* outPtr = destination)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.UncompactCells((ulong*)inPtr, cells.Length, (ulong*)outPtr, maxOut, res));
        }

        // Compact the H3_NULL (0) padding holes in place. The native body fills the
        // output front-to-back, so the write index never outruns the read index.
        int count = 0;
        for (long i = 0; i < maxOut; i++)
        {
            if (destination[(int)i].Value != 0)
            {
                destination[count++] = destination[(int)i];
            }
        }

        return count;
    }

    #endregion Hierarchy

    // Sentinel written by getIcosahedronFaces into unused slots; 0 is a valid face,
    // so the compaction passes strip -1, not H3_NULL.
    private const int InvalidFace = -1;

    internal int MaxFaceCount()
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.MaxFaceCount(Value, out int count));
        return count;
    }

    internal long CellToChildrenSize(int childRes)
    {
        H3ErrorMarshaller.ThrowIfError(NativeMethods.CellToChildrenSize(Value, childRes, out long size));
        return size;
    }

    internal static unsafe long UncompactCellsSize(ReadOnlySpan<H3Index> cells, int res)
    {
        fixed (H3Index* p = cells)
        {
            H3ErrorMarshaller.ThrowIfError(NativeMethods.UncompactCellsSize((ulong*)p, cells.Length, res, out long size));
            return size;
        }
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
