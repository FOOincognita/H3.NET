// SPDX-License-Identifier: Apache-2.0

using System;

namespace H3NET.Native;

/// <summary>
/// The base exception thrown when an H3 operation fails. Carries the numeric
/// <see cref="ErrorCode"/> reported by the native library together with its
/// human-readable description.
/// </summary>
public class H3Exception : Exception
{
    /// <summary>Initializes a new instance of the <see cref="H3Exception"/> class.</summary>
    public H3Exception()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3Exception"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public H3Exception(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3Exception"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public H3Exception(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3Exception"/> class for a specific native error.</summary>
    /// <param name="errorCode">The numeric H3 error code reported by the native library.</param>
    /// <param name="message">The message that describes the error.</param>
    internal H3Exception(uint errorCode, string message)
        : base(message) => ErrorCode = errorCode;

    /// <summary>
    /// Gets the numeric H3 error code reported by the native library, matching the
    /// upstream <c>H3Error</c> (<c>E_*</c>) values. <c>0</c> indicates that no native
    /// code was associated with this exception.
    /// </summary>
    public uint ErrorCode { get; }
}
