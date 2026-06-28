// SPDX-License-Identifier: Apache-2.0

namespace H3.NET.Native.Interop;

/// <summary>
/// Result codes returned by the H3 C library (the <c>H3Error</c> ABI type, a
/// <see langword="uint"/>). Values are 1:1 with the upstream <c>E_*</c> constants
/// defined in <c>h3api.h</c> for H3 v4.5.0.
/// </summary>
internal enum H3ErrorCode : uint
{
    /// <summary>Success (no error).</summary>
    Success = 0,

    /// <summary>The operation failed but a more specific error is not available.</summary>
    Failed = 1,

    /// <summary>Argument was outside of acceptable range (when a more specific code is not available).</summary>
    Domain = 2,

    /// <summary>Latitude or longitude argument was outside of acceptable range.</summary>
    LatLngDomain = 3,

    /// <summary>Resolution argument was outside of acceptable range.</summary>
    ResDomain = 4,

    /// <summary><c>H3Index</c> cell argument was not valid.</summary>
    CellInvalid = 5,

    /// <summary><c>H3Index</c> directed edge argument was not valid.</summary>
    DirEdgeInvalid = 6,

    /// <summary><c>H3Index</c> undirected edge argument was not valid.</summary>
    UndirEdgeInvalid = 7,

    /// <summary><c>H3Index</c> vertex argument was not valid.</summary>
    VertexInvalid = 8,

    /// <summary>Pentagon distortion was encountered, which the algorithm could not handle.</summary>
    Pentagon = 9,

    /// <summary>Duplicate input was encountered in the arguments and the algorithm could not handle it.</summary>
    DuplicateInput = 10,

    /// <summary><c>H3Index</c> cell arguments were not neighbors.</summary>
    NotNeighbors = 11,

    /// <summary><c>H3Index</c> cell arguments had incompatible resolutions.</summary>
    ResMismatch = 12,

    /// <summary>Necessary memory allocation failed.</summary>
    MemoryAlloc = 13,

    /// <summary>Bounds of provided memory were not large enough.</summary>
    MemoryBounds = 14,

    /// <summary>Mode or flags argument was not valid for the operation.</summary>
    OptionInvalid = 15,

    /// <summary><c>H3Index</c> argument was not valid (general index validity failure).</summary>
    IndexInvalid = 16,

    /// <summary>Base cell number argument was outside of acceptable range.</summary>
    BaseCellDomain = 17,

    /// <summary>Indexing digit argument was outside of acceptable range.</summary>
    DigitDomain = 18,

    /// <summary>Indexing digit was a deleted digit (pentagon distortion).</summary>
    DeletedDigit = 19,
}
