using System;
using System.Collections.Generic;

namespace ResponsiveFlow;

/// <summary>
/// Compares two exceptions to filter out duplicates from the message channel.
/// </summary>
/// <remarks>
/// The current implementation only compares the types, so all exceptions of the same type are considered equal.
/// </remarks>
internal sealed class ExceptionComparer : IEqualityComparer<Exception>
{
    internal static ExceptionComparer Instance { get; } = new();

    public bool Equals(Exception? x, Exception? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;
        return ReferenceEquals(x.GetType(), y.GetType());
    }

    public int GetHashCode(Exception obj) => obj.GetType().GetHashCode();
}
