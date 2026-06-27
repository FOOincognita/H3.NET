// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using H3NET.Native.Interop;

namespace H3NET.Native;

/// <summary>
/// Translates native <see cref="H3ErrorCode"/> results into the public exception
/// hierarchy. The single entry point is <see cref="ThrowIfError(H3ErrorCode)"/>,
/// invoked immediately after every error-returning interop call.
/// </summary>
internal static class H3ErrorMarshaller
{
    /// <summary>
    /// Returns silently on <see cref="H3ErrorCode.Success"/>; otherwise fetches the
    /// native description and throws the exception type mapped from the error code.
    /// </summary>
    /// <param name="error">The result code returned by an interop call.</param>
    public static void ThrowIfError(H3ErrorCode error)
    {
        if (error == H3ErrorCode.Success)
        {
            return;
        }

        var code = (uint)error;
        string message = Describe(error, code);

        throw error switch
        {
            H3ErrorCode.Domain
                or H3ErrorCode.LatLngDomain
                or H3ErrorCode.ResDomain
                or H3ErrorCode.OptionInvalid
                or H3ErrorCode.BaseCellDomain
                or H3ErrorCode.DigitDomain
                or H3ErrorCode.DeletedDigit
                => new H3DomainException(code, message),

            H3ErrorCode.CellInvalid
                or H3ErrorCode.IndexInvalid
                or H3ErrorCode.DirEdgeInvalid
                or H3ErrorCode.UndirEdgeInvalid
                or H3ErrorCode.VertexInvalid
                => new H3InvalidCellException(code, message),

            H3ErrorCode.Pentagon => new H3PentagonException(code, message),

            H3ErrorCode.MemoryAlloc
                or H3ErrorCode.MemoryBounds
                => new H3MemoryException(code, message),

            _ => new H3Exception(code, message),
        };
    }

    private static string Describe(H3ErrorCode error, uint code)
    {
        nint descriptionPtr = NativeMethods.DescribeH3Error(error);
        string? description = Marshal.PtrToStringUTF8(descriptionPtr);
        return description ?? $"H3 operation failed with error code {code}.";
    }
}
