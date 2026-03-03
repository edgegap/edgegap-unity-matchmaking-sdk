using System;
using Newtonsoft.Json;

namespace Edgegap
{
    public class UnescapedStringConverter<T> : JsonConverter<T>
    {
        public override T ReadJson(
            JsonReader reader,
            Type objectType,
            T existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            if (reader.TokenType == JsonToken.Null)
                return default;

            // Read the raw escaped string value
            var raw = reader.Value?.ToString();
            if (string.IsNullOrEmpty(raw))
                return default;

            // Unescape \" => " then deserialize into T
            var unescaped = raw.Replace("\\\"", "\"");
            return JsonConvert.DeserializeObject<T>(unescaped);
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            // Serialize T back to a JSON string with escaped quotes
            var json = JsonConvert.SerializeObject(value);
            writer.WriteValue(json);
        }
    }
}
