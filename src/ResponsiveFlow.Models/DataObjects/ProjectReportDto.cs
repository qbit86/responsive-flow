using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResponsiveFlow;

public class ProjectReportDto
{
    private ProjectReportDto(IReadOnlyList<UriReportDto> uriReports) => UriReports = uriReports;

    public IReadOnlyList<UriReportDto> UriReports { get; }

    public static ProjectReportDto Create(ProjectCollectedData projectCollectedData)
    {
        ArgumentNullException.ThrowIfNull(projectCollectedData);
        var uriReports = projectCollectedData.UriCollectedDataset.Select(UriReportDto.Create).ToList();
        return new(uriReports);
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(nameof(ProjectReportDto));
        builder.Append(" { ");
        if (PrintMembers(builder))
            builder.Append(' ');
        builder.Append('}');
        return builder.ToString();
    }

    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("UriReports.Count = ").Append(UriReports.Count);
        return true;
    }
}