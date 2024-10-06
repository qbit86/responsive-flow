using System.Collections.Generic;
using System.Text;

namespace ResponsiveFlow;

public readonly record struct ProjectCollectedData(IReadOnlyCollection<UriCollectedData> UriCollectedDataset)
{
    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(ProjectCollectedData));
        builder.Append(" { ");
        if (PrintMembers(builder))
            builder.Append(' ');
        builder.Append('}');
        return builder.ToString();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(UriCollectedDataset)}.Count = ").Append(UriCollectedDataset.Count);
        return true;
    }
}
