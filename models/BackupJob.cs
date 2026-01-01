using System.Text.Json.Serialization;

namespace backup_system.models
{
    public class BackupJob
    {
        [JsonPropertyName("sources")]
        public List<string> Sources { get; set; }

        [JsonPropertyName("targets")]
        public List<string> Targets { get; set; }

        [JsonPropertyName("timing")]
        public string Timing { get; set; }

        [JsonPropertyName("retention")]
        public BackupRetention Retention { get; set; } = new();

        [JsonPropertyName("method")]
        public BackupMethod Method { get; set; }

        // Constructor for deserialization
        public BackupJob() { }
    }
}
