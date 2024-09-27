using System.Diagnostics.CodeAnalysis;

namespace ResponsiveFlow;

public sealed class ProjectDto
{
    public string[]? Urls { get; set; }

    public string? OutputDir { get; set; }

    internal string[] GetUrlsOrEmpty() => Urls ?? [];

    internal bool TryGetOutputDir([NotNullWhen(true)] out string? outputDir)
    {
        outputDir = OutputDir;
        return !string.IsNullOrWhiteSpace(outputDir);
    }
}
