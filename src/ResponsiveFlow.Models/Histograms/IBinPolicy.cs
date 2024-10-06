namespace ResponsiveFlow;

internal interface IBinPolicy<in TBin>
{
    double Lower(TBin bin);

    double Upper(TBin bin);

    double Height(TBin bin);
}
