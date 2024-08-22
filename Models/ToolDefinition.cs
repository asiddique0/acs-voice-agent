namespace LumenicBackend.Models
{
    public class ToolDefinition
    {
        [JsonProperty("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonProperty("parameters")]
        [JsonPropertyName("parameters")]
        public Parameters Parameters { get; set; }
    }

    public class Parameters
    {
        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "object";

        [JsonProperty("properties")]
        [JsonPropertyName("properties")]
        public DynamicProperties Properties { get; set; }

        [JsonProperty("required")]
        [JsonPropertyName("required")]
        public string[] Required { get; set; }
    }

    public class DynamicProperties
    {
        [Newtonsoft.Json.JsonExtensionData]
        public JObject Properties { get; set; }
    }

    public class Property
    {
        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";

        [JsonProperty("description")]
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonProperty("enum")]
        [JsonPropertyName("enum")]
        public string[] Enum { get; set; }
    }
}
