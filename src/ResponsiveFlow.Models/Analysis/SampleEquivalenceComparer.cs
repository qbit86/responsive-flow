using System;
using System.Collections.Generic;
using Perfolizer;
using Perfolizer.Mathematics.Common;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.SignificanceTesting.MannWhitney;
using Perfolizer.Metrology;

namespace ResponsiveFlow;

/// <remarks>
/// This equality comparer is not transitive.
/// </remarks>
public sealed class SampleEquivalenceComparer : IEqualityComparer<Sample>
{
    private readonly SignificanceLevel _significanceLevel;
    private readonly SimpleEquivalenceTest _test;
    private readonly Threshold _threshold;

    public SampleEquivalenceComparer(
        SimpleEquivalenceTest test, Threshold threshold, SignificanceLevel significanceLevel)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(threshold);

        _significanceLevel = significanceLevel;
        _test = test;
        _threshold = threshold;
    }

    public static SampleEquivalenceComparer Default { get; } =
        new(new(MannWhitneyTest.Instance), PercentValue.Of(2).ToThreshold(), SignificanceLevel.P1E5);

    public bool Equals(Sample? x, Sample? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;

        var comparisonResult = _test.Perform(x, y, _threshold, _significanceLevel);
        return comparisonResult is ComparisonResult.Indistinguishable;
    }

    public int GetHashCode(Sample? obj) => (obj?.GetHashCode()).GetValueOrDefault();
}
