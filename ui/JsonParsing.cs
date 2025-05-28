using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace ui
{
    public class JsonParsing : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(List<string>) || objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                var list = new List<string>();
                JArray array = JArray.Load(reader);
                foreach (var item in array)
                {
                    list.Add(item.ToString());
                }
                return list;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return new List<string> { reader.Value.ToString() };
            }
            throw new JsonSerializationException("Unexpected token type");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var list = value as List<string>;
            if (list != null && list.Count > 1)
            {
                writer.WriteStartArray();
                foreach (var item in list)
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }
            else if (list != null && list.Count == 1)
            {
                writer.WriteValue(list[0]);
            }
        }
    }

    public class AnswersQuestions
    {
        [JsonConverter(typeof(JsonParsing))]
        public List<string> Defined { get; set; }
        public string Textboxes { get; set; }
    }

    public class JsonResults
    {
        public Dictionary<string, AnswersQuestions> Data { get; set; }
    }
}