using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sophon.Helper
{
    public class BoolConverter : JsonConverter<bool>
    {
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }

        public override bool Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String => bool.TryParse(reader.GetString(), out bool boolFromString)
                    ? boolFromString
                    : throw new JsonException(),

                JsonTokenType.Number => reader.TryGetInt64(out long boolFromNumber)
                    ? Convert.ToBoolean(boolFromNumber)
                    : reader.TryGetDouble(out double boolFromDouble) && Convert.ToBoolean(boolFromDouble),

                _ => throw new JsonException()
            };
        }
    }
}
