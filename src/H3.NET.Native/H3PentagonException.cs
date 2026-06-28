// SPDX-License-Identifier: Apache-2.0

using System;

namespace H3.NET.Native;

/// <summary>
/// Thrown when pentagon distortion was encountered and the algorithm could not
/// handle it.
/// </summary>
public sealed class H3PentagonException : H3Exception
{
    /// <summary>Initializes a new instance of the <see cref="H3PentagonException"/> class.</summary>
    public H3PentagonException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3PentagonException"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public H3PentagonException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3PentagonException"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public H3PentagonException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3PentagonException"/> class for a specific native error.</summary>
    /// <param name="errorCode">The numeric H3 error code reported by the native library.</param>
    /// <param name="message">The message that describes the error.</param>
    internal H3PentagonException(uint errorCode, string message)
        : base(errorCode, message)
    {
    }
}
