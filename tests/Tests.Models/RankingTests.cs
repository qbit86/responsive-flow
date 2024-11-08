using System.Collections.Generic;
using Xunit;

namespace ResponsiveFlow;

public sealed class RankingTests
{
    /// <see href="https://en.wikipedia.org/wiki/Ranking#Standard_competition_ranking_(%221224%22_ranking)" />
    [Fact]
    public void StandardRankingWithGaps()
    {
        double[] items = [90, 95, 95, 100, 101, 101, 101, 105];
        int[] actual = RankHelpers.GetRanksOrdered(items, EqualityComparer<double>.Default);
        int[] expected = [0, 1, 1, 3, 4, 4, 4, 7];
        Assert.Equal(expected, actual);
    }
}
