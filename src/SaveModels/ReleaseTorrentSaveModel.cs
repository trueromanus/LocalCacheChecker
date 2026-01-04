using LocalCacheChecker.ApiModels;
using System.Text.Json.Serialization;

namespace LocalCacheChecker.SaveModels {

    internal record ReleaseTorrentSaveModel : ReleaseTorrentModel {

        public int ReleaseId { get; set; }

        public long Time { get; set; }

        [JsonIgnore]
        public new string UpdatedAt { get; init; } = "";

    }

}
