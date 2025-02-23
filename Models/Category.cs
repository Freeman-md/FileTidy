using System.Text.Json.Serialization;

namespace FileTidy.Models
{
    public class Category
    {
        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }
    }
}