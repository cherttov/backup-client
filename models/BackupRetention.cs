using System.Text.Json.Serialization;

namespace backup_client.models
{
    public class BackupRetention
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }
}
