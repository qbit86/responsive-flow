using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;
using Xunit;

namespace ResponsiveFlow;

using TheoryData = TheoryData<double[], double[], double, ComparisonResult>;

public sealed class DistributionComparisonTests
{
    private static TheoryData? s_data;

    public static TheoryData Data => s_data ??= CreateData();

    [Theory]
    [MemberData(nameof(Data))]
    public void MannWhitney(
        double[] x, double[] y, double significanceLevel, ComparisonResult expected)
    {
        SimpleEquivalenceTest test = new(MannWhitneyTest.Instance);
        var actual = test.Perform(new(x), new(y), Threshold.Zero, new(significanceLevel));
        Assert.Equal(expected, actual);
    }

    private static TheoryData CreateData()
    {
        double[] x = [95, 94, 101, 89, 91, 87, 96];
        double[] y = [100, 96, 104, 105, 97, 94, 106];
        return new()
        {
            { x, y, 0.0001, ComparisonResult.Indistinguishable },
            { x, y, 0.05, ComparisonResult.Lesser }
        };
    }
}
