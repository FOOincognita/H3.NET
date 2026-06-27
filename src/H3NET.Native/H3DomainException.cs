// SPDX-License-Identifier: Apache-2.0

using System;

namespace H3NET.Native;

/// <summary>
/// Thrown when an argument passed to an H3 operation was outside its acceptable
/// range (a domain error), including invalid latitude/longitude, resolution, or
/// option/flags values.
/// </summary>
public sealed class H3DomainException : H3Exception
{
    /// <summary>Initializes a new instance of the <see cref="H3DomainException"/> class.</summary>
    public H3DomainException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3DomainException"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public H3DomainException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3DomainException"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public H3DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3DomainException"/> class for a specific native error.</summary>
    /// <param name="errorCode">The numeric H3 error code reported by the native library.</param>
    /// <param name="message">The message that describes the error.</param>
    internal H3DomainException(uint errorCode, string message)
        : base(errorCode, message)
    {
    }
}
