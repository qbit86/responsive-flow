using Xunit;

namespace ResponsiveFlow;

using TheoryData = TheoryData<double[], double[], bool>;

public sealed class SampleEquivalenceComparerTests
{
    private static TheoryData? s_data;

    public static TheoryData Data => s_data ??= CreateData();

    [Theory]
    [MemberData(nameof(Data))]
    public void Default(double[] x, double[] y, bool expected)
    {
        bool actual = SampleEquivalenceComparer.Default.Equals(new(x), new(y));
        Assert.Equal(expected, actual);
    }

    private static TheoryData CreateData()
    {
        double[] baseline = [100, 96, 104, 105, 97, 94, 106];
        return new()
        {
            { [100.5, 104.5, 96.5, 97.5, 105.5, 106.5, 94.5], baseline, true },
            { [70, 66, 74, 75, 67, 64, 76], baseline, false }
        };
    }
}
