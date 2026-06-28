// SPDX-License-Identifier: Apache-2.0

using System;

namespace H3.NET.Native;

/// <summary>
/// Thrown when an <see cref="H3Index"/> argument was not a valid cell, edge, or
/// vertex index.
/// </summary>
public sealed class H3InvalidCellException : H3Exception
{
    /// <summary>Initializes a new instance of the <see cref="H3InvalidCellException"/> class.</summary>
    public H3InvalidCellException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3InvalidCellException"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public H3InvalidCellException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3InvalidCellException"/> class with a message and inner exception.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public H3InvalidCellException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="H3InvalidCellException"/> class for a specific native error.</summary>
    /// <param name="errorCode">The numeric H3 error code reported by the native library.</param>
    /// <param name="message">The message that describes the error.</param>
    internal H3InvalidCellException(uint errorCode, string message)
        : base(errorCode, message)
    {
    }
}
