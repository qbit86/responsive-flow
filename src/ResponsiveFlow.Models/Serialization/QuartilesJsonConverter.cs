using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Perfolizer.Mathematics.QuantileEstimators;

namespace ResponsiveFlow;

/// <seealso href="https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?pivots=dotnet-8-0#sample-basic-converter" />
internal sealed class QuartilesJsonConverter : JsonConverter<Quartiles>
{
    public override Quartiles Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, Quartiles value, JsonSerializerOptions options)
    {
        FormattableString formattable = $"[{value.Q0}, {value.Q1}, {value.Q2}, {value.Q3}, {value.Q4}]";
        writer.WriteRawValue(FormattableString.Invariant(formattable));
    }
}
