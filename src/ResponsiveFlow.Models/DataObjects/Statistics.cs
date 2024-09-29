using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResponsiveFlow;

public sealed class Statistics
{
    private Statistics(double count, double mean, double variance, double standardDeviation, double standardError)
    {
        Count = count;
        Mean = mean;
        Variance = variance;
        StandardDeviation = standardDeviation;
        StandardError = standardError;
    }

    public double Count { get; }

    public double Mean { get; }

    public double Variance { get; }

    public double StandardDeviation { get; }

    public double StandardError { get; }

    public static Statistics Create(IReadOnlyCollection<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfZero(values.Count);

        int count = values.Count;
        double mean = values.Average();
        double variance = count is 1 ? 0.0 : values.Sum(Selector) / (count - 1.0);
        double standardDeviation = double.Sqrt(variance);
        double standardError = standardDeviation / double.Sqrt(count);
        return new(count, mean, variance, standardDeviation, standardError);

        double Selector(double value)
        {
            double diff = value - mean;
            return diff * diff;
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(Statistics));
        builder.Append(" { ");
        if (PrintMembers(builder))
            builder.Append(' ');
        builder.Append('}');
        return builder.ToString();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Mean = ").Append(Mean);
        builder.Append(", StdDev = ").Append(StandardDeviation);
        builder.Append(", Error = ").Append(StandardError);
        return true;
    }
}
