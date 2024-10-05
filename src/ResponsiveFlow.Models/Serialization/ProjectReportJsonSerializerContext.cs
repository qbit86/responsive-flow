using System.Text.Json.Serialization;

namespace ResponsiveFlow;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ProjectReportDto))]
internal sealed partial class ProjectReportJsonSerializerContext : JsonSerializerContext;
