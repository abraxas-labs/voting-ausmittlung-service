// (c) Copyright 2024 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Voting.Ausmittlung.Converters;

public class OptionalGuidConverter : JsonConverter<Guid?>
{
    public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var content = reader.GetString();

        if (string.IsNullOrEmpty(content))
        {
            return null;
        }

        return Guid.Parse(content);
    }

    public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
    {
        var content = value.HasValue
            ? value.Value.ToString()
            : string.Empty;
        writer.WriteStringValue(content);
    }
}
