using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Perfolizer;
using Perfolizer.Horology;
using Perfolizer.Mathematics.QuantileEstimators;

namespace ResponsiveFlow;

public sealed class Metrics
{
    private Metrics(
        double count, double mean, double variance, double standardDeviation, double standardError,
        Quartiles quartiles, double interquartileRange)
    {
        Count = count;
        Mean = mean;
        Variance = variance;
        StandardDeviation = standardDeviation;
        StandardError = standardError;
        Quartiles = quartiles;
        InterquartileRange = interquartileRange;
    }

    private static CultureInfo P => CultureInfo.InvariantCulture;

    public double Count { get; }

    public double Mean { get; }

    public double Variance { get; }

    public double StandardDeviation { get; }

    public double StandardError { get; }

    [JsonConverter(typeof(QuartilesJsonConverter))]
    public Quartiles Quartiles { get; }

    public double InterquartileRange { get; }

    [JsonIgnore] public double Min => Quartiles.Min;

    [JsonIgnore] public double Median => Quartiles.Q2;

    [JsonIgnore] public double Max => Quartiles.Max;

    public static Metrics Create(Sample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        ArgumentOutOfRangeException.ThrowIfZero(sample.Size);

        IReadOnlyCollection<double> values = sample.Values;
        return CreateUnchecked(sample, values);
    }

    public static Metrics Create(IReadOnlyList<double> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfZero(values.Count);

        Sample sample = new(values, TimeUnit.Millisecond);
        return CreateUnchecked(sample, values);
    }

    private static Metrics CreateUnchecked(Sample sample, IReadOnlyCollection<double> values)
    {
        int count = values.Count;
        double mean = values.Average();
        double variance = count is 1 ? 0.0 : values.Sum(Selector) / (count - 1.0);
        double standardDeviation = double.Sqrt(variance);
        double standardError = standardDeviation / double.Sqrt(count);

        var quartiles = Quartiles.Create(sample);
        return new(count, mean, variance, standardDeviation, standardError, quartiles, quartiles.InterquartileRange);

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
        builder.Append($"{nameof(Mean)} = ").Append(P, $"{Mean:F2}");
        builder.Append(", StdDev = ").Append(P, $"{StandardDeviation:F2}");
        builder.Append(", Error = ").Append(P, $"{StandardError:F2}");
        builder.Append($", {nameof(Count)} = ").Append(Count);
        var q = Quartiles;
        builder.Append($", {nameof(Quartiles)} = ")
            .Append(P, $"[{q.Q0:F2}, {q.Q1:F2}, {q.Q2:F2}, {q.Q3:F2}, {q.Q4:F2}]");
        return true;
    }
}
