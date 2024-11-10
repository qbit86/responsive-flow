using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ResponsiveFlow;

public static class RankHelpers
{
    public static int GetRanksOrdered<T, TComparer>(IReadOnlyList<T> orderedItems, Span<int> ranks, TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        ArgumentNullException.ThrowIfNull(orderedItems);
        if (comparer is null)
            throw new ArgumentNullException(nameof(comparer));

        int count = Math.Min(orderedItems.Count, ranks.Length);
        if (count > 0)
            GetRanksOrdered(orderedItems, ranks, count, comparer);
        return count;
    }

    public static int[] GetRanksOrdered<T, TComparer>(IReadOnlyList<T> orderedItems, TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        ArgumentNullException.ThrowIfNull(orderedItems);
        if (comparer is null)
            throw new ArgumentNullException(nameof(comparer));

        if (orderedItems.Count is 0)
            return [];

        int[] ranks = new int[orderedItems.Count];
        GetRanksOrdered(orderedItems, ranks, orderedItems.Count, comparer);
        return ranks;
    }

    private static void GetRanksOrdered<T, TComparer>(
        IReadOnlyList<T> items, Span<int> ranks, int count, TComparer comparer)
        where TComparer : IEqualityComparer<T>
    {
        Debug.Assert(count > 0);
        Debug.Assert(items.Count >= count);
        Debug.Assert(ranks.Length >= count);
        ranks[0] = 0;
        for (int i = 1; i < count; ++i)
            ranks[i] = comparer.Equals(items[i], items[i - 1]) ? ranks[i - 1] : i;
    }
}
