using System.Text.Json.Serialization;

namespace AnilibriaAPIClient {
    public record ReleaseDataModelPoster {

        [JsonIgnore ( Condition = JsonIgnoreCondition.WhenWritingNull )]
        public string Src { get; init; } = "";

        [JsonIgnore]
        public string Thumbnail { get; init; } = "";

    }
}