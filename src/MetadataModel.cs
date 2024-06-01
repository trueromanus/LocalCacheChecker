namespace LocalCacheChecker {

    internal record MetadataModel {

        public long LastReleaseTimeStamp { get; init; }

        public int CountEpisodes { get; init; }

        public int CountReleases { get; init; }

    }

}
