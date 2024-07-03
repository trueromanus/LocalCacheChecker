using LocalCacheChecker.ApiModels;

namespace LocalCacheChecker.SaveModels {

    internal record ReleaseTorrentSaveModel : ReleaseTorrentModel {

        public int ReleaseId { get; set; }

        public long Time { get; set; }

        private new string UpdatedAt { get; init; } = "";

    }

}
