using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ResponsiveFlow;

public sealed class ProjectDto
{
    public string[]? Urls { get; init; }

    public string? OutputDir { get; init; }

    internal string[] GetUrlsOrEmpty() => Urls ?? [];

    internal bool TryGetOutputDir([NotNullWhen(true)] out string? outputDir)
    {
        outputDir = OutputDir;
        return !string.IsNullOrWhiteSpace(outputDir);
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(ProjectDto));
        builder.Append(" { ");
        if (PrintMembers(builder))
            builder.Append(' ');
        builder.Append('}');
        return builder.ToString();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Urls.Count = ").Append(Urls?.Length ?? 0);
        if (OutputDir is { } outputDir)
            builder.Append(", OutputDir = ").Append(outputDir);
        return true;
    }
}
