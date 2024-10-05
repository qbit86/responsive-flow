using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ResponsiveFlow;

public sealed class Metrics
{
    private Metrics(double count, double mean, double variance, double standardDeviation, double standardError)
    {
        Count = count;
        Mean = mean;
        Variance = variance;
        StandardDeviation = standardDeviation;
        StandardError = standardError;
    }

    private static CultureInfo P => CultureInfo.InvariantCulture;

    public double Count { get; }

    public double Mean { get; }

    public double Variance { get; }

    public double StandardDeviation { get; }

    public double StandardError { get; }

    public static Metrics Create(IReadOnlyCollection<double> values)
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
        builder.Append(nameof(Metrics));
        builder.Append(" { ");
        if (PrintMembersUnchecked(builder))
            builder.Append(' ');
        builder.Append('}');
        return builder.ToString();
    }

    public bool PrintMembers(StringBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return PrintMembersUnchecked(builder);
    }

    private bool PrintMembersUnchecked(StringBuilder builder)
    {
        builder.Append("Mean = ").Append(P, $"{Mean:F2}");
        builder.Append(", StdDev = ").Append(P, $"{StandardDeviation:F2}");
        builder.Append(", Error = ").Append(P, $"{StandardError:F2}");
        builder.Append(", Count = ").Append(Count);
        return true;
    }
}
