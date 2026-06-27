// SPDX-License-Identifier: Apache-2.0

using System;

namespace H3NET.Native;

/// <summary>
/// Thrown when the native library failed to allocate memory, or when a caller-provided
/// buffer was not large enough for the result.
/// </summary>
public sealed class H3MemoryException : H3Exception
{
    /// <summary>Initializes a new instance of the <see cref="H3MemoryException"/> class.</summary>
    public H3MemoryException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3MemoryException"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public H3MemoryException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3MemoryException"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public H3MemoryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3MemoryException"/> class for a specific native error.</summary>
    /// <param name="errorCode">The numeric H3 error code reported by the native library.</param>
    /// <param name="message">The message that describes the error.</param>
    internal H3MemoryException(uint errorCode, string message)
        : base(errorCode, message)
    {
    }
}
