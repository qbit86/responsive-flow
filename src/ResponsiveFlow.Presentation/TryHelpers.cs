using System.Runtime.CompilerServices;

namespace ResponsiveFlow;

internal static class TryHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Some<T>(T valueToReturn, out T value)
    {
        value = valueToReturn;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool None<T>(out T? value)
    {
        value = default;
        return false;
    }
}
