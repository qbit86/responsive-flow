using System;
using System.Diagnostics.CodeAnalysis;

namespace ResponsiveFlow;

internal static class ThrowHelpers
{
    [DoesNotReturn]
    internal static void ThrowInvalidOperationException(string message) => throw new InvalidOperationException(message);
}
