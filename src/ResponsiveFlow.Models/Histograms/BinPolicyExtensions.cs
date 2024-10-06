namespace ResponsiveFlow;

internal static class BinPolicyExtensions
{
    internal static double Gap<TBin, TBinPolicy>(this TBinPolicy policy, TBin bin)
        where TBinPolicy : IBinPolicy<TBin>
        => policy.Upper(bin) - policy.Lower(bin);
}
