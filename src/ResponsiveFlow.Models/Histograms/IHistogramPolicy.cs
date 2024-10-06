using System.Collections.Generic;

namespace ResponsiveFlow;

internal interface IHistogramPolicy<out TBin, in THistogram>
{
    IReadOnlyList<TBin> GetBins(THistogram histogram);
}
