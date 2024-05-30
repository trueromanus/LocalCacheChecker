using AnilibriaAPIClient;

namespace LocalCacheChecker {

    internal record ReleaseEpisodeModel {

        public string Name { get; init; } = "";

        public string NameEnglish { get; init; } = "";

        public decimal Ordinal { get; set; }

        public int SortOrder { get; set; }

        public string Hls480 { get; set; } = "";

        public string Hls720 { get; set; } = "";

        public string Hls1080 { get; set; } = "";

        public long Duration { get; set; }

        public string RutubeId { get; set; } = "";

        public string YoutubeId { get; set; } = "";

        public string UpdatedAt { get; set; } = "";

        public TimeRange Opening { get; set; } = new TimeRange();

        public TimeRange Ending { get; set; } = new TimeRange ();

        public ReleaseDataModelPoster Preview { get; set; } = new ReleaseDataModelPoster ();

    }

}
