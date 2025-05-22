using System.Text.Json.Serialization;

namespace FileTidy.Core.Models
{
    public class Extension
    {
        [JsonPropertyName("extension")]
        public required string Name { get; set; }

        [JsonPropertyName("category_id")]
        public required int CategoryId { get; set; }
    }
}